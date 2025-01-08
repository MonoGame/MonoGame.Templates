using System;
using System.Collections.Generic;
using System.Diagnostics;
using ___SafeGameName___.Core.Inputs;
using ___SafeGameName___.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace ___SafeGameName___.ScreenManagers;

/// <summary>
/// The screen manager is a component which manages one or more GameScreen
/// instances. It maintains a stack of screens, calls their Update and Draw
/// methods at the appropriate times, and automatically routes input to the
/// topmost active screen.
/// </summary>
public class ScreenManager : DrawableGameComponent
{
    List<GameScreen> screens = new List<GameScreen>();
    List<GameScreen> screensToUpdate = new List<GameScreen>();

    InputState inputState = new InputState();



    Texture2D blankTexture;

    bool isInitialized;



    int backbufferWidth;
    public int BackbufferWidth { get => backbufferWidth; set => backbufferWidth = value; }

    int backbufferHeight;
    public int BackbufferHeight { get => backbufferHeight; set => backbufferHeight = value; }

    Vector2 baseScreenSize = new Vector2(800, 480);
    public Vector2 BaseScreenSize { get => baseScreenSize; set => baseScreenSize = value; }

    private Matrix globalTransformation;
    public Matrix GlobalTransformation { get => globalTransformation; set => globalTransformation = value; }


    SpriteBatch spriteBatch;

    /// <summary>
    /// A default SpriteBatch shared by all the screens. This saves
    /// each screen having to bother creating their own local instance.
    /// </summary>
    public SpriteBatch SpriteBatch
    {
        get { return spriteBatch; }
    }


    SpriteFont font;

    /// <summary>
    /// A default font shared by all the screens. This saves
    /// each screen having to bother loading their own local copy.
    /// </summary>
    public SpriteFont Font
    {
        get { return font; }
    }

    bool traceEnabled;

