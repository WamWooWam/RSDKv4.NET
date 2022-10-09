using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RSDKv4;

public static class Strings
{
    public static string strPressStart = "PRESS START";
    public static string strTouchToStart = "TOUCH TO START";
    public static string strStartGame = "START GAME";
    public static string strTimeAttack = "Time Attack";
    public static string strAchievements = "Achievements";
    public static string strLeaderboards = "Leaderboards";
    public static string strHelpAndOptions = "Help & Options";
    public static string strSoundTest = "Sound Test";
    public static string str2PlayerVS = "2P VS";
    public static string strSaveSelect = "Save Select";
    public static string strPlayerSelect = "Player Select";
    public static string strNoSave = "No Save Mode";
    public static string strNewGame = "New Game";
    public static string strDelete = "Delete";
    public static string strDeleteMessage = "Delete Message";
    public static string strYes = "Yes";
    public static string strNo = "No";
    public static string strSonic;
    public static string strTails;
    public static string strKnuckles;
    public static string strPause;
    public static string strContinue;
    public static string strRestart;
    public static string strExit;
    public static string strDevMenu;
    public static string strRestartMessage;
    public static string strExitMessage;
    public static string strNSRestartMessage;
    public static string strNSExitMessage;
    public static string strExitGame;
    public static string strNetworkMessage = "NETWORK UNAVAILABLE";
    public static string[] strStageList = new string[16];
    public static string[] strSaveStageList = new string[32];
    public static string strNewBestTime;
    public static string strRecords;
    public static string strNextAct;
    public static string strPlay;
    public static string strTotalTime;
    public static string strInstructions;
    public static string strSettings;
    public static string strStaffCredits;
    public static string strAbout;
    public static string strMusic;
    public static string strSoundFX;
    public static string strSpindash;
    public static string strBoxArt;
    public static string strControls;
    public static string strOn;
    public static string strOff;
    public static string strCustomizeDPad;
    public static string strDPadSize;
    public static string strDPadOpacity;
    public static string strHelpText1;
    public static string strHelpText2;
    public static string strHelpText3;
    public static string strHelpText4;
    public static string strHelpText5;
    public static string strVersionName;
    public static string strPrivacy;
    public static string strTerms;

    public static int stageStrCount = 0;

    private static Dictionary<string, Dictionary<string, string>> stringDictionary
        = new Dictionary<string, Dictionary<string, string>>();

