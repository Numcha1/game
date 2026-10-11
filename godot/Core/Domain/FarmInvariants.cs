using SuanPleng;

namespace SeasonsTogether.Core.Domain;

/// <summary>Structural trust boundary, not a second implementation of farming rules.</summary>
public static class FarmInvariants
{
    public static void Validate(Farm farm)
    {
        if (farm.Version != 1) throw new InvalidDataException("รองรับเซฟ Version 1 เท่านั้น");
        if (farm.Day < 1 || farm.Coins < 0 || !double.IsFinite(farm.Minute) || farm.Minute < 360 || farm.Minute > 1260 ||
            farm.Plots == null || farm.Plots.Count != 24 || farm.Players == null || farm.Players.Count > 40 ||
            farm.Journal == null || farm.Journal.Any(x => x == null) || farm.Effects == null || farm.Effects.Count > 32 ||
            farm.Weather == null || farm.Owner == null || farm.EffectSequence < 0)
            throw new InvalidDataException("ข้อมูลฟาร์มไม่สมบูรณ์");
        foreach (var plot in farm.Plots)
            if (plot == null || plot.Crop == null || plot.LastCrop == null ||
                (plot.Crop != "" && !Farm.Crops.ContainsKey(plot.Crop)) ||
                !double.IsFinite(plot.Growth) || plot.Growth < 0 || plot.RipeDay < -1 ||
                plot.Moisture is < 0 or > 100 || plot.Fertility is < 0 or > 100 || plot.Quality is < 0 or > 100)
                throw new InvalidDataException("ข้อมูลแปลงไม่ถูกต้อง");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var player in farm.Players)
            if (player == null || !ValidIdentity(player.Id) || !identities.Add(player.Id) || player.Name == null ||
                player.Energy is < 0 or > 100 || !Farm.Walkable(player.X, player.Y) || player.Bag == null ||
                player.Bag.Any(x => x.Key == null || !Farm.Crops.ContainsKey(x.Key) || x.Value < 0))
                throw new InvalidDataException("ข้อมูลผู้เล่นไม่ถูกต้อง");
        foreach (var effect in farm.Effects)
            if (effect == null || effect.Sequence < 1 || effect.Sequence > farm.EffectSequence ||
                effect.Actor == null || effect.Kind == null || effect.Crop == null || !double.IsFinite(effect.X) || !double.IsFinite(effect.Y))
                throw new InvalidDataException("ข้อมูลเอฟเฟกต์ไม่ถูกต้อง");
    }
    static bool ValidIdentity(string? id) => id is { Length: 24 } && id.All(Uri.IsHexDigit);
}
