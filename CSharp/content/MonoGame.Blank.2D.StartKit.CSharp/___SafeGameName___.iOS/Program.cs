﻿using ___SafeGameName___.Core;
using Foundation;
using UIKit;

namespace ___SafeGameName___.iOS;

[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    private static ___SafeGameName___Game game;

    internal static void RunGame()
    {
        game = new ___SafeGameName___Game();
        game.Run();
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication app)
    {
        RunGame();
    }
}