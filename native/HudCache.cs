using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SuanPleng;
public sealed partial class GameForm {
    readonly record struct HeaderStamp(int Day,int Minute,int Coins,int Energy,int Players,string Weather,string Mode,bool Invite,int Hover);
    readonly record struct BarStamp(string Seed,string Tool,int Hover);
    Bitmap? headerBitmap,barBitmap;
    Graphics? headerGraphics,barGraphics;
    HeaderStamp headerStamp;
    BarStamp barStamp;
    readonly List<(RectangleF Area,Action Action)> headerHits=new(),barHits=new();
    int HoverIndex(List<(RectangleF Area,Action Action)> hits) {
        if(renderPreview||uiPanel!="")return -1;
        Point cursor=PointToClient(Cursor.Position);var p=new PointF((cursor.X-ox)/scale,(cursor.Y-oy)/scale);
        for(int i=0;i<hits.Count;i++)if(hits[i].Area.Contains(p))return i;return -1;
    }
    void DrawCachedHud(Graphics g,Farm f) {
        int active=0;long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();foreach(var p in f.Players)if(now-p.LastSeen<30000)active++;
        var header=new HeaderStamp(f.Day,(int)f.Minute,f.Coins,Me?.Energy??0,active,f.Weather,mode,invite!="",HoverIndex(headerHits));
        var bar=new BarStamp(seed,tool,HoverIndex(barHits));
        if(headerBitmap==null){headerBitmap=new(1280,87,PixelFormat.Format32bppPArgb);headerGraphics=Graphics.FromImage(headerBitmap);headerGraphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;}
        if(barBitmap==null){barBitmap=new(1280,112,PixelFormat.Format32bppPArgb);barGraphics=Graphics.FromImage(barBitmap);barGraphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;barGraphics.TranslateTransform(0,-688);}
        if(headerHits.Count==0||header!=headerStamp){
            headerStamp=header;headerGraphics!.Clear(Color.Transparent);headerGraphics.SmoothingMode=SmoothingMode.AntiAlias;
            int first=buttons.Count;DrawHeader(headerGraphics,f);headerHits.Clear();for(int i=first;i<buttons.Count;i++)headerHits.Add(buttons[i]);
        }else buttons.AddRange(headerHits);
        if(barHits.Count==0||bar!=barStamp){
            barStamp=bar;barGraphics!.Clear(Color.Transparent);barGraphics.SmoothingMode=SmoothingMode.AntiAlias;
            int first=buttons.Count;DrawHotbar(barGraphics);barHits.Clear();for(int i=first;i<buttons.Count;i++)barHits.Add(buttons[i]);
        }else buttons.AddRange(barHits);
        g.DrawImage(headerBitmap,new Rectangle(0,0,1280,87),new Rectangle(0,0,1280,87),GraphicsUnit.Pixel);
        g.DrawImage(barBitmap,new Rectangle(0,688,1280,112),new Rectangle(0,0,1280,112),GraphicsUnit.Pixel);
    }
    void DisposeHudCache(){headerGraphics?.Dispose();barGraphics?.Dispose();headerBitmap?.Dispose();barBitmap?.Dispose();headerHits.Clear();barHits.Clear();}
}
