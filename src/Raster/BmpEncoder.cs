namespace IconifyBundle.Raster;

/// <summary>
/// Writes uncompressed 32-bit BMPs with a <c>BITMAPV5HEADER</c> whose bit masks include alpha, so readers
/// that honour BMP transparency (WIC, browsers, most image libraries) keep it. Rows are stored bottom-up,
/// as BGRA with straight alpha.
/// </summary>
static class BmpEncoder
{
    const int fileHeaderSize = 14;
    const int infoHeaderSize = 124;

    public static byte[] Encode(byte[] rgba, int width, int height)
    {
        var pixelBytes = width * height * 4;
        var offset = fileHeaderSize + infoHeaderSize;
        var result = new byte[offset + pixelBytes];

        // BITMAPFILEHEADER
        result[0] = (byte)'B';
        result[1] = (byte)'M';
        LittleEndian(result, 2, (uint)result.Length);
        LittleEndian(result, 10, (uint)offset);

        // BITMAPV5HEADER
        var h = fileHeaderSize;
        LittleEndian(result, h, infoHeaderSize);
        LittleEndian(result, h + 4, (uint)width);
        // A positive height means the rows run bottom-up, which every reader supports.
        LittleEndian(result, h + 8, (uint)height);
        result[h + 12] = 1;
        result[h + 14] = 32;
        // BI_BITFIELDS
        LittleEndian(result, h + 16, 3);
        LittleEndian(result, h + 20, (uint)pixelBytes);
        // 96 DPI (3780 pixels per metre).
        LittleEndian(result, h + 24, 3780);
        LittleEndian(result, h + 28, 3780);
        // Red, green, blue and alpha masks.
        LittleEndian(result, h + 40, 0x00FF0000);
        LittleEndian(result, h + 44, 0x0000FF00);
        LittleEndian(result, h + 48, 0x000000FF);
        LittleEndian(result, h + 52, 0xFF000000);
        // LCS_sRGB
        LittleEndian(result, h + 56, 0x73524742);
        // LCS_GM_IMAGES
        LittleEndian(result, h + 108, 4);

        var stride = width * 4;
        for (var y = 0; y < height; y++)
        {
            var source = y * stride;
            var target = offset + (height - 1 - y) * stride;
            for (var x = 0; x < stride; x += 4)
            {
                result[target + x] = rgba[source + x + 2];
                result[target + x + 1] = rgba[source + x + 1];
                result[target + x + 2] = rgba[source + x];
                result[target + x + 3] = rgba[source + x + 3];
            }
        }

        return result;
    }

    static void LittleEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}

/// <summary>Renders icons and encodes them in the requested format.</summary>
static class IconRaster
{
    public static RasterImage Render(string body, double width, double height, int size, uint color) =>
        SvgRenderer.Render(body, width, height, size, Rgba.FromArgb(color));

    public static byte[] Encode(RasterImage image, string format)
    {
        if (format == "bmp")
        {
            return BmpEncoder.Encode(image.Rgba, image.Width, image.Height);
        }

        return PngEncoder.Encode(image.Rgba, image.Width, image.Height);
    }

    public static byte[] Encode(string body, double width, double height, int size, uint color, string format) =>
        Encode(Render(body, width, height, size, color), format);
}
