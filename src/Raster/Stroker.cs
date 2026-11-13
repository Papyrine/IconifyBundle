namespace IconifyBundle.Raster;

enum LineCap
{
    Butt,
    Round,
    Square
}

enum LineJoin
{
    Miter,
    Round,
    Bevel
}

sealed class StrokeStyle(double width, LineCap cap, LineJoin join, double miterLimit)
{
    public double HalfWidth { get; } = width / 2;
    public LineCap Cap { get; } = cap;
    public LineJoin Join { get; } = join;
    public double MiterLimit { get; } = miterLimit;
}

/// <summary>
/// Turns a stroked polyline into convex pieces whose union is the stroke: a quad per segment, a wedge per
/// join, and a shape per cap. Every piece is wound the same way, so filling them together with the nonzero
/// rule paints their union with no seams or double coverage.
/// </summary>
static class Stroker
{
    public static void Stroke(
        List<Vec> input,
        bool closed,
        bool drawn,
        StrokeStyle style,
        double tolerance,
        List<List<Vec>> pieces)
    {
        var points = Dedupe(input, closed);
        if (points.Count == 0)
        {
            return;
        }

        var radius = style.HalfWidth;
        var circleSegments = CircleSegments(radius, tolerance);
        if (points.Count == 1)
        {
            if (drawn)
            {
                Dot(points[0], style, circleSegments, pieces);
            }

            return;
        }

        var count = points.Count;
        var segments = count - 1;
        if (closed)
        {
            segments = count;
        }

        for (var i = 0; i < segments; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % count];
            var normal = Normal(b - a) * radius;
            Add(pieces, [a + normal, b + normal, b - normal, a - normal]);
        }

        if (closed)
        {
            for (var i = 0; i < count; i++)
            {
                Join(points[(i - 1 + count) % count], points[i], points[(i + 1) % count], style, circleSegments, pieces);
            }

            return;
        }

        for (var i = 1; i < count - 1; i++)
        {
            Join(points[i - 1], points[i], points[i + 1], style, circleSegments, pieces);
        }

