using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Iconistic.Generator;

/// <summary>
/// Converts Iconify prefixes and kebab-case icon names into valid PascalCase C# identifiers.
/// </summary>
static class NameMangler
{
    public static string ToIdentifier(string name)
    {
        var builder = new StringBuilder(name.Length);
        var upperNext = true;
        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c))
            {
                upperNext = true;
                continue;
            }

            builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
            upperNext = false;
        }

        var identifier = builder.ToString();
        if (identifier.Length == 0)
        {
            identifier = "Icon";
        }

        if (char.IsDigit(identifier[0]))
        {
            identifier = "_" + identifier;
        }

        if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
        {
            identifier = "@" + identifier;
        }

        return identifier;
    }

    /// <summary>Returns a unique identifier, suffixing with a number on collision.</summary>
    public static string ToUniqueIdentifier(string name, HashSet<string> used)
    {
        var identifier = ToIdentifier(name);
        if (used.Add(identifier))
        {
            return identifier;
        }

        for (var i = 2; ; i++)
        {
            var candidate = identifier + "_" + i;
            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }
}
