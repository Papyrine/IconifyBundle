using IconifyBundle.Raster;

// The build-time rasteriser (src/Raster, shared as source by the generator and the build task).
public class RasterTests
{
    const uint black = 0xFF000000;

    // Real icon bodies (Feather and Fluent UI System Icons are MIT licensed).
    internal const string FeatherActivity =
        """<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M22 12h-4l-3 9L9 3l-3 9H2"/>""";

    const string featherAlertCircle =
        """<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 8v4m0 4h.01"/></g>""";

    const string featherAirplay =
        """<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M5 17H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2h-1"/><path d="m12 15l5 6H7z"/></g>""";

    const string fluentFolder24Regular =
        """<path fill="currentColor" d="M3.5 6.25V8h4.629a.75.75 0 0 0 .53-.22l1.53-1.53l-1.53-1.53a.75.75 0 0 0-.53-.22H5.25A1.75 1.75 0 0 0 3.5 6.25m-1.5 0A3.25 3.25 0 0 1 5.25 3h2.879a2.25 2.25 0 0 1 1.59.659L11.562 5.5h7.189A3.25 3.25 0 0 1 22 8.75v9A3.25 3.25 0 0 1 18.75 21H5.25A3.25 3.25 0 0 1 2 17.75zM3.5 9.5v8.25c0 .966.784 1.75 1.75 1.75h13.5a1.75 1.75 0 0 0 1.75-1.75v-9A1.75 1.75 0 0 0 18.75 7h-7.19L9.72 8.841a2.25 2.25 0 0 1-1.591.659z"/>""";

    const string fluentCut20Regular =
        """<path fill="currentColor" d="M5.92 2.228a.5.5 0 0 0-.84.544L9.405 9.43L7.48 12.39a3 3 0 1 0 .779.636L10 10.348l1.74 2.68a3 3 0 1 0 .779-.636zM14 17a2 2 0 1 1 0-4a2 2 0 0 1 0 4M4 15a2 2 0 1 1 4 0a2 2 0 0 1-4 0m7.192-6.489l-.596-.918l3.485-5.365a.5.5 0 0 1 .838.544z"/>""";

    // Miter (square caps), bevel (butt caps), round (round caps), and a miter over the miter limit.
    const string joinsAndCaps =
        """<g fill="none" stroke="currentColor" stroke-width="2.5"><polyline stroke-linejoin="miter" stroke-linecap="square" points="3 9 7 3 11 9"/><polyline stroke-linejoin="bevel" points="13 9 17 3 21 9"/><polyline stroke-linejoin="round" stroke-linecap="round" points="3 21 7 15 11 21"/><polyline stroke-miterlimit="1" points="13 21 17 15 21 21"/></g>""";

    const string transformsAndOpacity =
        """<g transform="rotate(45 12 12)"><rect x="6" y="6" width="12" height="12" rx="3" fill="currentColor" opacity=".5"/></g><ellipse cx="12" cy="12" rx="9" ry="4" fill="none" stroke="tomato" stroke-width="1.5"/>""";

    [Test]
    public Task Feather_activity() =>
        VerifyIcon(FeatherActivity, 24, 24);

    [Test]
    public Task Feather_alert_circle() =>
        VerifyIcon(featherAlertCircle, 24, 24);

    [Test]
    public Task Feather_airplay() =>
        VerifyIcon(featherAirplay, 24, 24);

    [Test]
    public Task Fluent_folder_24_regular() =>
        VerifyIcon(fluentFolder24Regular, 24, 24);

    [Test]
    public Task Fluent_cut_20_regular() =>
        VerifyIcon(fluentCut20Regular, 20, 20);

    [Test]
    public Task Joins_and_caps() =>
        VerifyIcon(joinsAndCaps, 24, 24);

    [Test]
    public Task Transforms_and_opacity() =>
        VerifyIcon(transformsAndOpacity, 24, 24);

