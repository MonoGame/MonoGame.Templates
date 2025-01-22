﻿using System;
using System.Collections.Generic;
using System.IO;
using ___SafeGameName___.Core.Effects;
using ___SafeGameName___.Core.Inputs;
using ___SafeGameName___.Core.Localization;
using ___SafeGameName___.Core.Settings;
using ___SafeGameName___.ScreenManagers;
using ___SafeGameName___.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ___SafeGameName___.Core;

/// <summary>
/// A uniform grid of tiles with collections of gems and enemies.
/// The level owns the player and controls the game's win and lose
/// conditions as well as scoring.
/// </summary>
class Level : IDisposable
{
    // Physical structure of the level.
    private Tile[,] tiles;
    private Layer[] layers;

    // The layer which entities are drawn on top of.
    private const int EntityLayer = 2;

    Player player;
    // Entities in the level.
    public Player Player
    {
        get { return player; }
    }

    private List<Gem> gems = new List<Gem>();
    internal List<Gem> Gems { get => gems; set => gems = value; }

    private List<Enemy> enemies = new List<Enemy>();

    // Key locations in the level.        
    private Vector2 start;
    private Point exit = InvalidPosition;
    internal Point Exit { get => exit; set => exit = value; }
    private static readonly Point InvalidPosition = new Point(-1, -1);

    // Level game state.
    private Random random = new Random(354668); // Arbitrary, but constant seed

    public int Score => score;
    int score;

    bool reachedExit;
    public bool ReachedExit => reachedExit;


    TimeSpan timeTaken;
    public TimeSpan TimeTaken => timeTaken;

    private string levelPath;
    private TimeSpan maximumTimeToCompleteLevel = TimeSpan.FromMinutes(2.0);
    public TimeSpan MaximumTimeToCompleteLevel { get => maximumTimeToCompleteLevel; }

    private const int PointsPerSecond = 5;

    int gemsCollected;
    public int GemsCollected => gemsCollected;

    int gemsCount;
    public int GemsCount => gemsCount;

    bool newHighScore;
    public bool NewHighScore => newHighScore;

    private ScreenManager screenManager;
    ContentManager content;
    // Level content.        
    public ContentManager Content
    {
        get { return content; }
    }

    private SoundEffect exitReachedSound;

    private SpriteFont hudFont;

    // When the time remaining is less than the warning time, it blinks on the hud
    private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Width of level measured in tiles.
    /// </summary>
    public int Width => tiles.GetLength(0);

    /// <summary>
    /// Height of the level measured in tiles.
    /// </summary>
    public int Height => tiles.GetLength(1);

    private ParticleManager particleManager;
    private bool particlesExploding;

    public ParticleManager ParticleManager { get => particleManager; set => particleManager = value; }

    private SettingsManager<___SafeGameName___Leaderboard> settingsManager;
    private bool saved;
    private bool readyToPlay;

    // Backpack related variables
    private Texture2D backpack;
    private Vector2 backpackPosition;
    public Vector2 BackpackPosition => backpackPosition;

    private float cameraPosition;
    private Vector2 collectionPoint = new Vector2();

    public SettingsManager<___SafeGameName___Leaderboard> LeaderboardManager
    {
        get => settingsManager;

        set
        {
            if (value != null
                && settingsManager != value)
            {
                settingsManager = value;
                settingsManager.Load();
            }
        }
    }

    public bool Paused { get; internal set; }

    // The number of levels in the Levels directory of our content. We assume that
    // levels in our content are 0-based and that all numbers under this constant
    // have a level file present. This allows us to not need to check for the file
    // or handle exceptions, both of which can add unnecessary time to level loading.
    public const int NUMBER_OF_LEVELS = 5;
    private const int NUMBER_OF_LAYERS = 3;

    public event EventHandler<(Gem, Player)> GemCollected;

