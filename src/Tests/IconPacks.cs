// begin-snippet: declare-icons
// Baked into the generated C# (the default): zero runtime files, trimmer friendly.
[assembly: IconPack("mdi", "home", "account", "account-outline", "cog", "heart")]

// Embedded in the assembly as a compact JSON blob, parsed lazily at runtime.
[assembly: IconPack("lucide", "house", "settings", Storage = IconStorage.EmbeddedResource)]

// Written to wwwroot/iconistic and fetched at runtime (great for Blazor).
[assembly: IconPack("tabler", "home", "heart", Storage = IconStorage.DeployedFile)]
// end-snippet
