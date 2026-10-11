using SeasonsTogether.Core.Domain;

namespace SeasonsTogether.Core.Application;

/// <summary>Detached daily estimate. Execution still delegates to the original Farm rules.</summary>
public sealed record SaleQuote(int Day, IReadOnlyList<InventoryItem> Items)
{
    public long Total => Items.Sum(item => (long)item.Quantity * item.UnitPrice);
    public bool Matches(SaleQuote current) => Day == current.Day && Items.SequenceEqual(current.Items);
}