    /// <summary>
    /// Constructs a new level.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider that will be used to construct a ContentManager.
    /// </param>
    /// <param name="fileStream">
    /// A stream containing the tile data.
    /// </param>
    public Level(ScreenManager screenManager, string levelPath, int levelIndex)
    {
        this.screenManager = screenManager;

        // Create a new content manager to load content used just by this level.
        content = new ContentManager(this.screenManager.Game.Services, "Content");

        timeTaken = TimeSpan.Zero;
        this.levelPath = levelPath;

        using (Stream fileStream = TitleContainer.OpenStream(levelPath))
        {
            LoadTiles(fileStream);
        }

        // Load background layer textures.
        layers = new Layer[3];

        var textures0 = new Texture2D[3];
        for (int i = 0; i < 3; ++i)
        {
            textures0[i] = Content.Load<Texture2D>("Backgrounds/Layer0" + "_" + i);
        }
        layers[0] = new Layer(textures0, 0.2f);

        var textures1 = new Texture2D[3];
        for (int i = 0; i < 3; ++i)
        {
            textures1[i] = Content.Load<Texture2D>("Backgrounds/Layer1" + "_" + i);
        }
        layers[1] = new Layer(textures1, 0.5f);

        var textures2 = new Texture2D[3];
        for (int i = 0; i < 3; ++i)
        {
            textures2[i] = Content.Load<Texture2D>("Backgrounds/Layer2" + "_" + i);
        }
        layers[2] = new Layer(textures2, 0.8f);

        // Load sounds.
        exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");

        gemsCount = gems.Count;

        // Load font
        hudFont = content.Load<SpriteFont>("Fonts/Hud");

        // Our backpack to store the collected gems :) 
        backpack = content.Load<Texture2D>("Sprites/backpack");

        // Hook into the GemCollected event
        GemCollected += Level_GemCollected;
    }

    private void Level_GemCollected(object sender, (Gem gem, Player collectedBy) e)
    {
        score += e.gem.Value;

        e.gem.OnCollected(e.collectedBy);
    }

