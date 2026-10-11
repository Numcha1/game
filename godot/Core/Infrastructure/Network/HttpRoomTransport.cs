using System.Net;
using System.Text;
using System.Text.Json;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SuanPleng;
namespace SeasonsTogether.Core.Infrastructure.Network;

/// <summary>Exact legacy POST /game, JSON field names and bearer protocol. Redirects disabled.</summary>
public sealed class HttpRoomTransport : IRoomTransport
{
    readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
    readonly Uri endpoint;
    public HttpRoomTransport(string address, string token)
    {
        if (!Uri.TryCreate(address,UriKind.Absolute,out var uri) || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != "" || uri.AbsolutePath != "/" ||
            !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback)) throw new ArgumentException("Room must use HTTPS or loopback HTTP");
        if (token.Length != 48 || !token.All(Uri.IsHexDigit)) throw new ArgumentException("Invalid bearer token");
        endpoint = new Uri(uri,"game"); http.DefaultRequestHeaders.Authorization = new("Bearer",token);
    }
    public async Task<RoomSnapshot> SendAsync(RoomRequest request, CancellationToken cancellationToken)
    {
        var dto = new Request { Session=request.Session, Name=request.Name, Action=request.Action, Command=request.Command, Index=request.Index, Seed=request.Seed, X=request.X,Y=request.Y };
        using var content = new StringContent(JsonSerializer.Serialize(dto),Encoding.UTF8,"application/json");
        using var message = new HttpRequestMessage(HttpMethod.Post,endpoint) { Content=content };
        using var response = await http.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            if ((int)response.StatusCode >= 500) throw new IOException("Room server unavailable");
            throw new RoomRejectedException((int)response.StatusCode);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream(); var chunk = new byte[8192]; int read;
        while ((read=await stream.ReadAsync(chunk,cancellationToken).ConfigureAwait(false)) > 0) {
            if (buffer.Length + read > 2 * 1024 * 1024) throw new InvalidDataException("Room snapshot too large");
            buffer.Write(chunk,0,read);
        }
        Reply reply;
        try { reply = JsonSerializer.Deserialize<Reply>(buffer.ToArray()) ?? throw new InvalidDataException("Missing room reply"); }
        catch (JsonException e) { throw new InvalidDataException("Invalid room JSON",e); }
        if (reply.Farm == null) throw new RoomRejectedException(200);
        FarmInvariants.Validate(reply.Farm);
        var player = reply.Farm.Find(request.Session) ?? throw new InvalidDataException("Room snapshot missing player");
        return new(new GameReadModel(reply.Farm,player),reply.Message);
    }
    public void Dispose() => http.Dispose();
}
public sealed class RoomRejectedException(int status) : Exception("Room rejected request (" + status + ")") { public int Status { get; } = status; }
