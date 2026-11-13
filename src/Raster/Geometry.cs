namespace IconifyBundle.Raster;

readonly struct Vec(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;

    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec operator *(Vec a, double scale) => new(a.X * scale, a.Y * scale);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public static double Cross(Vec a, Vec b) => a.X * b.Y - a.Y * b.X;

    public static double Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y;
}

/// <summary>
/// An SVG affine transform <c>[a c e; b d f]</c>: <c>x' = a x + c y + e</c>, <c>y' = b x + d y + f</c>.
/// </summary>
readonly struct Matrix(double a, double b, double c, double d, double e, double f)
{
    public static readonly Matrix Identity = new(1, 0, 0, 1, 0, 0);

    public double A { get; } = a;
    public double B { get; } = b;
    public double C { get; } = c;
    public double D { get; } = d;
    public double E { get; } = e;
    public double F { get; } = f;

    public Vec Apply(Vec p) =>
        new(A * p.X + C * p.Y + E, B * p.X + D * p.Y + F);

    /// <summary>The transform that applies <paramref name="inner"/> first, then <paramref name="outer"/>.</summary>
    public static Matrix operator *(Matrix outer, Matrix inner) =>
        new(
            outer.A * inner.A + outer.C * inner.B,
            outer.B * inner.A + outer.D * inner.B,
            outer.A * inner.C + outer.C * inner.D,
            outer.B * inner.C + outer.D * inner.D,
            outer.A * inner.E + outer.C * inner.F + outer.E,
            outer.B * inner.E + outer.D * inner.F + outer.F);

    public static Matrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);

    public static Matrix Scale(double x, double y) => new(x, 0, 0, y, 0, 0);

    public static Matrix Rotate(double degrees)
    {
        var radians = degrees * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new(cos, sin, -sin, cos, 0, 0);
    }

    /// <summary>
    /// The largest factor a unit length is stretched by (an upper bound across directions), used to turn a
    /// device-pixel tolerance into user units.
    /// </summary>
    public double MaxScale =>
        Math.Max(Math.Sqrt(A * A + B * B), Math.Sqrt(C * C + D * D));
}

enum SegmentKind
{
    Line,
    Cubic
}

readonly struct Segment(SegmentKind kind, Vec c1, Vec c2, Vec to)
{
    public SegmentKind Kind { get; } = kind;
    public Vec C1 { get; } = c1;
    public Vec C2 { get; } = c2;
    public Vec To { get; } = to;
}

/// <summary>One subpath: a start point, its segments, and whether it was closed.</summary>
sealed class PathFigure(Vec start)
{
    public Vec Start { get; } = start;
    public List<Segment> Segments { get; } = [];
    public bool Closed { get; set; }

    /// <summary>
    /// Whether a drawing command (or a close) followed the moveto. A lone moveto draws nothing, but a
    /// zero-length subpath such as <c>M5 5z</c> still gets round or square caps.
    /// </summary>
    public bool Drawn => Closed || Segments.Count > 0;
}

/// <summary>
/// A path made of lines and cubic Béziers (quadratics and arcs are converted on the way in), built by the
/// path-data parser and by the basic shapes.
/// </summary>
sealed class PathGeometry
{
    // 4/3 * (sqrt(2) - 1): the cubic control distance that best approximates a quarter circle.
    public const double Kappa = 0.55228474983079356;

    PathFigure? current;

    public List<PathFigure> Figures { get; } = [];

    public Vec Current { get; private set; }

    public void MoveTo(Vec point)
    {
        current = new(point);
        Figures.Add(current);
        Current = point;
    }

    public void LineTo(Vec point)
    {
        Figure().Segments.Add(new(SegmentKind.Line, point, point, point));
        Current = point;
    }

    public void CubicTo(Vec c1, Vec c2, Vec point)
    {
        Figure().Segments.Add(new(SegmentKind.Cubic, c1, c2, point));
        Current = point;
    }

    public void QuadTo(Vec control, Vec point)
    {
        var start = Current;
        CubicTo(
            start + (control - start) * (2.0 / 3),
            point + (control - point) * (2.0 / 3),
            point);
    }

    public void Close()
    {
        if (current == null)
        {
            return;
        }

        current.Closed = true;
        Current = current.Start;
    }

    // A drawing command after a close (or before any moveto) starts a new subpath at the current point.
    PathFigure Figure()
    {
        if (current == null ||
            current.Closed)
        {
            current = new(Current);
            Figures.Add(current);
        }

        return current;
    }