    public static void InitLocalizedStrings()
    {
        var langStr = Engine.language switch
        {
            LANGUAGE.EN => "en",
            LANGUAGE.FR => "fr",
            LANGUAGE.IT => "it",
            LANGUAGE.DE => "de",
            LANGUAGE.ES => "es",
            LANGUAGE.JP => "jp",
            LANGUAGE.PT => "pt",
            LANGUAGE.RU => "ru",
            LANGUAGE.KO => "ko",
            LANGUAGE.ZH => "zh",
            LANGUAGE.ZS => "zs",
            _ => "en"
        };

        LoadStringList();

        strPressStart = ReadLocalizedString("PressStart", langStr, "Data/Game/StringList.txt");
        strTouchToStart = ReadLocalizedString("TouchToStart", langStr, "Data/Game/StringList.txt");
        strStartGame = ReadLocalizedString("StartGame", langStr, "Data/Game/StringList.txt");
        strTimeAttack = ReadLocalizedString("TimeAttack", langStr, "Data/Game/StringList.txt");
        strAchievements = ReadLocalizedString("Achievements", langStr, "Data/Game/StringList.txt");
        strLeaderboards = ReadLocalizedString("Leaderboards", langStr, "Data/Game/StringList.txt");
        strHelpAndOptions = ReadLocalizedString("HelpAndOptions", langStr, "Data/Game/StringList.txt");

        // SoundTest & StageTest, both unused
        strSoundTest = ReadLocalizedString("SoundTest", langStr, "Data/Game/StringList.txt");
        // strStageTest      = ReadLocalizedString("StageTest", langStr, "Data/Game/StringList.txt");

        str2PlayerVS = ReadLocalizedString("TwoPlayerVS", langStr, "Data/Game/StringList.txt");
        strSaveSelect = ReadLocalizedString("SaveSelect", langStr, "Data/Game/StringList.txt");
        strPlayerSelect = ReadLocalizedString("PlayerSelect", langStr, "Data/Game/StringList.txt");
        strNoSave = ReadLocalizedString("NoSave", langStr, "Data/Game/StringList.txt");
        strNewGame = ReadLocalizedString("NewGame", langStr, "Data/Game/StringList.txt");
        strDelete = ReadLocalizedString("Delete", langStr, "Data/Game/StringList.txt");
        strDeleteMessage = ReadLocalizedString("DeleteSavedGame", langStr, "Data/Game/StringList.txt");
        strYes = ReadLocalizedString("Yes", langStr, "Data/Game/StringList.txt");
        strNo = ReadLocalizedString("No", langStr, "Data/Game/StringList.txt");
        strSonic = ReadLocalizedString("Sonic", langStr, "Data/Game/StringList.txt");
        strTails = ReadLocalizedString("Tails", langStr, "Data/Game/StringList.txt");
        strKnuckles = ReadLocalizedString("Knuckles", langStr, "Data/Game/StringList.txt");
        strPause = ReadLocalizedString("Pause", langStr, "Data/Game/StringList.txt");
        strContinue = ReadLocalizedString("Continue", langStr, "Data/Game/StringList.txt");
        strRestart = ReadLocalizedString("Restart", langStr, "Data/Game/StringList.txt");
        strExit = ReadLocalizedString("Exit", langStr, "Data/Game/StringList.txt");
        strDevMenu = ReadLocalizedString("DevMenu", "en", "Data/Game/StringList.txt");
        strRestartMessage = ReadLocalizedString("RestartMessage", langStr, "Data/Game/StringList.txt");
        strExitMessage = ReadLocalizedString("ExitMessage", langStr, "Data/Game/StringList.txt");
        if (Engine.language == LANGUAGE.JP)
        {
            strNSRestartMessage = ReadLocalizedString("NSRestartMessage", "ja", "Data/Game/StringList.txt");
            strNSExitMessage = ReadLocalizedString("NSExitMessage", "ja", "Data/Game/StringList.txt");
        }
        else
        {
            strNSRestartMessage = ReadLocalizedString("RestartMessage", langStr, "Data/Game/StringList.txt");
            strNSExitMessage = ReadLocalizedString("ExitMessage", langStr, "Data/Game/StringList.txt");
        }
        strExitGame = ReadLocalizedString("ExitGame", langStr, "Data/Game/StringList.txt");
        strNetworkMessage = ReadLocalizedString("NetworkMessage", langStr, "Data/Game/StringList.txt");
        for (int i = 0; i < 16; ++i)
        {
            strStageList[i] = ReadLocalizedString($"StageName{i + 1}", "en", "Data/Game/StringList.txt");
        }

        stageStrCount = 0;
        for (int i = 0; i < 32; ++i)
        {
            strSaveStageList[i] = ReadLocalizedString($"SaveStageName{i + 1}", "en", "Data/Game/StringList.txt");
            if (strSaveStageList[i] == null)
                break;
            stageStrCount++;
        }
        strNewBestTime = ReadLocalizedString("NewBestTime", langStr, "Data/Game/StringList.txt");
        strRecords = ReadLocalizedString("Records", langStr, "Data/Game/StringList.txt");
        strNextAct = ReadLocalizedString("NextAct", langStr, "Data/Game/StringList.txt");
        strPlay = ReadLocalizedString("Play", langStr, "Data/Game/StringList.txt");
        strTotalTime = ReadLocalizedString("TotalTime", langStr, "Data/Game/StringList.txt");
        strInstructions = ReadLocalizedString("Instructions", langStr, "Data/Game/StringList.txt");
        strSettings = ReadLocalizedString("Settings", langStr, "Data/Game/StringList.txt");
        strStaffCredits = ReadLocalizedString("StaffCredits", langStr, "Data/Game/StringList.txt");
        strAbout = ReadLocalizedString("About", langStr, "Data/Game/StringList.txt");
        strMusic = ReadLocalizedString("Music", langStr, "Data/Game/StringList.txt");
        strSoundFX = ReadLocalizedString("SoundFX", langStr, "Data/Game/StringList.txt");
        strSpindash = ReadLocalizedString("SpinDash", langStr, "Data/Game/StringList.txt");
        strBoxArt = ReadLocalizedString("BoxArt", langStr, "Data/Game/StringList.txt");
        strControls = ReadLocalizedString("Controls", langStr, "Data/Game/StringList.txt");
        strOn = ReadLocalizedString("On", langStr, "Data/Game/StringList.txt");
        strOff = ReadLocalizedString("Off", langStr, "Data/Game/StringList.txt");
        strCustomizeDPad = ReadLocalizedString("CustomizeDPad", langStr, "Data/Game/StringList.txt");
        strDPadSize = ReadLocalizedString("DPadSize", langStr, "Data/Game/StringList.txt");
        strDPadOpacity = ReadLocalizedString("DPadOpacity", langStr, "Data/Game/StringList.txt");
        strHelpText1 = ReadLocalizedString("HelpText1", langStr, "Data/Game/StringList.txt");
        strHelpText2 = ReadLocalizedString("HelpText2", langStr, "Data/Game/StringList.txt");
        strHelpText3 = ReadLocalizedString("HelpText3", langStr, "Data/Game/StringList.txt");
        strHelpText4 = ReadLocalizedString("HelpText4", langStr, "Data/Game/StringList.txt");
        strHelpText5 = ReadLocalizedString("HelpText5", langStr, "Data/Game/StringList.txt");
        strVersionName = ReadLocalizedString("Version", langStr, "Data/Game/StringList.txt");
        strPrivacy = ReadLocalizedString("Privacy", langStr, "Data/Game/StringList.txt");
        strTerms = ReadLocalizedString("Terms", langStr, "Data/Game/StringList.txt");

        // strMoreGames         = ReadLocalizedString("MoreGames", langStr, "Data/Game/StringList.txt");

        // Video Filter options
        // strVideoFilter       = ReadLocalizedString("VideoFilter", langStr, "Data/Game/StringList.txt");
        // strSharp             = ReadLocalizedString("Sharp", langStr, "Data/Game/StringList.txt");
        // strSmooth            = ReadLocalizedString("Smooth", langStr, "Data/Game/StringList.txt");
        // strNostalgic         = ReadLocalizedString("Nostalgic", langStr, "Data/Game/StringList.txt");

        // Login With Facebook
        // strFBLogin = ReadLocalizedString("LoginWithFacebook", langStr, "Data/Game/StringList.txt");

        // Unused Control Modes
        // strControlMethod = ReadLocalizedString("ControlMethod", langStr, "Data/Game/StringList.txt");
        // strSwipeAndTap   = ReadLocalizedString("SwipeAndTap", langStr, "Data/Game/StringList.txt");
        // strVirtualDPad   = ReadLocalizedString("VirtualDPad", langStr, "Data/Game/StringList.txt");
    }

    public static string ReadLocalizedString(string key, string language, string path)
    {
        return stringDictionary[language].TryGetValue(key, out var v) ? v : null;
    }

    public static void LoadStringList()
    {
        if (FileIO.LoadFile("Data/Game/StringList.txt", out var info))
        {
            using var stream = FileIO.CreateFileStream();
            using var reader = new StreamReader(stream, Encoding.Unicode);

            string line;
            string locale = null;
            string name = null;
            StringBuilder text = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                if (line.IndexOf(":") == 2)
                {
                    locale = line.Substring(0, 2);
                    name = line.Substring(3, line.Length - 4);
                    continue;
                }

                if (locale == null || name == null) continue;

                if (String.Compare(line, "end string", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!stringDictionary.ContainsKey(locale))
                        stringDictionary[locale] = new Dictionary<string, string>();
                    var str = text.ToString();

                    stringDictionary[locale][name] = str.Substring(0, str.Length - 2);

                    locale = null;
                    name = null;
                    text.Clear();
                }
                else
                {
                    text.AppendLine(line.Substring(1));
                }
            }
        }
    }
}
