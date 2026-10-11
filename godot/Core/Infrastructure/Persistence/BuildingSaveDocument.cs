using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeasonsTogether.Core.Domain;
using SuanPleng;

namespace SeasonsTogether.Core.Infrastructure.Persistence;

/// <summary>Version 2 is one atomic document, not a separately committed inventory sidecar.</summary>
internal sealed class BuildingSaveDocument
{
    [JsonRequired] public int Version { get; set; } = 2;
    [JsonRequired] public JsonElement Farm { get; set; }
    [JsonRequired] public BuildingState Buildings { get; set; } = new();
    public static (Farm Farm, BuildingState Buildings) Decode(ReadOnlySpan<byte> bytes)
    {
        using var reader=new StreamReader(new MemoryStream(bytes.ToArray()),Encoding.UTF8,true);
        byte[] utf8=Encoding.UTF8.GetBytes(reader.ReadToEnd());
        using var json=JsonDocument.Parse(utf8);
        if(json.RootElement.ValueKind==JsonValueKind.Object && json.RootElement.TryGetProperty("Version",out var version) && version.ValueKind==JsonValueKind.Number && version.TryGetInt32(out int number) && number==2) {
            var document=SaveJsonContract.Read<BuildingSaveDocument>(utf8,"Version","Farm","Buildings");
            var farm=SaveJsonContract.Read<Farm>(Encoding.UTF8.GetBytes(document.Farm.GetRawText()));
            FarmInvariants.Validate(farm);
            if(document.Buildings==null)throw new InvalidDataException("ไม่มีข้อมูลอาคารในเซฟ");
            document.Buildings.Validate(farm);
            return (farm,document.Buildings);
        }
        var original=SaveJsonContract.Read<Farm>(utf8);FarmInvariants.Validate(original);
        return (original,new BuildingState());
    }
    public static byte[] Encode(Farm farm, BuildingState buildings)
    {
        buildings.Validate(farm);
        if(buildings.Interiors.Count==0)return JsonSerializer.SerializeToUtf8Bytes(farm);
        return JsonSerializer.SerializeToUtf8Bytes(new BuildingSaveDocument { Farm=JsonSerializer.SerializeToElement(farm),Buildings=buildings });
    }
}
