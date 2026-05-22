namespace DiskModeConsumer;

// In Disk mode the generator also emits file-path members (e.g. Feather.ActivityPath) and the pack's
// build targets copy the SVG files next to the build output.
public class DiskModeTests
{
    [Test]
    public async Task File_path_member_points_to_copied_svg()
    {
        var path = Feather.ActivityPath;

        await Assert.That(File.Exists(path)).IsTrue();
        var svg = await File.ReadAllTextAsync(path);
        await Assert.That(svg.Contains("<svg")).IsTrue();
    }

    [Test]
    public async Task Icon_surface_still_available()
    {
        await Assert.That(Feather.Activity.Svg.Contains("<svg")).IsTrue();
        await Assert.That(Feather.AlertCircle.Name).IsEqualTo("alert-circle");
    }
}
