using System;
using ___SafeGameName___.Core;
using ___SafeGameName___.Core.Effects;
using ___SafeGameName___.Core.Inputs;
using ___SafeGameName___.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace ___SafeGameName___.Screens;

/// <summary>
/// Represents the primary gameplay screen, responsible for game logic, rendering,
/// and handling user input. This class provides a placeholder implementation
/// of a game's logic, which can be extended to include more complex functionality.
/// </summary>
class GameplayScreen : GameScreen
{
    /// <summary>
    /// Manages the content loaded for this screen.
    /// </summary>
    private ContentManager content;

    /// <summary>
    /// Controls the transparency of the pause overlay.
    /// </summary>
    private float pauseAlpha;

    /// <summary>
    /// The sprite batch used for rendering 2D elements.
    /// </summary>
    private SpriteBatch spriteBatch;

    /// <summary>
    /// The font used for the HUD display.
    /// </summary>
    private SpriteFont hudFont;

    /// <summary>
    /// Texture for the hamburger menu icon.
    /// </summary>
    private Texture2D hamburgerTexture;

    /// <summary>
    /// Tracks whether the continue action was pressed.
    /// </summary>
    private bool wasContinuePressed;

    /// <summary>
    /// Stores the current and previous gamepad states for input handling.
    /// We only poll once per frame, then we use the same input state wherever needed.
    /// </summary>
    private GamePadState currentGamePadState, previousGamePadState;

    /// <summary>
    /// Stores the current touch state for mobile input.
    /// </summary>
    private TouchCollection currentTouchState;

    /// <summary>
    /// Manages particle effects within the game.
    /// </summary>
    private ParticleManager particleManager;

    /// <summary>
    /// The position of the HUD display.
    /// </summary>
    private Vector2 hudPosition = new Vector2(400, 300);

    /// <summary>
    /// The position of the hamburger menu icon.
    /// </summary>
    private Vector2 hamburgerPosition = new Vector2(20, 20);

    /// <summary>
    /// The string displayed for tap/click instructions.
    /// </summary>
    private string tapAnywhereString;


