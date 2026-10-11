using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Persistence;

static class ItemTransactionChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        var checks=new List<string>();
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        Check(ItemTransactions.Transfer("unknown",1,2,0,24)==ItemTransactionError.UnknownItem,"unknown item transaction rejected");
        Check(ItemTransactions.Transfer("seed:carrot",1,2,0,24)==ItemTransactionError.UnsupportedItem,"seed IDs never inserted into legacy produce dictionaries");
        Check(ItemTransactions.Transfer("furniture:chest",1,2,0,24)==ItemTransactionError.UnsupportedItem,"unimplemented furniture transaction rejected");
        Check(ItemTransactions.Transfer("carrot",0,2,0,24)==ItemTransactionError.InvalidQuantity,"zero transfer rejected");
        Check(ItemTransactions.Transfer("carrot",3,2,0,24)==ItemTransactionError.InsufficientItems,"oversized transfer rejected");
        Check(ItemTransactions.Transfer("carrot",1,2,int.MaxValue,int.MaxValue)==ItemTransactionError.CapacityExceeded,"target integer overflow rejected using long arithmetic");
        Check(ItemTransactions.Transfer("carrot",2,2,22,24)==ItemTransactionError.None,"exact capacity transfer allowed");
        Check(ItemTransactions.BuySeed("seed:carrot",2,0,16,out var cost)==ItemTransactionError.None && cost==16,"seed transaction uses original buy price");
        Check(ItemTransactions.BuySeed("seed:carrot",2,0,15,out _)==ItemTransactionError.InsufficientFunds,"insufficient coins rejected");
        Check(ItemTransactions.BuySeed("seed:carrot",2,999999,100,out _)==ItemTransactionError.CapacityExceeded,"seed stack capacity rejected");
        Check(ItemTransactions.BuySeed("carrot",1,0,100,out _)==ItemTransactionError.UnsupportedItem,"produce cannot be purchased through seed service");
        Check(ItemTransactions.BuySeed("seed:carrot",100,0,10000,out _)==ItemTransactionError.InvalidQuantity,"seed quantity boundary preserved");
        string profile=Path.Combine(reports,"items-profile-"+Guid.NewGuid().ToString("N"));
        await using(var game=new GameSession(new SaveService(profile))) {
            game.Player.Bag["carrot"]=int.MaxValue;game.Move(20,8);
            int coins=game.State.Coins;var rejected=game.Execute(new("sell",Id:Guid.NewGuid()));
            Check(!rejected.Changed && game.Player.Bag["carrot"]==int.MaxValue && game.State.Coins==coins,"overflow sale leaves original items and currency unchanged");
            game.Player.Bag["carrot"]=3;
            game.Buildings.Village.Seeds["carrot"]=4;
            var stacks=game.Read.ItemStacks();
            Check(game.Read.BackpackSlots().Count==24 && game.Read.BackpackSlots().Count(i=>i!=null)==2,"backpack pads empty display slots without inventing items");
            Check(game.Read.BackpackSlots(ItemCategory.Seed).Count(i=>i!=null)==1 && game.Read.BackpackSlots(ItemCategory.Seed).First()!.ItemId=="seed:carrot","backpack filter retains seed identity and counts");
            Check(game.Read.BackpackSlots(sortByQuantity:true).First()!.Quantity==4,"backpack quantity sorting does not mutate legacy dictionaries");
            Check(stacks.Single(i=>i.ItemId=="carrot").Quantity==3 && stacks.Single(i=>i.ItemId=="seed:carrot").Quantity==4 && stacks.All(i=>i.Quality==null && i.Freshness==null),"legacy runtime stacks preserve totals without fabricated quality or freshness");
            Check(game.Read.ShopOffers("mira").Count==8 && game.Read.ShopOffers("rowan").Count==0,"shop offers list only implemented item sales");
            var id=Guid.NewGuid();int price=game.State.SalePrice("carrot");
            Check(game.Execute(new("sell",Id:id)).Changed && game.State.Coins==coins+price*3,"valid sell still delegates original daily price");
            Check(!game.Execute(new("sell",Id:id)).Changed && game.State.Coins==coins+price*3,"replayed selling command cannot credit twice");
            await game.SaveAsync();
        }
        await using(var game=new GameSession(new SaveService(profile)))
            Check(game.Read.ItemStacks().Single(i=>i.ItemId=="seed:carrot").Quantity==4 && game.Read.Inventory().Count==0,"definition projections survive original v2 save roundtrip without new schema");
        Check(ItemCatalog.All.Values.Where(i=>i.Category==ItemCategory.Tool).All(i=>GameSession.ValidAction(i.ActionId) && !i.Placeable && !i.Sellable && !i.Tradeable),"seven tool definitions route only existing farming actions");
        return checks;
    }
}
