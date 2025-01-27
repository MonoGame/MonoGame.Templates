namespace ___SafeGameName___.Screens;

/// <summary>
/// Represents the possible states of a screen during its lifecycle.
/// Used to manage screen transitions and visibility.
/// </summary>
public enum ScreenState
{
    /// <summary>
    /// The screen is transitioning on (e.g., fading in or sliding into view).
    /// </summary>
    TransitionOn,

    /// <summary>
    /// The screen is fully active and interactive.
    /// </summary>
    Active,

    /// <summary>
    /// The screen is transitioning off (e.g., fading out or sliding out of view).
    /// </summary>
    TransitionOff,

    /// <summary>
    /// The screen is not visible or interactive.
    /// </summary>
    Hidden,
}