using System.Text;

namespace Iconistic.Generator;

/// <summary>
/// Serializes resolved icons into the compact JSON format consumed at runtime by
/// <c>Iconistic.IconPack</c> (used for the EmbeddedResource and DeployedFile storage modes).
/// </summary>
static class CompactJsonWriter
{
    public static string Write(
        int defaultWidth,
        int defaultHeight,
        IReadOnlyList<KeyValuePair<string, NormIcon>> icons)
    {
        var builder = new StringBuilder();
        builder.Append("{\"w\":").Append(defaultWidth)
            .Append(",\"h\":").Append(defaultHeight)
            .Append(",\"icons\":{");

        var first = true;
        foreach (var pair in icons)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;

            var icon = pair.Value;
            builder.Append(Quote(pair.Key)).Append(":{\"b\":").Append(Quote(icon.Body));

            if (icon.Width != defaultWidth)
            {
                builder.Append(",\"w\":").Append(icon.Width);
            }

            if (icon.Height != defaultHeight)
            {
                builder.Append(",\"h\":").Append(icon.Height);
            }

            if (icon.Left != 0)
            {
                builder.Append(",\"l\":").Append(icon.Left);
            }

            if (icon.Top != 0)
            {
                builder.Append(",\"t\":").Append(icon.Top);
            }

            if (icon.Rotate != 0)
            {
                builder.Append(",\"r\":").Append(icon.Rotate);
            }

            if (icon.HFlip)
            {
                builder.Append(",\"hf\":true");
            }

            if (icon.VFlip)
            {
                builder.Append(",\"vf\":true");
            }

            builder.Append('}');
        }

        builder.Append("}}");
        return builder.ToString();
    }

    static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        builder.Append("\\u").Append(((int) c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }
}
