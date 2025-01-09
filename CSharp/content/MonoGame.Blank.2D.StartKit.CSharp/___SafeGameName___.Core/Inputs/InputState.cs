using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace ___SafeGameName___.Core.Inputs;

/// <summary>
/// Helper for reading input from keyboard, gamepad, and touch input. This class 
/// tracks both the current and previous state of the input devices, and implements 
/// query methods for high level input actions such as "move up through the menu"
/// or "pause the game".
/// </summary>
public class InputState
{
    /// <summary>
    /// The maximum number of input devices supported by the input state. This constant determines the size of the input arrays for gamepads and keyboards.
    /// </summary>
    public const int MaxInputs = 4;

    /// <summary>
    /// The current state of the accelerometer, representing motion or tilt data from a device with an accelerometer.
    /// </summary>
    public AccelerometerState CurrentAccelerometerState;

    /// <summary>
    /// An array holding the current state of each connected gamepad.
    /// The array size is defined by <see cref="MaxInputs"/> and stores the state for each gamepad.
    /// </summary>
    public readonly GamePadState[] CurrentGamePadStates;

    /// <summary>
    /// An array holding the current state of the keyboard, storing the current key states for each keyboard.
    /// The array size is defined by <see cref="MaxInputs"/> and stores the state for each keyboard.
    /// </summary>
    public readonly KeyboardState[] CurrentKeyboardStates;

    /// <summary>
    /// The current state of the mouse, which includes information such as the mouse position and button states.
    /// </summary>
    public MouseState CurrentMouseState;

    /// <summary>
    /// The count of current touch points, used to track how many touch points are active on the screen.
    /// </summary>
    private int touchCount;

    /// <summary>
    /// The current touch state, representing the collection of touch points currently detected on the device.
    /// </summary>
    public TouchCollection CurrentTouchState;

    /// <summary>
    /// The last known state of the accelerometer, representing motion or tilt data from a device with an accelerometer at the previous frame.
    /// </summary>
    public AccelerometerState LastAccelerometerState;

    /// <summary>
    /// An array holding the last known state of each connected gamepad.
    /// The array size is defined by <see cref="MaxInputs"/> and stores the last known state for each gamepad.
    /// </summary>
    public readonly GamePadState[] LastGamePadStates;

    /// <summary>
    /// An array holding the last known state of the keyboard, storing the previous key states for each keyboard.
    /// The array size is defined by <see cref="MaxInputs"/> and stores the last known state for each keyboard.
    /// </summary>
    public readonly KeyboardState[] LastKeyboardStates;

    /// <summary>
    /// The last known state of the mouse, including the position and button states from the previous frame.
    /// </summary>
    public MouseState LastMouseState;

    /// <summary>
    /// The last known touch state, representing the collection of touch points from the previous frame.
    /// </summary>
    public TouchCollection LastTouchState;

    /// <summary>
    /// A list of gesture samples detected by the input system. This collection holds the recognized gestures such as taps, swipes, etc.
    /// </summary>
    public readonly List<GestureSample> Gestures = new List<GestureSample>();

    /// <summary>
    /// Cursor move speed in pixels per second
    /// </summary>
    private const float cursorMoveSpeed = 250.0f;

    private Vector2 currentCursorLocation;
    /// <summary>
    /// Current location of our Cursor
    /// </summary>
    public Vector2 CurrentCursorLocation => currentCursorLocation;

    private Vector2 lastCursorLocation;
    /// <summary>
    /// Current location of our Cursor
    /// </summary>
    public Vector2 LastCursorLocation => lastCursorLocation;

    private bool isMouseWheelScrolledDown;
    /// <summary>
    /// Has the user scrolled the mouse wheel down?
    /// </summary>
    public bool IsMouseWheelScrolledDown => isMouseWheelScrolledDown;

    private bool isMouseWheelScrolledUp;
    private Matrix inputTransformation;

    /// <summary>
    /// Has the user scrolled the mouse wheel up?
    /// </summary>
    public bool IsMouseWheelScrolledUp => isMouseWheelScrolledUp;

