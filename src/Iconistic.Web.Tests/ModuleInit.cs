using System.Runtime.CompilerServices;

namespace Iconistic.Web.Tests;

static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        DiffEngine.DiffRunner.Disabled = true;
        VerifyDiffPlex.Initialize();
        VerifyPlaywright.Initialize();
    }
}
