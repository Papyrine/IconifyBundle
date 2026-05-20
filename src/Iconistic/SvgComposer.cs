using System.Globalization;
using System.Text;

namespace Iconistic;

/// <summary>
/// Builds a complete <c>&lt;svg&gt;</c> string from an <see cref="IconisticIcon"/>, applying
/// flips and rotations using the same algorithm as the Iconify renderer.
/// </summary>
static class SvgComposer
{
    public static string Compose(in IconisticIcon icon, SvgOptions options)
    {
        var body = icon.Body ?? "";
        var width = icon.Width <= 0 ? 16 : icon.Width;
        var height = icon.Height <= 0 ? 16 : icon.Height;

        var rotate = (icon.Rotate + options.Rotate) & 3;
        var hFlip = icon.HFlip ^ options.HFlip;
        var vFlip = icon.VFlip ^ options.VFlip;

        var boxLeft = icon.Left;
        var boxTop = icon.Top;
        var boxWidth = width;
        var boxHeight = height;

        var transforms = new List<string>();

        if (hFlip)
        {
            if (vFlip)
            {
                rotate = (rotate + 2) & 3;
            }
            else
            {
                transforms.Add($"translate({Num(width + icon.Left)} {Num(-icon.Top)})");
                transforms.Add("scale(-1 1)");
                boxLeft = 0;
                boxTop = 0;
            }
        }
        else if (vFlip)
        {
            transforms.Add($"translate({Num(-icon.Left)} {Num(height + icon.Top)})");
            transforms.Add("scale(1 -1)");
            boxLeft = 0;
            boxTop = 0;
        }

        switch (rotate)
        {
            case 1:
            {
                var pivot = height / 2d + icon.Top;
                transforms.Insert(0, $"rotate(90 {Num(pivot)} {Num(pivot)})");
                break;
            }
            case 2:
                transforms.Insert(0, $"rotate(180 {Num(width / 2d + icon.Left)} {Num(height / 2d + icon.Top)})");
                break;
            case 3:
            {
                var pivot = width / 2d + icon.Left;
                transforms.Insert(0, $"rotate(-90 {Num(pivot)} {Num(pivot)})");
                break;
            }
        }

        if ((rotate & 1) == 1)
        {
            if (boxLeft != boxTop)
            {
                (boxLeft, boxTop) = (boxTop, boxLeft);
            }

            if (boxWidth != boxHeight)
            {
                (boxWidth, boxHeight) = (boxHeight, boxWidth);
            }
        }

        if (transforms.Count > 0)
        {
            body = $"<g transform=\"{string.Join(" ", transforms)}\">{body}</g>";
        }

        var svgWidth = options.Width is { Length: > 0 } w ? w : "1em";
        var svgHeight = options.Height is { Length: > 0 } h ? h : "1em";

        var style = options.Style;
        if (options.Color is { Length: > 0 } color)
        {
            var colorStyle = $"color:{color}";
            style = string.IsNullOrEmpty(style) ? colorStyle : $"{style};{colorStyle}";
        }

        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"");
        builder.Append(" width=\"").Append(svgWidth).Append('"');
        builder.Append(" height=\"").Append(svgHeight).Append('"');
        builder.Append(" viewBox=\"")
            .Append(Num(boxLeft)).Append(' ')
            .Append(Num(boxTop)).Append(' ')
            .Append(Num(boxWidth)).Append(' ')
            .Append(Num(boxHeight)).Append('"');

        if (options.CssClass is { Length: > 0 } cssClass)
        {
            builder.Append(" class=\"").Append(cssClass).Append('"');
        }

        if (!string.IsNullOrEmpty(style))
        {
            builder.Append(" style=\"").Append(style).Append('"');
        }

        builder.Append('>');
        builder.Append(body);
        builder.Append("</svg>");
        return builder.ToString();
    }

    static string Num(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    static string Num(double value) =>
        value == Math.Floor(value)
            ? ((long) value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
