using System.Globalization;

namespace IconifyBundle.Raster;

/// <summary>Reads SVG numbers and flags with the path-data grammar (e.g. <c>1.5.5</c> is 1.5 then .5).</summary>
sealed class NumberScanner(string text)
{
    int index;

    public bool AtEnd
    {
        get
        {
            SkipSeparators();
            return index >= text.Length;
        }
    }

    public char Peek => text[index];

    public void Advance() => index++;

    public void SkipSeparators()
    {
        while (index < text.Length &&
               (char.IsWhiteSpace(text[index]) || text[index] == ','))
        {
            index++;
        }
    }

    public bool TryNumber(out double value)
    {
        value = 0;
        SkipSeparators();
        var start = index;
        if (index < text.Length &&
            text[index] is '+' or '-')
        {
            index++;
        }

        var digits = SkipDigits();
        if (index < text.Length &&
            text[index] == '.')
        {
            index++;
            digits |= SkipDigits();
        }

        if (!digits)
        {
            index = start;
            return false;
        }

        if (index < text.Length &&
            text[index] is 'e' or 'E')
        {
            var mark = index;
            index++;
            if (index < text.Length &&
                text[index] is '+' or '-')
            {
                index++;
            }

            if (!SkipDigits())
            {
                // An 'e' with no digits after it is not part of the number.
                index = mark;
            }
        }

        // netstandard2.0 (the generator and the build task) has no span overload.
#pragma warning disable CA1846
        return double.TryParse(
            text.Substring(start, index - start),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
#pragma warning restore CA1846
    }

    public bool TryPair(out Vec point)
    {
        point = default;
        if (!TryNumber(out var x) ||
            !TryNumber(out var y))
        {
            return false;
        }

        point = new(x, y);
        return true;
    }

    // Arc flags are a single '0' or '1' and may run straight into the next number ("a1 1 0 00 1 1").
    public bool TryFlag(out bool flag)
    {
        flag = false;
        SkipSeparators();
        if (index >= text.Length ||
            text[index] is not ('0' or '1'))
        {
            return false;
        }

        flag = text[index] == '1';
        index++;
        return true;
    }

    bool SkipDigits()
    {
        var any = false;
        while (index < text.Length &&
               text[index] is >= '0' and <= '9')
        {
            index++;
            any = true;
        }

        return any;
    }
}

/// <summary>
/// Parses SVG path data (<c>d</c>) into a <see cref="PathGeometry"/>. Like a browser, it renders the path
/// up to the first error rather than rejecting it.
/// </summary>
static class PathData
{
    public static void Parse(string data, PathGeometry path)
    {
        var scanner = new NumberScanner(data);
        var command = '\0';
        Vec? lastCubic = null;
        Vec? lastQuad = null;
        while (!scanner.AtEnd)
        {
            var c = scanner.Peek;
            if (IsCommand(c))
            {
                command = c;
                scanner.Advance();
            }
            else if (command == '\0' ||
                     command is 'Z' or 'z')
            {
                return;
            }

            var relative = char.IsLower(command);
            var current = path.Current;

            Vec Point(Vec value)
            {
                if (relative)
                {
                    return current + value;
                }

                return value;
            }

            Vec? cubic = null;
            Vec? quad = null;
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                {
                    if (!scanner.TryPair(out var to))
                    {
                        return;
                    }

                    path.MoveTo(Point(to));
                    // Further coordinate pairs after a moveto are implicit linetos.
                    command = 'L';
                    if (relative)
                    {
                        command = 'l';
                    }

                    break;
                }
                case 'L':
                {
                    if (!scanner.TryPair(out var to))
                    {
                        return;
                    }

                    path.LineTo(Point(to));
                    break;
                }
                case 'H':
                {
                    if (!scanner.TryNumber(out var x))
                    {
                        return;
                    }

                    if (relative)
                    {
                        x += current.X;
                    }

                    path.LineTo(new(x, current.Y));
                    break;
                }
                case 'V':
                {
                    if (!scanner.TryNumber(out var y))
                    {
                        return;
                    }

                    if (relative)
                    {
                        y += current.Y;
                    }

                    path.LineTo(new(current.X, y));
                    break;
                }
                case 'C':
                {
                    if (!scanner.TryPair(out var c1) ||
                        !scanner.TryPair(out var c2) ||
                        !scanner.TryPair(out var to))
                    {
                        return;
                    }

                    cubic = Point(c2);
                    path.CubicTo(Point(c1), cubic.Value, Point(to));
                    break;
                }
                case 'S':
                {
                    if (!scanner.TryPair(out var c2) ||
                        !scanner.TryPair(out var to))
                    {
                        return;
                    }

                    var c1 = current;
                    if (lastCubic.HasValue)
                    {
                        c1 = current * 2 - lastCubic.Value;
                    }

                    cubic = Point(c2);
                    path.CubicTo(c1, cubic.Value, Point(to));
                    break;
                }
                case 'Q':
                {
                    if (!scanner.TryPair(out var control) ||
                        !scanner.TryPair(out var to))
                    {
                        return;
                    }

                    quad = Point(control);
                    path.QuadTo(quad.Value, Point(to));
                    break;
                }
                case 'T':
                {
                    if (!scanner.TryPair(out var to))
                    {
                        return;
                    }

                    var control = current;
                    if (lastQuad.HasValue)
                    {
                        control = current * 2 - lastQuad.Value;
                    }

                    quad = control;
                    path.QuadTo(control, Point(to));
                    break;
                }
                case 'A':
                {
                    if (!scanner.TryNumber(out var rx) ||
                        !scanner.TryNumber(out var ry) ||
                        !scanner.TryNumber(out var rotation) ||
                        !scanner.TryFlag(out var largeArc) ||
                        !scanner.TryFlag(out var sweep) ||
                        !scanner.TryPair(out var to))
                    {
                        return;
                    }

                    path.ArcTo(rx, ry, rotation, largeArc, sweep, Point(to));
                    break;
                }
                case 'Z':
                    path.Close();
                    break;
                default:
                    return;
            }

            lastCubic = cubic;
            lastQuad = quad;
        }
    }

    static bool IsCommand(char c) =>
        c is 'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v' or 'C' or 'c' or 'S' or 's' or
            'Q' or 'q' or 'T' or 't' or 'A' or 'a' or 'Z' or 'z';
}
