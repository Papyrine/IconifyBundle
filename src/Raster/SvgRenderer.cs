using System.Globalization;
using System.Xml.Linq;

namespace IconifyBundle.Raster;

/// <summary>The rendered pixels of one icon at one size.</summary>
sealed class RasterImage(int width, int height, byte[] rgba, IReadOnlyCollection<string> unsupported)
{
    public int Width { get; } = width;
    public int Height { get; } = height;

    /// <summary>Straight-alpha RGBA, row by row from the top.</summary>
    public byte[] Rgba { get; } = rgba;

    /// <summary>The SVG features in the icon that were ignored or approximated.</summary>
    public IReadOnlyCollection<string> Unsupported { get; } = unsupported;
}

/// <summary>
/// Renders an Iconify icon body (the markup inside <c>&lt;svg&gt;</c>) to pixels. It covers what monochrome
/// icon sets are made of: <c>path</c>, the basic shapes, groups, <c>use</c>, fills (nonzero and even-odd),
/// strokes (caps, joins, miter limit), opacity, transforms, and <c>currentColor</c>. Masks, clip paths,
/// filters, dashes, animation and text are not rendered, and gradients are flattened to their average
/// colour; each is reported in <see cref="RasterImage.Unsupported"/> so the build can warn.
/// </summary>
sealed class SvgRenderer
{
    const string svgNamespace = "http://www.w3.org/2000/svg";
    const string xlinkNamespace = "http://www.w3.org/1999/xlink";

    // How far (in output pixels) a flattened curve may stray from the true outline.
    const double tolerance = 0.05;

    readonly Dictionary<string, XElement> ids = new(StringComparer.Ordinal);
    readonly SortedSet<string> unsupported = new(StringComparer.Ordinal);
    readonly Canvas? canvas;
    readonly Matrix viewport;
    readonly double viewWidth;
    readonly double viewHeight;

    SvgRenderer(Canvas? canvas, Matrix viewport, double viewWidth, double viewHeight)
    {
        this.canvas = canvas;
        this.viewport = viewport;
        this.viewWidth = viewWidth;
        this.viewHeight = viewHeight;
    }

    /// <summary>
    /// Renders the icon into a <paramref name="size"/> pixel square. The icon's <c>0 0 width height</c>
    /// view box is scaled to fit and centred, as an <c>&lt;svg&gt;</c> with equal width and height (and the
    /// default <c>preserveAspectRatio</c>) would show it.
    /// </summary>
    public static RasterImage Render(string body, double width, double height, int size, Rgba currentColor)
    {
        var scale = Math.Min(size / width, size / height);
        var viewport = new Matrix(scale, 0, 0, scale, (size - width * scale) / 2, (size - height * scale) / 2);
        var canvas = new Canvas(size, size);
        var renderer = new SvgRenderer(canvas, viewport, width, height);
        renderer.Run(body, currentColor);
        return new(size, size, canvas.ToRgba(), renderer.unsupported.ToList());
    }

    /// <summary>The SVG features in <paramref name="body"/> that rendering would ignore or approximate.</summary>
    public static IReadOnlyCollection<string> Analyse(string body)
    {
        var renderer = new SvgRenderer(null, Matrix.Identity, 24, 24);
        renderer.Run(body, new(0, 0, 0, 255));
        return renderer.unsupported.ToList();
    }

    void Run(string body, Rgba currentColor)
    {
        var root = XElement.Parse($"<svg xmlns=\"{svgNamespace}\" xmlns:xlink=\"{xlinkNamespace}\">{body}</svg>");
        foreach (var element in root.Descendants())
        {
            if (element.Attribute("id") is { } id &&
                !ids.ContainsKey(id.Value))
            {
                ids[id.Value] = element;
            }
        }

        var style = new Style
        {
            Color = currentColor
        };
        foreach (var child in root.Elements())
        {
            Walk(child, style, 0);
        }
    }

