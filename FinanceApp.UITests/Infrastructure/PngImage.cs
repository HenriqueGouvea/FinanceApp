using System.Buffers.Binary;
using System.IO.Compression;

namespace FinanceApp.UITests.Infrastructure;

/// <summary>
/// Minimal decoder for the 8-bit RGB/RGBA, non-interlaced PNGs that Appium returns as element screenshots.
/// Lets tests assert on rendered colors, which the accessibility tree doesn't expose.
/// </summary>
public sealed class PngImage
{
    private const int ColorTypeRgb = 2;
    private const int ColorTypeRgba = 6;

    private readonly byte[] _pixels;
    private readonly int _bytesPerPixel;

    public int Width { get; }
    public int Height { get; }

    private PngImage(int width, int height, int bytesPerPixel, byte[] pixels)
    {
        Width = width;
        Height = height;
        _bytesPerPixel = bytesPerPixel;
        _pixels = pixels;
    }

    public (byte R, byte G, byte B) GetPixel(int x, int y)
    {
        var offset = (y * Width + x) * _bytesPerPixel;
        return (_pixels[offset], _pixels[offset + 1], _pixels[offset + 2]);
    }

    public IEnumerable<(byte R, byte G, byte B)> Pixels() =>
        Enumerable.Range(0, Height).SelectMany(y => Enumerable.Range(0, Width).Select(x => GetPixel(x, y)));

    public static PngImage Decode(byte[] png)
    {
        int width = 0, height = 0, bytesPerPixel = 0;
        using var compressed = new MemoryStream();

        for (var position = 8; position < png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position));
            var type = System.Text.Encoding.ASCII.GetString(png, position + 4, 4);
            var data = png.AsSpan(position + 8, length);

            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                var bitDepth = data[8];
                var colorType = data[9];
                var interlace = data[12];
                if (bitDepth != 8 || interlace != 0 || colorType is not (ColorTypeRgb or ColorTypeRgba))
                    throw new NotSupportedException($"Unsupported PNG (bit depth {bitDepth}, color type {colorType}, interlace {interlace}).");
                bytesPerPixel = colorType == ColorTypeRgba ? 4 : 3;
            }
            else if (type == "IDAT")
            {
                compressed.Write(data);
            }

            position += 12 + length;
        }

        compressed.Position = 0;
        using var raw = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress))
            zlib.CopyTo(raw);

        return new PngImage(width, height, bytesPerPixel, Unfilter(raw.ToArray(), width, height, bytesPerPixel));
    }

    // Reverses the per-scanline filters described in the PNG spec (section 9).
    private static byte[] Unfilter(byte[] data, int width, int height, int bpp)
    {
        var stride = width * bpp;
        var pixels = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            var filter = data[y * (stride + 1)];
            var source = y * (stride + 1) + 1;
            var row = y * stride;
            var previousRow = row - stride;

            for (var x = 0; x < stride; x++)
            {
                int left = x >= bpp ? pixels[row + x - bpp] : 0;
                int up = y > 0 ? pixels[previousRow + x] : 0;
                int upLeft = x >= bpp && y > 0 ? pixels[previousRow + x - bpp] : 0;

                var predictor = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, upLeft),
                    _ => throw new NotSupportedException($"Unknown PNG filter {filter}.")
                };

                pixels[row + x] = (byte)(data[source + x] + predictor);
            }
        }

        return pixels;
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
