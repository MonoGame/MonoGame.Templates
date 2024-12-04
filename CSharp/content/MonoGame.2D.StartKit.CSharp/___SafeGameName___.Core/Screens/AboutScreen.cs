using ___SafeGameName___.Core.Localization;

namespace ___SafeGameName___.Screens;

/// <summary>
/// The options screen is brought up over the top of the main menu
/// screen, and gives the user a chance to configure the game
/// in various hopefully useful ways.
/// </summary>
class AboutScreen : MenuScreen
{
    MenuEntry builtWithMonoGameMenuEntry;
    MenuEntry monoGameWebsiteMenuEntry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AboutScreen()
        : base(Resources.About)
    {
        // Create our menu entries.
        builtWithMonoGameMenuEntry = new MenuEntry("#BuiltWithMonoGame", false);
        monoGameWebsiteMenuEntry = new MenuEntry(Resources.MonoGameSite);
        MenuEntry back = new MenuEntry(Resources.Back);

        // Hook up menu event handlers.
        monoGameWebsiteMenuEntry.Selected += MonoGameWebsiteMenuSelected;
        back.Selected += OnCancel;

        // Add entries to the menu.
        MenuEntries.Add(builtWithMonoGameMenuEntry);
        MenuEntries.Add(monoGameWebsiteMenuEntry);
        MenuEntries.Add(back);
    }

    /// <summary>
    /// Event handler for when the MonoGame Website menu entry is selected.
    /// </summary>
    void MonoGameWebsiteMenuSelected(object sender, PlayerIndexEventArgs e)
    {
        LaunchDefaultBrowser("https://www.monogame.net/");
    }

    /// <summary>
    /// Launch defaut browser using the URL that's been passed in
    /// <param name="url">
    /// </summary>
    private static void LaunchDefaultBrowser(string url)
    {
        // More tweaks required to make this work on all platforms :)
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }
}