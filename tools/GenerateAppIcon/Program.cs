using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: GenerateAppIcon <input.svg> <output.ico>");
    return 1;
}

string svgPath = Path.GetFullPath(args[0]);
string icoPath = Path.GetFullPath(args[1]);
if (!File.Exists(svgPath))
{
    Console.Error.WriteLine($"Fichier introuvable : {svgPath}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);

var settings = new WpfDrawingSettings { IncludeRuntime = false };
var reader = new FileSvgReader(settings);
DrawingGroup drawing = reader.Read(new Uri(svgPath));

Rect bounds = drawing.Bounds;
if (bounds.IsEmpty)
{
    bounds = new Rect(0, 0, 1024, 1024);
}

double scaleBase = Math.Min(1024.0 / bounds.Width, 1024.0 / bounds.Height);
Vector center = new Vector((1024 - bounds.Width * scaleBase) / 2, (1024 - bounds.Height * scaleBase) / 2);

int[] sizes = [256, 64, 48, 32, 16];
var frames = new List<(int Size, byte[] PngBytes)>();

foreach (int size in sizes)
{
    var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    var dv = new DrawingVisual();
    using (DrawingContext dc = dv.RenderOpen())
    {
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, size, size));
        dc.PushTransform(new TranslateTransform(center.X / 1024.0 * size, center.Y / 1024.0 * size));
        double s = scaleBase * (size / 1024.0);
        dc.PushTransform(new ScaleTransform(s, s));
        dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
        dc.DrawDrawing(drawing);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    rtb.Render(dv);
    rtb.Freeze();

    using var ms = new MemoryStream();
    var enc = new PngBitmapEncoder();
    enc.Frames.Add(BitmapFrame.Create(rtb));
    enc.Save(ms);
    frames.Add((size, ms.ToArray()));
}

WriteIco(icoPath, frames);
Console.WriteLine($"Écrit : {icoPath}");
return 0;

static void WriteIco(string path, List<(int Size, byte[] PngBytes)> pngFrames)
{
    // ICO avec images PNG embarquées (format Vista+)
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);

    bw.Write((short)0);
    bw.Write((short)1);
    bw.Write((short)pngFrames.Count);

    int offset = 6 + pngFrames.Count * 16;
    foreach (var (size, png) in pngFrames)
    {
        int dim = size >= 256 ? 0 : size;
        bw.Write((byte)dim);
        bw.Write((byte)dim);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(png.Length);
        bw.Write(offset);
        offset += png.Length;
    }

    foreach (var (_, png) in pngFrames)
        bw.Write(png);
}
