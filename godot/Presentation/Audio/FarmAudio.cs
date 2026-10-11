using Godot;
namespace SeasonsTogether;

/// <summary>Eight reusable voices; cached action resources, no runtime file reads.</summary>
public partial class FarmAudio : Node
{
    readonly AudioStreamPlayer[] voices = new AudioStreamPlayer[8];
    int cursor;
    public int Volume { get; set; } = 70;
    public bool DiagnosticMuted { get; set; }
    public int ActiveVoices => voices.Count(v=>v.Playing);
    public override void _Ready()
    {
        for(int i=0;i<voices.Length;i++){voices[i]=new AudioStreamPlayer { Bus="Effects" };AddChild(voices[i]);}
    }
    public void Play(string action,bool success=true)
    {
        var key=!success?"error":action=="door"?"door":action=="water"?"water":action=="harvest"?"harvest":action=="sell"?"coin":"click";
        var voice=voices[cursor++%voices.Length];voice.Stop();voice.Stream=AssetCatalog.Shared.Sound(key);
        voice.VolumeDb=DiagnosticMuted || Volume==0 ? -80 : Mathf.LinearToDb(Math.Clamp(Volume,0,100)/100f);
        voice.Play();
    }
}