    /// <summary>
    /// An elliptical arc, converted to cubic Béziers of at most 90 degrees each, following the SVG
    /// implementation notes (endpoint to centre parameterisation, with out-of-range radii scaled up).
    /// </summary>
    public void ArcTo(double rx, double ry, double rotation, bool largeArc, bool sweep, Vec to)
    {
        var from = Current;
        rx = Math.Abs(rx);
        ry = Math.Abs(ry);
        if (from.X == to.X &&
            from.Y == to.Y)
        {
            return;
        }

        if (rx == 0 ||
            ry == 0)
        {
            LineTo(to);
            return;
        }

        var phi = rotation * Math.PI / 180;
        var cos = Math.Cos(phi);
        var sin = Math.Sin(phi);
        var dx = (from.X - to.X) / 2;
        var dy = (from.Y - to.Y) / 2;
        var x1 = cos * dx + sin * dy;
        var y1 = -sin * dx + cos * dy;

        var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
        if (lambda > 1)
        {
            var root = Math.Sqrt(lambda);
            rx *= root;
            ry *= root;
        }

        var numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
        var denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1;
        var coefficient = Math.Sqrt(Math.Max(0, numerator / denominator));
        if (largeArc == sweep)
        {
            coefficient = -coefficient;
        }

        var cx1 = coefficient * rx * y1 / ry;
        var cy1 = -coefficient * ry * x1 / rx;
        var cx = cos * cx1 - sin * cy1 + (from.X + to.X) / 2;
        var cy = sin * cx1 + cos * cy1 + (from.Y + to.Y) / 2;

        var ux = (x1 - cx1) / rx;
        var uy = (y1 - cy1) / ry;
        var vx = (-x1 - cx1) / rx;
        var vy = (-y1 - cy1) / ry;
        var start = Math.Atan2(uy, ux);
        var delta = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
        if (!sweep &&
            delta > 0)
        {
            delta -= 2 * Math.PI;
        }
        else if (sweep &&
                 delta < 0)
        {
            delta += 2 * Math.PI;
        }

        var count = (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2) - 1e-9);
        if (count < 1)
        {
            count = 1;
        }

        var step = delta / count;
        var k = 4.0 / 3 * Math.Tan(step / 4);

        Vec Map(double x, double y) =>
            new(cx + rx * cos * x - ry * sin * y, cy + rx * sin * x + ry * cos * y);

        var angle = start;
        for (var i = 0; i < count; i++)
        {
            var next = angle + step;
            var cosA = Math.Cos(angle);
            var sinA = Math.Sin(angle);
            var cosB = Math.Cos(next);
            var sinB = Math.Sin(next);
            var end = to;
            if (i < count - 1)
            {
                end = Map(cosB, sinB);
            }

            CubicTo(
                Map(cosA - k * sinA, sinA + k * cosA),
                Map(cosB + k * sinB, sinB - k * cosB),
                end);
            angle = next;
        }
    }

    public void Ellipse(double cx, double cy, double rx, double ry)
    {
        var kx = rx * Kappa;
        var ky = ry * Kappa;
        MoveTo(new(cx + rx, cy));
        CubicTo(new(cx + rx, cy + ky), new(cx + kx, cy + ry), new(cx, cy + ry));
        CubicTo(new(cx - kx, cy + ry), new(cx - rx, cy + ky), new(cx - rx, cy));
        CubicTo(new(cx - rx, cy - ky), new(cx - kx, cy - ry), new(cx, cy - ry));
        CubicTo(new(cx + kx, cy - ry), new(cx + rx, cy - ky), new(cx + rx, cy));
        Close();
    }

    public void Rect(double x, double y, double width, double height, double rx, double ry)
    {
        if (rx <= 0 ||
            ry <= 0)
        {
            MoveTo(new(x, y));
            LineTo(new(x + width, y));
            LineTo(new(x + width, y + height));
            LineTo(new(x, y + height));
            Close();
            return;
        }

        var kx = rx * Kappa;
        var ky = ry * Kappa;
        var right = x + width;
        var bottom = y + height;
        MoveTo(new(x + rx, y));
        LineTo(new(right - rx, y));
        CubicTo(new(right - rx + kx, y), new(right, y + ry - ky), new(right, y + ry));
        LineTo(new(right, bottom - ry));
        CubicTo(new(right, bottom - ry + ky), new(right - rx + kx, bottom), new(right - rx, bottom));
        LineTo(new(x + rx, bottom));
        CubicTo(new(x + rx - kx, bottom), new(x, bottom - ry + ky), new(x, bottom - ry));
        LineTo(new(x, y + ry));
        CubicTo(new(x, y + ry - ky), new(x + rx - kx, y), new(x + rx, y));
        Close();
    }

    /// <summary>
    /// The figure as a polyline (start point first), with each curve split into enough straight pieces
    /// that none strays more than <paramref name="tolerance"/> from it.
    /// </summary>
    public static List<Vec> Flatten(PathFigure figure, double tolerance)
    {
        var points = new List<Vec>
        {
            figure.Start
        };
        var from = figure.Start;
        foreach (var segment in figure.Segments)
        {
            if (segment.Kind == SegmentKind.Line)
            {
                points.Add(segment.To);
                from = segment.To;
                continue;
            }

            // The flattening error of n equal steps is at most 3/4 * max|second difference| / n^2.
            var dd = Math.Max(
                (from - segment.C1 * 2 + segment.C2).Length,
                (segment.C1 - segment.C2 * 2 + segment.To).Length);
            var count = (int)Math.Ceiling(Math.Sqrt(0.75 * dd / tolerance));
            count = Math.Max(1, Math.Min(count, 1024));
            for (var i = 1; i < count; i++)
            {
                var t = (double)i / count;
                var mt = 1 - t;
                points.Add(
                    from * (mt * mt * mt) +
                    segment.C1 * (3 * mt * mt * t) +
                    segment.C2 * (3 * mt * t * t) +
                    segment.To * (t * t * t));
            }

            points.Add(segment.To);
            from = segment.To;
        }

        return points;
    }
}