    /// <summary>
    /// Initializes a new instance of the <see cref="GameplayScreen"/> class.
    /// Sets up the transition timings for screen appearance and disappearance.
    /// </summary>
    public GameplayScreen()
    {
        TransitionOnTime = TimeSpan.FromSeconds(1.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    /// <summary>
    /// Loads all necessary content for the gameplay screen.
    /// It will be called once per game and is the place to load
    /// all of your content for the game, before it starts
    /// </summary>
    public override void LoadContent()
    {
        base.LoadContent();

        if (content == null)
            content = new ContentManager(ScreenManager.Game.Services, "Content");

        // Use our "App" SpriteBatch
        spriteBatch = ScreenManager.SpriteBatch;

        // Use our "App" Font
        hudFont = ScreenManager.Font;

        // Load hamburger menu texture
        hamburgerTexture = content.Load<Texture2D>("Sprites/hamburger");

        // Load correct text depending on platform
        tapAnywhereString = ___SafeGameName___Game.IsMobile
            ? Resources.TapAnywhere
            : Resources.ClickAnywhere;
        particleManager ??= ScreenManager.Game.Services.GetService<ParticleManager>();

        // Once the load has finished, we use ResetElapsedTime to tell the game's
        // timing mechanism that we have just finished a very long frame, and that
        // it should not try to catch up.
        ScreenManager.Game.ResetElapsedTime();
    }

    /// <summary>
    /// Unloads content associated with this screen.
    /// </summary>
    public override void UnloadContent()
    {
        // TODO Be sure to unload or free up resources as needed.

        content.Unload();
    }

    /// <summary>
    /// Updates the game state, including managing transitions, updating HUD positions,
    /// and advancing particle system logic. This method is called every frame, provided
    /// the screen is active and not paused.
    /// </summary>
    /// <param name="gameTime">The current game time, providing timing values for the update.</param>
    /// <param name="otherScreenHasFocus">Indicates whether another screen currently has focus,
    /// potentially pausing this screen's logic.</param>
    /// <param name="coveredByOtherScreen">Indicates whether this screen is visually covered by
    /// another screen, influencing pause transition effects.</param>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        // Call the base class to handle general update behavior for the game screen.
        base.Update(gameTime, otherScreenHasFocus, false);

        // Adjust the pause overlay's alpha value to create a fade-in or fade-out effect
        // depending on whether the screen is covered by another screen.
        pauseAlpha = coveredByOtherScreen
            ? Math.Min(pauseAlpha + 1f / 32, 1) // Gradually increase alpha to 1 (fade in).
            : Math.Max(pauseAlpha - 1f / 32, 0); // Gradually decrease alpha to 0 (fade out).

        // If the screen is active, perform gameplay-specific updates.
        if (IsActive)
        {
            // Center the HUD text on the screen by calculating its dimensions
            // and positioning it relative to the base screen size.
            var textSize = hudFont.MeasureString(tapAnywhereString);
            hudPosition = new Vector2(
                (ScreenManager.BaseScreenSize.X - textSize.X) / 2,
                (ScreenManager.BaseScreenSize.Y - textSize.Y) / 2);

            // Update the particle manager, advancing its state based on the elapsed game time.
            particleManager.Update(gameTime);
        }
    }

    /// <summary>
    /// Processes player input and handles game interactions, such as pausing, exiting,
    /// and triggering particle effects. This method is called when the gameplay screen
    /// is active and receives input.
    /// </summary>
    /// <param name="gameTime">Provides the current game time for timing-related calculations.</param>
    /// <param name="inputState">Represents the current state of user input, including
    /// keyboard, gamepad, mouse, and touch inputs.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inputState"/> is null.</exception>
    public override void HandleInput(GameTime gameTime, InputState inputState)
    {
        // Call the base class method to handle general input processing.
        base.HandleInput(gameTime, inputState);

        // Determine the active player index, defaulting to PlayerIndex.One if no controlling player is set.
        int playerIndex = ControllingPlayer != null
            ? (int)ControllingPlayer.Value
            : (int)PlayerIndex.One;

        // Check for gamepad disconnection.
        bool gamePadDisconnected = !currentGamePadState.IsConnected
            && previousGamePadState.IsConnected;

        // For mobile platforms, define the bounding rectangle of the hamburger menu for touch input detection.
        Rectangle? hamburgerMenuTouched = ___SafeGameName___Game.IsMobile
            ? new Rectangle((int)hamburgerPosition.X, (int)hamburgerPosition.Y, hamburgerTexture.Width, hamburgerTexture.Height)
            : null;

        // Check if the game should pause or exit based on input or gamepad disconnection.
        if (inputState.IsPauseGame(ControllingPlayer, hamburgerMenuTouched)
            || gamePadDisconnected)
        {
            // We'll exit for now, as we don't have a Pause screen
            ScreenManager.Game.Exit();
        }
        else
        {
            // Update input states for the current frame.
            previousGamePadState = inputState.LastGamePadStates[playerIndex];
            currentGamePadState = inputState.CurrentGamePadStates[playerIndex];
            currentTouchState = inputState.CurrentTouchState;

            // Exit the game if the Back button is pressed on the gamepad.
            if (currentGamePadState.Buttons.Back == ButtonState.Pressed)
                ScreenManager.Game.Exit();

            // Trigger particle effects if the A button is pressed, a mouse click occurs,
            // or there is any touch input.
            if (currentGamePadState.Buttons.A == ButtonState.Pressed
                || inputState.IsLeftMouseButtonClicked()
                || currentTouchState.Count > 0)
            {
                particleManager.Position = inputState.CurrentCursorLocation;
                particleManager.Emit(100, ParticleEffectType.Fireworks);
            }

            if (wasContinuePressed)
            {
                // Perform the appropriate action to advance the game and
                // to get the player back to playing.

                wasContinuePressed = false;
            }
        }
    }

    /// <summary>
    /// Renders the gameplay screen, including the background, particles, HUD, 
    /// and transition effects.
    /// </summary>
    /// <param name="gameTime">Provides the current game time for any time-based rendering updates.</param>
    public override void Draw(GameTime gameTime)
    {
        // Clear the screen with a Cornflower Blue color.
        ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);

        // Begin a sprite batch for rendering game objects with the global transformation applied.
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

        // TODO Your drawing fanciness here
        // But for now...

        // Draw particle effects managed by the ParticleManager.
        particleManager.Draw(spriteBatch);

        // Draw the HUD elements.
        DrawHud(gameTime);

        // End the sprite batch after rendering is complete.
        spriteBatch.End();

        // Call the base class to allow additional drawing operations if necessary.
        base.Draw(gameTime);

        // Apply a fade effect if the screen is transitioning on or off, or if the pause overlay is active.
        if (TransitionPosition > 0 || pauseAlpha > 0)
        {
            // Calculate the fade alpha value based on the transition position and pause alpha.
            float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

            // Render a fade-to-black effect on the back buffer with the calculated alpha value.
            ScreenManager.FadeBackBufferToBlack(alpha);
        }
    }

    /// <summary>
    /// Renders the Heads-Up Display (HUD), including dynamic text and an optional hamburger menu icon for mobile platforms.
    /// </summary>
    /// <param name="gameTime">Provides the current game time for determining animation effects and timing.</param>
    private void DrawHud(GameTime gameTime)
    {
        // Determine the color of the HUD text based on time.
        // Alternates between Yellow and Red every second.
        Color timeColor = gameTime.ElapsedGameTime.TotalSeconds % 2 == 0 ? Color.Yellow : Color.Red;

        // Render the HUD string with a shadow effect at the specified position and color.
        DrawShadowedString(tapAnywhereString, hudPosition, timeColor);

        // If running on a mobile platform, draw the hamburger menu icon at its position.
        if (___SafeGameName___Game.IsMobile)
        {
            spriteBatch.Draw(hamburgerTexture, hamburgerPosition, Color.White);
        }
    }

    /// <summary>
    /// Draws a string with a shadow effect for enhanced readability.
    /// </summary>
    /// <param name="value">The string to render on the screen.</param>
    /// <param name="position">The position on the screen where the string will be drawn.</param>
    /// <param name="color">The primary color of the text.</param>
    private void DrawShadowedString(string value, Vector2 position, Color color)
    {
        // Draw the shadow of the text slightly offset from the original position with a gray color.
        spriteBatch.DrawString(hudFont, value, position + new Vector2(2.0f, 2.0f), Color.Gray);

        // Draw the main text on top of the shadow at the original position with the specified color.
        spriteBatch.DrawString(hudFont, value, position, color);
    }
}