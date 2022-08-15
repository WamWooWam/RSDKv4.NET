using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RSDKv4.Utility
{
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
            FileInfo info;
            MiniAnimationFile animFile = null;
            if (rsdkStream.LoadFile("Data/Animations/" + fileName, out info))
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
            var toLoad = new HashSet<string>();
            for (int i = 0; i < spriteAnimation.frameCount; i++)
                toLoad.Add(spriteAnimation.spriteFrames[i].sheetName);

            foreach (var spriteSheet in toLoad)
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
            var animation = LoadAniFile("Sonic.ani");
            var sheet = animation.spriteAnimations.FirstOrDefault(a => a.name == anim);
            LoadAnimation(sheet);

            return new MiniEntity() { animation = sheet, frame = 0 };
        }

        public void DrawEntity(MiniEntity entity)
        {
            ProcessObjectAnimation(entity);

            var frame = entity.animation.spriteFrames[entity.frame];
            spriteBatch.Draw(texture,
                new Rectangle(entity.x + frame.pivotX, entity.y + frame.pivotY, (int)(frame.width * 2.0), (int)(frame.height * 2.0)),
                new Rectangle(frame.spriteX, frame.spriteY, frame.width, frame.height),
                Color.White);
        }

        public void ProcessObjectAnimation(MiniEntity entity)
        {
            MiniSpriteAnimation sprAnim = entity.animation;

            if (entity.animationSpeed <= 0)
            {
                entity.animationTimer += sprAnim.speed;
            }
            else
            {
                if (entity.animationSpeed > 0xF0)
                    entity.animationSpeed = 0xF0;
                entity.animationTimer += entity.animationSpeed;
            }

            if (entity.animation != entity.prevAnimation)
            {
                entity.prevAnimation = entity.animation;
                entity.frame = 0;
                entity.animationTimer = 0;
                entity.animationSpeed = 0;
            }

            if (entity.animationTimer >= 0xF0)
            {
                entity.animationTimer -= 0xF0;
                ++entity.frame;
            }

            if (entity.frame >= sprAnim.frameCount)
                entity.frame = sprAnim.loopPoint;
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
}
