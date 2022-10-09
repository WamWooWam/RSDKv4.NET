using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RSDKv4.External;

namespace RSDKv4.Utility;

internal class MiniAnimationFile
{
    public int animCount;
    public MiniSpriteAnimation[] spriteAnimations;
}

internal class MiniSpriteAnimation
{
    public string name;
    public byte frameCount;
    public byte speed;
    public byte loopPoint;
    public byte rotationStyle;
    public MiniSpriteFrame[] spriteFrames;
}

internal class MiniSpriteFrame
{
    public int spriteX;
    public int spriteY;
    public int width;
    public int height;
    public int pivotX;
    public int pivotY;
    public string sheetName;
    public int hitboxId;
}

internal class MiniEntity
{
    public int x;
    public int y;

    public MiniSpriteAnimation animation;
    public MiniSpriteAnimation prevAnimation;

    public int animationSpeed;
    public int animationTimer;
    public int frame;

    public Texture2D texture;

    public void ProcessObjectAnimation()
    {
        MiniSpriteAnimation sprAnim = animation;

        if (animationSpeed <= 0)
        {
            animationTimer += sprAnim.speed;
        }
        else
        {
            if (animationSpeed > 0xF0)
                animationSpeed = 0xF0;
            animationTimer += animationSpeed;
        }

        if (animation != prevAnimation)
        {
            prevAnimation = animation;
            frame = 0;
            animationTimer = 0;
            animationSpeed = 0;
        }

        if (animationTimer >= 0xF0)
        {
            animationTimer -= 0xF0;
            ++frame;
        }

        if (frame >= sprAnim.frameCount)
            frame = sprAnim.loopPoint;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        ProcessObjectAnimation();

        var frame = animation.spriteFrames[this.frame];
        spriteBatch.Draw(texture,
            new Rectangle(x + frame.pivotX, y + frame.pivotY, (int)(frame.width * 2.0), (int)(frame.height * 2.0)),
            new Rectangle(frame.spriteX, frame.spriteY, frame.width, frame.height),
            Color.White);
    }
}


internal class MiniEngine
{
    private RSDKContainer container = new RSDKContainer();
    private RSDKFileStream rsdkStream;

    private byte[] readBuffer = new byte[8];

    private Game game;
    private SpriteBatch spriteBatch;
    private Texture2D texture;

    public MiniEngine(Game game, SpriteBatch spriteBatch, string rsdkFile)
    {
        this.game = game;
        this.spriteBatch = spriteBatch;

        var fileName = $"Content\\{rsdkFile}";
        var fileHandle = TitleContainer.OpenStream(fileName);
        var fileReader = new BinaryReader(fileHandle);

        if (!container.LoadRSDK(fileHandle, fileReader, fileName))
            throw new InvalidOperationException("Invalid RSDK!");

        rsdkStream = new RSDKFileStream(container, fileHandle);
    }

    public MiniAnimationFile LoadAniFile(string fileName)
    {
        MiniAnimationFile animFile = null;
        if (rsdkStream.LoadFile("Data/Animations/" + fileName, out _))
        {
            var sheetIDs = new List<string>();
            byte sheetCount = ReadByte();
            for (int s = 0; s < sheetCount; ++s)
            {
                var sheetNameLen = ReadByte();
                if (sheetNameLen != 0)
                {
                    sheetIDs.Add(ReadString(sheetNameLen));
                }
            }

            var animCount = ReadByte();
            animFile = new MiniAnimationFile();
            animFile.animCount = animCount;
            animFile.spriteAnimations = new MiniSpriteAnimation[animCount];

            for (int a = 0; a < animCount; ++a)
            {
                var anim = animFile.spriteAnimations[a] = new MiniSpriteAnimation();
                anim.name = ReadLengthPrefixedString();
                anim.frameCount = ReadByte();
                anim.speed = ReadByte();
                anim.loopPoint = ReadByte();
                anim.rotationStyle = ReadByte();
                anim.spriteFrames = new MiniSpriteFrame[anim.frameCount];

                for (int j = 0; j < anim.frameCount; ++j)
                {
                    var frame = anim.spriteFrames[j] = new MiniSpriteFrame();
                    var sheetId = ReadByte();
                    frame.sheetName = sheetIDs[sheetId];
                    frame.hitboxId = ReadByte();
                    frame.spriteX = ReadByte();
                    frame.spriteY = ReadByte();
                    frame.width = ReadByte();
                    frame.height = ReadByte();
                    frame.pivotX = ReadSByte();
                    frame.pivotY = ReadSByte();
                }

                // 90 Degree (Extra rotation Frames) rotation
                if (anim.rotationStyle == ROTSTYLE.STATICFRAMES)
                    anim.frameCount >>= 1;
            }
        }

        return animFile;
    }

    public void LoadAnimation(MiniSpriteAnimation spriteAnimation)
    {
        var toLoad = new Dictionary<string, object>();
        for (int i = 0; i < spriteAnimation.frameCount; i++)
            if (!toLoad.ContainsKey(spriteAnimation.spriteFrames[i].sheetName))
                toLoad.Add(spriteAnimation.spriteFrames[i].sheetName, new object());

        foreach (var spriteSheet in toLoad.Keys)
        {
            var reader = new GifReader();
            if (rsdkStream.LoadFile("Data/Sprites/" + spriteSheet, out _))
            {
                texture = reader.LoadGIFFile(game.GraphicsDevice, rsdkStream);
            }
        }
    }

    public MiniEntity CreateEntity(string animationFile, string anim)
    {
        var animation = LoadAniFile(animationFile);
        var sheet = animation.spriteAnimations.FirstOrDefault(a => a.name == anim);
        LoadAnimation(sheet);

        return new MiniEntity() { animation = sheet, frame = 0, texture = texture };
    }

    public byte ReadByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return readBuffer[0];
    }

    public sbyte ReadSByte()
    {
        rsdkStream.Read(readBuffer, 0, 1);
        return (sbyte)readBuffer[0];
    }

    public int ReadInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToInt32(readBuffer, 0);
    }

    public uint ReadUInt32()
    {
        rsdkStream.Read(readBuffer, 0, 4);
        return BitConverter.ToUInt32(readBuffer, 0);
    }

    public ushort ReadUInt16()
    {
        rsdkStream.Read(readBuffer, 0, 2);
        return BitConverter.ToUInt16(readBuffer, 0);
    }

    public string ReadLengthPrefixedString()
    {
        return ReadString(ReadByte());
    }

    public string ReadString(int length)
    {
        var buff = new char[length];
        for (int i = 0; i < length; i++)
            buff[i] = (char)ReadByte();

        return new string(buff);
    }
}
