using System;
using System.IO;
using System.Reflection.Emit;
using ___SafeGameName___.Core;
using ___SafeGameName___.Core.Effects;
using ___SafeGameName___.Core.Inputs;
using ___SafeGameName___.Core.Localization;
using ___SafeGameName___.Core.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace ___SafeGameName___.Screens;

/// <summary>
/// This screen implements the actual game logic. It is just a
/// placeholder to get the idea across: you'll probably want to
/// put some more interesting gameplay in here!
/// </summary>
class GameplayScreen : GameScreen
{
    ContentManager content;

    float pauseAlpha;

    private SpriteBatch spriteBatch;
    private SpriteFont hudFont;
    private Texture2D hamburgerTexture;

    // Meta-level game state.
    private bool wasContinuePressed;

    // We store our input states so that we only poll once per frame, 
    // then we use the same input state wherever needed
    private GamePadState currentGamePadState;
    private GamePadState previousGamePadState;
    private TouchCollection currentTouchState;

    private ParticleManager particleManager;
    private SettingsManager<___SafeGameName___Leaderboard> leaderboardManager;
    private Vector2 hudPosition = new Vector2(400, 300);
    private Vector2 hamburgerPosition = new Vector2(20, 20);
    private string tapAnywhereString;

    /// <summary>
    /// Constructor.
    /// </summary>
    public GameplayScreen()
    {
        TransitionOnTime = TimeSpan.FromSeconds(1.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content for the game.
    /// </summary>
    public override void LoadContent()
    {
        base.LoadContent();

        if (content == null)
            content = new ContentManager(ScreenManager.Game.Services, "Content");

        spriteBatch = ScreenManager.SpriteBatch;

        // Load fonts
        hudFont = content.Load<SpriteFont>("Fonts/Hud");

        // Load hamburger menu texture
        hamburgerTexture = content.Load<Texture2D>("Sprites/hamburger");

        tapAnywhereString = ___SafeGameName___Game.IsMobile ? Resources.TapAnywhere : Resources.ClickAnywhere;

        particleManager ??= ScreenManager.Game.Services.GetService<ParticleManager>();

        leaderboardManager ??= ScreenManager.Game.Services.GetService<SettingsManager<___SafeGameName___Leaderboard>>();

        // once the load has finished, we use ResetElapsedTime to tell the game's
        // timing mechanism that we have just finished a very long frame, and that
        // it should not try to catch up.
        ScreenManager.Game.ResetElapsedTime();
    }

    /// <summary>
    /// Unload graphics content used by the game.
    /// </summary>
    public override void UnloadContent()
    {
        // TODO Be sure to unload or free up resources as needed.

        content.Unload();
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    ///
    /// This method checks the GameScreen.IsActive
    /// property, so the game will stop updating when the pause menu is active,
    /// or if you tab away to a different application.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="otherScreenHasFocus">If another screen has focus</param>
    /// <param name="coveredByOtherScreen">If currently covered by another screen</param>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                   bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, false);

        // Gradually fade in or out depending on whether we are covered by the pause screen.
        if (coveredByOtherScreen)
            pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
        else
            pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);


        if (IsActive)
        {
            // TODO Your update fanciness here

            // Centre our text
            var tapAnywhereStringDimensions = hudFont.MeasureString(tapAnywhereString);
            hudPosition = new Vector2((ScreenManager.BaseScreenSize.X - tapAnywhereStringDimensions.X) / 2, (ScreenManager.BaseScreenSize.Y - tapAnywhereStringDimensions.Y) / 2);

            // But for now we'll just update the currectly selected particle system
            particleManager.Update(gameTime);
        }
    }

    /// <summary>
    /// Lets the game respond to player input. Unlike the Update method,
    /// this will only be called when the gameplay screen is active.
    /// </summary>
    public override void HandleInput(GameTime gameTime, InputState inputState)
    {
        ArgumentNullException.ThrowIfNull(inputState);

        base.HandleInput(gameTime, inputState);

        // Get all of our input states for the active player profile.
        int playerIndex = ControllingPlayer != null ? (int)ControllingPlayer.Value : (int)PlayerIndex.One;

        // The game pauses either if the user presses the pause button, or if
        // they unplug the active gamepad. This requires us to keep track of
        // whether a gamepad was ever plugged in, because we don't want to pause
        // on PC if they are playing with a keyboard and have no gamepad at all!
        bool gamePadDisconnected = !currentGamePadState.IsConnected && previousGamePadState.IsConnected;

        Rectangle? hamburgerMenuTouched = null;
        if (___SafeGameName___Game.IsMobile)
        {
            hamburgerMenuTouched = new Rectangle((int)hamburgerPosition.X, (int)hamburgerPosition.Y, hamburgerTexture.Width, hamburgerTexture.Height);
        }

        if (inputState.IsPauseGame(ControllingPlayer, hamburgerMenuTouched)
                || gamePadDisconnected)
        {
            // We'll exit for now, as we don't have a Pause screen
            ScreenManager.Game.Exit();
        }
        else
        {
            previousGamePadState = inputState.LastGamePadStates[playerIndex];
            currentGamePadState = inputState.CurrentGamePadStates[playerIndex];

            currentTouchState = inputState.CurrentTouchState;

            // Exit the game when back is pressed.
            if (currentGamePadState.Buttons.Back == ButtonState.Pressed)
                ScreenManager.Game.Exit();

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
    /// Draws the gameplay screen.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        // This game has a blue background.
        ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);

        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

        // TODO Your drawing fanciness here

        // But for now...
        particleManager.Draw(spriteBatch);

        DrawHud(gameTime);

        spriteBatch.End();

        base.Draw(gameTime);

        // If the game is transitioning on or off, fade it out to black.
        if (TransitionPosition > 0 || pauseAlpha > 0)
        {
            float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

            ScreenManager.FadeBackBufferToBlack(alpha);
        }
    }

    private void DrawHud(GameTime gameTime)
    {
        Color timeColor = gameTime.ElapsedGameTime.TotalSeconds % 2 == 0 ? Color.Yellow : Color.Red;

        DrawShadowedString(tapAnywhereString, hudPosition, timeColor);

        if (___SafeGameName___Game.IsMobile)
        {
            spriteBatch.Draw(hamburgerTexture, hamburgerPosition, Color.White);
        }
    }

    private void DrawShadowedString(string value, Vector2 position, Color color)
    {
        spriteBatch.DrawString(hudFont, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        spriteBatch.DrawString(hudFont, value, position, color);
    }
}