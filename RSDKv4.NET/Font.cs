using RSDKv4.Utility;
using System;
using System.Collections.Generic;

namespace RSDKv4;

public class Font
{
    public const int FONTLIST_CHAR_COUNT = 0x1000;
    public const int FONTLIST_COUNT = 0x4;

    public static BitmapFont[] fontList = new BitmapFont[FONTLIST_COUNT];

    static Font()
    {
        Helpers.Memset(fontList, () => new BitmapFont());
    }

    public static void LoadBitmapFont(string filePath, int index, int textureId)
    {
        var entry = fontList[index];
        if (entry.count == 0)
            entry.count = 2;

        if (FileIO.LoadFile(filePath, out _))
        {
            FileIO.ReadStringLine();
            var line = FileIO.ReadStringLine();

            // commonlineHeight=76base=62scaleW=1024scaleH=512pages=1packed=0
            var lineHeightPos = line.IndexOf("lineHeight=");
            var basePos = line.IndexOf("base=");
            var scaleWPos = line.IndexOf("scaleW=");

            var num = ParseInt(line, lineHeightPos, basePos, 11);
            if (fontList[index].lineHeight < 1.0)
                fontList[index].lineHeight = num;

            num = ParseInt(line, basePos, scaleWPos, 5);
            if (fontList[index].baseline < 1.0)
                fontList[index].baseline = num;

            FileIO.ReadStringLine();
            line = FileIO.ReadStringLine();

            var countPos = line.IndexOf("count=");
            var count = int.Parse(line.Substring(countPos + 6));

            int start = entry.count;
            entry.count += (ushort)count;

            for (int c = start; c < entry.count; ++c)
            {
                var character = fontList[index].characters[c];
                line = FileIO.ReadStringLine();

                int idPos = line.IndexOf("id=");
                int xPos = line.IndexOf("x=");
                int yPos = line.IndexOf("y=");
                int wPos = line.IndexOf("width=");
                int hPos = line.IndexOf("height=");
                int xOffPos = line.IndexOf("xoffset=");
                int yOffPos = line.IndexOf("yoffset=");
                int xAdvPos = line.IndexOf("xadvance=");
                int pagePos = line.IndexOf("page=");

                character.id = (ushort)ParseInt(line, idPos, xPos, 3);
                character.x = ParseInt(line, xPos, yPos, 2);
                character.y = ParseInt(line, yPos, wPos, 2);
                character.width = ParseInt(line, wPos, hPos, 6);
                character.height = ParseInt(line, hPos, xOffPos, 7);
                character.xOffset = ParseInt(line, xOffPos, yOffPos, 8);
                character.yOffset = ParseInt(line, yOffPos, xAdvPos, 8);
                character.xAdvance = ParseInt(line, xAdvPos, pagePos, 9);
                character.textureID = textureId;

                fontList[index].characters[c] = character;
            }

            FileIO.CloseFile();
        }
    }

    public static ushort[] GetCharactersForString(string str, int fontId)
    {
        var chars = new List<ushort>(str.Length);
        var font = fontList[fontId];

        for (int i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (c == '\n') continue;
            if (c == '\r')
            {
                chars.Add(1);
            }
            else
            {
                for (ushort j = 2; j < FONTLIST_CHAR_COUNT; j++)
                {
                    if (font.characters[j].id == (ushort)c)
                        chars.Add(j);
                }
            }
        }

        return chars.ToArray();
    }

    public static float GetTextWidth(ushort[] text, int fontId, float scaleX)
    {
        float width = 0.0f;
        float lineMax = 0.0f;
        float w = 0.0f;
        for (int i = 0; i < text.Length; i++)
        {
            var character = text[i];
            w += Font.fontList[fontId].characters[character].xAdvance;
            if (character == 1)
            {
                if (w > lineMax)
                    lineMax = w;
                w = 0.0f;
            }
        }

        width = Math.Max(w, lineMax);
        return width * scaleX;
    }

    public static float GetTextHeight(ushort[] text, int fontId, float scaleY)
    {
        float height = 0.0f;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == 1)
            {
                height += Font.fontList[fontId].lineHeight;
            }
        }
        return height * scaleY;
    }

    private static int ParseInt(string line, int firstPos, int nextPos, int len)
    {
        var pos = firstPos + len >= nextPos ? 0 : nextPos - len - firstPos;
        var str = line.Substring(firstPos + len, pos);
        if (!int.TryParse(str, out var num))
            num = 0;

        return num;
    }
}
