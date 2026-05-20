using System.Globalization;
using System.Text;

namespace Iconistic.Generator;

/// <summary>
/// A tiny, dependency-free JSON reader. The generator cannot rely on System.Text.Json being
/// available in the Roslyn host, and the Iconify icon-data format is simple, so we parse it here.
/// Objects map to <see cref="Dictionary{TKey,TValue}"/>, arrays to <see cref="List{T}"/>,
/// numbers to <see cref="double"/>, plus string, bool and null.
/// </summary>
static class MiniJson
{
    public static object? Parse(string text)
    {
        var position = 0;
        var value = ParseValue(text, ref position);
        SkipWhitespace(text, ref position);
        if (position != text.Length)
        {
            throw new FormatException($"Unexpected trailing characters at position {position}.");
        }

        return value;
    }

    static object? ParseValue(string text, ref int position)
    {
        SkipWhitespace(text, ref position);
        if (position >= text.Length)
        {
            throw new FormatException("Unexpected end of JSON.");
        }

        var c = text[position];
        switch (c)
        {
            case '{':
                return ParseObject(text, ref position);
            case '[':
                return ParseArray(text, ref position);
            case '"':
                return ParseString(text, ref position);
            case 't':
            case 'f':
                return ParseBool(text, ref position);
            case 'n':
                ParseLiteral(text, ref position, "null");
                return null;
            default:
                return ParseNumber(text, ref position);
        }
    }

    static Dictionary<string, object?> ParseObject(string text, ref int position)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        position++; // {
        SkipWhitespace(text, ref position);
        if (text[position] == '}')
        {
            position++;
            return result;
        }

        while (true)
        {
            SkipWhitespace(text, ref position);
            var key = ParseString(text, ref position);
            SkipWhitespace(text, ref position);
            Expect(text, ref position, ':');
            result[key] = ParseValue(text, ref position);
            SkipWhitespace(text, ref position);
            var c = text[position++];
            if (c == ',')
            {
                continue;
            }

            if (c == '}')
            {
                return result;
            }

            throw new FormatException($"Expected ',' or '}}' at position {position - 1}.");
        }
    }

    static List<object?> ParseArray(string text, ref int position)
    {
        var result = new List<object?>();
        position++; // [
        SkipWhitespace(text, ref position);
        if (text[position] == ']')
        {
            position++;
            return result;
        }

        while (true)
        {
            result.Add(ParseValue(text, ref position));
            SkipWhitespace(text, ref position);
            var c = text[position++];
            if (c == ',')
            {
                continue;
            }

            if (c == ']')
            {
                return result;
            }

            throw new FormatException($"Expected ',' or ']' at position {position - 1}.");
        }
    }

    static string ParseString(string text, ref int position)
    {
        Expect(text, ref position, '"');
        var builder = new StringBuilder();
        while (true)
        {
            var c = text[position++];
            switch (c)
            {
                case '"':
                    return builder.ToString();
                case '\\':
                    var escape = text[position++];
                    switch (escape)
                    {
                        case '"':
                            builder.Append('"');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '/':
                            builder.Append('/');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            var hex = text.Substring(position, 4);
                            builder.Append((char) int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            position += 4;
                            break;
                        default:
                            throw new FormatException($"Invalid escape '\\{escape}' at position {position - 1}.");
                    }

                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
    }

    static bool ParseBool(string text, ref int position)
    {
        if (text[position] == 't')
        {
            ParseLiteral(text, ref position, "true");
            return true;
        }

        ParseLiteral(text, ref position, "false");
        return false;
    }

    static double ParseNumber(string text, ref int position)
    {
        var start = position;
        while (position < text.Length && "+-0123456789.eE".IndexOf(text[position]) >= 0)
        {
            position++;
        }

        return double.Parse(text.Substring(start, position - start), CultureInfo.InvariantCulture);
    }

    static void ParseLiteral(string text, ref int position, string literal)
    {
        if (position + literal.Length > text.Length ||
            text.Substring(position, literal.Length) != literal)
        {
            throw new FormatException($"Expected '{literal}' at position {position}.");
        }

        position += literal.Length;
    }

    static void Expect(string text, ref int position, char expected)
    {
        if (text[position] != expected)
        {
            throw new FormatException($"Expected '{expected}' at position {position}.");
        }

        position++;
    }

    static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
    }
}