    /// Initializes a new instance of the <see cref="InputState"/> class.
    /// This constructor initializes arrays to hold the current and previous states of input devices (keyboard, gamepad),
    /// and sets up the appropriate gesture settings based on the platform (mobile or desktop).
    /// </summary>
    /// <remarks>
    /// The constructor performs the following actions:
    /// <list type="bullet">
    /// <item>Initializes arrays for the current and previous keyboard and gamepad states, based on a maximum number of inputs.</item>
    /// <item>If the game is running on a mobile platform, it enables tap gestures via <see cref="TouchPanel.EnabledGestures"/>.</item>
    /// <item>If the game is running on a desktop platform, it does nothing (can be extended later if needed).</item>
    /// <item>If the platform is unknown or unsupported, a <see cref="PlatformNotSupportedException"/> is thrown.</item>
    /// </list>
    /// </remarks>
    public InputState()
    {
        // Initialize arrays for storing current input states for up to MaxInputs devices.
        CurrentKeyboardStates = new KeyboardState[MaxInputs];
        CurrentGamePadStates = new GamePadState[MaxInputs];

        // Initialize arrays for storing the previous input states for up to MaxInputs devices.
        LastKeyboardStates = new KeyboardState[MaxInputs];
        LastGamePadStates = new GamePadState[MaxInputs];

        // Check the platform type to configure gesture settings.
        if (___SafeGameName___Game.IsMobile)
        {
            // On mobile platforms, enable tap gestures for touch input.
            TouchPanel.EnabledGestures = GestureType.Tap;
        }
        else if (___SafeGameName___Game.IsDesktop)
        {
            // Placeholder for any desktop-specific input initialization.
            // Can be extended for future customization if needed.
        }
        else
        {
            // Throw an exception if the platform is not recognized.
            throw new PlatformNotSupportedException();
        }
    }

    /// <summary>
    /// Reads the latest state of all the inputs.
    /// </summary>
    /// <summary>
    /// Updates the input state, collecting the current state of input devices (keyboard, gamepad, mouse, touch) and processing the events.
    /// This method also handles cursor movement, touch events, and gesture input, as well as scroll wheel actions.
    /// </summary>
    /// <param name="gameTime">Provides timing information for the game, including elapsed time between frames.</param>
    /// <param name="viewport">The viewport of the game screen, used to constrain cursor movement to within the screen boundaries.</param>
    public void Update(GameTime gameTime, Viewport viewport)
    {
        // Update the current accelerometer state with the latest data
        CurrentAccelerometerState = Accelerometer.GetState();

        // Loop through each possible input device (up to MaxInputs), storing the previous states and updating the current states
        for (int i = 0; i < MaxInputs; i++)
        {
            // Store the last known states for keyboard and gamepad inputs
            LastKeyboardStates[i] = CurrentKeyboardStates[i];
            LastGamePadStates[i] = CurrentGamePadStates[i];

            // Get the current states for keyboard and gamepad
            CurrentKeyboardStates[i] = Keyboard.GetState();
            CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
        }

        // Store the last mouse state and update the current mouse state
        LastMouseState = CurrentMouseState;
        CurrentMouseState = Mouse.GetState();

        // Reset touch count and update touch states
        touchCount = 0;
        LastTouchState = CurrentTouchState;
        CurrentTouchState = TouchPanel.GetState();

        // Clear previous gesture samples and capture any new available gestures
        Gestures.Clear();
        while (TouchPanel.IsGestureAvailable)
        {
            Gestures.Add(TouchPanel.ReadGesture());
        }

        // Iterate through all current touch locations to track touch events (e.g., press, move, release)
        foreach (TouchLocation location in CurrentTouchState)
        {
            switch (location.State)
            {
                case TouchLocationState.Pressed:
                    touchCount++; // Increment touch count on a new touch
                    lastCursorLocation = currentCursorLocation;

                    // Transform the touch location to the cursor's position
                    currentCursorLocation = TransformCursorLocation(location.Position);
                    break;
                case TouchLocationState.Moved:
                    // Handle touch movement (if necessary)
                    break;
                case TouchLocationState.Released:
                    // Handle touch release (if necessary)
                    break;
            }
        }

        // If the left mouse button is clicked, update the cursor position accordingly
        if (IsLeftMouseButtonClicked())
        {
            lastCursorLocation = currentCursorLocation;
            currentCursorLocation = TransformCursorLocation(new Vector2(CurrentMouseState.X, CurrentMouseState.Y));
            touchCount = 1;
        }

        // If the middle mouse button is clicked, set the touch count to 2 (for handling multiple inputs)
        if (IsMiddleMouseButtonClicked())
        {
            touchCount = 2;
        }

        // If the right mouse button is clicked, set the touch count to 3 (for handling multiple inputs)
        if (IsRightMoustButtonClicked())
        {
            touchCount = 3;
        }

        // Reset scroll wheel scroll flags
        isMouseWheelScrolledUp = false;
        isMouseWheelScrolledDown = false;

        // Detect scroll wheel movement and handle the scroll direction
        if (CurrentMouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue)
        {
            int scrollWheelDelta = CurrentMouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue;

            if (scrollWheelDelta > 0)
            {
                // Mouse wheel scrolled up
                isMouseWheelScrolledUp = true;
            }
            else if (scrollWheelDelta < 0)
            {
                // Mouse wheel scrolled down
                isMouseWheelScrolledDown = true;
            }
        }

        // Update the cursor location based on input from the GamePad left thumbstick or keyboard arrow keys
        // The cursor movement is adjusted by the elapsed time and the cursor speed factor
        float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Check if the GamePad is connected and update the cursor location based on the left thumbstick
        if (CurrentGamePadStates[0].IsConnected)
        {
            lastCursorLocation = currentCursorLocation;

            // Move the cursor based on the left thumbstick movement
            currentCursorLocation.X += CurrentGamePadStates[0].ThumbSticks.Left.X * elapsedTime * cursorMoveSpeed;
            currentCursorLocation.Y -= CurrentGamePadStates[0].ThumbSticks.Left.Y * elapsedTime * cursorMoveSpeed;
        }

        // Update the cursor location based on keyboard arrow key input
        if (CurrentKeyboardStates[0].IsKeyDown(Keys.Up))
        {
            currentCursorLocation.Y -= elapsedTime * cursorMoveSpeed;
        }
        if (CurrentKeyboardStates[0].IsKeyDown(Keys.Down))
        {
            currentCursorLocation.Y += elapsedTime * cursorMoveSpeed;
        }
        if (CurrentKeyboardStates[0].IsKeyDown(Keys.Left))
        {
            currentCursorLocation.X -= elapsedTime * cursorMoveSpeed;
        }
        if (CurrentKeyboardStates[0].IsKeyDown(Keys.Right))
        {
            currentCursorLocation.X += elapsedTime * cursorMoveSpeed;
        }

        // Clamp the cursor position to ensure it stays within the viewport boundaries
        currentCursorLocation.X = MathHelper.Clamp(currentCursorLocation.X, 0f, viewport.Width);
        currentCursorLocation.Y = MathHelper.Clamp(currentCursorLocation.Y, 0f, viewport.Height);
    }

