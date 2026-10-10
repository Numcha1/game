using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SuanPleng;

public class Request {
    public string Session { get; set; } = "";
    public string Name { get; set; } = "";
    public string Action { get; set; } = "sync";
    public string Command { get; set; } = "";
    public int Index { get; set; } = -1;
    public string Seed { get; set; } = "carrot";
    public double X { get; set; } = 6;
    public double Y { get; set; } = 4;
}
public record Reply(Farm? Farm, string Message = "");
public sealed class RoomServer : IDisposable {
    readonly TcpListener listener = new(IPAddress.Loopback, 0);
    readonly CancellationTokenSource stop = new();
    readonly object gate = new();
    readonly Farm farm;
    readonly string? save;
    readonly Dictionary<string,long> lastMoves = new();
    readonly Dictionary<string,string> commands = new();
    readonly System.Threading.Timer clock;
    readonly SemaphoreSlim connections = new(32);
    public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public int Port { get; }
    public string Url => $"http://127.0.0.1:{Port}";
    public RoomServer(Farm world, string? savePath = null) {
        farm = world; save = savePath; listener.Start(); Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _ = Accept(); int ticks=0;
        clock = new(_ => { lock(gate) { farm.Tick(1); if (++ticks % 15 == 0 && save != null) TrySave(); } }, null, 1000, 1000);
    }
    public void ClaimOwner(string session) { lock(gate) farm.Owner = Farm.Identity(session); }
    void TrySave() { try { farm.Save(save!); } catch(Exception e) when(e is IOException or UnauthorizedAccessException) { farm.Log("บันทึกไม่ได้: ตรวจพื้นที่ว่างและสิทธิ์โฟลเดอร์เซฟ"); } }
    async Task Accept() {
        while(!stop.IsCancellationRequested) {
            try { var client = await listener.AcceptTcpClientAsync(stop.Token); if (!connections.Wait(0)) { client.Dispose(); continue; } _ = Handle(client); }
            catch(Exception e) when(e is OperationCanceledException or SocketException or ObjectDisposedException) { break; }
        }
    }
    async Task Handle(TcpClient client) {
        using(client) using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token)) {
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            try {
                var stream = client.GetStream(); var header = new List<byte>(); var one = new byte[1];
                while(header.Count < 8192) {
                    if (await stream.ReadAsync(one, timeout.Token) == 0) return;
                    header.Add(one[0]); int n=header.Count;
                    if(n>=4 && header[n-4]==13 && header[n-3]==10 && header[n-2]==13 && header[n-1]==10) break;
                }
                string text = Encoding.ASCII.GetString(header.ToArray()); var lines = text.Split("\r\n");
                bool authorized = lines.Skip(1).Any(l => l.Equals("Authorization: Bearer " + Token, StringComparison.OrdinalIgnoreCase));
                if (lines[0] != "POST /game HTTP/1.1") { await Send(stream, 404, new Reply(null,"ไม่พบเส้นทาง"), timeout.Token); return; }
                string? lengthText = lines.FirstOrDefault(l => l.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))?.Split(':',2)[1].Trim();
                if (!int.TryParse(lengthText,out int length) || length <= 0 || length > 8192 || lines.Any(l=>l.StartsWith("Transfer-Encoding:",StringComparison.OrdinalIgnoreCase))) { await Send(stream,400,new Reply(null,"รูปแบบคำขอไม่ถูกต้อง"),timeout.Token);return; }
                byte[] body = new byte[length]; await stream.ReadExactlyAsync(body,timeout.Token);
                if (!authorized) { await Send(stream, 403, new Reply(null,"รหัสห้องไม่ถูกต้อง"), timeout.Token); return; }
                var req = JsonSerializer.Deserialize<Request>(body);
                if(req == null || !Guid.TryParseExact(req.Session,"N",out _)) { await Send(stream,400,new Reply(null,"ผู้เล่นไม่ถูกต้อง"),timeout.Token);return; }
                byte[] response;
                lock(gate) {
                    Farmer? p = farm.Find(req.Session);
                    if(req.Action=="join") {
                        try { p = farm.Join(req.Session,req.Name); } catch(InvalidOperationException e) { response=JsonSerializer.SerializeToUtf8Bytes(new Reply(null,e.Message)); goto done; }
                    }
                    if(p==null) { response=JsonSerializer.SerializeToUtf8Bytes(new Reply(null,"กรุณาเข้าห้องใหม่")); goto done; }
                    if(farm.Players.Count(v=>v.Id!=p.Id && Now()-v.LastSeen<30000)>=4) { response=JsonSerializer.SerializeToUtf8Bytes(new Reply(null,"ห้องเต็มแล้ว"));goto done; }
                    p.LastSeen=Now();
                    if(req.Action!="join" && !p.Sleeping && !farm.Paused) {
                        double allowed=Math.Clamp((Now()-lastMoves.GetValueOrDefault(p.Id,Now()))/1000d*5+0.5,0.5,5);
                        if(Farm.Walkable(req.X,req.Y) && Farm.Distance(p,req.X,req.Y)<=allowed) { p.X=req.X; p.Y=req.Y; }
                    }
                    lastMoves[p.Id]=Now();
                    string message="";
                    if(req.Action is not "join" and not "sync") {
                        if(!Guid.TryParseExact(req.Command,"N",out _)) { response=JsonSerializer.SerializeToUtf8Bytes(new Reply(null,"คำสั่งไม่ถูกต้อง"));goto done; }
                        string key=p.Id+req.Command;
                        if(!commands.TryGetValue(key,out message!)) {
                            int energy=p.Energy,coins=farm.Coins;
                            string crop=req.Index>=0 && req.Index<farm.Plots.Count ? farm.Plots[req.Index].Crop : "";
                            message=req.Action=="rescue" ? farm.Rescue(p) : farm.Action(p,req.Action,req.Index,req.Seed);
                            if(p.Energy<energy || farm.Coins!=coins)
                                farm.Effect(p,req.Action,req.Index,req.Action=="plant"?req.Seed:crop,farm.Coins-coins);
                            commands[key]=message; if(commands.Count>512) commands.Remove(commands.Keys.First());
                            if(save!=null) TrySave();
                        }
                    }
                    response=JsonSerializer.SerializeToUtf8Bytes(new Reply(farm,message));
                    done:;
                }
                await SendBytes(stream,200,response,timeout.Token);
            } catch(Exception e) when(e is IOException or OperationCanceledException or JsonException or SocketException) { /* peer disconnected or malformed */ }
            finally { connections.Release(); }
        }
    }
    static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static Task Send(NetworkStream stream,int code,Reply reply,CancellationToken ct)=>SendBytes(stream,code,JsonSerializer.SerializeToUtf8Bytes(reply),ct);
    static async Task SendBytes(NetworkStream stream,int code,byte[] body,CancellationToken ct) {
        byte[] head=Encoding.ASCII.GetBytes($"HTTP/1.1 {code} {(code==200?"OK":"Error")}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head,ct);await stream.WriteAsync(body,ct);
    }
    public void Dispose() { stop.Cancel();listener.Stop();clock.Dispose();lock(gate) if(save!=null) TrySave();stop.Dispose(); }
}
public sealed class RoomClient : IDisposable {
    readonly HttpClient http = new(new HttpClientHandler{AllowAutoRedirect=false}) { Timeout=TimeSpan.FromSeconds(8) };
    readonly string url;
    public RoomClient(string address,string token) { url=address.TrimEnd('/')+"/game"; http.DefaultRequestHeaders.Authorization=new("Bearer",token); }
    public async Task<Reply> Send(Request request) {
        using var content=new StringContent(JsonSerializer.Serialize(request),Encoding.UTF8,"application/json");
        using var res=await http.PostAsync(url,content);
        if(!res.IsSuccessStatusCode) throw new IOException($"เชื่อมต่อห้องไม่ได้ ({(int)res.StatusCode})");
        return JsonSerializer.Deserialize<Reply>(await res.Content.ReadAsStringAsync()) ?? throw new IOException("ข้อมูลจากห้องไม่สมบูรณ์");
    }
    public void Dispose()=>http.Dispose();
}
public sealed class InternetTunnel : IDisposable {
    Process? process;
    public async Task<string> Start(int port) {
        string exe=Path.Combine(AppContext.BaseDirectory,"cloudflared.exe");
        if(!File.Exists(exe)) {
            string tools=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SuanPleng","tools");
            Directory.CreateDirectory(tools);exe=Path.Combine(tools,"cloudflared-2026.10.0.exe");
            const string expected="86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c";
            bool valid=false;if(File.Exists(exe)){using var existing=File.OpenRead(exe);valid=Convert.ToHexString(SHA256.HashData(existing)).Equals(expected,StringComparison.OrdinalIgnoreCase);}
            if(!valid){using var resource=typeof(InternetTunnel).Assembly.GetManifestResourceStream("SuanPleng.cloudflared.exe") ?? throw new IOException("ส่วนเชื่อมต่อออนไลน์ไม่สมบูรณ์");string temporary=exe+"."+Guid.NewGuid().ToString("N")+".tmp";using(var output=File.Create(temporary))await resource.CopyToAsync(output);using(var input=File.OpenRead(temporary))if(!Convert.ToHexString(SHA256.HashData(input)).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new IOException("ส่วนเชื่อมต่อออนไลน์ตรวจสอบไม่ผ่าน");File.Move(temporary,exe,true);}
        }
        var ready=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        process=new Process { StartInfo=new(exe) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true } };
        foreach(string arg in new[]{"tunnel","--no-autoupdate","--protocol","http2","--url",$"http://127.0.0.1:{port}"}) process.StartInfo.ArgumentList.Add(arg);
        void Line(object sender,DataReceivedEventArgs e) { if(e.Data==null)return; var m=Regex.Match(e.Data,@"https://[a-z0-9-]+\.trycloudflare\.com"); if(m.Success)ready.TrySetResult(m.Value); }
        process.ErrorDataReceived+=Line;process.OutputDataReceived+=Line;process.EnableRaisingEvents=true;
        process.Exited+=(_,_)=>ready.TrySetException(new IOException("ช่องทางออนไลน์ปิดลง กรุณาสร้างห้องใหม่"));
        process.Start();process.BeginErrorReadLine();process.BeginOutputReadLine();
        try { return await ready.Task.WaitAsync(TimeSpan.FromSeconds(60)); } catch { Dispose();throw; }
    }
    public static string Invite(string url,string token)=>url+"#"+token;
    public static (string Url,string Token) Parse(string invite) {
        var parts=invite.Trim().Split('#');
        if(parts.Length!=2 || !Uri.TryCreate(parts[0],UriKind.Absolute,out var u) || u.Scheme!="https" || !Regex.IsMatch(u.Host,@"^[a-z0-9-]+\.trycloudflare\.com$") || u.AbsolutePath!="/" || u.Query!="" || u.UserInfo!="" || !u.IsDefaultPort || !Regex.IsMatch(parts[1],"^[A-F0-9]{48}$")) throw new InvalidOperationException("วางรหัสเชิญเต็มที่เจ้าของส่งให้ (https://…trycloudflare.com#…)");
        return(parts[0],parts[1]);
    }
    public void Dispose() { try { if(process is {HasExited:false})process.Kill(true); } catch(InvalidOperationException){} process?.Dispose();process=null; }
}