    /// <summary>
    /// If true, the manager prints out a list of all the screens
    /// each time it is updated. This can be useful for making sure
    /// everything is being added and removed at the right times.
    /// </summary>
    public bool TraceEnabled
    {
        get { return traceEnabled; }
        set { traceEnabled = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenManager"/> class.
    /// This class manages the game's screen stack and transitions between screens.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance that this manager is associated with.</param>
    /// <remarks>
    /// During initialization, this constructor ensures that touch gestures are disabled
    /// by default by setting <see cref="TouchPanel.EnabledGestures"/> to <see cref="GestureType.None"/>.
    /// This allows the game to enable specific gesture types as needed later in the gameplay logic.
    /// </remarks>
    public ScreenManager(Game game)
        : base(game)
    {
        // We must set EnabledGestures before we can query for them,
        // but we don't assume the game wants to read them initially.
        TouchPanel.EnabledGestures = GestureType.None;
    }

    /// <summary>
    /// Initializes the screen manager component.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        Accelerometer.Initialize();

        isInitialized = true;
    }


    /// <summary>
    /// Load your graphics content.
    /// </summary>
    protected override void LoadContent()
    {
        // Load content belonging to the screen manager.
        ContentManager content = Game.Content;

        spriteBatch = new SpriteBatch(GraphicsDevice);
        font = content.Load<SpriteFont>("Fonts/Hud");
        blankTexture = content.Load<Texture2D>("Sprites/blank");

        // Tell each of the screens to load their content.
        foreach (GameScreen screen in screens)
        {
            screen.LoadContent();
        }
    }


    /// <summary>
    /// Unload your graphics content.
    /// </summary>
    protected override void UnloadContent()
    {
        // Tell each of the screens to unload their content.
        foreach (GameScreen screen in screens)
        {
            screen.UnloadContent();
        }
    }

    /// <summary>
    /// Updates the state of the screen manager and all screens it contains.
    /// Handles input, updates the game logic, and manages transitions between screens.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of the game's timing values.</param>
    /// <remarks>
    /// This method is responsible for:
    /// - Updating the input state by polling the keyboard and gamepad.
    /// - Iterating through all active screens in the stack to update their logic.
    /// - Managing focus and visibility for each screen based on the state of the screens above it in the stack.
    /// - Ensuring screens are updated in the correct order while preventing modification of the screen list
    ///   during the update process by working with a copy of the master screen list.
    /// </remarks>
    public override void Update(GameTime gameTime)
    {
        // Update input state for the current frame.
        inputState.Update(gameTime, GraphicsDevice.Viewport);

        // Create a temporary list of screens to process to avoid modification issues.
        screensToUpdate.Clear();
        foreach (GameScreen screen in screens)
            screensToUpdate.Add(screen);

        bool otherScreenHasFocus = !Game.IsActive; // Tracks whether any screen has input focus.
        bool coveredByOtherScreen = false;         // Tracks whether a screen is covered by another.

        // Process each screen in reverse order (top-most screen first).
        while (screensToUpdate.Count > 0)
        {
            // Retrieve and remove the last screen from the list.
            GameScreen screen = screensToUpdate[screensToUpdate.Count - 1];
            screensToUpdate.RemoveAt(screensToUpdate.Count - 1);

            // Update the screen's logic.
            screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Check if the screen is active or transitioning on.
            if (screen.ScreenState == ScreenState.TransitionOn ||
                screen.ScreenState == ScreenState.Active)
            {
                // Grant input handling to the first active screen.
                if (!otherScreenHasFocus)
                {
                    screen.HandleInput(gameTime, inputState);
                    otherScreenHasFocus = true;
                }

                // Mark subsequent screens as covered if this screen is not a popup.
                if (!screen.IsPopup)
                    coveredByOtherScreen = true;
            }
        }

        // Output debug information about the screen stack if tracing is enabled.
        if (traceEnabled)
            TraceScreens();
    }


    /// <summary>
    /// Prints a list of all the screens, for debugging.
    /// </summary>
    void TraceScreens()
    {
        List<string> screenNames = new List<string>();

        foreach (GameScreen screen in screens)
            screenNames.Add(screen.GetType().Name);

        Debug.WriteLine(string.Join(", ", screenNames.ToArray()));
    }


    /// <summary>
    /// Draws all active screens in the screen manager.
    /// Loops through each screen in the stack and draws it if it is not hidden.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of the game's timing values.</param>
    /// <remarks>
    /// This method iterates through all screens in the manager's stack and calls their respective
    /// <see cref="GameScreen.Draw"/> method if the screen is not in a <see cref="ScreenState.Hidden"/> state.
    /// This ensures that only visible screens are rendered during the current frame.
    /// </remarks>
    public override void Draw(GameTime gameTime)
    {
        // Loop through each screen and draw it if it's not hidden.
        foreach (GameScreen screen in screens)
        {
            // Skip hidden screens that shouldn't be drawn.
            if (screen.ScreenState == ScreenState.Hidden)
                continue;

            // Draw the screen.
            screen.Draw(gameTime);
        }
    }

    /// <summary>
    /// Disposes of the resources used by the ScreenManager.
    /// Releases any unmanaged or managed resources allocated by the ScreenManager.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from the Dispose() method (true)
    /// or from a finalizer (false).</param>
    /// <remarks>
    /// This method disposes of the resources used by the ScreenManager, including the sprite batch.
    /// It ensures that any resources are released correctly when the ScreenManager is no longer needed.
    /// The base <see cref="GameComponent.Dispose(bool)"/> method is called after disposing managed resources
    /// to ensure proper cleanup of any additional resources by the base class.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        try
        {
            // Dispose of managed resources if disposing is true.
            if (disposing)
            {
                spriteBatch?.Dispose(); // Dispose of the sprite batch.
            }
        }
        finally
        {
            // Ensure the base class can dispose of any resources.
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Adds a screen to the screen manager and sets up its initial state.
    /// Initializes the screen by setting its controlling player, screen manager, and loading its content if the manager is initialized.
    /// </summary>
    /// <param name="screen">The screen to be added to the manager.</param>
    /// <param name="controllingPlayer">The player index associated with the screen. This can be null if no specific player is controlling the screen.</param>
    /// <remarks>
    /// This method sets up a new screen by assigning it a controlling player and linking it to the current screen manager. 
    /// If the screen manager has already been initialized, the method will load the screen's content.
    /// Additionally, this method ensures that the screen is registered to respond to touch gestures according to its settings.
    /// </remarks>
    public void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
    {
        // Set the controlling player and screen manager for the screen.
        screen.ControllingPlayer = controllingPlayer;
        screen.ScreenManager = this;
        screen.IsExiting = false; // Ensure the screen is not marked as exiting initially.

        // Load the screen's content if the manager has been initialized.
        if (isInitialized)
        {
            screen.LoadContent(); // Load screen content.
        }

        // Add the screen to the list of managed screens.
        screens.Add(screen);

        // Update the TouchPanel to enable gestures the screen is interested in.
        TouchPanel.EnabledGestures = screen.EnabledGestures;
    }

    /// <summary>
    /// Removes a screen from the screen manager. This method is typically not called directly.
    /// It is recommended to use <see cref="GameScreen.ExitScreen"/> for proper screen transition.
    /// </summary>
    /// <param name="screen">The screen to be removed from the manager.</param>
    /// <remarks>
    /// This method removes a screen from the screen manager and unloads its content if the manager has been initialized.
    /// If there are remaining screens, it updates the TouchPanel to reflect the gestures the current top screen is interested in.
    /// This method is typically invoked when a screen exits, but it's more common to use <see cref="GameScreen.ExitScreen"/> to gradually remove a screen with a transition.
    /// </remarks>
    public void RemoveScreen(GameScreen screen)
    {
        // Unload the screen's content if the manager is initialized.
        if (isInitialized)
        {
            screen.UnloadContent(); // Unload screen content.
        }

        // Remove the screen from the list of active screens.
        screens.Remove(screen);
        screensToUpdate.Remove(screen); // Remove screen from the list of screens to update.

        // If there are any remaining screens, update the TouchPanel to respond to gestures for the new topmost screen.
        if (screens.Count > 0)
        {
            TouchPanel.EnabledGestures = screens[screens.Count - 1].EnabledGestures;
        }
    }


    /// <summary>
    /// Expose an array holding all the screens. We return a copy rather
    /// than the real master list, because screens should only ever be added
    /// or removed using the AddScreen and RemoveScreen methods.
    /// </summary>
    public GameScreen[] GetScreens()
    {
        return screens.ToArray();
    }

    /// <summary>
    /// Fades the back buffer to black by drawing a semi-transparent black fullscreen sprite over the entire screen.
    /// This is typically used for screen transitions or to darken the background behind popups.
    /// </summary>
    /// <param name="alpha">The transparency level of the black fullscreen sprite. A value of 0.0f is fully transparent (no fade), and a value of 1.0f is fully opaque (black screen).</param>
    /// <remarks>
    /// This method draws a full-screen rectangle with a black color multiplied by the provided alpha value, 
    /// which controls the transparency of the rectangle. The `spriteBatch` is used to perform the drawing operation, 
    /// and the viewport dimensions are used to ensure the rectangle covers the entire screen.
    /// This is commonly used to create fade-in or fade-out effects during screen transitions.
    /// </remarks>
    public void FadeBackBufferToBlack(float alpha)
    {
        // Get the current viewport (screen size).
        Viewport viewport = GraphicsDevice.Viewport;

        // Begin drawing with the sprite batch using the appropriate settings for the fade effect.
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, GlobalTransformation);

        // Draw the black rectangle over the entire screen, adjusting its opacity based on the alpha parameter.
        spriteBatch.Draw(blankTexture,
                         new Rectangle(0, 0, viewport.Width, viewport.Height), // Full screen rectangle.
                         Color.Black * alpha); // Multiply black color by the alpha value for transparency effect.

        // End drawing with the sprite batch.
        spriteBatch.End();
    }

    /// <summary>
    /// Scales the presentation area to match the aspect ratio of the base screen size.
    /// This method calculates the necessary scaling and offsets to ensure that the game's content 
    /// is properly scaled and centered within the screen area while maintaining the base screen's aspect ratio.
    /// </summary>
    /// <remarks>
    /// The method performs the following tasks:
    /// - Validates the graphics configuration and ensures valid screen dimensions.
    /// - Calculates the scaling factor required to fit the base screen size within the device's backbuffer while maintaining the aspect ratio.
    /// - Adjusts the offset to center the content within the screen, depending on whether the screen is wider or taller than the base screen.
    /// - Updates the global transformation matrix used for rendering and input transformation matrix for accurate input handling.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the graphics configuration is invalid (e.g., invalid screen size or backbuffer dimensions).</exception>
    public void ScalePresentationArea()
    {
        ArgumentNullException.ThrowIfNull(GraphicsDevice);

        // Validate the graphics configuration before proceeding
        if (baseScreenSize.X <= 0 || baseScreenSize.Y <= 0)
        {
            throw new InvalidOperationException($"Invalid graphics configuration. Screen Dimensions are Width: {baseScreenSize.X} - Height: {baseScreenSize.Y}");
        }

        // Fetch the current screen dimensions from the graphics device's presentation parameters
        backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        // Prevent division by zero if height is zero
        if (backbufferHeight == 0 || baseScreenSize.Y == 0)
        {
            return;
        }

        // Calculate the aspect ratios for both the base screen and the backbuffer
        float baseAspectRatio = baseScreenSize.X / baseScreenSize.Y;
        float screenAspectRatio = backbufferWidth / (float)backbufferHeight;

        // Initialize scaling factor and offsets
        float scalingFactor;
        float horizontalOffset = 0;
        float verticalOffset = 0;

        // Determine if the screen is wider or taller, and calculate the scaling accordingly
        if (screenAspectRatio > baseAspectRatio)
        {
            // If the screen is wider, scale based on the height
            scalingFactor = backbufferHeight / baseScreenSize.Y;

            // Center the content horizontally
            horizontalOffset = (backbufferWidth - baseScreenSize.X * scalingFactor) / 2;
        }
        else
        {
            // If the screen is taller, scale based on the width
            scalingFactor = backbufferWidth / baseScreenSize.X;

            // Center the content vertically
            verticalOffset = (backbufferHeight - baseScreenSize.Y * scalingFactor) / 2;
        }

        // Update the global transformation matrix with the calculated scaling and offsets
        globalTransformation = Matrix.CreateScale(scalingFactor) *
                               Matrix.CreateTranslation(horizontalOffset, verticalOffset, 0);

        // Update the input transformation matrix by inverting the global transformation
        inputState.UpdateInputTransformation(Matrix.Invert(globalTransformation));

        // Output debug information regarding the scaling process
        Debug.WriteLine($"Screen Size - Width[{backbufferWidth}] Height[{backbufferHeight}] ScalingFactor[{scalingFactor}]");
    }
}