    /// <summary>
    /// Checks whether the right mouse button was clicked (pressed down and then released) during the current frame.
    /// This is determined by comparing the current and last mouse states to detect a transition from the button being pressed to being released.
    ///
    /// <para>
    /// A "click" is considered to have occurred if the button was pressed in the previous frame (i.e., `LastMouseState`),
    /// and it is now released in the current frame (i.e., `CurrentMouseState`).
    /// </para>
    ///
    /// <returns>
    /// Returns true if the right mouse button was clicked (i.e., transitioned from pressed to released),
    /// otherwise returns false.
    /// </returns>
    internal bool IsRightMoustButtonClicked()
    {
        return CurrentMouseState.RightButton == ButtonState.Released
            && LastMouseState.RightButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks whether the middle mouse button was clicked (pressed down and then released) during the current frame.
    /// This is determined by comparing the current and last mouse states to detect a transition from the button being pressed to being released.
    ///
    /// <para>
    /// A "click" is considered to have occurred if the button was pressed in the previous frame (i.e., `LastMouseState`),
    /// and it is now released in the current frame (i.e., `CurrentMouseState`).
    /// </para>
    ///
    /// <returns>
    /// Returns true if the middle mouse button was clicked (i.e., transitioned from pressed to released),
    /// otherwise returns false.
    /// </returns>
    internal bool IsMiddleMouseButtonClicked()
    {
        return CurrentMouseState.MiddleButton == ButtonState.Released
            && LastMouseState.MiddleButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks whether the left mouse button was clicked (pressed down and then released) during the current frame.
    /// This is determined by comparing the current and last mouse states to detect a transition from the button being pressed to being released.
    ///
    /// <para>
    /// A "click" is considered to have occurred if the button was pressed in the previous frame (i.e., `LastMouseState`),
    /// and it is now released in the current frame (i.e., `CurrentMouseState`).
    /// </para>
    ///
    /// <returns>
    /// Returns true if the left mouse button was clicked (i.e., transitioned from pressed to released),
    /// otherwise returns false.
    /// </returns>
    internal bool IsLeftMouseButtonClicked()
    {
        return CurrentMouseState.LeftButton == ButtonState.Released
            && LastMouseState.LeftButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks if a specific key was pressed for the first time during the current frame, based on the given player index.
    /// This method detects a new key press by comparing the current and previous keyboard states for the specified player.
    /// It returns true if the key was not pressed in the previous frame but is pressed in the current frame.
    ///
    /// <para>
    /// If a specific player is given, only that player's keyboard state is checked. If no player is specified, the method will check
    /// all players' keyboard states to see if the key was pressed for the first time by any player.
    /// </para>
    ///
    /// <param name="key">The keyboard key to check for a new press (e.g., <see cref="Keys.Space"/>).</param>
    /// <param name="controllingPlayer">The index of the player whose input to check. If null, checks for any player.</param>
    /// <param name="playerIndex">The player index for which the key press was detected, if any. This output parameter is populated if a key press is detected.</param>
    /// <returns>
    /// Returns true if the key was pressed for the first time during the current frame by the specified player, otherwise false.
    /// </returns>
    public bool IsNewKeyPress(Keys key, PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
    {
        if (controllingPlayer.HasValue)
        {
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            int i = (int)playerIndex;

            // Check if the key was pressed down in the current frame and was released in the previous frame
            return CurrentKeyboardStates[i].IsKeyDown(key)
                && LastKeyboardStates[i].IsKeyUp(key);
        }
        else
        {
            // Accept input from any player.
            // Check for a new key press from each player
            return IsNewKeyPress(key, PlayerIndex.One, out playerIndex)
                || IsNewKeyPress(key, PlayerIndex.Two, out playerIndex)
                || IsNewKeyPress(key, PlayerIndex.Three, out playerIndex)
                || IsNewKeyPress(key, PlayerIndex.Four, out playerIndex);
        }
    }


    /// <summary>
    /// Checks if a specific gamepad button was pressed for the first time during the current frame, based on the given player index.
    /// This method detects a new button press by comparing the current and previous gamepad states for the specified player.
    /// It returns true if the button was not pressed in the previous frame but is pressed in the current frame.
    ///
    /// <para>
    /// If a specific player is given, only that player's gamepad state is checked. If no player is specified, the method will check
    /// all players' gamepad states to see if the button was pressed for the first time by any player.
    /// </para>
    ///
    /// <param name="button">The gamepad button to check for a new press (e.g., <see cref="Buttons.A"/>).</param>
    /// <param name="controllingPlayer">The index of the player whose input to check. If null, checks for any player.</param>
    /// <param name="playerIndex">The player index for which the button press was detected, if any. This output parameter is populated if a button press is detected.</param>
    /// <returns>
    /// Returns true if the button was pressed for the first time during the current frame by the specified player, otherwise false.
    /// </returns>
    public bool IsNewButtonPress(Buttons button, PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
    {
        if (controllingPlayer.HasValue)
        {
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            int i = (int)playerIndex;

            // Check if the button was pressed down in the current frame and was released in the previous frame
            return CurrentGamePadStates[i].IsButtonDown(button)
                && LastGamePadStates[i].IsButtonUp(button);
        }
        else
        {
            // Accept input from any player.
            // Check for a new button press from each player
            return IsNewButtonPress(button, PlayerIndex.One, out playerIndex)
                || IsNewButtonPress(button, PlayerIndex.Two, out playerIndex)
                || IsNewButtonPress(button, PlayerIndex.Three, out playerIndex)
                || IsNewButtonPress(button, PlayerIndex.Four, out playerIndex);
        }
    }

    /// <summary>
    /// Determines whether the "menu select" input has been triggered during the current frame.
    /// This method checks for common input actions (keyboard and gamepad) typically used to confirm or select items in menus.
    ///
    /// <para>
    /// The method supports input from both keyboard and gamepad:
    /// - Keyboard keys: <see cref="Keys.Space"/> and <see cref="Keys.Enter"/>
    /// - Gamepad buttons: <see cref="Buttons.A"/> and <see cref="Buttons.Start"/>
    /// </para>
    ///
    /// <para>
    /// If a specific player is given via the `controllingPlayer` parameter, only that player's input is checked.
    /// If no player is specified, the method will check all players' inputs for a menu select action.
    /// </para>
    ///
    /// </summary>
    /// <param name="controllingPlayer">
    /// The index of the player whose input to check. If null, input from all players is considered.
    /// </param>
    /// <param name="playerIndex">
    /// An output parameter that indicates which player's input triggered the menu select action, if any.
    /// </param>
    /// <returns>
    /// Returns true if a menu select action was detected during the current frame (from either keyboard or gamepad),
    /// otherwise false.
    /// </returns>
    public bool IsMenuSelected(PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
    {
        return IsNewKeyPress(Keys.Space, controllingPlayer, out playerIndex)
            || IsNewKeyPress(Keys.Enter, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.A, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
    }


    /// <summary>
    /// Determines whether the "menu cancel" input has been triggered during the current frame.
    /// This method checks for common input actions (keyboard and gamepad) typically used to cancel or back out of menus.
    ///
    /// <para>
    /// The method supports input from both keyboard and gamepad:
    /// - Keyboard key: <see cref="Keys.Escape"/>
    /// - Gamepad buttons: <see cref="Buttons.B"/> and <see cref="Buttons.Back"/>
    /// </para>
    ///
    /// <para>
    /// If a specific player is given via the `controllingPlayer` parameter, only that player's input is checked.
    /// If no player is specified, the method will check all players' inputs for a menu cancel action.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The index of the player whose input to check. If null, input from all players is considered.
    /// </param>
    /// <param name="playerIndex">
    /// An output parameter that indicates which player's input triggered the menu cancel action, if any.
    /// </param>
    /// <returns>
    /// Returns true if a menu cancel action was detected during the current frame (from either keyboard or gamepad),
    /// otherwise false.
    /// </returns>
    public bool IsMenuCanceled(PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
    {
        return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.B, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex);
    }

    /// <summary>
    /// Checks if a "menu up" input action has been triggered during the current frame.
    /// This action is typically used to navigate upwards in menus or UI lists.
    ///
    /// <para>
    /// The method supports input from various devices, including:
    /// - **Keyboard**: The <see cref="Keys.Up"/> key.
    /// - **Gamepad**: The <see cref="Buttons.DPadUp"/> or <see cref="Buttons.RightThumbstickUp"/> inputs.
    /// - **Mouse**: A scroll wheel movement in the upward direction.
    /// </para>
    ///
    /// <para>
    /// If a specific player is specified via the `controllingPlayer` parameter, only that player's input is checked.
    /// If no player is specified, input from all players and the mouse is considered.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The index of the player whose input to check. If null, input from all players is considered.
    /// </param>
    /// <returns>
    /// Returns true if a "menu up" action was detected during the current frame, otherwise false.
    /// </returns>
    public bool IsMenuUp(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Up, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.DPadUp, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.RightThumbstickUp, controllingPlayer, out playerIndex)
            || IsMouseWheelScrolledUp;
    }


    /// <summary>
    /// Checks if a "menu down" input action has been triggered during the current frame.
    /// This action is typically used to navigate upwards in menus or UI lists.
    ///
    /// <para>
    /// The method supports input from various devices, including:
    /// - **Keyboard**: The <see cref="Keys.Down"/> key.
    /// - **Gamepad**: The <see cref="Buttons.DPadDown"/> or <see cref="Buttons.LeftThumbstickDown"/> inputs.
    /// - **Mouse**: A scroll wheel movement in the downward direction.
    /// </para>
    ///
    /// <para>
    /// If a specific player is specified via the `controllingPlayer` parameter, only that player's input is checked.
    /// If no player is specified, input from all players and the mouse is considered.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The index of the player whose input to check. If null, input from all players is considered.
    /// </param>
    /// <returns>
    /// Returns true if a "menu down" action was detected during the current frame, otherwise false.
    /// </returns>
    public bool IsMenuDown(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Down, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.DPadDown, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.LeftThumbstickDown, controllingPlayer, out playerIndex)
            || IsMouseWheelScrolledDown;
    }


    /// <summary>
    /// Checks if a "pause the game" input action has been triggered during the current frame.
    /// This action is typically used to pause gameplay and display a pause menu or overlay.
    ///
    /// <para>
    /// The method supports input from various devices, including:
    /// - **Keyboard**: The <see cref="Keys.Escape"/> key.
    /// - **Gamepad**: The <see cref="Buttons.Back"/> or <see cref="Buttons.Start"/> buttons.
    /// - **Mouse or Touch Input**: Clicking or tapping within a specified rectangle (if provided).
    /// </para>
    ///
    /// <para>
    /// If a specific player is specified via the `controllingPlayer` parameter, only that player's input is checked.
    /// If no player is specified, input from all players is considered.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The index of the player whose input to check. If null, input from all players is considered.
    /// </param>
    /// <param name="rectangle">
    /// An optional rectangle representing a clickable UI area that can also trigger the pause action.
    /// If this is provided, the method checks if a click or tap occurred within the rectangle.
    /// </param>
    /// <returns>
    /// Returns true if a "pause the game" action was detected during the current frame, otherwise false.
    /// </returns>
    public bool IsPauseGame(PlayerIndex? controllingPlayer, Rectangle? rectangle = null)
    {
        PlayerIndex playerIndex;

        bool pointInRect = false;

        if (rectangle.HasValue)
        {
            pointInRect = IsUIClicked(rectangle.Value);
        }

        return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex)
            || pointInRect;
    }

    /// <summary>
    /// Determines if the player has triggered a "select next" action during the current frame.
    ///
    /// <para>
    /// This action is typically used to navigate to the next option in a menu or UI.
    /// The method detects input from both the keyboard and gamepad:
    /// - **Keyboard**: The <see cref="Keys.Right"/> key.
    /// - **Gamepad**: The <see cref="Buttons.DPadRight"/> button.
    /// </para>
    ///
    /// <para>
    /// If a specific player is specified in the `controllingPlayer` parameter, 
    /// the method checks input only for that player. If no player is specified, 
    /// input from all players is considered.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The player index to check for input. If null, input from all players is checked.
    /// </param>
    /// <returns>
    /// Returns true if a "select next" action is detected; otherwise, false.
    /// </returns>
    public bool IsSelectNext(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Right, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.DPadRight, controllingPlayer, out playerIndex);
    }

    /// <summary>
    /// Determines if the player has triggered a "select previous" action during the current frame.
    ///
    /// <para>
    /// This action is typically used to navigate to the previous option in a menu or UI.
    /// The method detects input from both the keyboard and gamepad:
    /// - **Keyboard**: The <see cref="Keys.Left"/> key.
    /// - **Gamepad**: The <see cref="Buttons.DPadLeft"/> button.
    /// </para>
    ///
    /// <para>
    /// If a specific player is specified in the `controllingPlayer` parameter,
    /// the method checks input only for that player. If no player is specified,
    /// input from all players is considered.
    /// </para>
    /// </summary>
    /// <param name="controllingPlayer">
    /// The player index to check for input. If null, input from all players is checked.
    /// </param>
    /// <returns>
    /// Returns true if a "select previous" action is detected; otherwise, false.
    /// </returns>
    public bool IsSelectPrevious(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Left, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.DPadLeft, controllingPlayer, out playerIndex);
    }

    /// <summary>
    /// Updates the input transformation matrix used to map input coordinates
    /// (such as mouse or touch positions) to a transformed coordinate space.
    ///
    /// <para>
    /// This method is typically used to adjust input processing when applying
    /// scaling, rotation, or translation transformations to the game viewport.
    /// </para>
    /// </summary>
    /// <param name="inputTransformation">
    /// A <see cref="Matrix"/> representing the transformation to apply to input coordinates.
    /// </param>
    internal void UpdateInputTransformation(Matrix inputTransformation)
    {
        this.inputTransformation = inputTransformation;
    }

    /// <summary>
    /// Transforms a given cursor location (e.g., mouse or touch position)
    /// based on the current input transformation matrix.
    ///
    /// <para>
    /// This is used to map raw input coordinates to the transformed coordinate space,
    /// ensuring proper alignment with the game world.
    /// </para>
    /// </summary>
    /// <param name="mousePosition">
    /// The raw input position in screen coordinates.
    /// </param>
    /// <returns>
    /// The transformed input position in the game’s logical coordinate space.
    /// </returns>
    public Vector2 TransformCursorLocation(Vector2 mousePosition)
    {
        // Transform back to cursor location
        return Vector2.Transform(mousePosition, inputTransformation);
    }

    /// <summary>
    /// Determines whether the user has clicked on a UI element within the specified rectangle.
    ///
    /// <para>
    /// This method checks if the cursor is within the given rectangle and detects
    /// either a left mouse button click or a touch event.
    /// </para>
    /// </summary>
    /// <param name="rectangle">
    /// A <see cref="Rectangle"/> representing the bounds of the UI element to check.
    /// </param>
    /// <returns>
    /// Returns true if the UI element is clicked; otherwise, false.
    /// </returns>
    internal bool IsUIClicked(Rectangle rectangle)
    {
        bool pointInRect = false;

        if (rectangle.Contains(CurrentCursorLocation)
            && (IsLeftMouseButtonClicked() || touchCount > 0))
        {
            pointInRect = true;
        }

        return pointInRect;
    }
}