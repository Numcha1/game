using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SuanPleng;

public record Crop(string Name, int Cost, int Days, int Price, int[] Seasons, int Window = 2);
public record ActionEffect(long Sequence, string Actor, string Kind, int Plot, string Crop, int Amount, double X, double Y);
public class Plot {
    public string Crop { get; set; } = "";
    public double Growth { get; set; }
    public int RipeDay { get; set; } = -1;
    public int Moisture { get; set; } = 50;
    public int Fertility { get; set; } = 80;
    public int Quality { get; set; } = 100;
    public bool Disease { get; set; }
    public bool Stake { get; set; }
    public string LastCrop { get; set; } = "";
}
public class Farmer {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double X { get; set; } = 6;
    public double Y { get; set; } = 6;
    public int Energy { get; set; } = 100;
    public bool Sleeping { get; set; }
    public Dictionary<string, int> Bag { get; set; } = new();
    public long LastSeen { get; set; }
}
public class Farm {
    public int Version { get; set; } = 1;
    public int Day { get; set; } = 1;
    public double Minute { get; set; } = 360;
    public int Coins { get; set; } = 160;
    public string Weather { get; set; } = "แดดอ่อน";
    public bool Greenhouse { get; set; }
    public int Harvests { get; set; }
    public string Owner { get; set; } = "";
    public bool Paused { get; set; }
    public long EffectSequence { get; set; }
    public List<ActionEffect> Effects { get; set; } = new();
    public List<Plot> Plots { get; set; } = Enumerable.Range(0, 24).Select(_ => new Plot()).ToList();
    public List<Farmer> Players { get; set; } = new();
    public List<string> Journal { get; set; } = new() { "ยินดีต้อนรับ: เลือกเมล็ด เดินใกล้แปลงแล้วคลิกเพื่อปลูก" };
    public int Season => ((Day - 1) / 14) % 4;
    public int SeasonDay => (Day - 1) % 14 + 1;
    public static readonly string[] SeasonNames = { "ใบไม้ผลิ", "ฤดูร้อน", "ใบไม้ร่วง", "ฤดูหนาว" };
    public static readonly Dictionary<string, Crop> Crops = new() {
        ["carrot"] = new("แครอต", 8, 3, 24, new[]{0,2}),
        ["turnip"] = new("หัวไชเท้า", 6, 2, 16, new[]{0,3}, 1),
        ["potato"] = new("มันฝรั่ง", 12, 4, 38, new[]{0,2}),
        ["tomato"] = new("มะเขือเทศ", 18, 5, 58, new[]{1}),
        ["corn"] = new("ข้าวโพด", 20, 6, 70, new[]{1,2}, 3),
        ["berry"] = new("สตรอว์เบอร์รี", 25, 6, 85, new[]{0}, 1),
        ["pumpkin"] = new("ฟักทอง", 28, 7, 100, new[]{2}, 3),
        ["cabbage"] = new("กะหล่ำ", 15, 5, 50, new[]{2,3})
    };
    public static string Identity(string session) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(session)))[..24];
    public static (int X, int Y) Tile(int i) => (8 + i % 6, 6 + i / 6);
    public static bool Walkable(double x, double y) => double.IsFinite(x) && double.IsFinite(y) && x >= 1 && x <= 23 && y >= 1 && y <= 16 && !(x >= 2 && x <= 5 && y >= 1 && y <= 3) && !(x >= 19 && x <= 22 && y >= 1 && y <= 3) && !(x >= 17 && x <= 22 && y >= 12 && y <= 15);
    public void Log(string text) { Journal.Insert(0, text); if (Journal.Count > 12) Journal.RemoveAt(12); }
    public void Effect(Farmer p, string kind, int plot, string crop, int amount=0) {
        Effects.Add(new(++EffectSequence,p.Id,kind,plot,crop,amount,p.X,p.Y));
        if(Effects.Count>32)Effects.RemoveAt(0);
    }
    public Farmer? Find(string session) => Players.FirstOrDefault(p => p.Id == Identity(session));
    public Farmer Join(string session, string name) {
        if (!Guid.TryParseExact(session, "N", out _)) throw new InvalidOperationException("ข้อมูลผู้เล่นไม่ถูกต้อง");
        var player = Find(session);
        if (player == null) {
            if (Players.Count(p => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.LastSeen < 30000) >= 4) throw new InvalidOperationException("ห้องเต็มแล้ว (4 คน)");
            if (Players.Count >= 40) throw new InvalidOperationException("เซฟนี้มีรายชื่อผู้เล่นครบแล้ว");
            player = new Farmer { Id = Identity(session), Name = string.IsNullOrWhiteSpace(name) ? "ชาวสวน" : new string(name.Where(c => !char.IsControl(c)).Take(18).ToArray()) };
            Players.Add(player); Log($"{player.Name} เข้าฟาร์มแล้ว");
        }
        if (Players.Count(p => p.Id != player.Id && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.LastSeen < 30000) >= 4) throw new InvalidOperationException("ห้องเต็มแล้ว (4 คน)");
        player.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        player.Sleeping = false;
        if (Owner == "") Owner = player.Id;
        return player;
    }
    public string Action(Farmer p, string action, int index, string seed) {
        if (action == "pause") { if (p.Id != Owner) return "เจ้าของฟาร์มเท่านั้นที่หยุดเวลาได้"; Paused = !Paused; return Paused ? "หยุดเวลาแล้ว" : "เวลาเดินต่อแล้ว"; }
        if (Paused) return "ฟาร์มหยุดเวลาอยู่";
        if (p.Sleeping && action != "wake") return "กำลังรอเพื่อนเข้านอน กด Space เพื่อยกเลิก";
        if (action == "wake") { p.Sleeping = false; return "ลุกจากเตียงแล้ว"; }
        if (action == "sleep") {
            if (Distance(p, 4, 4) > 2.5) return "เดินไปหน้าบ้านก่อน กด Space เพื่อเข้านอน";
            p.Sleeping = true; CheckSleep(); return "เข้านอนแล้ว รอเพื่อนที่ยังออนไลน์";
        }
        if (action == "sell") {
            if (Distance(p, 20, 8) > 2.5) return "เดินไปที่ลังขายด้านขวาของฟาร์มก่อน";
            int total = p.Bag.Sum(pair => Crops.TryGetValue(pair.Key, out var c) ? pair.Value * SalePrice(pair.Key) : 0);
            if (total == 0) return "กระเป๋ายังไม่มีผลผลิต";
            Coins += total; p.Bag.Clear(); Log($"{p.Name} ขายผลผลิต +{total} เหรียญ"); return $"ขายแล้ว +{total} เหรียญเข้ากองกลาง";
        }
        if (action == "greenhouse") {
            if (p.Id != Owner) return "เจ้าของฟาร์มเป็นคนอนุมัติการซื้อ";
            if (Distance(p, 20, 4) > 2.5) return "เดินไปหน้าร้านก่อน";
            if (Greenhouse) return "ติดตั้งหลังคาเรือนกระจกแล้ว";
            if (Coins < 300) return "หลังคาเรือนกระจกใช้ 300 เหรียญ";
            Coins -= 300; Greenhouse = true; Log("ติดตั้งหลังคาเรือนกระจก ปลูกได้ทุกฤดูและป้องกันพายุ"); return "ติดตั้งหลังคาเรือนกระจกแล้ว";
        }
        if (index < 0 || index >= Plots.Count) return "เลือกแปลงผักก่อน";
        var tile = Tile(index); if (Distance(p, tile.X, tile.Y) > 2.4) return "เดินใกล้แปลงก่อน (WASD / ลูกศร)";
        if (p.Energy < 5) return "หมดแรงแล้ว ไปนอนที่บ้านเพื่อเริ่มวันใหม่";
        var plot = Plots[index];
        if (action == "plant") {
            if (plot.Crop != "") return "แปลงนี้มีต้นอยู่แล้ว";
            if (!Crops.TryGetValue(seed, out var crop)) return "เลือกเมล็ดก่อน";
            if (!Greenhouse && !crop.Seasons.Contains(Season)) return "เมล็ดนี้ปลูกไม่ได้ในฤดูนี้";
            if (Coins < crop.Cost) return "เหรียญไม่พอ: หัวไชเท้าฟรีได้เมื่อเงินต่ำกว่า 6";
            Coins -= crop.Cost; plot.Crop = seed; plot.Growth = 0; plot.RipeDay = -1; plot.Quality = 100; plot.Disease = false;
            if (plot.LastCrop == seed) { plot.Fertility = Math.Max(0, plot.Fertility - 15); Log("ปลูกซ้ำชนิดเดิม ดินเสื่อมเพิ่ม 15: สลับพืชครั้งหน้า"); }
            p.Energy -= 5; return $"ปลูก{crop.Name}แล้ว ต้องรดน้ำทุกวัน ใช้ {crop.Days} วันเติบโต";
        }
        if (action == "water") { plot.Moisture = Math.Min(100, plot.Moisture + 45); p.Energy -= 5; return "รดน้ำแล้ว: ระวังดินแฉะเมื่อฝนตกต่อเนื่อง"; }
        if (action == "fertilize") { if (Coins < 10) return "ปุ๋ยใช้ 10 เหรียญ"; Coins -= 10; plot.Fertility = Math.Min(100, plot.Fertility + 30); p.Energy -= 5; return "ใส่ปุ๋ยแล้ว +30 ความอุดมสมบูรณ์"; }
        if (action == "stake") { if (plot.Stake) return "มีไม้ค้ำอยู่แล้ว"; if (Coins < 8) return "ไม้ค้ำใช้ 8 เหรียญ"; Coins -= 8; plot.Stake = true; p.Energy -= 5; return "ตั้งไม้ค้ำป้องกันพายุแล้ว"; }
        if (plot.Crop == "") return "แปลงนี้ว่างอยู่";
        if (action == "treat") { if (!plot.Disease) return "ต้นนี้ยังไม่ป่วย"; if (Coins < 10) return "รักษาใช้ 10 เหรียญ"; Coins -= 10; plot.Disease = false; p.Energy -= 5; return "รักษาต้นแล้ว"; }
        if (action == "clear") { plot.LastCrop = plot.Crop; plot.Crop = ""; plot.Growth = 0; plot.RipeDay = -1; plot.Disease = false; p.Energy -= 5; return "ถอนต้นแล้ว เพื่อเริ่มปลูกใหม่"; }
        if (action == "harvest") {
            if (plot.RipeDay < 0) return "ยังไม่โตเต็มที่";
            var amount = plot.Quality >= 70 ? 3 : plot.Quality >= 40 ? 2 : 1;
            p.Bag[plot.Crop] = p.Bag.GetValueOrDefault(plot.Crop) + amount;
            plot.LastCrop = plot.Crop; plot.Crop = ""; plot.Growth = 0; plot.RipeDay = -1; plot.Disease = false;
            p.Energy -= 5; Harvests++; return $"เก็บผลผลิต {amount} ชิ้น ใส่กระเป๋าแล้ว เดินไปลังขายเพื่อขาย";
        }
        return "เลือกเครื่องมือก่อน";
    }
    public string Rescue(Farmer p) {
        if (Paused) return "ฟาร์มหยุดเวลาอยู่";
        if (Coins >= 6) return "ทุนสำรองรับได้เมื่อเงินต่ำกว่า 6 เหรียญ";
        Coins = 18; Log("รับทุนฉุกเฉิน 18 เหรียญ สำหรับเริ่มปลูกใหม่"); return "รับทุนฉุกเฉิน 18 เหรียญแล้ว";
    }
    public int SalePrice(string crop) => Math.Max(1, (int)(Crops[crop].Price * (0.7 + ((Day * 13 + Array.IndexOf(Crops.Keys.ToArray(), crop) * 7) % 9) * 0.075)));
    public string Forecast(int day) => new[] { "แดดอ่อน", "แดดอ่อน", "ฝนตก", "ฝนตก", "คลื่นร้อน", "พายุ", "แดดอ่อน" }[(day * 17 + day / 5) % 7];
    public void Tick(double seconds) {
        if (Paused || !Players.Any(p => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.LastSeen < 30000)) return;
        Minute += seconds * 1.5; // 600 seconds for the 06:00–21:00 farming day.
        if (Minute >= 1260) NextDay(); else CheckSleep();
    }
    public void CheckSleep() {
        var active = Players.Where(p => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.LastSeen < 30000).ToList();
        if (active.Count > 0 && active.All(p => p.Sleeping)) NextDay();
    }
    public void NextDay() {
        int oldSeason = Season;
        foreach (var p in Players) {
            foreach (var key in p.Bag.Keys.ToArray()) { p.Bag[key] = (int)Math.Floor(p.Bag[key] * .75); if (p.Bag[key] == 0) p.Bag.Remove(key); }
            p.Energy = 100; p.Sleeping = false; p.X = 6; p.Y = 4;
        }
        foreach (var plot in Plots) {
            if (Weather is "ฝนตก" or "พายุ") plot.Moisture = Math.Min(100, plot.Moisture + 40);
            if (plot.Crop == "") { plot.Fertility = Math.Min(100, plot.Fertility + 3); plot.Moisture = Math.Max(0, plot.Moisture - 15); continue; }
            var crop = Crops[plot.Crop];
            if (plot.Moisture >= 90 && Weather is "ฝนตก" or "พายุ") plot.Disease = true;
            if (Weather == "พายุ" && !Greenhouse && !plot.Stake) plot.Quality = Math.Max(0, plot.Quality - 30);
            if (plot.Moisture < 25) { plot.Quality = Math.Max(0, plot.Quality - 20); Log($"{crop.Name} ขาดน้ำ: คุณภาพลดและหยุดโต"); }
            else { plot.Growth += plot.Fertility < 30 ? .5 : 1; plot.Fertility = Math.Max(0, plot.Fertility - 6); }
            if (plot.Disease) { plot.Quality = Math.Max(0, plot.Quality - 25); Log($"{crop.Name} ป่วย: ใช้เครื่องมือรักษาหรือถอนก่อนลาม"); }
            if (plot.Growth >= crop.Days && plot.RipeDay < 0) plot.RipeDay = Day + 1;
            plot.Moisture = Math.Max(0, plot.Moisture - (Weather == "คลื่นร้อน" ? 45 : 25));
        }
        Day++; Minute = 360; Weather = Forecast(Day);
        foreach (var plot in Plots.Where(p => p.Crop != "")) {
            var crop = Crops[plot.Crop];
            if (plot.Quality <= 0 || (plot.RipeDay >= 0 && Day - plot.RipeDay > crop.Window) || (!Greenhouse && oldSeason != Season && !crop.Seasons.Contains(Season))) {
                Log($"{crop.Name} เสียหาย: {(plot.Quality <= 0 ? "สุขภาพต้นหมด" : plot.RipeDay >= 0 && Day - plot.RipeDay > crop.Window ? "เก็บเกี่ยวช้าเกินไป" : "พ้นฤดูที่ปลูกได้")}");
                plot.LastCrop = plot.Crop; plot.Crop = ""; plot.Growth = 0; plot.RipeDay = -1; plot.Disease = false;
            }
        }
        // Disease spreads to adjacent plots, predictably once per day.
        var sick = Plots.Select((p,i) => (p,i)).Where(v => v.p.Disease).Select(v => v.i).ToArray();
        foreach (int i in sick) foreach (int j in new[]{i-6,i+6,i-1,i+1}) if (j >= 0 && j < 24 && Math.Abs(Tile(i).X - Tile(j).X) + Math.Abs(Tile(i).Y - Tile(j).Y) == 1 && Plots[j].Crop != "" && (Day+j)%3==0) Plots[j].Disease = true;
        Log($"วันที่ {SeasonDay} {SeasonNames[Season]} · {Weather} · ผลผลิตในกระเป๋าลด 25% ข้ามคืน");
    }
    public static double Distance(Farmer p, double x, double y) => Math.Sqrt((p.X-x)*(p.X-x)+(p.Y-y)*(p.Y-y));
    public static Farm Load(string path) {
        if (!File.Exists(path)) return new Farm();
        var f = JsonSerializer.Deserialize<Farm>(File.ReadAllText(path)) ?? throw new InvalidDataException("เซฟไม่สมบูรณ์");
        if (f.Version != 1 || f.Day < 1 || f.Coins < 0 || f.Plots == null || f.Plots.Count != 24 || f.Players == null || f.Players.Count > 40 || f.Journal == null || !double.IsFinite(f.Minute) || f.Minute < 360 || f.Minute > 1260 || f.Plots.Any(p => p == null || (p.Crop != "" && !Crops.ContainsKey(p.Crop)) || !double.IsFinite(p.Growth) || p.Growth < 0) || f.Players.Any(p => p == null || p.Bag == null || !Walkable(p.X,p.Y) || p.Bag.Any(b => !Crops.ContainsKey(b.Key) || b.Value < 0))) throw new InvalidDataException("ข้อมูลเซฟไม่ถูกต้อง สำรองเซฟเดิมก่อนเริ่มใหม่");
        foreach (var p in f.Players) { p.LastSeen = 0; p.Sleeping = false; }
        f.Paused = false; return f;
    }
    public void Save(string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp"; File.WriteAllText(temp, JsonSerializer.Serialize(this));
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        File.Move(temp, path, true);
    }
}
