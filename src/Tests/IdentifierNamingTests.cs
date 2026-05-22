public class IdentifierNamingTests
{
    [Test]
    [Arguments("alert-circle", "AlertCircle")]
    [Arguments("home_outline", "HomeOutline")]
    [Arguments("foo.bar", "FooBar")]
    [Arguments("activity", "Activity")]
    [Arguments("arrow-down-circle", "ArrowDownCircle")]
    [Arguments("1password", "_1password")]
    public async Task ToPascalCase(string input, string expected) =>
        await Assert.That(IdentifierNaming.ToPascalCase(input)).IsEqualTo(expected);

    [Test]
    public async Task Deduplicate_appends_suffix()
    {
        var used = new HashSet<string>();
        await Assert.That(IdentifierNaming.Deduplicate("Home", used)).IsEqualTo("Home");
        await Assert.That(IdentifierNaming.Deduplicate("Home", used)).IsEqualTo("Home2");
        await Assert.That(IdentifierNaming.Deduplicate("Home", used)).IsEqualTo("Home3");
    }

    [Test]
    public async Task Empty_becomes_underscore() =>
        await Assert.That(IdentifierNaming.ToPascalCase("---")).IsEqualTo("_");
}
