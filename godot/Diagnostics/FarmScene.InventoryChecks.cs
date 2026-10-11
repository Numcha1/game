using Godot;
using SeasonsTogether.Core.Domain;
using System.Text.Json;

namespace SeasonsTogether;
public partial class FarmScene
{
    async Task RunInventoryChecks()
    {
        var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        string reports=ProjectSettings.GlobalizePath("res://reports");
        async Task Shot(string name){await NextDraw();using var texture=GetViewport().GetTexture();using var image=texture.GetImage();image.SavePng(Path.Combine(reports,name+".png"));}
        Session.Player.Bag["carrot"]=10;Session.Player.Bag["turnip"]=3;Session.Buildings.Village.Seeds["carrot"]=4;
        Session.State.Paused=true;
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1600,900),new Vector2I(1920,1080)}) {
            GetWindow().Size=size;Hud.OpenBag();for(int i=0;i<5;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            var grid=(GridContainer)Hud.FindChild("BackpackSlots",true,false);
            Check(grid.GetChildCount()==24,"24 display slots "+size);
            Check(Hud.FindChild("Item-carrot",true,false) is FarmItemCard && Hud.FindChild("Item-seed_carrot",true,false) is FarmItemCard,"produce and actual seeds share backpack "+size);
            Check(grid.GetGlobalRect().Size.X<=GetViewport().GetVisibleRect().Size.X,"backpack fits logical viewport "+size);
            Check(((Button)Hud.FindChild("BagFilter-All",true,false)).Size.Y<=42,"Thai filter label does not wrap vertically "+size);
            Check(Farmer.InputBlocked && GetViewport().GuiGetFocusOwner() is Button,"backpack modal blocks world and keeps keyboard focus "+size);
            await Shot("items-backpack-"+size.X+"x"+size.Y);
        }
        ((Button)Hud.FindChild("BagFilter-Seed",true,false)).EmitSignal(Button.SignalName.Pressed);
        Check(Hud.FindChild("Item-carrot",true,false)==null && Hud.FindChild("Item-seed_carrot",true,false)!=null,"seed filter preserves owned quantities");
        ((Button)Hud.FindChild("Item-seed_carrot",true,false)).EmitSignal(Button.SignalName.Pressed);
        Check(!Hud.ModalVisible && Seed=="carrot" && Session.SeedCount("carrot")==4,"seed slot chooses existing planting seed without consuming it");
        Hud.OpenBag();((Button)Hud.FindChild("BagFilter-All",true,false)).EmitSignal(Button.SignalName.Pressed);
        ((Button)Hud.FindChild("BagSort",true,false)).EmitSignal(Button.SignalName.Pressed);
        Check(((GridContainer)Hud.FindChild("BackpackSlots",true,false)).GetChild(0).Name=="Item-carrot","quantity sort puts largest stack first");
        Hud.CloseModal();Session.State.Paused=false;
        Session.Move(3.5,4.5);RequestBuildingEntry(BuildingRegistry.Farmhouse.Id);await TransitionCompletion;
        Session.Move(BuildingRegistry.Farmhouse.Chest.X,BuildingRegistry.Farmhouse.Chest.Y);Hud.OpenStorage();
        var deposit=(Button)Hud.FindChild("StorageQuickDeposit-carrot",true,false);deposit.EmitSignal(Button.SignalName.Pressed);
        Check(Session.Read.Storage().Single(i=>i.Crop=="carrot").Quantity==10 && !Session.Player.Bag.ContainsKey("carrot"),"quick deposit conserves whole stack");
        // Replayed command checked by Core transaction suite; freed UI nodes are never invoked.
        ((Button)Hud.FindChild("StorageQuickWithdraw-carrot",true,false)).EmitSignal(Button.SignalName.Pressed);
        Check(Session.Player.Bag["carrot"]==10 && Session.Read.Storage().Count==0,"quick withdraw restores whole stack");
        await Shot("items-storage");Hud.CloseModal();await Session.SaveAsync();finalCheckpoint=true;
        Check(!Farmer.InputBlocked,"closing inventory/storage restores movement");
        File.WriteAllText(Path.Combine(reports,"items-inventory-Windows.json"),JsonSerializer.Serialize(new{passed=checks.Count,checks,scope="Actual legacy-compatible backpack filters sort seed selection and quick storage; no persistent slot/placement claim"},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("INVENTORY PASS "+checks.Count);GetTree().CallDeferred(SceneTree.MethodName.Quit,0);
    }
}
