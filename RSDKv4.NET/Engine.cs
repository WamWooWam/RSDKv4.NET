using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.Native;
using RSDKv4.Render;

namespace RSDKv4;

public delegate void NativeFunction1();
public delegate void NativeFunction2(ref int param1, string param2);
public delegate void NativeFunction3(ref int param1, ref int param2);
public delegate void NativeFunction4(ref int param1, string param2, ref int param3, ref int param4);
public delegate void NativeFunction5(ref int param1, ref int param2, ref int param3, ref int param4);

public class Engine
{
    public string gameWindowText;
    public string gameDescriptionText;

    public string gamePlatform = "Mobile";
    public string gameRenderType = "HW_RENDERING";
    public string gameHapticsSetting = "USE_F_FEEDBACK";

    public string gameVersion = "1.0.0";

    public bool usingDataFile;
    public bool usingBytecode;

    public int objectCount;
    public string[] objectNames;
    public string[] scriptPaths;

    public int globalVariablesCount;
    public string[] globalVariableNames;
    public int[] globalVariables;

    public int globalSfxCount;
    public string[] globalSfxNames;
    public string[] globalSfxPaths;

    public int playerCount;
    public string[] playerNames;

    public int[] stageListCount = new int[4];
    public string[] stageListNames = new string[4] {
        "Presentation Stages",
        "Regular Stages",
        "Bonus Stages",
        "Special Stages",
    };

    public SceneInfo[][] stageList = new SceneInfo[4][];

    public const int NATIVEFUNCTION_MAX = 0x10;

    public int nativeFunctionCount;
    public Delegate[] nativeFunctions = new Delegate[NATIVEFUNCTION_MAX];

    public int engineState = ENGINE_STATE.WAIT;
#if SILVERLIGHT
    public int deviceType = DEVICE.MOBILE;
#else
    public int deviceType = DEVICE.STANDARD;
#endif
    public int language = LANGUAGE.EN;

    public bool trialMode = false;
    public bool hapticsEnabled = false;
    public bool onlineActive = true;

    public float deltaTime = 0.016f;

    public int globalBoxRegion = REGION.EU;
    public int gameType = GAME.SONIC1;
    public int engineType = ENGINE_TYPE.STANDARD;

    public Hooks hooks = new Hooks();

    public bool devMenu = false;
    public bool useHighResAssets = true;
    public bool skipStartMenu = false;
    public bool nativeMenuFadeIn = false;

    public int message = 0;

    public EngineRevision engineRevision = EngineRevision.Rev2;

    public readonly Animation Animation;
    public readonly Audio Audio;
    public readonly Collision Collision;
    public readonly Drawing Drawing;
    public readonly FileIO FileIO;
    public readonly Font Font;
    public readonly Input Input;
    public readonly Objects Objects;
    public readonly Palette Palette;
    public readonly SaveData SaveData;
    public readonly Scene Scene;
    public readonly Scene3D Scene3D;
    public readonly Script Script;
    public readonly Strings Strings;
    public readonly Text Text;
    public readonly DevMenu DevMenu;

    public readonly NativeRenderer Renderer;

    public Engine(Game game, GraphicsDevice graphicsDevice)
    {
        FileIO = new FileIO(this);

        Animation = new Animation();
        Audio = new Audio();
        Collision = new Collision();
        Drawing = new HardwareDrawing(game, graphicsDevice);
        Font = new Font();
        Input = new Input();
        Objects = new Objects();
        Palette = new Palette();
        SaveData = new SaveData();
        Scene = new Scene();
        Scene3D = new Scene3D();
        Script = new Script();
        Strings = new Strings();
        Text = new Text();
        DevMenu = new DevMenu();
        Renderer = new NativeRenderer();

        Renderer.Initialize(this);
        Renderer.InitRenderDevice(game, graphicsDevice);

        Animation.Initialize(this);
        Audio.Initialize(this);
        Collision.Initialize(this);
        Drawing.Initialize(this);
        Font.Initialize(this);
        Input.Initialize(this);
        Objects.Initialize(this);
        Palette.Initialize(this);
        //SaveData.Initialize(this);
        Scene.Initialize(this);
        Scene3D.Initialize(this);
        Script.Initialize(this);
        Strings.Initialize(this);
        Text.Initialize(this);
        DevMenu.Initialize(this);
        
    }

