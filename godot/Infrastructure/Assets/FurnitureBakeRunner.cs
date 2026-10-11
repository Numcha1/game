using Godot;
namespace SeasonsTogether;
public partial class FurnitureBakeRunner : Node
{
    public override void _Ready(){try{FurnitureAssetBake.Write();GD.Print("FURNITURE BAKE PASS");GetTree().Quit();}catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}}
}
