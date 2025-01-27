using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ___SafeGameName___.Screens;

/// <summary>
/// The background screen is drawn behind all other menu screens.
/// It displays a fixed background image, which does not move or transition,
/// regardless of the transitions happening on top of it.
/// </summary>
class BackgroundScreen : GameScreen
{
    private ContentManager content;
    private Texture2D backgroundTexture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScreen"/> class.
    /// Sets transition times for when the background screen is shown or hidden.
    /// </summary>
    public BackgroundScreen()
    {
        TransitionOnTime = TimeSpan.FromSeconds(0.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    /// <summary>
    /// Loads the content for this screen.
    /// Specifically, it loads the background texture. This method uses a local
    /// ContentManager to ensure the content is unloaded when transitioning out of
    /// the menu, preventing it from staying loaded permanently.
    /// </summary>
    public override void LoadContent()
    {
        if (content == null)
            content = new ContentManager(ScreenManager.Game.Services, "Content");

        backgroundTexture = content.Load<Texture2D>("Sprites/gradient");
    }

    /// <summary>
    /// Unloads the content for this screen.
    /// This ensures the background texture is unloaded before transitioning to the game.
    /// </summary>
    public override void UnloadContent()
    {
        content.Unload();
    }

    /// <summary>
    /// Updates the background screen. This screen does not transition off when covered
    /// by another screen, as it is intended to be a static background.
    /// </summary>
    /// <param name="gameTime">The time elapsed since the last update.</param>
    /// <param name="otherScreenHasFocus">Whether another screen has focus.</param>
    /// <param name="coveredByOtherScreen">Whether this screen is covered by another screen.</param>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, false); // Prevents background from transitioning off.
    }

    /// <summary>
    /// Draws the background screen. The background texture is drawn with a fading effect
    /// determined by the screen's transition alpha.
    /// </summary>
    /// <param name="gameTime">The time elapsed since the last draw call.</param>
    public override void Draw(GameTime gameTime)
    {
        // Clear the screen to avoid visual artifacts from previous screens.
        ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);

        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
        Rectangle fullscreen = new Rectangle(0, 0, (int)ScreenManager.BaseScreenSize.X, (int)ScreenManager.BaseScreenSize.Y);

        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

        spriteBatch.Draw(backgroundTexture, fullscreen,
                         new Color(TransitionAlpha, TransitionAlpha, TransitionAlpha)); // Apply transition fade effect

        spriteBatch.End();
    }
}