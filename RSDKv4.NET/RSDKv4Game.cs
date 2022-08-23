using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using RSDKv4.External;
using System.IO;
using System.Diagnostics;
using RSDKv4.Utility;
using System.Threading;
using RSDKv4.Native;

namespace RSDKv4;

/// <summary>
/// This is the main type for your game
/// </summary>
public class RSDKv4Game : Game
{
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    private Texture2D texture;
    private MiniEngine miniEngine;
    private MiniEntity miniEntity;
    private Thread loadThread;
    private bool isLoaded;

    private AnimationFile sonicAni;
    private ObjectScript script;
    private Entity entity;

    public static float loadPercent = 0.0f;
    private MiniSpriteAnimation sheet;

    private bool needsResize = false;

    public RSDKv4Game()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreparingDeviceSettings += OnPreparingDeviceSettings;
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
        // TODO: Add your initialization logic here       
        base.Initialize();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        // Create a new SpriteBatch, which can be used to draw textures.
        spriteBatch = new SpriteBatch(GraphicsDevice);
        texture = new Texture2D(GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });

        miniEngine = new MiniEngine(this, spriteBatch, "Data.rsdk");
        miniEntity = miniEngine.CreateEntity("Sonic.ani", "Running");
        miniEntity.x = 710;
        miniEntity.y = 380;

        loadThread = new Thread(() =>
        {
            LoadRetroEngine();
            isLoaded = true;
        });

        loadThread.Start();

        // TODO: use this.Content to load your game content here
    }

    private void LoadRetroEngine()
    {
        NativeRenderer.InitRenderDevice(this, GraphicsDevice);
        FastMath.CalculateTrigAngles();
        FileIO.CheckRSDKFile("Data.rsdk");

        if (Engine.LoadGameConfig("Data/Game/GameConfig.bin"))
        {
            loadPercent = 0.05f;
            if (Renderer.InitRenderDevice(this, GraphicsDevice))
            {
                loadPercent = 0.10f;
                if (Audio.InitAudioPlayback())
                {
                    //Objects.CreateNativeObject(() => new RetroGameLoop());

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

                    Scene.InitStartingStage(STAGELIST.PRESENTATION, 5, 0);
                    loadPercent = 0.95f;

                    Scene.ProcessStage();
                    loadPercent = 1f;

                    Renderer.UpdateSurfaces();
                }
            }
        }
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent()
    {
        // TODO: Unload any non ContentManager content here
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

            Renderer.SetScreenDimensions(Window.ClientBounds.Width, Window.ClientBounds.Height);

            needsResize = false;
        }

        // Allows the game to exit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            this.Exit();

        if (!isLoaded) return;

        Drawing.indexCount = 0;
        Drawing.vertexCount = 0;

        Drawing.drawBlendStates[0] = new DrawBlendState();
        Drawing.drawBlendStateIdx = 0;

        Palette.activePalettes[0] = new PaletteEntry(0, 0, Renderer.SCREEN_YSIZE);
        Palette.activePaletteCount = 0;

        Input.ProcessInput();
        Scene.ProcessStage();
        Renderer.Draw();
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

            // var amyBlue = new Color(0x69, 0x6d, 0xb8);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
            miniEngine.DrawEntity(miniEntity);
            spriteBatch.Draw(texture, new Rectangle(0, 464, (int)(800 * loadPercent), 16), new Color(0x20, 0x40, 0xc0));
            spriteBatch.End();
        }
        else
        {
            Engine.deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Renderer.Present();

            // Objects.ProcessNativeObjects();
        }
    }
}