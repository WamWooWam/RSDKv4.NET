using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RSDKv4.Native;
using RSDKv4.Render;
using RSDKv4.Utility;

#if NO_THREADS
using System.Threading.Tasks;
#endif

#if XNA
using Microsoft.Xna.Framework.GamerServices;
#endif

namespace RSDKv4;

/// <summary>
/// This is the main type for your game
/// </summary>
public class RSDKv4Game : Game
{
    GraphicsDeviceManager graphics;
#if NO_THREADS
    private Task loadTask;
#else
    private Thread loadThread;
#endif
    private bool isLoaded;

    public static float loadPercent = 0.0f;

    private bool needsResize = false;

    public const int WIDTH = 800;
    public const int HEIGHT = 480;

    private LoadingScreen loadingScreen;
    private Engine engine;

    public RSDKv4Game()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreparingDeviceSettings += OnPreparingDeviceSettings;
        graphics.PreferredBackBufferWidth = WIDTH;
        graphics.PreferredBackBufferHeight = HEIGHT;
        //graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        //graphics.PreferredBackBufferFormat = SurfaceFormat.Bgr565;
#if SILVERLIGHT || WINDOWS_UWP
        graphics.IsFullScreen = true;
        graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#endif
#if FAST_PALETTE && !MONOGAME
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
        graphics.PreferMultiSampling = true;
#endif
        Content.RootDirectory = "Content";
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        InactiveSleepTime = TimeSpan.FromSeconds(1.0);
        IsFixedTimeStep = true;

#if !SILVERLIGHT
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChange;
#else
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
        e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.One;
#if FAST_PALETTE
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 4;
#endif
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
        this.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.One;
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        loadingScreen = new LoadingScreen(this, GraphicsDevice);
#if NO_THREADS
        loadTask = Task.Run(LoadRetroEngine);
#else
        loadThread = new Thread(LoadRetroEngine);
        loadThread.Start();
#endif
    }

    private void LoadRetroEngine()
    {
        // new InputPlayer().Install(Engine.hooks);
        // new PaletteHack().Install(Engine.hooks);
        engine = new Engine(this, GraphicsDevice);

        FastMath.CalculateTrigAngles();
        //if (!FileIO.CheckRSDKFile("Data.rsdk"))
        engine.FileIO.CheckRSDKFile("Data.rsdk");

        engine.Strings.InitLocalizedStrings();
        engine.SaveData.InitializeSaveRAM();

        var saveData = engine.SaveData.saveGame;
        var saveFile = saveData.files[0];

        var newGame = false;
        if (saveFile.stageId == 0)
        {
            newGame = true;
            saveFile.lives = int.MaxValue;
            saveFile.score = 0;
            saveFile.scoreBonus = 500000;
            saveFile.stageId = 1;
            saveFile.emeralds = 0;
            saveFile.specialStageId = 0;
            saveFile.characterId = 3;

            engine.SaveData.WriteSaveRAMData();
        }

        //NativeRenderer.InitRenderDevice(this, GraphicsDevice);

        if (engine.LoadGameConfig("Data/Game/GameConfig.bin"))
        {
            loadPercent = 0.05f;
            //if (engine.InitRenderDevice())
            //{
                loadPercent = 0.10f;
                if (engine.Audio.InitAudioPlayback())
                {
                    engine.Objects.CreateNativeObject(() => new TitleScreen());

                    loadPercent = 0.85f;

                    engine.SetGlobalVariableByName("options.saveSlot", 0);
                    engine.SetGlobalVariableByName("options.gameMode", 1);
                    engine.SetGlobalVariableByName("options.stageSelectFlag", 0);

                    engine.SetGlobalVariableByName("player.lives", saveFile.lives);
                    engine.SetGlobalVariableByName("player.score", saveFile.score);
                    engine.SetGlobalVariableByName("player.scoreBonus", saveFile.scoreBonus);

                    engine.SetGlobalVariableByName("specialStage.emeralds", saveFile.emeralds);
                    engine.SetGlobalVariableByName("specialStage.listPos", saveFile.specialStageId);

                    engine.SetGlobalVariableByName("stage.player2Enabled", 0);

                    engine.SetGlobalVariableByName("lampPostID", 0); // For S1
                    engine.SetGlobalVariableByName("starPostID", 0); // For S2

                    engine.SetGlobalVariableByName("options.vsMode", 0);

                    //Scene.InitFirstStage();

                    engine.Script.ClearScriptData();
                    loadPercent = 0.9f;

                    //if (newGame)
                    //{
                    //    Scene.InitStartingStage(STAGELIST.PRESENTATION, 0, saveFile.characterId);
                    //}
                    //else if (saveFile.stageId >= 0x80)
                    //{
                    //    Engine.SetGlobalVariableByName("specialStage.nextZone", saveFile.stageId - 0x81);
                    //    Scene.InitStartingStage(STAGELIST.SPECIAL, saveFile.specialStageId, saveFile.characterId);
                    //}
                    //else
                    //{
                    //    Engine.SetGlobalVariableByName("specialStage.nextZone", saveFile.stageId - 1);
                    //    Scene.InitStartingStage(STAGELIST.REGULAR, saveFile.stageId - 1, saveFile.characterId);
                    //}

                    // stage select
                    // Scene.InitStartingStage(STAGELIST.PRESENTATION, 0, 0);

                    loadPercent = 0.95f;

                    // Scene.ProcessStage();

                    engine.engineState = ENGINE_STATE.WAIT;
                    loadPercent = 1f;
                }
            //}
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

    private bool hold = false;

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Allows the game to exit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            this.Exit();

        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.O))
        {
            if (!hold)
            {
                hold = true;
                engine.Renderer.shaderNum--;
                if (engine.Renderer.shaderNum < 0)
                    engine.Renderer.shaderNum = 0;
            }
        }
        else if (keyboard.IsKeyDown(Keys.P))
        {
            if (!hold)
            {
                hold = true;
                engine.Renderer.shaderNum++;
                if (engine.Renderer.shaderNum > 4)
                    engine.Renderer.shaderNum = 4;
            }
        }
        else
        {
            hold = false;
        }

        if (keyboard.IsKeyDown(Keys.Escape))
        {
#if RETRO_USE_MOD_LOADER
                            // hacky patch because people can escape
                            if (Engine.gameMode == ENGINE_DEVMENU && stageMode == DEVMENU_MODMENU)
                                RefreshEngine();
#endif
            engine.Objects.ClearNativeObjects();
            engine.Objects.CreateNativeObject(() => new RetroGameLoop());
            if (engine.deviceType == DEVICE.MOBILE)
                engine.Objects.CreateNativeObject(() => new VirtualDPad());
            engine.engineState = ENGINE_STATE.INITDEVMENU;
        }

        if (needsResize)
        {
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.ApplyChanges();

            engine.Drawing.SetScreenDimensions(Window.ClientBounds.Width, Window.ClientBounds.Height);

            needsResize = false;
        }

        if (!isLoaded)
            return;

        if (IsActive)
            engine.Input.ProcessInput();
        if (engine.engineState == ENGINE_STATE.MAINGAME)
            engine.Scene.ProcessStage();
    }


    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (!isLoaded)
        {
            loadingScreen.Draw(gameTime);
        }
        else
        {
            engine.deltaTime = 1.0f / 60;
            //Drawing.Draw();
            //Drawing.Present();

            engine.Objects.ProcessNativeObjects();
        }

        base.Draw(gameTime);
    }
}