    static Task VerifyIcon(string body, double width, double height) =>
        Verify(
            new[] { 16, 20, 32, 64 }
                .Select(size => new Target("png", new MemoryStream(IconRaster.Encode(body, width, height, size, black, "png")), $"{size}px")));

    [Test]
    public async Task Filled_square_covers_every_pixel_in_the_colour()
    {
        var image = IconRaster.Render("""<path fill="currentColor" d="M0 0h24v24H0z"/>""", 24, 24, 16, 0xFF336699);

        for (var i = 0; i < image.Rgba.Length; i += 4)
        {
            await Assert.That(Pixel(image, i)).IsEqualTo(0xFF336699u);
        }
    }

    [Test]
    public async Task Stroke_width_is_exact()
    {
        // A 2-unit horizontal stroke at y=12 in a 24-unit box rendered at 24px covers exactly rows 11 and 12.
        var image = IconRaster.Render("""<path fill="none" stroke="currentColor" stroke-width="2" d="M0 12H24"/>""", 24, 24, 24, black);

        for (var y = 0; y < 24; y++)
        {
            var expected = (byte)0;
            if (y is 11 or 12)
            {
                expected = 255;
            }

            for (var x = 0; x < 24; x++)
            {
                await Assert.That(Alpha(image, x, y)).IsEqualTo(expected);
            }
        }
    }

    [Test]
    public async Task Even_odd_leaves_a_hole_that_nonzero_fills()
    {
        // Two squares wound the same way: nonzero fills the inner one, even-odd leaves it empty.
        const string data = "M2 2h20v20H2z M7 7h10v10H7z";
        var nonzero = IconRaster.Render($"""<path d="{data}"/>""", 24, 24, 24, black);
        var evenOdd = IconRaster.Render($"""<path fill-rule="evenodd" d="{data}"/>""", 24, 24, 24, black);

        await Assert.That(Alpha(nonzero, 12, 12)).IsEqualTo((byte)255);
        await Assert.That(Alpha(evenOdd, 12, 12)).IsEqualTo((byte)0);
        await Assert.That(Alpha(nonzero, 4, 4)).IsEqualTo((byte)255);
        await Assert.That(Alpha(evenOdd, 4, 4)).IsEqualTo((byte)255);
    }

    [Test]
    public async Task Current_color_is_configured_and_literal_colours_are_kept()
    {
        var image = IconRaster.Render(
            """<rect width="12" height="24" fill="currentColor"/><rect x="12" width="12" height="24" fill="#f00"/><rect x="12" y="12" width="12" height="12" style="fill: rgb(0, 0, 255)"/>""",
            24,
            24,
            24,
            0xFF1E90FF);

        await Assert.That(Pixel(image, 3, 3)).IsEqualTo(0xFF1E90FFu);
        await Assert.That(Pixel(image, 20, 3)).IsEqualTo(0xFFFF0000u);
        await Assert.That(Pixel(image, 20, 20)).IsEqualTo(0xFF0000FFu);
    }

    [Test]
    public async Task Translucent_colour_and_opacity_multiply()
    {
        var image = IconRaster.Render("""<path fill="currentColor" opacity=".5" d="M0 0h24v24H0z"/>""", 24, 24, 8, 0x80FFFFFF);

        // 0.5 (colour alpha) * 0.5 (opacity) = 0.25.
        await Assert.That(Alpha(image, 4, 4)).IsEqualTo((byte)64);
    }

