namespace ___SafeGameName___.Core.Effects;

/// <summary>
/// Represents the types of particle effects supported in the game.
/// </summary>
/// <remarks>
/// This enum is used to identify and manage various particle effect styles 
/// within the game. Each effect type corresponds to a specific visual behavior 
/// and animation style, allowing developers to easily create or trigger the desired 
/// particle effect.
/// </remarks>
public enum ParticleEffectType
{
    /// <summary>
    /// A celebratory confetti effect, often used for events such as achievements or victories.
    /// </summary>
    Confetti,

    /// <summary>
    /// A dynamic explosion effect, typically used for destruction events or action sequences.
    /// </summary>
    Explosions,

    /// <summary>
    /// A colorful fireworks effect, often used for celebrations or festive displays.
    /// </summary>
    Fireworks,

    /// <summary>
    /// A shimmering sparkle effect, useful for magical or whimsical moments.
    /// </summary>
    Sparkles,
}