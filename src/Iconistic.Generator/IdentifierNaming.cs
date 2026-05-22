using System.Collections.Generic;
using System.Text;

namespace Iconistic.Generator;

public static class IdentifierNaming
{
    /// <summary>
    /// Converts an Iconify icon name (e.g. <c>alert-circle</c>, <c>1password</c>) into a valid
    /// PascalCase C# identifier (e.g. <c>AlertCircle</c>, <c>_1Password</c>).
    /// </summary>
    public static string ToPascalCase(string name)
    {
        var builder = new StringBuilder(name.Length);
        var upperNext = true;
        foreach (var c in name)
        {
            if (c is '-' or '_' or '.' or ' ' or '/' or ':' or '+')
            {
                upperNext = true;
                continue;
            }

            if (!IsIdentifierPart(c))
            {
                upperNext = true;
                continue;
            }

            builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
            upperNext = false;
        }

        if (builder.Length == 0)
        {
            return "_";
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        // A PascalCased identifier always starts with an upper-case letter (or '_'), so it can never
        // collide with a C# keyword (all keywords are lower case). No keyword escaping is required.
        return builder.ToString();
    }

    /// <summary>
    /// Returns a unique identifier, appending a numeric suffix if <paramref name="candidate"/>
    /// has already been used.
    /// </summary>
    public static string Deduplicate(string candidate, HashSet<string> used)
    {
        if (used.Add(candidate))
        {
            return candidate;
        }

        var index = 2;
        string next;
        do
        {
            next = candidate + index;
            index++;
        }
        while (!used.Add(next));

        return next;
    }

    static bool IsIdentifierPart(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}