    [Test]
    public async Task Non_square_icon_is_fitted_and_centred()
    {
        // A 32x16 icon in a 16px square is drawn 16x8, with 4 empty rows above and below.
        var image = IconRaster.Render("""<path d="M0 0h32v16H0z"/>""", 32, 16, 16, black);

        for (var y = 0; y < 16; y++)
        {
            var expected = (byte)0;
            if (y is >= 4 and < 12)
            {
                expected = 255;
            }

            await Assert.That(Alpha(image, 0, y)).IsEqualTo(expected);
            await Assert.That(Alpha(image, 15, y)).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Zero_length_subpath_with_round_caps_draws_a_dot()
    {
        // Feather's "M12 16h.01" style dot, and the SVG "M x y z" form.
        var image = IconRaster.Render("""<path stroke="currentColor" stroke-linecap="round" stroke-width="4" d="M6 12z M18 12h.01"/>""", 24, 24, 24, black);

        await Assert.That(Alpha(image, 6, 12)).IsEqualTo((byte)255);
        await Assert.That(Alpha(image, 18, 12)).IsEqualTo((byte)255);
        await Assert.That(Alpha(image, 12, 12)).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Png_decodes_to_the_rendered_pixels()
    {
        foreach (var (body, size) in new[] { (FeatherActivity, 16), (featherAirplay, 20), (fluentFolder24Regular, 32), (transformsAndOpacity, 64) })
        {
            var image = IconRaster.Render(body, 24, 24, size, 0xFF336699);
            var png = PngEncoder.Encode(image.Rgba, size, size);

            var (width, height, rgba) = PngReader.Read(png);

            await Assert.That(width).IsEqualTo(size);
            await Assert.That(height).IsEqualTo(size);
            await Assert.That(rgba.SequenceEqual(image.Rgba)).IsTrue();
        }
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(1000)]
    [Arguments(70000)]
    public async Task Deflate_round_trips(int length)
    {
        var random = new Random(length);
        var noise = new byte[length];
        random.NextBytes(noise);
        // Long runs and repeats exercise the length/distance codes, up to the 258-byte maximum match.
        var repetitive = Enumerable.Range(0, length).Select(_ => (byte)(_ / 300 % 7)).ToArray();

        foreach (var data in new[] { noise, repetitive })
        {
            await Assert.That(PngReader.Inflate(Zlib.Compress(data)).SequenceEqual(data)).IsTrue();
        }
    }

    [Test]
    public async Task Bmp_is_bottom_up_bgra_with_an_alpha_mask()
    {
        // Top half red, bottom half translucent blue.
        var image = IconRaster.Render(
            """<rect width="24" height="12" fill="red"/><rect y="12" width="24" height="12" fill="blue" fill-opacity=".5"/>""",
            24,
            24,
            4,
            black);
        var bmp = BmpEncoder.Encode(image.Rgba, 4, 4);

        await Assert.That(bmp.Length).IsEqualTo(14 + 124 + 4 * 4 * 4);
        await Assert.That(Encoding.ASCII.GetString(bmp, 0, 2)).IsEqualTo("BM");
        await Assert.That(BitConverter.ToInt32(bmp, 2)).IsEqualTo(bmp.Length);
        await Assert.That(BitConverter.ToInt32(bmp, 10)).IsEqualTo(138);
        await Assert.That(BitConverter.ToInt32(bmp, 14)).IsEqualTo(124);
        await Assert.That(BitConverter.ToInt32(bmp, 18)).IsEqualTo(4);
        await Assert.That(BitConverter.ToInt32(bmp, 22)).IsEqualTo(4);
        await Assert.That(BitConverter.ToInt16(bmp, 28)).IsEqualTo((short)32);
        await Assert.That(BitConverter.ToInt32(bmp, 30)).IsEqualTo(3);
        await Assert.That(BitConverter.ToUInt32(bmp, 66)).IsEqualTo(0xFF000000u);

        // The first stored row is the bottom one: translucent blue as B, G, R, A.
        await Assert.That(bmp.Skip(138).Take(4).ToArray().SequenceEqual(new byte[] { 255, 0, 0, 128 })).IsTrue();
        // The last stored row is the top one: red.
        await Assert.That(bmp.Skip(138 + 3 * 16).Take(4).ToArray().SequenceEqual(new byte[] { 0, 0, 255, 255 })).IsTrue();
    }

    [Test]
    public async Task Unsupported_features_are_reported()
    {
        const string body =
            """
            <defs><linearGradient id="g"><stop stop-color="#f00"/><stop offset="1" stop-color="#00f"/></linearGradient></defs>
            <path fill="url(#g)" mask="url(#m)" d="M0 0h24v24H0z"/>
            <path stroke="currentColor" stroke-dasharray="2 2" d="M0 12h24"><animate attributeName="stroke-dashoffset" values="4;0" dur="1s"/></path>
            <text>hi</text>
            """;

        await Assert.That(string.Join(", ", SvgRenderer.Analyse(body))).IsEqualTo("animation, gradient, mask, stroke-dasharray, text");
        await Assert.That(SvgRenderer.Analyse(FeatherActivity).Count).IsEqualTo(0);
        await Assert.That(SvgRenderer.Analyse(fluentCut20Regular).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Gradient_is_flattened_to_its_average_stop()
    {
        var image = IconRaster.Render(
            """<defs><linearGradient id="g"><stop stop-color="#f00"/><stop offset="1" stop-color="#00f"/></linearGradient></defs><path fill="url(#g)" d="M0 0h24v24H0z"/>""",
            24,
            24,
            4,
            black);

        await Assert.That(Pixel(image, 2, 2)).IsEqualTo(0xFF800080u);
    }

    [Test]
    public async Task Use_renders_the_referenced_element_offset()
    {
        var image = IconRaster.Render(
            """<defs><rect id="r" width="6" height="6"/></defs><use href="#r" x="12" y="12"/>""",
            24,
            24,
            24,
            black);

        await Assert.That(Alpha(image, 14, 14)).IsEqualTo((byte)255);
        await Assert.That(Alpha(image, 2, 2)).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Path_data_compact_forms()
    {
        // Arc flags with no separators ("00"), numbers split by a second '.', and an exponent.
        var path = new PathGeometry();
        PathData.Parse("M1.5.5a1 1 0 002 0l1e1-1", path);

        await Assert.That(path.Figures.Count).IsEqualTo(1);
        await Assert.That(path.Figures[0].Start.X).IsEqualTo(1.5);
        await Assert.That(path.Figures[0].Start.Y).IsEqualTo(.5);
        await Assert.That(path.Current.X).IsEqualTo(13.5);
        await Assert.That(path.Current.Y).IsEqualTo(-.5);
    }

    [Test]
    public async Task Path_data_stops_at_the_first_error()
    {
        var path = new PathGeometry();
        PathData.Parse("M0 0L10 0L10 x L0 10", path);

        await Assert.That(path.Figures[0].Segments.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(null, "16;24;16", null, "png sizes=16,24 color=FF000000")]
    [Arguments("png;bmp", "32,16", "1e90ff", "png,bmp sizes=16,32 color=FF1E90FF")]
    [Arguments("BMP", "20", "#ff000080", "bmp sizes=20 color=80FF0000")]
    [Arguments(" png , bmp ", " 16 ; 20 ", "DodgerBlue", "png,bmp sizes=16,20 color=FF1E90FF")]
    [Arguments(null, null, null, "off")]
    [Arguments(null, null, "#fff", "off")]
    [Arguments("png", "", null, "error: IconifyBundleRasterFormat is set but IconifyBundleRasterSizes is empty: give one or more pixel sizes, for example 16;24;32")]
    [Arguments("gif", "16", null, "error: IconifyBundleRasterFormat 'gif' is not valid: 'gif' is not a supported format (use png, bmp, or both)")]
    [Arguments(null, "16;0", null, "error: IconifyBundleRasterSizes '16;0' is not valid: '0' is not a whole number of pixels from 1 to 1024")]
    [Arguments(null, "2048", null, "error: IconifyBundleRasterSizes '2048' is not valid: '2048' is not a whole number of pixels from 1 to 1024")]
    [Arguments(null, "16", "nope", "error: IconifyBundleRasterColor 'nope' is not a colour: use #RRGGBB, #RRGGBBAA or a CSS colour name")]
    public async Task Options_parse(string? formats, string? sizes, string? color, string expected)
    {
        var options = RasterOptions.Parse(formats, sizes, color);

        string actual;
        if (options.Error != null)
        {
            actual = "error: " + options.Error;
        }
        else if (!options.Enabled)
        {
            actual = "off";
        }
        else
        {
            actual = $"{string.Join(",", options.Formats)} sizes={string.Join(",", options.Sizes)} color={options.Color:X8}";
        }

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("#abc", 0xFFAABBCCu)]
    [Arguments("#abcd", 0xDDAABBCCu)]
    [Arguments("#336699", 0xFF336699u)]
    [Arguments("336699", 0xFF336699u)]
    [Arguments("#33669980", 0x80336699u)]
    [Arguments("rebeccapurple", 0xFF663399u)]
    [Arguments("rgb(255, 0, 0)", 0xFFFF0000u)]
    [Arguments("rgb(0% 100% 0% / 50%)", 0x8000FF00u)]
    [Arguments("rgba(0,0,255,.5)", 0x800000FFu)]
    [Arguments("transparent", 0x00000000u)]
    public async Task Colours_parse(string value, uint expected)
    {
        await Assert.That(SvgColor.TryParse(value, out var color)).IsTrue();
        await Assert.That(color.ToArgb()).IsEqualTo(expected);
    }

    static uint Pixel(RasterImage image, int x, int y) =>
        Pixel(image, (y * image.Width + x) * 4);

    static uint Pixel(RasterImage image, int index)
    {
        var p = image.Rgba;
        return ((uint)p[index + 3] << 24) | ((uint)p[index] << 16) | ((uint)p[index + 1] << 8) | p[index + 2];
    }

    static byte Alpha(RasterImage image, int x, int y) =>
        image.Rgba[(y * image.Width + x) * 4 + 3];
}

/// <summary>
/// A minimal PNG reader, independent of the encoder under test: it checks each chunk's CRC with a bitwise
/// CRC-32, inflates with .NET's zlib, and reverses the row filters.
/// </summary>
static class PngReader
{
    public static (int Width, int Height, byte[] Rgba) Read(byte[] png)
    {
        if (!png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            throw new("Not a PNG");
        }

        int width = 0, height = 0;
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset < png.Length)
        {
            var length = BigEndian(png, offset);
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var chunk = png.AsSpan(offset + 4, length + 4).ToArray();
            if (Crc(chunk) != (uint)BigEndian(png, offset + 8 + length))
            {
                throw new($"Bad CRC in {type}");
            }

            var data = png.AsSpan(offset + 8, length);
            switch (type)
            {
                case "IHDR":
                    width = BigEndian(png, offset + 8);
                    height = BigEndian(png, offset + 12);
                    if (data[8] != 8 ||
                        data[9] != 6)
                    {
                        throw new("Expected 8-bit RGBA");
                    }

                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
            }

            offset += 12 + length;
        }

        var raw = Inflate(idat.ToArray());
        var stride = width * 4;
        var rgba = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            for (var x = 0; x < stride; x++)
            {
                var value = raw[y * (stride + 1) + 1 + x];
                var left = x >= 4 ? rgba[y * stride + x - 4] : 0;
                var up = y > 0 ? rgba[(y - 1) * stride + x] : 0;
                var upLeft = x >= 4 && y > 0 ? rgba[(y - 1) * stride + x - 4] : 0;
                var predictor = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, upLeft),
                    _ => throw new($"Bad filter {filter}")
                };
                rgba[y * stride + x] = (byte)(value + predictor);
            }
        }

        return (width, height, rgba);
    }

    public static byte[] Inflate(byte[] zlib)
    {
        using var input = new ZLibStream(new MemoryStream(zlib), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb &&
            pa <= pc)
        {
            return a;
        }

        if (pb <= pc)
        {
            return b;
        }

        return c;
    }

    static int BigEndian(byte[] buffer, int offset) =>
        (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];

    static uint Crc(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc >> 1) ^ (0xEDB88320u & (0u - (crc & 1)));
            }
        }

        return ~crc;
    }
}
