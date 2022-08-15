using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSDKv4;

public delegate void NativeFunction1();
public delegate void NativeFunction2(ref int param1, string param2);
public delegate void NativeFunction3(ref int param1, ref int param2);
public delegate void NativeFunction4(ref int param1, string param2, ref int param3, ref int param4);
public delegate void NativeFunction5(ref int param1, ref int param2, ref int param3, ref int param4);

public static class Engine
{
    public static string gameWindowText;
    public static string gameDescriptionText;

    public static string gamePlatform = "Mobile";
    public static string gameRenderType = "HW_RENDERING";
    public static string gameHapticsSetting = "USE_F_FEEDBACK";

    public static string gameVersion = "1.0.0";

    public static bool usingDataFile;
    public static bool usingBytecode;

    public static int objectCount;
    public static string[] objectNames;
    public static string[] scriptPaths;

    public static int globalVariablesCount;
    public static string[] globalVariableNames;
    public static int[] globalVariables;

    public static int globalSfxCount;
    public static string[] globalSfxNames;
    public static string[] globalSfxPaths;

    public static int playerCount;
    public static string[] playerNames;

    public static int[] stageListCount = new int[4];
    public static string[] stageListNames = new string[4] {
        "Presentation Stages",
        "Regular Stages",
        "Bonus Stages",
        "Special Stages",
    };

    public static SceneInfo[][] stageList = new SceneInfo[4][];

    public const int NATIVEFUNCTION_MAX = 0x10;

    public static int nativeFunctionCount;
    public static Delegate[] nativeFunctions = new Delegate[NATIVEFUNCTION_MAX];

    public static int gameMode;
    public static int deviceType;
    public static int language;

    public static bool trialMode = false;
    public static bool hapticsEnabled = false;
    public static bool onlineActive = false;

    public static float deltaTime = 0.016f;
    public static int globalBoxRegion = 2;

    public static bool LoadGameConfig(string filePath)
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

                // Special Stages are stored as cat 2 in file, but cat 3 in game :(
                if (i == 2)
                    cat = 3;
                else if (i == 3)
                    cat = 2;

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
            return true;
        }

        return false;
    }

    public static int GetGlobalVariableByName(string name)
    {
        for (int v = 0; v < globalVariablesCount; ++v)
        {
            if (name == globalVariableNames[v])
                return globalVariables[v];
        }
        return 0;
    }

    public static void SetGlobalVariableByName(string name, int value)
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

    public static int GetGlobalVariableID(string name)
    {
        for (int v = 0; v < globalVariablesCount; ++v)
        {
            if (name == globalVariableNames[v])
                return v;
        }
        return 0xFF;
    }

    public static void AddNativeFunction(string name, Delegate function)
    {
        if (nativeFunctionCount < nativeFunctions.Length)
        {
            SetGlobalVariableByName(name, nativeFunctionCount);
            nativeFunctions[nativeFunctionCount++] = function;
        }
    }
}