    public bool LoadGameConfig(string filePath)
    {
        FileInfo info;
        byte[] buffer = new byte[256];

        gameWindowText = "Retro-Engine";
        globalVariablesCount = 0;

        if (FileIO.LoadFile(filePath, out info))
        {
            gameWindowText = FileIO.ReadLengthPrefixedString();
            gameDescriptionText = FileIO.ReadLengthPrefixedString();

            for (int i = 0; i < 0x60; i++)
            {
                FileIO.ReadFile(buffer, 0, 3);
                Palette.SetPaletteEntry(255, (byte)i, buffer[0], buffer[1], buffer[2]);
            }

            objectCount = FileIO.ReadByte();
            objectNames = new string[objectCount];
            scriptPaths = new string[objectCount];
            for (int i = 0; i < objectCount; i++)
                objectNames[i] = FileIO.ReadLengthPrefixedString();
            for (int i = 0; i < objectCount; i++)
                scriptPaths[i] = FileIO.ReadLengthPrefixedString();

            globalVariablesCount = FileIO.ReadByte();
            globalVariableNames = new string[globalVariablesCount];
            globalVariables = new int[globalVariablesCount];

            for (int i = 0; i < globalVariablesCount; i++)
            {
                globalVariableNames[i] = FileIO.ReadLengthPrefixedString();
                globalVariables[i] = FileIO.ReadInt32();
            }

            globalSfxCount = FileIO.ReadByte();
            globalSfxNames = new string[globalSfxCount];
            globalSfxPaths = new string[globalSfxCount];
            for (int i = 0; i < globalSfxCount; i++)
                globalSfxNames[i] = FileIO.ReadLengthPrefixedString();
            for (int i = 0; i < globalSfxCount; i++)
                globalSfxPaths[i] = FileIO.ReadLengthPrefixedString();

            playerCount = FileIO.ReadByte();
            playerNames = new string[playerCount];
            for (int i = 0; i < playerCount; i++)
                playerNames[i] = FileIO.ReadLengthPrefixedString();

            for (int i = 0; i < 4; i++)
            {
                int cat = i;

                if (engineType == ENGINE_TYPE.STANDARD)
                {
                    // Special Stages are stored as cat 2 in file, but cat 3 in game :(
                    // Except in Origins :((((
                    if (i == 2)
                        cat = 3;
                    else if (i == 3)
                        cat = 2;

                }

                stageListCount[cat] = FileIO.ReadByte();
                stageList[cat] = new SceneInfo[stageListCount[cat]];

                for (int j = 0; j < stageListCount[cat]; j++)
                {
                    var sceneInfo = new SceneInfo();
                    sceneInfo.folder = FileIO.ReadLengthPrefixedString();
                    sceneInfo.id = FileIO.ReadLengthPrefixedString();
                    sceneInfo.name = FileIO.ReadLengthPrefixedString();
                    sceneInfo.highlighted = FileIO.ReadByte() != 0;

                    stageList[cat][j] = sceneInfo;
                }
            }

            FileIO.CloseFile();

            hooks.OnGameConfigLoaded();

            return true;
        }

        return false;
    }

    public int GetGlobalVariableByName(string name)
    {
        for (int v = 0; v < globalVariablesCount; ++v)
        {
            if (name == globalVariableNames[v])
                return globalVariables[v];
        }
        return -1;
    }

    public void SetGlobalVariableByName(string name, int value)
    {
        for (int v = 0; v < globalVariablesCount; ++v)
        {
            if (name == globalVariableNames[v])
            {
                globalVariables[v] = value;
                break;
            }
        }
    }

    public int GetGlobalVariableID(string name)
    {
        for (int v = 0; v < globalVariablesCount; ++v)
        {
            if (name == globalVariableNames[v])
                return v;
        }
        return -1;
    }

    public void AddNativeFunction(string name, Delegate function)
    {
        if (nativeFunctionCount < nativeFunctions.Length)
        {
            SetGlobalVariableByName(name, nativeFunctionCount);
            nativeFunctions[nativeFunctionCount++] = function;
        }
    }
}
