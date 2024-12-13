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
    public const int MaxInputs = 4;

    // Current Inputstates
    public AccelerometerState CurrentAccelerometerState;
    public readonly GamePadState[] CurrentGamePadStates;
    public readonly KeyboardState[] CurrentKeyboardStates;
    public MouseState CurrentMouseState;
    private int touchCount;
    public TouchCollection CurrentTouchState;

    // Last Inputstates
    public AccelerometerState LastAccelerometerState;
    public readonly GamePadState[] LastGamePadStates;
    public readonly KeyboardState[] LastKeyboardStates;
    public MouseState LastMouseState;
    public TouchCollection LastTouchState;

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

    /// <summary>
    /// Constructs a new input state.
    /// </summary>
    public InputState()
    {
        CurrentKeyboardStates = new KeyboardState[MaxInputs];
        CurrentGamePadStates = new GamePadState[MaxInputs];

        LastKeyboardStates = new KeyboardState[MaxInputs];
        LastGamePadStates = new GamePadState[MaxInputs];

        if (___SafeGameName___Game.IsMobile)
        {
            TouchPanel.EnabledGestures = GestureType.Tap;
        }
        else if (___SafeGameName___Game.IsDesktop)
        {

        }
        else
        {
            // For now, we'll throw an exception if we don't know the platform
            throw new PlatformNotSupportedException();
        }
    }

    /// <summary>
    /// Reads the latest state of all the inputs.
    /// </summary>
    public void Update(GameTime gameTime, Viewport viewport)
    {
        CurrentAccelerometerState = Accelerometer.GetState();

        for (int i = 0; i < MaxInputs; i++)
        {
            LastKeyboardStates[i] = CurrentKeyboardStates[i];
            LastGamePadStates[i] = CurrentGamePadStates[i];

            CurrentKeyboardStates[i] = Keyboard.GetState();
            CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
        }

        LastMouseState = CurrentMouseState;
        CurrentMouseState = Mouse.GetState();

        touchCount = 0;
        LastTouchState = CurrentTouchState;
        CurrentTouchState = TouchPanel.GetState();

        Gestures.Clear();
        while (TouchPanel.IsGestureAvailable)
        {
            Gestures.Add(TouchPanel.ReadGesture());
        }

        foreach (TouchLocation location in CurrentTouchState)
        {
            switch (location.State)
            {
                case TouchLocationState.Pressed:
                    touchCount++;
                    lastCursorLocation = currentCursorLocation;

                    currentCursorLocation = TransformCursorLocation(location.Position);
                    break;
                case TouchLocationState.Moved:
                    break;
                case TouchLocationState.Released:
                    break;
            }
        }

        if (IsLeftMouseButtonClicked())
        {
            lastCursorLocation = currentCursorLocation;

            currentCursorLocation = TransformCursorLocation(new Vector2(CurrentMouseState.X, CurrentMouseState.Y));
            touchCount = 1;
        }

        if (IsMiddleMouseButtonClicked())
        {
            touchCount = 2;
        }

        if (IsRightMoustButtonClicked())
        {
            touchCount = 3;
        }

        isMouseWheelScrolledUp = false;
        isMouseWheelScrolledDown = false;

        if (CurrentMouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue)
        {
            int scrollWheelDelta = CurrentMouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue;

            // Handle the scroll wheel event based on the delta
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

        // Update the cursor location by listening for left thumbstick input on
        // the 1st GamePad and direction key input on the Keyboard, making sure to
        // keep the cursor inside the screen boundary
        float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (CurrentGamePadStates[0].IsConnected)
        {
            lastCursorLocation = currentCursorLocation;

            currentCursorLocation.X += CurrentGamePadStates[0].ThumbSticks.Left.X * elapsedTime * cursorMoveSpeed;
            currentCursorLocation.Y -= CurrentGamePadStates[0].ThumbSticks.Left.Y * elapsedTime * cursorMoveSpeed;
        }

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

        currentCursorLocation.X = MathHelper.Clamp(currentCursorLocation.X, 0f, viewport.Width);
        currentCursorLocation.Y = MathHelper.Clamp(currentCursorLocation.Y, 0f, viewport.Height);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal bool IsRightMoustButtonClicked()
    {
        return CurrentMouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal bool IsMiddleMouseButtonClicked()
    {
        return CurrentMouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal bool IsLeftMouseButtonClicked()
    {
        return CurrentMouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed;
    }


    /// <summary>
    /// Helper for checking if a key was newly pressed during this update. The
    /// controllingPlayer parameter specifies which player to read input for.
    /// If this is null, it will accept input from any player. When a keypress
    /// is detected, the output playerIndex reports which player pressed it.
    /// </summary>
    public bool IsNewKeyPress(Keys key, PlayerIndex? controllingPlayer,
                                        out PlayerIndex playerIndex)
    {
        if (controllingPlayer.HasValue)
        {
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            int i = (int)playerIndex;

            return (CurrentKeyboardStates[i].IsKeyDown(key) &&
                    LastKeyboardStates[i].IsKeyUp(key));
        }
        else
        {
            // Accept input from any player.
            return (IsNewKeyPress(key, PlayerIndex.One, out playerIndex) ||
                    IsNewKeyPress(key, PlayerIndex.Two, out playerIndex) ||
                    IsNewKeyPress(key, PlayerIndex.Three, out playerIndex) ||
                    IsNewKeyPress(key, PlayerIndex.Four, out playerIndex));
        }
    }


    /// <summary>
    /// Helper for checking if a button was newly pressed during this update.
    /// The controllingPlayer parameter specifies which player to read input for.
    /// If this is null, it will accept input from any player. When a button press
    /// is detected, the output playerIndex reports which player pressed it.
    /// </summary>
    public bool IsNewButtonPress(Buttons button, PlayerIndex? controllingPlayer,
                                                 out PlayerIndex playerIndex)
    {
        if (controllingPlayer.HasValue)
        {
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            int i = (int)playerIndex;

            return (CurrentGamePadStates[i].IsButtonDown(button) &&
                    LastGamePadStates[i].IsButtonUp(button));
        }
        else
        {
            // Accept input from any player.
            return (IsNewButtonPress(button, PlayerIndex.One, out playerIndex) ||
                    IsNewButtonPress(button, PlayerIndex.Two, out playerIndex) ||
                    IsNewButtonPress(button, PlayerIndex.Three, out playerIndex) ||
                    IsNewButtonPress(button, PlayerIndex.Four, out playerIndex));
        }
    }


    /// <summary>
    /// Checks for a "menu select" input action.
    /// The controllingPlayer parameter specifies which player to read input for.
    /// If this is null, it will accept input from any player. When the action
    /// is detected, the output playerIndex reports which player pressed it.
    /// </summary>
    public bool IsMenuSelect(PlayerIndex? controllingPlayer,
                             out PlayerIndex playerIndex)
    {
        return IsNewKeyPress(Keys.Space, controllingPlayer, out playerIndex) ||
               IsNewKeyPress(Keys.Enter, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.A, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
    }


    /// <summary>
    /// Checks for a "menu cancel" input action.
    /// The controllingPlayer parameter specifies which player to read input for.
    /// If this is null, it will accept input from any player. When the action
    /// is detected, the output playerIndex reports which player pressed it.
    /// </summary>
    public bool IsMenuCancel(PlayerIndex? controllingPlayer,
                             out PlayerIndex playerIndex)
    {
        return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.B, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex);
    }


    /// <summary>
    /// Checks for a "menu up" input action.
    /// The controllingPlayer parameter specifies which player to read
    /// input for. If this is null, it will accept input from any player.
    /// </summary>
    public bool IsMenuUp(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Up, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.DPadUp, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.LeftThumbstickUp, controllingPlayer, out playerIndex) ||
               IsMouseWheelScrolledUp;
    }


    /// <summary>
    /// Checks for a "menu down" input action.
    /// The controllingPlayer parameter specifies which player to read
    /// input for. If this is null, it will accept input from any player.
    /// </summary>
    public bool IsMenuDown(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Down, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.DPadDown, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.LeftThumbstickDown, controllingPlayer, out playerIndex) ||
               IsMouseWheelScrolledDown;
    }


    /// <summary>
    /// Checks for a "pause the game" input action.
    /// The controllingPlayer parameter specifies which player to read
    /// input for. If this is null, it will accept input from any player.
    /// </summary>
    public bool IsPauseGame(PlayerIndex? controllingPlayer, Rectangle? rectangle = null)
    {
        PlayerIndex playerIndex;

        bool pointInRect = false;

        if (rectangle.HasValue)
        {
            if (rectangle.Value.Contains(CurrentCursorLocation)
                && (IsLeftMouseButtonClicked() || touchCount > 0))
            {
                pointInRect = true;
            }
        }

        return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex)
            || IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex)
            || pointInRect;
    }

    /// <summary>
    /// Checks if player has selected next
    /// on either keyboard or gamepad.
    /// </summary>
    public bool IsSelectNext(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Right, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.DPadRight, controllingPlayer, out playerIndex);
    }

    /// <summary>
    /// Checks if player has selected previous
    /// on either keyboard or gamepad.
    /// </summary>
    public bool IsSelectPrevious(PlayerIndex? controllingPlayer)
    {
        PlayerIndex playerIndex;

        return IsNewKeyPress(Keys.Left, controllingPlayer, out playerIndex) ||
               IsNewButtonPress(Buttons.DPadLeft, controllingPlayer, out playerIndex);
    }

    internal void UpdateInputTransformation(Matrix inputTransformation)
    {
        this.inputTransformation = inputTransformation;
    }

    public Vector2 TransformCursorLocation(Vector2 mousePosition)
    {
        // Transform back to cursor location
        return Vector2.Transform(mousePosition, inputTransformation);
    }

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