    /// <summary>
    /// Iterates over every tile in the structure file and loads its
    /// appearance and behavior. This method also validates that the
    /// file is well-formed with a player start point, exit, etc.
    /// </summary>
    /// <param name="fileStream">
    /// A stream containing the tile data.
    /// </param>
    private void LoadTiles(Stream fileStream)
    {
        // Load the level and ensure all of the lines are the same length.
        int width;
        List<string> lines = new List<string>();
        using (StreamReader reader = new StreamReader(fileStream))
        {
            string line = reader.ReadLine();
            width = line.Length;
            while (line != null)
            {
                lines.Add(line);
                if (line.Length != width)
                    throw new Exception(String.Format(Resources.ErrorLevelLineLength, lines.Count));
                line = reader.ReadLine();
            }
        }

        // Allocate the tile grid.
        tiles = new Tile[width, lines.Count];

        // Loop over every tile position,
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                // to load each tile.
                char tileType = lines[y][x];
                tiles[x, y] = LoadTile(tileType, x, y);
            }
        }

        // Verify that the level has a beginning and an end.
        if (Player == null)
            throw new NotSupportedException(Resources.ErrorLevelStartingPoint);
        if (exit == InvalidPosition)
            throw new NotSupportedException(Resources.ErrorLevelExit);

    }

    /// <summary>
    /// Loads an individual tile's appearance and behavior.
    /// </summary>
    /// <param name="tileType">
    /// The character loaded from the structure file which
    /// indicates what should be loaded.
    /// </param>
    /// <param name="x">
    /// The X location of this tile in tile space.
    /// </param>
    /// <param name="y">
    /// The Y location of this tile in tile space.
    /// </param>
    /// <returns>The loaded tile.</returns>
    private Tile LoadTile(char tileType, int x, int y)
    {
        switch (tileType)
        {
            // Blank space
            case '.':
                return new Tile(null, TileCollision.Passable);

            // Exit
            case 'X':
                return LoadExitTile(x, y);

            // Minimal value Gem
            case '1':
                return LoadGemTile(x, y, tileType);
            // Mediuam value Gem
            case '2':
                return LoadGemTile(x, y, tileType);
            // Maximum value Gem
            case '3':
                return LoadGemTile(x, y, tileType);
            // PowerUp Gem
            case '4':
                return LoadGemTile(x, y, tileType);

            // Floating platform
            case '-':
                return LoadTile("Platform", TileCollision.Platform);

            // Various enemy types
            case 'A':
            case 'B':
            case 'C':
            case 'D':
                return LoadEnemyTile(x, y, tileType);

            // Platform block
            case '~':
                return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

            // Passable block
            case ':':
                return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

            // Impassable block
            case '#':
                return LoadVarietyTile("BlockA", 7, TileCollision.Impassable);

            // Breakable block
            case ';':
                return LoadVarietyTile("BlockB", 2, TileCollision.Breakable);

            // Player 1 start point
            case 'P':
                return LoadStartTile(x, y);

            // Unknown tile type character
            default:
                throw new NotSupportedException(String.Format(Resources.ErrorUnsupportedTileType, tileType, x, y));
        }
    }

    /// <summary>
    /// Creates a new tile. The other tile loading methods typically chain to this
    /// method after performing their special logic.
    /// </summary>
    /// <param name="name">
    /// Path to a tile texture relative to the Content/Tiles directory.
    /// </param>
    /// <param name="collision">
    /// The tile collision type for the new tile.
    /// </param>
    /// <returns>The new tile.</returns>
    private Tile LoadTile(string name, TileCollision collision)
    {
        return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
    }

    /// <summary>
    /// Loads a tile with a random appearance.
    /// </summary>
    /// <param name="baseName">
    /// The content name prefix for this group of tile variations. Tile groups are
    /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
    /// </param>
    /// <param name="variationCount">
    /// The number of variations in this group.
    /// </param>
    private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
    {
        int index = random.Next(variationCount);
        return LoadTile(baseName + index, collision);
    }

    /// <summary>
    /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
    /// </summary>
    private Tile LoadStartTile(int x, int y)
    {
        if (Player != null)
            throw new NotSupportedException(Resources.ErrorLevelOneStartingPoint);

        start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
        player = new Player(this, start);

        return new Tile(null, TileCollision.Passable);
    }

    /// <summary>
    /// Remembers the location of the level's exit.
    /// </summary>
    private Tile LoadExitTile(int x, int y)
    {
        if (exit != InvalidPosition)
            throw new NotSupportedException(Resources.ErrorLevelOneExit);

        exit = GetBounds(x, y).Center;

        return LoadTile("Exit", TileCollision.Passable);
    }

    /// <summary>
    /// Instantiates an enemy and puts him in the level.
    /// </summary>
    private Tile LoadEnemyTile(int x, int y, char monsterType)
    {
        Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
        enemies.Add(new Enemy(this, position, "Monster" + monsterType));

        return new Tile(null, TileCollision.Passable);
    }

    /// <summary>
    /// Instantiates a gem and puts it in the level.
    /// </summary>
    private Tile LoadGemTile(int x, int y, char gemType)
    {
        Point position = GetBounds(x, y).Center;
        gems.Add(new Gem(this, new Vector2(position.X, position.Y), gemType, new Vector2(Width * Tile.Width, Height * Tile.Height)));

        return new Tile(null, TileCollision.Passable);
    }

    /// <summary>
    /// Unloads the level content.
    /// </summary>
    public void Dispose()
    {
        Content.Unload();
    }

    /// <summary>
    /// Gets the collision mode of the tile at a particular location.
    /// This method handles tiles outside of the levels boundries by making it
    /// impossible to escape past the left or right edges, but allowing things
    /// to jump beyond the top of the level and fall off the bottom.
    /// </summary>
    public TileCollision GetCollision(int x, int y)
    {
        // Prevent escaping past the level ends.
        if (x < 0 || x >= Width)
            return TileCollision.Impassable;
        // Allow jumping past the level top and falling through the bottom.
        if (y < 0 || y >= Height)
            return TileCollision.Passable;

        return tiles[x, y].Collision;
    }

    /// <summary>
    /// Gets the bounding rectangle of a tile in world space.
    /// </summary>
    public Rectangle GetBounds(int x, int y)
    {
        return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
    }

    /// <summary>
    /// Updates all objects in the world, performs collision between them,
    /// and handles the time limit with scoring.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="keyboardState">Provides a snapshot of timing values.</param>
    /// <param name="gamePadState">Provides a snapshot of timing values.</param>
    /// <param name="accelerometerState">Provides a snapshot of timing values.</param>
    /// <param name="displayOrientation">Provides a snapshot of timing values.</param>
    public void Update(
        GameTime gameTime,
        InputState inputState,
        DisplayOrientation displayOrientation,
        bool readyToPlay = true)
    {
        if (gameTime == null)
            throw new ArgumentNullException(nameof(gameTime));
        if (inputState == null)
            throw new ArgumentNullException(nameof(inputState));

        this.readyToPlay = readyToPlay;
        particleManager.Update(gameTime);

        if (ReachedExit
            && !particlesExploding)
        {
            particleManager.Position = Player.Position;
            particleManager.Emit(100, SettingsScreen.CurrentParticleEffect);
            particlesExploding = true;
        }

        // Pause while the player is dead or we've reached maximum time allowed.
        if (!Player.IsAlive || TimeTaken == MaximumTimeToCompleteLevel)
        {
            // Still want to perform physics on the player.
            Player.ApplyPhysics(gameTime);
        }
        else if (ReachedExit)
        {
            // If it's the MainMenu/Tutorial level, ignore stats and giving it a score.
            if (levelPath.Contains("00.txt"))
                return;

            if (!saved)
            {
                // We only flag a high score, if it's a faster time and all gems were collected.
                if (timeTaken < settingsManager.Settings.FastestTime
                    && gemsCollected == gemsCount)
                {
                    newHighScore = true;
                }

                if (newHighScore)
                {
                    // If it already exists update it, otherwise add it
                    if (settingsManager.Settings.FastestTime != timeTaken)
                    {
                        settingsManager.Settings.FastestTime = timeTaken;
                    }

                    if (settingsManager.Settings.GemsCollected < gemsCollected)
                    {
                        settingsManager.Settings.GemsCollected = gemsCollected;
                    }

                    if (!saved)
                    {
                        settingsManager.Save();
                        saved = true;
                    }
                }
            }
            // Animate the time being converted into points.
            int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
            seconds = Math.Min(seconds, (int)Math.Ceiling(TimeTaken.TotalSeconds));
            timeTaken += TimeSpan.FromSeconds(seconds);
            score += seconds * PointsPerSecond;
        }
        else
        {

            UpdateGems(gameTime);

            if (readyToPlay)
            {
                timeTaken += gameTime.ElapsedGameTime;

                Player.Update(gameTime, inputState, displayOrientation);

                // Parralax Scroll if necessary
                UpdateCamera(screenManager.BaseScreenSize);

                UpdateEnemies(gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    OnExitReached();
                }
            }
        }

        if (timeTaken > maximumTimeToCompleteLevel)
        {
            timeTaken = maximumTimeToCompleteLevel;
        }
    }

    /// <summary>
    /// Animates each gem and checks to allows the player to collect them.
    /// </summary>
    private void UpdateGems(GameTime gameTime)
    {
        // We don't recreate a new Vector2 object each frame, we just update it
        // Calculate the collectionPoint relative to the current camera view
        // This will help the gems track the backpack, as the camera moves.
        // Like a homing missle :)
        collectionPoint.X = cameraPosition + backpackPosition.X + (backpack.Width / 2);
        collectionPoint.Y = backpackPosition.Y + (backpack.Height / 2);

        for (int i = 0; i < gems.Count; ++i)
        {
            Gem gem = gems[i];

            gem.Update(gameTime, collectionPoint);

            switch (gem.State)
            {
                case GemState.Collected:
                    gems.RemoveAt(i--);
                    break;

                case GemState.Collecting:
                    break;

                case GemState.Waiting:
                    if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                    {
                        gemsCollected++;
                        gem.Scale = new Vector2(1.5f, 1.5f);
                        gem.State = GemState.Collecting;
                        OnGemCollected(gem, Player);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Animates each enemy and allow them to kill the player.
    /// </summary>
    private void UpdateEnemies(GameTime gameTime)
    {
        foreach (Enemy enemy in enemies)
        {
            enemy.Update(gameTime);

            if (enemy.IsAlive && enemy.BoundingRectangle.Intersects(Player.BoundingRectangle))
            {
                // Touching an enemy while having the power-up kills the enemy
                if (Player.IsPoweredUp)
                {
                    OnEnemyKilled(enemy, Player);
                }
                // Touching an enemy instantly kills the player
                else
                {
                    OnPlayerKilled(enemy);
                }
            }
        }
    }

    /// <summary>
    /// Called when a gem is collected.
    /// </summary>
    /// <param name="gem">The gem that was collected.</param>
    /// <param name="collectedBy">The player who collected this gem.</param>
    private void OnGemCollected(Gem gem, Player collectedBy)
    {
        // Call any associated events
        GemCollected?.Invoke(this, new(gem, collectedBy));
    }

    /// <summary>
    /// Called when the player is killed.
    /// </summary>
    /// <param name="killedBy">
    /// The enemy who killed the player. This is null if the player was not killed by an
    /// enemy, such as when a player falls into a hole.
    /// </param>
    private void OnPlayerKilled(Enemy killedBy)
    {
        Player.OnKilled(killedBy);
    }

    /// <summary>
    /// Called when the enemy is killed.
    /// </summary>
    /// <param name="enemy">
    /// The enemy who died.
    /// </param>
    /// <param name="killedBy">
    /// The player who killed the enemy. Could be used when we have extra players
    /// </param>
    private void OnEnemyKilled(Enemy enemy, Player killedBy)
    {
        enemy.OnKilled(killedBy);
    }

    /// <summary>
    /// Called when the player reaches the level's exit.
    /// </summary>
    private void OnExitReached()
    {
        Player.OnReachedExit();
        exitReachedSound.Play();
        reachedExit = true;
    }

    /// <summary>
    /// Restores the player to the starting point to try the level again.
    /// </summary>
    public void StartNewLife()
    {
        Player.Reset(start);
    }

    /// <summary>
    /// Draw everything in the level from background to foreground.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition, 0.0f, 0.0f);

        // Get the scale once at the start of the Draw method
        float transformScale = screenManager.GlobalTransformation.M11;

        // Draw background layers
        for (int i = 0; i <= EntityLayer; ++i)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, screenManager.GlobalTransformation);
            layers[i].Draw(gameTime, spriteBatch, cameraPosition / transformScale);
            spriteBatch.End();
        }

        // Draw main game elements
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, cameraTransform * screenManager.GlobalTransformation);

        DrawTiles(spriteBatch);

        float cameraRight = cameraPosition + screenManager.BaseScreenSize.X;

        foreach (Gem gem in gems)
        {
            // Draw visible gems
            if (IsInView(gem.Position.X, cameraPosition, cameraRight))
            {
                gem.Draw(gameTime, spriteBatch);
            }
        }

        Player.Draw(gameTime, spriteBatch);

        foreach (Enemy enemy in enemies)
        {
            // Draw visible enemies
            if (IsInView(enemy.Position.X, cameraPosition, cameraRight))
            {
                enemy.Draw(gameTime, spriteBatch);
            }
        }

        spriteBatch.End();

        // Draw foreground layers
        for (int i = EntityLayer + 1; i < layers.Length; ++i)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, screenManager.GlobalTransformation);
            layers[i].Draw(gameTime, spriteBatch, cameraPosition / transformScale);
            spriteBatch.End();
        }

        // Draw HUD
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, screenManager.GlobalTransformation);

        particleManager.Draw(spriteBatch);
        DrawHud(spriteBatch);

        spriteBatch.End();
    }

    private bool IsInView(float positionX, float cameraLeft, float cameraRight)
    {
        return positionX >= cameraLeft - Tile.Width
            && positionX <= cameraRight + Tile.Width;
    }

    /// <summary>
    /// Draws each tile in the level.
    /// </summary>
    private void DrawTiles(SpriteBatch spriteBatch)
    {
        // Calculate the visible range of tiles.
        int left = (int)Math.Floor(cameraPosition / Tile.Width);
        int right = (int)(left + screenManager.BaseScreenSize.X / Tile.Width);
        right = Math.Min(right, Width - 1);

        // Create the position variable just once. Less expensive to re-use the object
        // Than re-create it.
        var position = new Vector2();

        // For each tile position
        for (int y = 0; y < Height; ++y)
        {
            for (int x = left; x <= right; ++x)
            {
                // If there is a visible tile in that position
                Texture2D texture = tiles[x, y].Texture;
                if (texture != null)
                {
                    // Draw it in screen space.
                    position.X = x * Tile.Size.X;
                    position.Y = y * Tile.Size.Y;

                    spriteBatch.Draw(texture, position, Color.White);
                }
            }
        }
    }

    // BreakTile method should handle triggering its destruction animation
    internal void BreakTile(int x, int y)
    {
        RemoveTile(x, y);

        // Use Particle effect to explode the removed tile, above the player's head
        particleManager.Position = new Vector2(Player.Position.X, Player.Position.Y - 20);
        particleManager.Emit(50, ParticleEffectType.Confetti, Color.SandyBrown);
    }

    internal void RemoveTile(int x, int y)
    {
        // By making the tile passable with no nexture, it no longer "exists" in the game world
        // Thus making the level layout dynamic
        tiles[x, y] = new Tile(null, TileCollision.Passable);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="spriteBatch"></param>
    private void DrawHud(SpriteBatch spriteBatch)
    {
        // We need this check here so that only the backpack is drawn on the MainMenu
        // We need the backpack on the MainMenu so we can point to it during the tutorial.
        if (readyToPlay)
        {
            // Draw time taken
            string drawableString = Resources.Time +
            TimeTaken.Minutes.ToString("00") + ":" +
            TimeTaken.Seconds.ToString("00");
            Color timeColor = TimeTaken < MaximumTimeToCompleteLevel - WarningTime
                || ReachedExit
                || (int)TimeTaken.TotalSeconds % 2 == 0 ? Color.Yellow : Color.Red;

            DrawShadowedString(spriteBatch, hudFont, drawableString,
                               new Vector2(20, 20),
                               timeColor);

            // Draw score
            drawableString = Resources.Score + Score.ToString();
            Vector2 scoreDimensions = hudFont.MeasureString(drawableString);
            Vector2 scorePosition = new Vector2(
                screenManager.BaseScreenSize.X - scoreDimensions.X - 20,
                20
            );

            DrawShadowedString(spriteBatch, hudFont, drawableString, scorePosition, Color.Yellow);
        }

        // Draw backpack in the center
        backpackPosition = new Vector2(
            (screenManager.BaseScreenSize.X - backpack.Width) / 2,
            20
        );

        spriteBatch.Draw(backpack, backpackPosition, Color.White);
    }

    private void DrawShadowedString(SpriteBatch spriteBatch, SpriteFont font, string value, Vector2 position, Color color)
    {
        spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
        spriteBatch.DrawString(font, value, position, color);
    }

    const float ViewMargin = 0.35f;
    private void UpdateCamera(Vector2 viewport)
    {
        if (!readyToPlay || Player == null)
            return;

        // Calculate the edges of the screen.
        float marginWidth = viewport.X * ViewMargin;
        float marginLeft = cameraPosition + marginWidth;
        float marginRight = cameraPosition + viewport.X - marginWidth;

        // Calculate how far to scroll when the player is near the edges of the screen.
        float cameraMovement = 0.0f;
        if (Player.Position.X < marginLeft)
            cameraMovement = Player.Position.X - marginLeft;
        else if (Player.Position.X > marginRight)
            cameraMovement = Player.Position.X - marginRight;

        // Update the camera position, but prevent scrolling off the ends of the level.
        float maxCameraPosition = Tile.Width * Width - viewport.X;
        cameraPosition = MathHelper.Clamp(cameraPosition + cameraMovement, 0.0f, maxCameraPosition);
    }
}