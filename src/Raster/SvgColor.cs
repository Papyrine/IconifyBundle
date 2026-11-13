using System.Globalization;

namespace IconifyBundle.Raster;

/// <summary>A straight (not premultiplied) alpha colour with 8-bit components.</summary>
readonly struct Rgba(byte r, byte g, byte b, byte a)
{
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;
    public byte A { get; } = a;

    public static Rgba FromArgb(uint argb) =>
        new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));

    public uint ToArgb() =>
        ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;
}

/// <summary>Parses CSS/SVG colour values: hex, <c>rgb()</c>/<c>rgba()</c>, and the CSS named colours.</summary>
static class SvgColor
{
    public static bool TryParse(string value, out Rgba color)
    {
        color = default;
        value = value.Trim();
        if (value.Length == 0)
        {
            return false;
        }

        if (value[0] == '#')
        {
            return TryParseHex(value.Substring(1), out color);
        }

        var lower = value.ToLowerInvariant();
        if (lower == "transparent")
        {
            return true;
        }

        if (named.Value.TryGetValue(lower, out var rgb))
        {
            color = Rgba.FromArgb(0xFF000000 | rgb);
            return true;
        }

        if (lower.StartsWith("rgb", StringComparison.Ordinal))
        {
            return TryParseFunction(lower, out color);
        }

        // The build hands the configured colour to the generator without its '#' (see IconifyBundle.targets).
        return TryParseHex(value, out color);
    }

    static bool TryParseHex(string hex, out Rgba color)
    {
        color = default;
        foreach (var c in hex)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        switch (hex.Length)
        {
            case 3:
            case 4:
            {
                var r = Nibble(hex[0]);
                var g = Nibble(hex[1]);
                var b = Nibble(hex[2]);
                var a = 15;
                if (hex.Length == 4)
                {
                    a = Nibble(hex[3]);
                }

                color = new((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), (byte)(a * 17));
                return true;
            }
            case 6:
            case 8:
            {
                var a = 255;
                if (hex.Length == 8)
                {
                    a = Byte(hex, 6);
                }

                color = new((byte)Byte(hex, 0), (byte)Byte(hex, 2), (byte)Byte(hex, 4), (byte)a);
                return true;
            }
        }

        return false;
    }

    static int Nibble(char c) =>
        Uri.FromHex(c);

    static int Byte(string hex, int index) =>
        Nibble(hex[index]) * 16 + Nibble(hex[index + 1]);