        Cap(points[0], Direction(points[0] - points[1]), style, circleSegments, pieces);
        Cap(points[count - 1], Direction(points[count - 1] - points[count - 2]), style, circleSegments, pieces);
    }

    static List<Vec> Dedupe(List<Vec> input, bool closed)
    {
        const double epsilon = 1e-9;
        var points = new List<Vec>(input.Count);
        foreach (var point in input)
        {
            if (points.Count > 0 &&
                (point - points[points.Count - 1]).Length <= epsilon)
            {
                continue;
            }

            points.Add(point);
        }

        if (closed &&
            points.Count > 1 &&
            (points[points.Count - 1] - points[0]).Length <= epsilon)
        {
            points.RemoveAt(points.Count - 1);
        }

        return points;
    }

    // Segments for a full circle so that each chord strays less than the tolerance from the arc
    // (sagitta r(1 - cos(θ/2)) ≈ rθ²/8).
    static int CircleSegments(double radius, double tolerance) =>
        Math.Max(8, Math.Min(256, (int)Math.Ceiling(Math.PI * Math.Sqrt(radius / (2 * tolerance)))));

    static Vec Direction(Vec v)
    {
        var length = v.Length;
        return new(v.X / length, v.Y / length);
    }

    static Vec Normal(Vec v)
    {
        var direction = Direction(v);
        return new(-direction.Y, direction.X);
    }

    static void Join(Vec previous, Vec point, Vec next, StrokeStyle style, int circleSegments, List<List<Vec>> pieces)
    {
        var d1 = Direction(point - previous);
        var d2 = Direction(next - point);
        var cross = Vec.Cross(d1, d2);
        var dot = Vec.Dot(d1, d2);
        if (Math.Abs(cross) < 1e-12 &&
            dot > 0)
        {
            return;
        }

        var radius = style.HalfWidth;
        if (Math.Abs(cross) < 1e-12)
        {
            // The path doubles back on itself: only a round join adds anything.
            if (style.Join == LineJoin.Round)
            {
                Add(pieces, Circle(point, radius, circleSegments));
            }

            return;
        }

        // The outer side of the turn is the one away from the direction the path turns towards.
        var side = 1.0;
        if (cross > 0)
        {
            side = -1;
        }

        var n1 = new Vec(-d1.Y, d1.X) * side;
        var n2 = new Vec(-d2.Y, d2.X) * side;
        var o1 = point + n1 * radius;
        var o2 = point + n2 * radius;

        switch (style.Join)
        {
            case LineJoin.Round:
            {
                var start = Math.Atan2(n1.Y, n1.X);
                var sweep = Math.Atan2(n2.Y, n2.X) - start;
                if (sweep > Math.PI)
                {
                    sweep -= 2 * Math.PI;
                }
                else if (sweep < -Math.PI)
                {
                    sweep += 2 * Math.PI;
                }

                Add(pieces, Pie(point, radius, start, sweep, circleSegments));
                return;
            }
            case LineJoin.Miter:
            {
                // The miter length over the stroke width is 1/sin(θ/2) for the angle θ between the
                // segments, which is 1/cos(φ/2) for the turning angle φ.
                var cosHalf = Math.Sqrt((1 + dot) / 2);
                if (cosHalf * style.MiterLimit >= 1)
                {
                    var tip = point + Direction(n1 + n2) * (radius / cosHalf);
                    Add(pieces, [point, o1, tip, o2]);
                    return;
                }

                break;
            }
        }

        Add(pieces, [point, o1, o2]);
    }

    static void Cap(Vec point, Vec outward, StrokeStyle style, int circleSegments, List<List<Vec>> pieces)
    {
        var radius = style.HalfWidth;
        switch (style.Cap)
        {
            case LineCap.Round:
                Add(pieces, Circle(point, radius, circleSegments));
                return;
            case LineCap.Square:
            {
                var normal = new Vec(-outward.Y, outward.X) * radius;
                var extension = outward * radius;
                Add(pieces, [point + normal, point + normal + extension, point - normal + extension, point - normal]);
                return;
            }
        }
    }

    // A zero-length subpath: a dot with round caps, an axis-aligned square with square caps.
    static void Dot(Vec point, StrokeStyle style, int circleSegments, List<List<Vec>> pieces)
    {
        var radius = style.HalfWidth;
        switch (style.Cap)
        {
            case LineCap.Round:
                Add(pieces, Circle(point, radius, circleSegments));
                return;
            case LineCap.Square:
                Add(
                    pieces,
                    [
                        new(point.X - radius, point.Y - radius),
                        new(point.X + radius, point.Y - radius),
                        new(point.X + radius, point.Y + radius),
                        new(point.X - radius, point.Y + radius)
                    ]);
                return;
        }
    }

    static List<Vec> Circle(Vec center, double radius, int segments)
    {
        var points = new List<Vec>(segments);
        for (var i = 0; i < segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            points.Add(new(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)));
        }

        return points;
    }

    static List<Vec> Pie(Vec center, double radius, double start, double sweep, int circleSegments)
    {
        var steps = Math.Max(1, (int)Math.Ceiling(circleSegments * Math.Abs(sweep) / (2 * Math.PI)));
        var points = new List<Vec>(steps + 2)
        {
            center
        };
        for (var i = 0; i <= steps; i++)
        {
            var angle = start + sweep * i / steps;
            points.Add(new(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)));
        }

        return points;
    }

    // Adds a convex piece wound positively, dropping degenerate ones.
    public static void Add(List<List<Vec>> pieces, List<Vec> piece)
    {
        var area = SignedArea(piece);
        if (Math.Abs(area) < 1e-12)
        {
            return;
        }

        if (area < 0)
        {
            piece.Reverse();
        }

        pieces.Add(piece);
    }

    static double SignedArea(List<Vec> polygon)
    {
        var area = 0.0;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            area += a.X * b.Y - b.X * a.Y;
        }

        return area / 2;
    }
}
