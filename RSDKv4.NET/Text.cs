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
}
