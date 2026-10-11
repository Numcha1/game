namespace SeasonsTogether.Core.Domain;

public enum ItemTransactionError { None, UnknownItem, UnsupportedItem, InvalidQuantity, InsufficientItems, CapacityExceeded, InsufficientFunds }

/// <summary>Pure validation; callers commit once only after access/replay checks.</summary>
public static class ItemTransactions
{
    public static ItemTransactionError Transfer(string? itemId, int quantity, int sourceCount, int targetCount, int capacity)
    {
        var item=ItemCatalog.Find(itemId);
        if(item==null)return ItemTransactionError.UnknownItem;
        if(item.Category!=ItemCategory.Produce)return ItemTransactionError.UnsupportedItem;
        if(quantity<=0 || sourceCount<0 || targetCount<0 || capacity<0)return ItemTransactionError.InvalidQuantity;
        if(quantity>sourceCount)return ItemTransactionError.InsufficientItems;
        return (long)targetCount+quantity>Math.Min(capacity,item.MaxStack)?ItemTransactionError.CapacityExceeded:ItemTransactionError.None;
    }
    public static ItemTransactionError BuySeed(string? itemId,int quantity,int existingCount,int coins,out long cost)
    {
        cost=0;var item=ItemCatalog.Find(itemId);
        if(item==null)return ItemTransactionError.UnknownItem;
        if(item.Category!=ItemCategory.Seed || item.BaseBuyPrice is not { } price)return ItemTransactionError.UnsupportedItem;
        if(quantity is <1 or >99 || existingCount<0 || coins<0)return ItemTransactionError.InvalidQuantity;
        cost=(long)price*quantity;
        if((long)existingCount+quantity>item.MaxStack)return ItemTransactionError.CapacityExceeded;
        return cost>coins?ItemTransactionError.InsufficientFunds:ItemTransactionError.None;
    }
}
