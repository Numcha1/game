using Godot;
namespace SeasonsTogether;
public static class RoomLight
{
    public static Color Ambient(double minute,bool lights,string weather)
    {
        float night=Math.Clamp(((float)minute-1020)/180,0,1);
        var dusk=lights?new Color(1,.98f,.94f):new Color(.65f,.69f,.78f);
        var tint=Colors.White.Lerp(dusk,night);
        if(weather is "ฝนตก" or "พายุ")tint*=new Color(.96f,.98f,1);
        return tint;
    }
}
