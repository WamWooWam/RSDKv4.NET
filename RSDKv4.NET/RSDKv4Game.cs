using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RSDKv4.Native;
using RSDKv4.Utility;

#if NO_THREADS
using System.Threading.Tasks;
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

    public const int WIDTH = 1280;
    public const int HEIGHT = 720;

    private LoadingScreen loadingScreen;

    public RSDKv4Game()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreparingDeviceSettings += OnPreparingDeviceSettings;
        graphics.PreferredBackBufferWidth = WIDTH;
        graphics.PreferredBackBufferHeight = HEIGHT;
        //graphics.PreferredBackBufferFormat = SurfaceFormat.Bgr565;
#if SILVERLIGHT || WINDOWS_UWP
        graphics.IsFullScreen = true;
        graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#endif
#if FAST_PALETTE
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
#endif
        Content.RootDirectory = "Content";
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        InactiveSleepTime = TimeSpan.FromSeconds(1.0);
        IsFixedTimeStep = true;

#if !SILVERLIGHT
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChange;
#endif
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

        FastMath.CalculateTrigAngles();
        if (!FileIO.CheckRSDKFile("DataS2u.rsdk"))
            FileIO.CheckRSDKFile("DataS2.rsdk");

        Strings.InitLocalizedStrings();
        SaveData.InitializeSaveRAM();

        var saveData = SaveData.saveGame;
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

            SaveData.WriteSaveRAMData();
        }

        NativeRenderer.InitRenderDevice(this, GraphicsDevice);

        if (Engine.LoadGameConfig("Data/Game/GameConfig.bin"))
        {
            loadPercent = 0.05f;
            if (Drawing.InitRenderDevice(this, GraphicsDevice))
            {
                loadPercent = 0.10f;
                if (Audio.InitAudioPlayback())
                {
                    Objects.CreateNativeObject(() => new RetroGameLoop());

                    loadPercent = 0.85f;

                    Engine.SetGlobalVariableByName("options.saveSlot", 0);
                    Engine.SetGlobalVariableByName("options.gameMode", 1);
                    Engine.SetGlobalVariableByName("options.stageSelectFlag", 0);

                    Engine.SetGlobalVariableByName("player.lives", saveFile.lives);
                    Engine.SetGlobalVariableByName("player.score", saveFile.score);
                    Engine.SetGlobalVariableByName("player.scoreBonus", saveFile.scoreBonus);

                    Engine.SetGlobalVariableByName("specialStage.emeralds", saveFile.emeralds);
                    Engine.SetGlobalVariableByName("specialStage.listPos", saveFile.specialStageId);

                    Engine.SetGlobalVariableByName("stage.player2Enabled", 0);

                    Engine.SetGlobalVariableByName("lampPostID", 0); // For S1
                    Engine.SetGlobalVariableByName("starPostID", 0); // For S2

                    Engine.SetGlobalVariableByName("options.vsMode", 0);

                    //Scene.InitFirstStage();

                    Script.ClearScriptData();
                    loadPercent = 0.9f;

                    if (newGame)
                    {
                        Scene.InitStartingStage(STAGELIST.PRESENTATION, 0, saveFile.characterId);
                    }
                    else if (saveFile.stageId >= 0x80)
                    {
                        Engine.SetGlobalVariableByName("specialStage.nextZone", saveFile.stageId - 0x81);
                        Scene.InitStartingStage(STAGELIST.SPECIAL, saveFile.specialStageId, saveFile.characterId);
                    }
                    else
                    {
                        Engine.SetGlobalVariableByName("specialStage.nextZone", saveFile.stageId - 1);
                        Scene.InitStartingStage(STAGELIST.REGULAR, saveFile.stageId - 1, saveFile.characterId);
                    }

                    // stage select
                    // Scene.InitStartingStage(STAGELIST.REGULAR, 0, 0);

                    loadPercent = 0.95f;

                    Scene.ProcessStage();

                    Engine.gameMode = ENGINE.MAINGAME;
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

        // Allows the game to exit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            this.Exit();

        if (!isLoaded)
            return;

        if (needsResize)
        {
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.ApplyChanges();

            Drawing.SetScreenDimensions(Window.ClientBounds.Width, Window.ClientBounds.Height);

            needsResize = false;
        }

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
            //Drawing.Draw();
            //Drawing.Present();

            Objects.ProcessNativeObjects();
        }

        base.Draw(gameTime);
    }
}