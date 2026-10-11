using Godot;
namespace SeasonsTogether;

public partial class FarmBoot : Control
{
    readonly FarmPreloader loader=new();
    Label status=null!;
    ProgressBar progress=null!;
    bool finished;
    double elapsed;
    public override void _Ready()
    {
        try { Theme=FarmTypography.Shared; }
        catch(InvalidDataException e) {
            finished=true;GD.PushError(e.Message);
            var failure=new AcceptDialog { DialogText="ไฟล์ตัวอักษรเกมไม่สมบูรณ์ · โปรดติดตั้งเกมใหม่",OkButtonText="ออกจากเกม" };
            AddChild(failure);failure.Confirmed+=()=>GetTree().Quit(1);failure.PopupCentered();return;
        }
        var backdrop=new ColorRect { Color=new Color("304338"),MouseFilter=MouseFilterEnum.Ignore };
        AddChild(backdrop);backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center=new CenterContainer();AddChild(center);center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel=new PanelContainer { CustomMinimumSize=new Vector2(440,160) };center.AddChild(panel);
        var margin=new MarginContainer();foreach(string side in new[]{"left","top","right","bottom"})margin.AddThemeConstantOverride("margin_"+side,20);panel.AddChild(margin);
        var rows=new VBoxContainer();margin.AddChild(rows);
        rows.AddChild(new Label { Text="Seasons Together",ThemeTypeVariation="Heading",HorizontalAlignment=HorizontalAlignment.Center });
        status=new Label { Text="กำลังเตรียมฟาร์ม…",HorizontalAlignment=HorizontalAlignment.Center,AutowrapMode=TextServer.AutowrapMode.WordSmart };rows.AddChild(status);
        progress=new ProgressBar { MaxValue=loader.Total,ShowPercentage=false,CustomMinimumSize=new Vector2(400,18) };rows.AddChild(progress);
        loader.Begin();
    }
    public override void _Process(double delta)
    {
        if(finished)return;
        elapsed+=delta;
        bool ready=loader.Poll();progress.Value=loader.Completed;
        if(loader.Error!=null || elapsed>30) {
            finished=true;status.Text="โหลดฟาร์มไม่ได้ · โปรดตรวจไฟล์เกมแล้วเปิดใหม่";
            GD.PushError(loader.Error ?? "Farm preload timeout");
            var quit=new Button { Text="ออกจากเกม" };status.GetParent().AddChild(quit);quit.Pressed+=()=>GetTree().Quit(1);quit.GrabFocus();return;
        }
        status.Text=$"กำลังเตรียมฟาร์ม · {loader.Completed}/{loader.Total}";
        if(!ready)return;
        finished=true;
        if(GetTree().ChangeSceneToPacked(loader.Scene!)!=Error.Ok) { finished=false;elapsed=31; }
    }
}
