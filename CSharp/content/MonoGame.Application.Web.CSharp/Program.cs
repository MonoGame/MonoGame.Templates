using System.Runtime.Versioning;
using System.Threading.Tasks;
using MonoGame.Runtime.Browser.WebGL2;

namespace MGNamespace;

[SupportedOSPlatform("browser")]
internal static class Program
{
    private static async Task Main()
    {
        await BrowserGame.RunAsync(new Game1());
    }
}
