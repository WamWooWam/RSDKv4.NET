using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RSDKv4;

public class Strings
{
    public string strPressStart;
    public string strTouchToStart;
    public string strStartGame;
    public string strTimeAttack;
    public string strAchievements;
    public string strLeaderboards;
    public string strHelpAndOptions;
    public string strSoundTest;
    public string str2PlayerVS;
    public string strSaveSelect;
    public string strPlayerSelect;
    public string strNoSave;
    public string strNewGame;
    public string strDelete;
    public string strDeleteMessage;
    public string strYes;
    public string strNo;
    public string strSonic;
    public string strTails;
    public string strKnuckles;
    public string strPause;
    public string strContinue;
    public string strRestart;
    public string strExit;
    public string strDevMenu;
    public string strRestartMessage;
    public string strExitMessage;
    public string strNSRestartMessage;
    public string strNSExitMessage;
    public string strExitGame;
    public string strNetworkMessage;
    public string[] strStageList = new string[16];
    public string[] strSaveStageList = new string[32];
    public string strNewBestTime;
    public string strRecords;
    public string strNextAct;
    public string strPlay;
    public string strTotalTime;
    public string strInstructions;
    public string strSettings;
    public string strStaffCredits;
    public string strAbout;
    public string strMusic;
    public string strSoundFX;
    public string strSpindash;
    public string strBoxArt;
    public string strControls;
    public string strOn;
    public string strOff;
    public string strCustomizeDPad;
    public string strDPadSize;
    public string strDPadOpacity;
    public string strHelpText1;
    public string strHelpText2;
    public string strHelpText3;
    public string strHelpText4;
    public string strHelpText5;
    public string strVersionName;
    public string strPrivacy;
    public string strTerms;

    public int stageStrCount = 0;

    private Dictionary<string, Dictionary<string, string>> stringDictionary
        = new Dictionary<string, Dictionary<string, string>>();

    private Engine Engine;
    private FileIO FileIO;

    public Strings()
    {
    }

    public void Initialize(Engine engine)
    {
        Engine = engine;
        FileIO = engine.FileIO;
    }

    public void InitLocalizedStrings()
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

    public string ReadLocalizedString(string key, string language, string path)
    {
        return stringDictionary[language].TryGetValue(key, out var v) ? v : null;
    }

    public void LoadStringList()
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
