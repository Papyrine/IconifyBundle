using IconifyBundle;

// Static reference: the generator materialises 'activity' by baking it into this assembly. The
// registration runs from a [ModuleInitializer]; this line proves it survives trimming.
var activitySvg = Feather.Activity.Svg;
Console.WriteLine($"static-activity-len={activitySvg.Length}");

var pack = IconPack.ForPrefix("feather");

// Dynamic (string-based) lookup of the materialised icon resolves.
Console.WriteLine($"dynamic-activity-name={pack["activity"].Name}");

// Dynamic lookup of an icon that was never referenced statically is not materialised, so it throws -
// trimming must not turn this into a different/silent failure.
try
{
    _ = pack["zap"];
    Console.WriteLine("dynamic-zap=NO-THROW");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("dynamic-zap=THREW");
}