    void Walk(XElement element, Style parent, int depth)
    {
        if (depth > 32)
        {
            return;
        }

        var name = element.Name.LocalName;
        switch (name)
        {
            case "animate":
            case "animateColor":
            case "animateMotion":
            case "animateTransform":
            case "set":
                unsupported.Add("animation");
                return;
            // Definitions only render where they are referenced.
            case "defs":
            case "linearGradient":
            case "radialGradient":
            case "stop":
            case "clipPath":
            case "mask":
            case "filter":
            case "pattern":
            case "marker":
            case "symbol":
            case "title":
            case "desc":
            case "metadata":
            case "script":
                return;
            case "style":
                unsupported.Add("style element");
                return;
        }

        var declarations = Declarations(element);
        if (Property(element, declarations, "display") == "none")
        {
            return;
        }

        var style = Derive(element, declarations, parent);
        switch (name)
        {
            case "g":
            case "svg":
            case "a":
            case "switch":
                foreach (var child in element.Elements())
                {
                    Walk(child, style, depth + 1);
                }

                return;
            case "use":
                Use(element, style, depth);
                return;
        }

        var geometry = Shape(element, name);
        if (geometry == null)
        {
            unsupported.Add(name);
            return;
        }

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName.StartsWith("animate", StringComparison.Ordinal) ||
                child.Name.LocalName == "set")
            {
                unsupported.Add("animation");
            }
        }

        Draw(geometry, style);
    }

    void Use(XElement element, Style style, int depth)
    {
        var href = (string?)element.Attribute("href") ?? (string?)element.Attribute(XName.Get("href", xlinkNamespace));
        if (href == null ||
            href.Length == 0 ||
            href[0] != '#' ||
            !ids.TryGetValue(href.Substring(1), out var target))
        {
            return;
        }

        var x = Length(element, "x", viewWidth);
        var y = Length(element, "y", viewHeight);
        var placed = style.Clone();
        placed.Transform = style.Transform * Matrix.Translate(x, y);

        if (target.Name.LocalName == "symbol")
        {
            if (target.Attribute("viewBox") != null)
            {
                unsupported.Add("symbol");
            }

            var symbolStyle = Derive(target, Declarations(target), placed);
            foreach (var child in target.Elements())
            {
                Walk(child, symbolStyle, depth + 1);
            }

            return;
        }

        Walk(target, placed, depth + 1);
    }

    PathGeometry? Shape(XElement element, string name)
    {
        var path = new PathGeometry();
        var diagonal = Math.Sqrt((viewWidth * viewWidth + viewHeight * viewHeight) / 2);
        switch (name)
        {
            case "path":
                if ((string?)element.Attribute("d") is { } data)
                {
                    PathData.Parse(data, path);
                }

                return path;
            case "rect":
            {
                var width = Length(element, "width", viewWidth);
                var height = Length(element, "height", viewHeight);
                if (width <= 0 ||
                    height <= 0)
                {
                    return path;
                }

                var rx = OptionalLength(element, "rx", viewWidth);
                var ry = OptionalLength(element, "ry", viewHeight);
                rx ??= ry;
                ry ??= rx;
                path.Rect(
                    Length(element, "x", viewWidth),
                    Length(element, "y", viewHeight),
                    width,
                    height,
                    Math.Min(rx ?? 0, width / 2),
                    Math.Min(ry ?? 0, height / 2));
                return path;
            }
            case "circle":
            {
                var r = Length(element, "r", diagonal);
                if (r > 0)
                {
                    path.Ellipse(Length(element, "cx", viewWidth), Length(element, "cy", viewHeight), r, r);
                }

                return path;
            }
            case "ellipse":
            {
                var rx = OptionalLength(element, "rx", viewWidth);
                var ry = OptionalLength(element, "ry", viewHeight);
                rx ??= ry;
                ry ??= rx;
                if (rx > 0 &&
                    ry > 0)
                {
                    path.Ellipse(Length(element, "cx", viewWidth), Length(element, "cy", viewHeight), rx.Value, ry.Value);
                }

                return path;
            }
            case "line":
                path.MoveTo(new(Length(element, "x1", viewWidth), Length(element, "y1", viewHeight)));
                path.LineTo(new(Length(element, "x2", viewWidth), Length(element, "y2", viewHeight)));
                return path;
            case "polyline":
            case "polygon":
            {
                var scanner = new NumberScanner((string?)element.Attribute("points") ?? "");
                var first = true;
                while (scanner.TryPair(out var point))
                {
                    if (first)
                    {
                        path.MoveTo(point);
                        first = false;
                    }
                    else
                    {
                        path.LineTo(point);
                    }
                }

                if (name == "polygon" &&
                    !first)
                {
                    path.Close();
                }

                return path;
            }
        }

        return null;
    }

    void Draw(PathGeometry geometry, Style style)
    {
        if (canvas == null ||
            !style.Visible ||
            geometry.Figures.Count == 0)
        {
            return;
        }

        var matrix = viewport * style.Transform;
        var scale = matrix.MaxScale;
        if (scale <= 0)
        {
            return;
        }

        // Flatten in user space, with the tolerance scaled so it holds in output pixels.
        var flatness = tolerance / scale;
        var figures = geometry.Figures
            .Select(_ => (Figure: _, Points: PathGeometry.Flatten(_, flatness)))
            .ToList();

        if (Resolve(style.Fill, style) is { } fill)
        {
            var polygons = new List<List<Vec>>();
            foreach (var (_, points) in figures)
            {
                if (points.Count > 2)
                {
                    polygons.Add(points.Select(matrix.Apply).ToList());
                }
            }

            canvas.Paint(Scanline.Coverage(polygons, style.EvenOdd, canvas.Width, canvas.Height), fill, style.FillOpacity * style.Opacity);
        }

        if (style.StrokeWidth > 0 &&
            Resolve(style.Stroke, style) is { } stroke)
        {
            var strokeStyle = new StrokeStyle(style.StrokeWidth, style.Cap, style.Join, style.MiterLimit);
            var pieces = new List<List<Vec>>();
            foreach (var (figure, points) in figures)
            {
                Stroker.Stroke(points, figure.Closed, figure.Drawn, strokeStyle, flatness, pieces);
            }

            var transformed = pieces
                .Select(_ => _.Select(matrix.Apply).ToList())
                .ToList();
            canvas.Paint(Scanline.Coverage(transformed, false, canvas.Width, canvas.Height), stroke, style.StrokeOpacity * style.Opacity);
        }
    }

    static Rgba? Resolve(Paint paint, Style style)
    {
        switch (paint.Kind)
        {
            case PaintKind.Color:
                return paint.Color;
            case PaintKind.CurrentColor:
                return style.Color;
        }

        return null;
    }

    Style Derive(XElement element, Dictionary<string, string> declarations, Style parent)
    {
        var style = parent.Clone();

        string? Get(string name) =>
            Property(element, declarations, name);

        // 'color' first: currentColor in this element's own fill/stroke means this element's colour.
        if (Get("color") is { } colorValue &&
            SvgColor.TryParse(colorValue, out var color))
        {
            style.Color = color;
        }

        if (Get("fill") is { } fill)
        {
            style.Fill = ParsePaint(fill, style.Fill);
        }

        if (Get("stroke") is { } stroke)
        {
            style.Stroke = ParsePaint(stroke, style.Stroke);
        }

        if (Get("fill-opacity") is { } fillOpacity)
        {
            style.FillOpacity = Opacity(fillOpacity, style.FillOpacity);
        }

        if (Get("stroke-opacity") is { } strokeOpacity)
        {
            style.StrokeOpacity = Opacity(strokeOpacity, style.StrokeOpacity);
        }

        // Group opacity is applied to each shape, which matches the SVG unless shapes in the group overlap.
        if (Get("opacity") is { } opacity)
        {
            style.Opacity *= Opacity(opacity, 1);
        }

        switch (Get("fill-rule"))
        {
            case "evenodd":
                style.EvenOdd = true;
                break;
            case "nonzero":
                style.EvenOdd = false;
                break;
        }

        if (Get("stroke-width") is { } strokeWidth &&
            TryLength(strokeWidth, Math.Sqrt((viewWidth * viewWidth + viewHeight * viewHeight) / 2), out var width) &&
            width >= 0)
        {
            style.StrokeWidth = width;
        }

        switch (Get("stroke-linecap"))
        {
            case "butt":
                style.Cap = LineCap.Butt;
                break;
            case "round":
                style.Cap = LineCap.Round;
                break;
            case "square":
                style.Cap = LineCap.Square;
                break;
        }

        switch (Get("stroke-linejoin"))
        {
            case "miter":
            case "miter-clip":
            case "arcs":
                style.Join = LineJoin.Miter;
                break;
            case "round":
                style.Join = LineJoin.Round;
                break;
            case "bevel":
                style.Join = LineJoin.Bevel;
                break;
        }

        if (Get("stroke-miterlimit") is { } miterLimit &&
            double.TryParse(miterLimit, NumberStyles.Float, CultureInfo.InvariantCulture, out var limit) &&
            limit >= 1)
        {
            style.MiterLimit = limit;
        }

        switch (Get("visibility"))
        {
            case "hidden":
            case "collapse":
                style.Visible = false;
                break;
            case "visible":
                style.Visible = true;
                break;
        }

        if ((string?)element.Attribute("transform") is { } transform)
        {
            style.Transform = parent.Transform * ParseTransform(transform);
        }

        foreach (var feature in ignoredProperties)
        {
            if (Get(feature) is { } value &&
                value != "none")
            {
                unsupported.Add(feature);
            }
        }

        return style;
    }

    static readonly string[] ignoredProperties =
    [
        "clip-path",
        "mask",
        "filter",
        "stroke-dasharray",
        "marker-start",
        "marker-mid",
        "marker-end"
    ];

    Paint ParsePaint(string value, Paint inherited)
    {
        value = value.Trim();
        switch (value)
        {
            case "inherit":
                return inherited;
            case "none":
                return Paint.None;
        }

        if (string.Equals(value, "currentColor", StringComparison.OrdinalIgnoreCase))
        {
            return Paint.Current;
        }

        if (value.StartsWith("url(", StringComparison.Ordinal))
        {
            return PaintServer(value, inherited);
        }

        if (SvgColor.TryParse(value, out var color))
        {
            return Paint.Of(color);
        }

        return inherited;
    }

    // url(#id) [fallback]: a gradient is flattened to the average of its stops; anything else uses the
    // fallback colour when one is given.
    Paint PaintServer(string value, Paint inherited)
    {
        var close = value.IndexOf(')');
        if (close < 0)
        {
            return inherited;
        }

        var reference = value.Substring(4, close - 4).Trim().Trim('\'', '"');
        var fallback = value.Substring(close + 1).Trim();
        if (reference.Length > 0 &&
            reference[0] == '#' &&
            ids.TryGetValue(reference.Substring(1), out var server) &&
            server.Name.LocalName is "linearGradient" or "radialGradient")
        {
            unsupported.Add("gradient");
            if (AverageStop(server) is { } average)
            {
                return Paint.Of(average);
            }

            return Paint.None;
        }

        unsupported.Add("paint server");
        if (fallback.Length > 0)
        {
            return ParsePaint(fallback, inherited);
        }

        return Paint.None;
    }

    Rgba? AverageStop(XElement gradient)
    {
        // Stops may be inherited from the gradient this one references.
        var current = gradient;
        for (var hops = 0; current != null && hops < 8; hops++)
        {
            var stops = current.Elements().Where(_ => _.Name.LocalName == "stop").ToList();
            if (stops.Count > 0)
            {
                double r = 0, g = 0, b = 0, a = 0;
                foreach (var stop in stops)
                {
                    var declarations = Declarations(stop);
                    var color = new Rgba(0, 0, 0, 255);
                    if (Property(stop, declarations, "stop-color") is { } stopColor &&
                        SvgColor.TryParse(stopColor, out var parsed))
                    {
                        color = parsed;
                    }

                    var opacity = 1.0;
                    if (Property(stop, declarations, "stop-opacity") is { } stopOpacity)
                    {
                        opacity = Opacity(stopOpacity, 1);
                    }

                    r += color.R;
                    g += color.G;
                    b += color.B;
                    a += color.A * opacity;
                }

                var count = stops.Count;
                return new((byte)Math.Round(r / count), (byte)Math.Round(g / count), (byte)Math.Round(b / count), (byte)Math.Round(a / count));
            }

            var href = (string?)current.Attribute("href") ?? (string?)current.Attribute(XName.Get("href", xlinkNamespace));
            current = null;
            if (href != null &&
                href.Length > 0 &&
                href[0] == '#' &&
                ids.TryGetValue(href.Substring(1), out var next))
            {
                current = next;
            }
        }

        return null;
    }

    static Matrix ParseTransform(string value)
    {
        var result = Matrix.Identity;
        var index = 0;
        while (true)
        {
            while (index < value.Length &&
                   (char.IsWhiteSpace(value[index]) || value[index] == ','))
            {
                index++;
            }

            if (index >= value.Length)
            {
                return result;
            }

            var open = value.IndexOf('(', index);
            var close = value.IndexOf(')', index);
            if (open < 0 ||
                close < open)
            {
                // An invalid transform is ignored as a whole.
                return Matrix.Identity;
            }

            var name = value.Substring(index, open - index).Trim();
            var scanner = new NumberScanner(value.Substring(open + 1, close - open - 1));
            var args = new List<double>();
            while (scanner.TryNumber(out var number))
            {
                args.Add(number);
            }

            index = close + 1;
            Matrix step;
            switch (name)
            {
                case "matrix" when args.Count == 6:
                    step = new(args[0], args[1], args[2], args[3], args[4], args[5]);
                    break;
                case "translate" when args.Count is 1 or 2:
                    step = Matrix.Translate(args[0], args.Count == 2 ? args[1] : 0);
                    break;
                case "scale" when args.Count is 1 or 2:
                    step = Matrix.Scale(args[0], args.Count == 2 ? args[1] : args[0]);
                    break;
                case "rotate" when args.Count == 1:
                    step = Matrix.Rotate(args[0]);
                    break;
                case "rotate" when args.Count == 3:
                    step = Matrix.Translate(args[1], args[2]) * Matrix.Rotate(args[0]) * Matrix.Translate(-args[1], -args[2]);
                    break;
                case "skewX" when args.Count == 1:
                    step = new(1, 0, Math.Tan(args[0] * Math.PI / 180), 1, 0, 0);
                    break;
                case "skewY" when args.Count == 1:
                    step = new(1, Math.Tan(args[0] * Math.PI / 180), 0, 1, 0, 0);
                    break;
                default:
                    return Matrix.Identity;
            }

            result *= step;
        }
    }

    // Presentation attributes, overridden by declarations in the style attribute.
    static string? Property(XElement element, Dictionary<string, string> declarations, string name)
    {
        if (declarations.TryGetValue(name, out var value))
        {
            return value;
        }

        return ((string?)element.Attribute(name))?.Trim();
    }

    static readonly Dictionary<string, string> noDeclarations = new(StringComparer.Ordinal);

    static Dictionary<string, string> Declarations(XElement element)
    {
        var style = (string?)element.Attribute("style");
        if (string.IsNullOrWhiteSpace(style))
        {
            return noDeclarations;
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var declaration in style!.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var value = declaration.Substring(colon + 1).Replace("!important", "").Trim();
            result[declaration.Substring(0, colon).Trim().ToLowerInvariant()] = value;
        }

        return result;
    }

    static double Opacity(string value, double fallback)
    {
        value = value.Trim();
        var percent = value.Length > 0 && value[value.Length - 1] == '%';
        if (percent)
        {
            value = value.Substring(0, value.Length - 1);
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return fallback;
        }

        if (percent)
        {
            number /= 100;
        }

        return Math.Max(0, Math.Min(1, number));
    }

    static double Length(XElement element, string name, double percentOf) =>
        OptionalLength(element, name, percentOf) ?? 0;

    static double? OptionalLength(XElement element, string name, double percentOf)
    {
        if ((string?)element.Attribute(name) is { } value &&
            TryLength(value, percentOf, out var length))
        {
            return length;
        }

        return null;
    }

    // A number with an optional CSS unit, in user units (px).
    static bool TryLength(string value, double percentOf, out double length)
    {
        value = value.Trim();
        var unitStart = value.Length;
        while (unitStart > 0 &&
               (char.IsLetter(value[unitStart - 1]) || value[unitStart - 1] == '%'))
        {
            unitStart--;
        }

        var unit = value.Substring(unitStart).ToLowerInvariant();
        // netstandard2.0 (the generator and the build task) has no span overload.
#pragma warning disable CA1846
        if (!double.TryParse(value.Substring(0, unitStart), NumberStyles.Float, CultureInfo.InvariantCulture, out length))
#pragma warning restore CA1846
        {
            return false;
        }

        switch (unit)
        {
            case "":
            case "px":
                return true;
            case "%":
                length = length * percentOf / 100;
                return true;
            case "pt":
                length *= 96.0 / 72;
                return true;
            case "pc":
                length *= 16;
                return true;
            case "in":
                length *= 96;
                return true;
            case "cm":
                length *= 96 / 2.54;
                return true;
            case "mm":
                length *= 96 / 25.4;
                return true;
            case "em":
                length *= 16;
                return true;
            case "ex":
                length *= 8;
                return true;
        }

        return false;
    }

    enum PaintKind
    {
        None,
        Color,
        CurrentColor
    }

    readonly struct Paint(PaintKind kind, Rgba color)
    {
        public static readonly Paint None = new(PaintKind.None, default);
        public static readonly Paint Current = new(PaintKind.CurrentColor, default);

        public PaintKind Kind { get; } = kind;
        public Rgba Color { get; } = color;

        public static Paint Of(Rgba color) => new(PaintKind.Color, color);
    }

    // The inherited painting state. Opacity is the product of the ancestors' opacities.
    sealed class Style
    {
        public Paint Fill = Paint.Of(new(0, 0, 0, 255));
        public double FillOpacity = 1;
        public bool EvenOdd;
        public Paint Stroke = Paint.None;
        public double StrokeOpacity = 1;
        public double StrokeWidth = 1;
        public LineCap Cap = LineCap.Butt;
        public LineJoin Join = LineJoin.Miter;
        public double MiterLimit = 4;
        public Rgba Color;
        public double Opacity = 1;
        public bool Visible = true;
        public Matrix Transform = Matrix.Identity;

        public Style Clone() => (Style)MemberwiseClone();
    }
}
