using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SuanPleng;

namespace SeasonsTogether.Core.Infrastructure.Persistence;

/// <summary>Reject unsupported or ambiguous data before a legacy model can silently discard it.</summary>
internal static class SaveJsonContract
{
    static readonly JsonSerializerOptions Strict = new() { UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
    static readonly string[] RequiredFarmMembers={nameof(Farm.Version),nameof(Farm.Day),nameof(Farm.Minute),nameof(Farm.Coins),nameof(Farm.Plots),nameof(Farm.Players)};
    public static T Read<T>(ReadOnlySpan<byte> bytes, params string[] requiredMembers)
    {
        byte[] utf8;
        // Preserve historical UTF-8/16/32 BOM acceptance at this persistence boundary.
        if(bytes is [0xEF,0xBB,0xBF,..] or [0xFF,0xFE,..] or [0xFE,0xFF,..] or [0x00,0x00,0xFE,0xFF,..]) {
            using var reader=new StreamReader(new MemoryStream(bytes.ToArray()),Encoding.UTF8,true);
            utf8=Encoding.UTF8.GetBytes(reader.ReadToEnd());
        } else utf8=bytes.ToArray();
        using var document=JsonDocument.Parse(utf8);
        if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("ข้อมูลเซฟต้องเป็น JSON object");
        ValidateNames(document.RootElement);
        foreach(string member in typeof(T)==typeof(Farm) ? RequiredFarmMembers : requiredMembers)
            if(!document.RootElement.TryGetProperty(member,out _))throw new InvalidDataException("ข้อมูลหลักในเซฟไม่ครบ ไม่มีการสร้างฟาร์มใหม่ทับข้อมูล");
        return document.RootElement.Deserialize<T>(Strict) ?? throw new InvalidDataException("ข้อมูลเซฟไม่สมบูรณ์");
    }
    static void ValidateNames(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object) {
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var property in element.EnumerateObject()) {
                if(!names.Add(property.Name))throw new InvalidDataException("เซฟมีชื่อข้อมูลซ้ำ ไม่สามารถเลือกข้อมูลแทนผู้เล่นได้");
                ValidateNames(property.Value);
            }
        } else if(element.ValueKind==JsonValueKind.Array)
            foreach(var item in element.EnumerateArray())ValidateNames(item);
    }
}
