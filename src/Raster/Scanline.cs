namespace IconifyBundle.Raster;

/// <summary>
/// Anti-aliased polygon coverage. Each pixel row is sampled at 16 sub-scanlines; on each one the spans
/// inside the shape (by the nonzero or even-odd rule) are found from the sorted edge crossings, and the
/// coverage of the partly-covered pixels at each span's ends is exact horizontally. Overlapping polygons
/// are resolved by the winding count, so a union of stroke pieces is never counted twice.
/// </summary>
static class Scanline
{
    const int subScanlines = 16;

    readonly struct Edge(double x0, double y0, double x1, double y1, int direction)
    {
        public double X0 { get; } = x0;
        public double Y0 { get; } = y0;
        public double Y1 { get; } = y1;
        public double Slope { get; } = (x1 - x0) / (y1 - y0);
        public int Direction { get; } = direction;
    }

    /// <summary>The coverage (0-1) of each pixel of a <paramref name="width"/> by <paramref name="height"/> image.</summary>
    public static double[] Coverage(List<List<Vec>> polygons, bool evenOdd, int width, int height)
    {
        var coverage = new double[width * height];
        var edges = Edges(polygons, height);
        if (edges.Count == 0)
        {
            return coverage;
        }

        edges.Sort((a, b) => a.Y0.CompareTo(b.Y0));
        var maxY = edges.Max(_ => _.Y1);
        var firstRow = Math.Max(0, (int)Math.Floor(edges[0].Y0));
        var lastRow = Math.Min(height - 1, (int)Math.Floor(maxY));

        var active = new List<Edge>();
        var next = 0;
        var xs = new double[16];
        var directions = new int[16];
        const double weight = 1.0 / subScanlines;

        for (var row = firstRow; row <= lastRow; row++)
        {
            var rowStart = row * width;
            for (var sample = 0; sample < subScanlines; sample++)
            {
                var y = row + (sample + 0.5) / subScanlines;
                while (next < edges.Count &&
                       edges[next].Y0 <= y)
                {
                    active.Add(edges[next]);
                    next++;
                }

                var kept = 0;
                for (var i = 0; i < active.Count; i++)
                {
                    if (active[i].Y1 > y)
                    {
                        active[kept++] = active[i];
                    }
                }

                active.RemoveRange(kept, active.Count - kept);
                if (active.Count == 0)
                {
                    continue;
                }

                if (xs.Length < active.Count)
                {
                    xs = new double[active.Count * 2];
                    directions = new int[active.Count * 2];
                }

                for (var i = 0; i < active.Count; i++)
                {
                    var edge = active[i];
                    xs[i] = edge.X0 + (y - edge.Y0) * edge.Slope;
                    directions[i] = edge.Direction;
                }

                Array.Sort(xs, directions, 0, active.Count);

                var winding = 0;
                var spanStart = 0.0;
                for (var i = 0; i < active.Count; i++)
                {
                    var wasInside = Inside(winding, evenOdd);
                    winding += directions[i];
                    var isInside = Inside(winding, evenOdd);
                    if (!wasInside && isInside)
                    {
                        spanStart = xs[i];
                    }
                    else if (wasInside && !isInside)
                    {
                        AddSpan(coverage, rowStart, width, spanStart, xs[i], weight);
                    }
                }
            }
        }

        return coverage;
    }

    static bool Inside(int winding, bool evenOdd)
    {
        if (evenOdd)
        {
            return (winding & 1) != 0;
        }

        return winding != 0;
    }

    static void AddSpan(double[] coverage, int rowStart, int width, double from, double to, double weight)
    {
        from = Math.Max(0, from);
        to = Math.Min(width, to);
        if (to <= from)
        {
            return;
        }

        var first = (int)Math.Floor(from);
        var last = (int)Math.Floor(to);
        if (first == last)
        {
            coverage[rowStart + first] += (to - from) * weight;
            return;
        }

        coverage[rowStart + first] += (first + 1 - from) * weight;
        for (var x = first + 1; x < last; x++)
        {
            coverage[rowStart + x] += weight;
        }

        if (last < width)
        {
            coverage[rowStart + last] += (to - last) * weight;
        }
    }

    // Each polygon is a closed ring. Edges run top to bottom; the direction keeps the original winding.
    static List<Edge> Edges(List<List<Vec>> polygons, int height)
    {
        var edges = new List<Edge>();
        foreach (var polygon in polygons)
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                if (a.Y == b.Y)
                {
                    continue;
                }

                Edge edge;
                if (a.Y < b.Y)
                {
                    edge = new(a.X, a.Y, b.X, b.Y, 1);
                }
                else
                {
                    edge = new(b.X, b.Y, a.X, a.Y, -1);
                }

                if (edge.Y1 <= 0 ||
                    edge.Y0 >= height)
                {
                    continue;
                }

                edges.Add(edge);
            }
        }

        return edges;
    }
}

/// <summary>
/// A square RGBA canvas, kept premultiplied in doubles, that shapes are painted onto in document order with
/// source-over compositing.
/// </summary>
sealed class Canvas(int width, int height)
{
    readonly double[] pixels = new double[width * height * 4];

    public int Width { get; } = width;
    public int Height { get; } = height;

    public void Paint(double[] coverage, Rgba color, double opacity)
    {
        var alpha = color.A / 255.0 * opacity;
        if (alpha <= 0)
        {
            return;
        }

        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        for (var i = 0; i < coverage.Length; i++)
        {
            var c = coverage[i];
            if (c <= 0)
            {
                continue;
            }

            if (c > 1)
            {
                c = 1;
            }

            var source = alpha * c;
            var keep = 1 - source;
            var p = i * 4;
            pixels[p] = r * source + pixels[p] * keep;
            pixels[p + 1] = g * source + pixels[p + 1] * keep;
            pixels[p + 2] = b * source + pixels[p + 2] * keep;
            pixels[p + 3] = source + pixels[p + 3] * keep;
        }
    }

    /// <summary>The pixels as straight-alpha RGBA bytes, row by row from the top.</summary>
    public byte[] ToRgba()
    {
        var result = new byte[pixels.Length];
        for (var p = 0; p < pixels.Length; p += 4)
        {
            var alpha = pixels[p + 3];
            var a = ToByte(alpha);
            if (a == 0)
            {
                continue;
            }

            result[p] = ToByte(pixels[p] / alpha);
            result[p + 1] = ToByte(pixels[p + 1] / alpha);
            result[p + 2] = ToByte(pixels[p + 2] / alpha);
            result[p + 3] = a;
        }

        return result;
    }

    static byte ToByte(double value) =>
        (byte)Math.Max(0, Math.Min(255, (int)(value * 255 + 0.5)));
}
