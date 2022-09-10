using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RSDKv4.Native;
using RSDKv4.Patches;
using RSDKv4.Utility;
using System;
using System.Threading;

#if !SILVERLIGHT
using System.Threading.Tasks;
#endif

namespace RSDKv4;

/// <summary>
/// This is the main type for your game
/// </summary>
public class RSDKv4Game : Game
{
    GraphicsDeviceManager graphics;
#if !NETSTANDARD1_6
    private Thread loadThread;
#else
    private Task loadTask;
#endif
    private bool isLoaded;

    public static float loadPercent = 0.0f;

    private bool needsResize = false;

    public const int WIDTH = 800;
    public const int HEIGHT = 480;

    private LoadingScreen loadingScreen;

    public RSDKv4Game()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreparingDeviceSettings += OnPreparingDeviceSettings;
        graphics.PreferredBackBufferWidth = WIDTH;
        graphics.PreferredBackBufferHeight = HEIGHT;
#if SILVERLIGHT
        graphics.IsFullScreen = true;
        graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#endif
#if FAST_PALETTE
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
#endif
        Content.RootDirectory = "Content";
        TargetElapsedTime = TimeSpan.FromTicks(166666);
        IsFixedTimeStep = true;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChange;

#if SILVERLIGHT
        System.Windows.Application.Current.UnhandledException += (o, e) =>
        {
            System.Windows.MessageBox.Show(e.ExceptionObject.ToString());
        };
#endif
    }

    private void OnClientSizeChange(object sender, EventArgs e)
    {
        needsResize = true;
    }

    private void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.One;
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        loadingScreen = new LoadingScreen(this, GraphicsDevice);
#if !NETSTANDARD1_6
        loadThread = new Thread(LoadRetroEngine);
        loadThread.Start();
#else
        loadTask = Task.Run(LoadRetroEngine);
#endif
    }

    private void LoadRetroEngine()
    {
        // new InputPlayer().Install(Engine.hooks);
        // new PaletteHack().Install(Engine.hooks);

        NativeRenderer.InitRenderDevice(this, GraphicsDevice);
        FastMath.CalculateTrigAngles();
        FileIO.CheckRSDKFile("DataS2.rsdk");

        if (Engine.LoadGameConfig("Data/Game/GameConfig.bin"))
        {
            loadPercent = 0.05f;
            if (Drawing.InitRenderDevice(this, GraphicsDevice))
            {
                loadPercent = 0.10f;
                if (Audio.InitAudioPlayback())
                {
                    // Objects.CreateNativeObject(() => new RetroGameLoop());

                    loadPercent = 0.85f;

                    Engine.SetGlobalVariableByName("options.saveSlot", 0);
                    Engine.SetGlobalVariableByName("options.gameMode", 1);
                    Engine.SetGlobalVariableByName("options.stageSelectFlag", 0);

                    Engine.SetGlobalVariableByName("player.lives", 69);
                    Engine.SetGlobalVariableByName("player.score", 0);
                    Engine.SetGlobalVariableByName("player.scoreBonus", 50000);

                    Engine.SetGlobalVariableByName("specialStage.emeralds", 0);
                    Engine.SetGlobalVariableByName("specialStage.listPos", 0);

                    Engine.SetGlobalVariableByName("stage.player2Enabled", 0);

                    Engine.SetGlobalVariableByName("lampPostID", 0); // For S1
                    Engine.SetGlobalVariableByName("starPostID", 0); // For S2

                    Engine.SetGlobalVariableByName("options.vsMode", 0);

                    Engine.SetGlobalVariableByName("specialStage.nextZone", 0);

                    Scene.InitFirstStage();
                    Script.ClearScriptData();
                    loadPercent = 0.9f;

                    Scene.InitStartingStage(STAGELIST.PRESENTATION, 1, 0);
                    loadPercent = 0.95f;

                    Scene.ProcessStage();
                    loadPercent = 1f;
                }
            }
        }

        isLoaded = true;
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent()
    {

    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (needsResize)
        {
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.ApplyChanges();

            Drawing.SetScreenDimensions(Window.ClientBounds.Width, Window.ClientBounds.Height);

            needsResize = false;
        }

        // Allows the game to exit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            this.Exit();

        if (!isLoaded)
            return;

        Input.ProcessInput();
        Scene.ProcessStage();
    }


    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        if (!isLoaded)
        {
            GraphicsDevice.Clear(Color.Black);
            loadingScreen.Draw(gameTime);
        }
        else
        {
            Engine.deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Drawing.Draw();
            Drawing.Present();

            // Objects.ProcessNativeObjects();
        }
    }
}