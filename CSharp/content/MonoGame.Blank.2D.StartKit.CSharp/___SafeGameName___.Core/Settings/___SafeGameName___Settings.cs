using System.ComponentModel;
using System.Runtime.CompilerServices;
using ___SafeGameName___.Core.Effects;

namespace ___SafeGameName___.Core.Settings;

/// <summary>
/// Represents the game settings for ___SafeGameName___. Provides properties for managing user preferences,
/// such as full-screen mode, language, and particle effects. Implements INotifyPropertyChanged
/// to support data binding and notify of property changes.
/// </summary>
public class ___SafeGameName___Settings : INotifyPropertyChanged
{
    private bool fullScreen;
    private int language;
    private ParticleEffectType particleEffect;

    /// <summary>
    /// Gets or sets a value indicating whether the game runs in full-screen mode.
    /// </summary>
    public bool FullScreen
    {
        get => fullScreen;
        set
        {
            if (fullScreen != value)
            {
                fullScreen = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the index representing the selected language.
    /// </summary>
    public int Language
    {
        get => language;
        set
        {
            if (language != value)
            {
                language = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the type of particle effect to be displayed in the game.
    /// </summary>
    public ParticleEffectType ParticleEffect
    {
        get => particleEffect;
        set
        {
            if (particleEffect != value)
            {
                particleEffect = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for a given property.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that changed. This is optional and
    /// defaults to the name of the calling property.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}