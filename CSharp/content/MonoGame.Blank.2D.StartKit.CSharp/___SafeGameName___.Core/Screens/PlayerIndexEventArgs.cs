using System;
using Microsoft.Xna.Framework;

namespace ___SafeGameName___.Screens;

/// <summary>
/// Custom event argument that includes the index of the player who triggered the event.
/// This class is used by the <see cref="MenuEntry.Selected"/> event to identify which player
/// initiated the action.
/// </summary>
class PlayerIndexEventArgs : EventArgs
{
    private PlayerIndex playerIndex;

    /// <summary>
    /// Gets the index of the player who triggered this event.
    /// </summary>
    public PlayerIndex PlayerIndex
    {
        get { return playerIndex; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerIndexEventArgs"/> class with the specified player index.
    /// </summary>
    /// <param name="playerIndex">The index of the player who triggered the event.</param>
    public PlayerIndexEventArgs(PlayerIndex playerIndex)
    {
        this.playerIndex = playerIndex;
    }
}