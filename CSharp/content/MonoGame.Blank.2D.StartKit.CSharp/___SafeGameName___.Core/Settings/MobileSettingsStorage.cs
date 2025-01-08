using System;

namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Provides a mobile-specific implementation of settings storage,
/// using the user's personal folder as the base directory for storing settings.
/// </summary>
public class MobileSettingsStorage : BaseSettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MobileSettingsStorage"/> class.
    /// Sets the base directory for settings to the personal folder on mobile platforms.
    /// </summary>
    public MobileSettingsStorage()
    {
        SpecialFolderPath = Environment.SpecialFolder.Personal;
    }
}