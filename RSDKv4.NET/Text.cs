
namespace RSDKv4;

public class Text
{
    public const int TEXTDATA_COUNT = 0x2800;
    public const int TEXTENTRY_COUNT = 0x200;
    public const int TEXTMENU_COUNT = 0x2;

    public static TextMenu[] gameMenu = new TextMenu[TEXTMENU_COUNT];

    static Text()
    {
        for (int i = 0; i < TEXTMENU_COUNT; i++)
            gameMenu[i] = new TextMenu();
    }

    public static void LoadTextFile(TextMenu menu, string filePath, byte mapCode)
    {
        if (FileIO.LoadFile(filePath, out var fileInfo))
        {
            menu.textDataPos = 0;
            menu.rowCount = 0;
            menu.entryStart[0] = 0;
            menu.entrySize[0] = 0;

            while (menu.textDataPos < TEXTDATA_COUNT && !FileIO.ReachedEndOfFile())
            {
                var data = FileIO.ReadByte();
                if (data != '\n')
                {
                    if (data == '\r')
                    {
                        menu.rowCount++;
                        menu.entryStart[menu.rowCount] = menu.textDataPos;
                        menu.entrySize[menu.rowCount] = 0;
                    }
                    else
                    {
                        menu.textData[menu.textDataPos++] = (char)data;
                        menu.entrySize[menu.rowCount]++;
                    }
                }
            }

            menu.rowCount++;
            FileIO.CloseFile();
        }
    }

    public static void SetupTextMenu(TextMenu menu, int rowCount)
    {
        menu.textDataPos = 0;
        menu.rowCount = (ushort)rowCount;
    }

    public static void AddTextMenuEntry(TextMenu menu, string text)
    {
        menu.entryStart[menu.rowCount] = menu.textDataPos;
        menu.entrySize[menu.rowCount] = 0;
        menu.entryHighlight[menu.rowCount] = false;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] != '\0')
            {
                menu.textData[menu.textDataPos++] = text[i];
                menu.entrySize[menu.rowCount]++;
                ++i;
            }
            else
            {
                break;
            }
        }
        menu.rowCount++;
    }

    public static void SetTextMenuEntry(TextMenu menu, string text, int rowID)
    {
        menu.entryStart[rowID] = menu.textDataPos;
        menu.entrySize[rowID] = 0;
        menu.entryHighlight[menu.rowCount] = false;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] != '\0')
            {
                menu.textData[menu.textDataPos++] = text[i];
                menu.entrySize[rowID]++;
                ++i;
            }
            else
            {
                break;
            }
        }
    }

    public static void EditTextMenuEntry(TextMenu menu, string text, int rowID)
    {
        int entryPos = menu.entryStart[rowID];
        menu.entrySize[rowID] = 0;
        menu.entryHighlight[menu.rowCount] = false;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] != '\0')
            {
                menu.textData[entryPos++] = text[i];
                menu.entrySize[rowID]++;
                ++i;
            }
            else
            {
                break;
            }
        }
    }

    public static void LoadConfigListText(TextMenu menu, int listNo)
    {
        if (true)
        {
            if (listNo == 0)
            {
                for (int i = 0; i < Engine.playerCount; i++)
                {
                    AddTextMenuEntry(menu, Engine.playerNames[i]);
                }
            }
            else
            {
                for (byte c = 1; c <= 4; ++c)
                {
                    if (listNo != c)
                        continue;

                    int cat = c - 1;
                    if (Engine.engineType == ENGINE_TYPE.STANDARD)
                    {
                        // Special Stages are stored as cat 2 in file, but cat 3 in game :(
                        // Except in Origins :((((
                        if (c == 3)
                            cat = 3;
                        else if (c == 4)
                            cat = 2;
                    }

                    int stageCnt = Engine.stageList[cat].Length;
                    for (byte s = 0; s < stageCnt; ++s)
                    {
                        AddTextMenuEntry(menu, Engine.stageList[cat][s].name);
                        menu.entryHighlight[menu.rowCount - 1] = Engine.stageList[cat][s].highlighted;
                    }
                }
            }
        }
        else
        {
            FileInfo info;
            //char strBuf[0x100];
            byte fileBuffer = 0;
            byte count = 0;
            byte strLen = 0;
            if (FileIO.LoadFile("Data/Game/GameConfig.bin", out info))
            {
                // Name
                FileIO.ReadLengthPrefixedString();

                // About
                FileIO.ReadLengthPrefixedString();

                byte[] buf = new byte[3];
                for (int c = 0; c < 0x60; ++c) FileIO.ReadFile(buf, 0, 3);

                // Object Names
                count = FileIO.ReadByte();
                for (byte o = 0; o < count; ++o)
                {
                    FileIO.ReadLengthPrefixedString();
                }

                // Script Paths
                for (byte s = 0; s < count; ++s)
                {
                    FileIO.ReadLengthPrefixedString();
                }

                // Variables
                count = FileIO.ReadByte();
                for (byte v = 0; v < count; ++v)
                {
                    // Var Name
                    FileIO.ReadLengthPrefixedString();

                    // Var Value
                    FileIO.ReadInt32();
                }

                // SFX Names
                count = FileIO.ReadByte();
                for (byte s = 0; s < count; ++s)
                {
                    FileIO.ReadLengthPrefixedString();
                }

                // SFX Paths
                for (byte s = 0; s < count; ++s)
                {
                    FileIO.ReadLengthPrefixedString();
                }

                // Players
                count = FileIO.ReadByte();
                for (byte p = 0; p < count; ++p)
                {
                    var name = FileIO.ReadLengthPrefixedString();

                    if (listNo == 0)
                    {
                        // Player List
                        AddTextMenuEntry(menu, name);
                        Engine.playerNames[menu.rowCount] = name;
                    }
                }

                // Categories
                int entryID = 0;
                for (byte c = 1; c <= 4; ++c)
                {
                    byte stageCnt = FileIO.ReadByte();
                    for (byte s = 0; s < stageCnt; ++s)
                    {
                        // Stage Folder
                        FileIO.ReadLengthPrefixedString();

                        // Stage ID
                        FileIO.ReadLengthPrefixedString();

                        // Stage Name
                        var name = FileIO.ReadLengthPrefixedString();

                        // IsHighlighted
                        var isHighlighted = FileIO.ReadByte();

                        if (listNo == c)
                        {
                            AddTextMenuEntry(menu, name);
                            menu.entryHighlight[menu.rowCount - 1] = isHighlighted != 0;
                        }
                    }
                }
                FileIO.CloseFile();

#if RETRO_USE_MOD_LOADER
        if (listNo == 0)
            Engine.LoadXMLPlayers(menu);
        else
            Engine.LoadXMLStages(menu, listNo);
#endif
            }
        }
    }
}