    // rgb(255, 0, 0), rgb(100% 0% 0%), rgba(255, 0, 0, 0.5), rgb(255 0 0 / 50%)
    static bool TryParseFunction(string value, out Rgba color)
    {
        color = default;
        var open = value.IndexOf('(');
        var close = value.LastIndexOf(')');
        if (open < 0 ||
            close < open)
        {
            return false;
        }

        var parts = value.Substring(open + 1, close - open - 1)
            .Split([',', ' ', '/', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (3 or 4))
        {
            return false;
        }

        var channels = new byte[4];
        channels[3] = 255;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            var percent = part.Length > 0 && part[part.Length - 1] == '%';
            if (percent)
            {
                part = part.Substring(0, part.Length - 1);
            }

            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            double scaled;
            if (i == 3)
            {
                scaled = number * 255;
                if (percent)
                {
                    scaled = number / 100 * 255;
                }
            }
            else
            {
                scaled = number;
                if (percent)
                {
                    scaled = number / 100 * 255;
                }
            }

            channels[i] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(scaled)));
        }

        color = new(channels[0], channels[1], channels[2], channels[3]);
        return true;
    }

    // The CSS named colours (CSS Color Module Level 4), as name:rrggbb.
    const string namedColors =
        "aliceblue:f0f8ff,antiquewhite:faebd7,aqua:00ffff,aquamarine:7fffd4,azure:f0ffff,beige:f5f5dc," +
        "bisque:ffe4c4,black:000000,blanchedalmond:ffebcd,blue:0000ff,blueviolet:8a2be2,brown:a52a2a," +
        "burlywood:deb887,cadetblue:5f9ea0,chartreuse:7fff00,chocolate:d2691e,coral:ff7f50," +
        "cornflowerblue:6495ed,cornsilk:fff8dc,crimson:dc143c,cyan:00ffff,darkblue:00008b,darkcyan:008b8b," +
        "darkgoldenrod:b8860b,darkgray:a9a9a9,darkgreen:006400,darkgrey:a9a9a9,darkkhaki:bdb76b," +
        "darkmagenta:8b008b,darkolivegreen:556b2f,darkorange:ff8c00,darkorchid:9932cc,darkred:8b0000," +
        "darksalmon:e9967a,darkseagreen:8fbc8f,darkslateblue:483d8b,darkslategray:2f4f4f," +
        "darkslategrey:2f4f4f,darkturquoise:00ced1,darkviolet:9400d3,deeppink:ff1493,deepskyblue:00bfff," +
        "dimgray:696969,dimgrey:696969,dodgerblue:1e90ff,firebrick:b22222,floralwhite:fffaf0," +
        "forestgreen:228b22,fuchsia:ff00ff,gainsboro:dcdcdc,ghostwhite:f8f8ff,gold:ffd700,goldenrod:daa520," +
        "gray:808080,green:008000,greenyellow:adff2f,grey:808080,honeydew:f0fff0,hotpink:ff69b4," +
        "indianred:cd5c5c,indigo:4b0082,ivory:fffff0,khaki:f0e68c,lavender:e6e6fa,lavenderblush:fff0f5," +
        "lawngreen:7cfc00,lemonchiffon:fffacd,lightblue:add8e6,lightcoral:f08080,lightcyan:e0ffff," +
        "lightgoldenrodyellow:fafad2,lightgray:d3d3d3,lightgreen:90ee90,lightgrey:d3d3d3,lightpink:ffb6c1," +
        "lightsalmon:ffa07a,lightseagreen:20b2aa,lightskyblue:87cefa,lightslategray:778899," +
        "lightslategrey:778899,lightsteelblue:b0c4de,lightyellow:ffffe0,lime:00ff00,limegreen:32cd32," +
        "linen:faf0e6,magenta:ff00ff,maroon:800000,mediumaquamarine:66cdaa,mediumblue:0000cd," +
        "mediumorchid:ba55d3,mediumpurple:9370db,mediumseagreen:3cb371,mediumslateblue:7b68ee," +
        "mediumspringgreen:00fa9a,mediumturquoise:48d1cc,mediumvioletred:c71585,midnightblue:191970," +
        "mintcream:f5fffa,mistyrose:ffe4e1,moccasin:ffe4b5,navajowhite:ffdead,navy:000080,oldlace:fdf5e6," +
        "olive:808000,olivedrab:6b8e23,orange:ffa500,orangered:ff4500,orchid:da70d6,palegoldenrod:eee8aa," +
        "palegreen:98fb98,paleturquoise:afeeee,palevioletred:db7093,papayawhip:ffefd5,peachpuff:ffdab9," +
        "peru:cd853f,pink:ffc0cb,plum:dda0dd,powderblue:b0e0e6,purple:800080,rebeccapurple:663399," +
        "red:ff0000,rosybrown:bc8f8f,royalblue:4169e1,saddlebrown:8b4513,salmon:fa8072,sandybrown:f4a460," +
        "seagreen:2e8b57,seashell:fff5ee,sienna:a0522d,silver:c0c0c0,skyblue:87ceeb,slateblue:6a5acd," +
        "slategray:708090,slategrey:708090,snow:fffafa,springgreen:00ff7f,steelblue:4682b4,tan:d2b48c," +
        "teal:008080,thistle:d8bfd8,tomato:ff6347,turquoise:40e0d0,violet:ee82ee,wheat:f5deb3," +
        "white:ffffff,whitesmoke:f5f5f5,yellow:ffff00,yellowgreen:9acd32";

    static readonly Lazy<Dictionary<string, uint>> named = new(() =>
    {
        var result = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (var entry in namedColors.Split(','))
        {
            var colon = entry.IndexOf(':');
            // netstandard2.0 (the generator and the build task) has no span overload.
#pragma warning disable CA1846
            result[entry.Substring(0, colon)] = uint.Parse(entry.Substring(colon + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
#pragma warning restore CA1846
        }

        return result;
    });
}
