using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

if (args.Length != 3) throw new ArgumentException("Usage: PolishBake growth-source.png tools-source.png output-directory");
Directory.CreateDirectory(args[2]);
string[] crops = { "carrot", "turnip", "potato", "tomato", "corn", "berry", "pumpkin", "cabbage" };
string[] tools = { "plant", "water", "harvest", "treat", "fertilize", "stake", "clear", "coin" };
var regions = new Dictionary<string, int[]>();
Rectangle Bounds(Bitmap source, Rectangle cell) {
    int x0=cell.Right,y0=cell.Bottom,x1=-1,y1=-1;
    for(int y=cell.Top;y<cell.Bottom;y++) for(int x=cell.Left;x<cell.Right;x++) if(source.GetPixel(x,y).A>128) {
        x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
    }
    if(x1<0) throw new InvalidDataException("Empty source sprite at " + cell);
    return Rectangle.FromLTRB(x0,y0,x1+1,y1+1);
}
using(var source=new Bitmap(args[0])) using(var atlas=new Bitmap(160,448,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(atlas)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
    for(int row=0;row<8;row++) for(int stage=0;stage<4;stage++) {
        var cell=Rectangle.FromLTRB(stage*source.Width/4,row*source.Height/8,(stage+1)*source.Width/4,(row+1)*source.Height/8);
        var b=Bounds(source,cell);int width=new[]{12,20,27,30}[stage];
        int height=Math.Max(1,(int)Math.Round((double)b.Height/b.Width*width));
        if(height>48){width=Math.Max(1,(int)Math.Round(width*48.0/height));height=48;}
        g.DrawImage(source,new Rectangle(stage*40+(40-width)/2,row*56+52-height,width,height),b,GraphicsUnit.Pixel);
        regions[crops[row]+"-"+stage]=new[]{stage*40,row*56,40,56};
    }
    atlas.Save(Path.Combine(args[2],"crop-growth.png"),ImageFormat.Png);
}
using(var source=new Bitmap(args[1])) using(var atlas=new Bitmap(96,48,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(atlas)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
    for(int i=0;i<8;i++) {
        int col=i%4,row=i/4;
        var b=Bounds(source,Rectangle.FromLTRB(col*source.Width/4,row*source.Height/2,(col+1)*source.Width/4,(row+1)*source.Height/2));
        float scale=Math.Min(22f/b.Width,22f/b.Height);int w=Math.Max(1,(int)Math.Round(b.Width*scale)),h=Math.Max(1,(int)Math.Round(b.Height*scale));
        g.DrawImage(source,new Rectangle(col*24+(24-w)/2,row*24+(24-h)/2,w,h),b,GraphicsUnit.Pixel);
        regions["tool-"+tools[i]]=new[]{col*24,row*24,24,24};
    }
    atlas.Save(Path.Combine(args[2],"tool-icons.png"),ImageFormat.Png);
}
File.WriteAllText(Path.Combine(args[2],"polish-regions.json"),JsonSerializer.Serialize(regions,new JsonSerializerOptions{WriteIndented=true}));
// Rasterize tool rotation once at build time; runtime poses stay on an integer pixel grid.
using(var source=new Bitmap(Path.Combine(args[2],"tool-icons.png"))) using(var poses=new Bitmap(128,224,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(poses)) {
    g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.SmoothingMode=SmoothingMode.None;
    float[][] angles={new[]{-10f,5,20,0},new[]{0f,15,30,5},new[]{-30f,-5,30,5},new[]{-5f,0,10,5},new[]{-10f,10,25,5},new[]{-10f,-5,0,5},new[]{-40f,-20,5,25}};
    for(int tool=0;tool<7;tool++) for(int frame=0;frame<4;frame++) {
        var state=g.Save();g.TranslateTransform(frame*32+16,tool*32+16);g.RotateTransform(angles[tool][frame]);
        g.DrawImage(source,new Rectangle(-12,-12,24,24),new Rectangle(tool%4*24,tool/4*24,24,24),GraphicsUnit.Pixel);g.Restore(state);
    }
    poses.Save(Path.Combine(args[2],"tool-poses.png"),ImageFormat.Png);
}
Console.WriteLine("Baked 32 crop stages and eight tool/currency icons; source alpha preserved, nearest-only packing.");
