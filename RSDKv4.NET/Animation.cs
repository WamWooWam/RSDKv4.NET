using RSDKv4.Utility;

namespace RSDKv4;

public class Animation
{
    public const int ANIFILE_COUNT = 0x100;
    public const int ANIMATION_COUNT = 0x400;
    public const int SPRITEFRAME_COUNT = 0x1000;
    public const int HITBOX_COUNT = 0x20;
    public const int HITBOX_DIR_COUNT = 0x8;

    public static AnimationFile[] animationFileList = new AnimationFile[ANIFILE_COUNT];
    public static int animationFileCount;

    public static SpriteFrame[] scriptFrames = new SpriteFrame[SPRITEFRAME_COUNT];
    public static int scriptFrameCount;

    public static SpriteFrame[] animFrames = new SpriteFrame[SPRITEFRAME_COUNT];
    public static int animFrameCount;

    public static SpriteAnimation[] animationList = new SpriteAnimation[ANIMATION_COUNT];
    public static int animationCount;

    public static Hitbox[] hitboxList = new Hitbox[HITBOX_COUNT];
    public static int hitboxCount;

    static Animation()
    {
        ClearAnimationData();
    }

    public static void LoadAnimationFile(string filePath)
    {
        FileInfo info;
        if (FileIO.LoadFile(filePath, out info))
        {
            byte[] sheetIDs = new byte[0x18];
            sheetIDs[0] = 0;

            byte sheetCount = FileIO.ReadByte();

            for (int s = 0; s < sheetCount; ++s)
            {
                var sheetNameLen = FileIO.ReadByte();
                if (sheetNameLen != 0)
                {
                    var name = FileIO.ReadString(sheetNameLen);

                    FileIO.GetFileInfo(out info);
                    FileIO.CloseFile();
                    sheetIDs[s] = (byte)Drawing.AddGraphicsFile(name);
                    FileIO.SetFileInfo(info);
                }
            }

            var animCount = FileIO.ReadByte();
            var animFile = animationFileList[animationFileCount];
            animFile.animCount = animCount;
            animFile.animListOffset = animationCount;

            for (int a = 0; a < animCount; ++a)
            {
                var anim = animationList[animationCount++] = new SpriteAnimation();
                anim.frameListOffset = animFrameCount;
                anim.name = FileIO.ReadLengthPrefixedString();
                anim.frameCount = FileIO.ReadByte();
                anim.speed = FileIO.ReadByte();
                anim.loopPoint = FileIO.ReadByte();
                anim.rotationStyle = FileIO.ReadByte();

                for (int j = 0; j < anim.frameCount; ++j)
                {
                    var frame = animFrames[animFrameCount++] = new SpriteFrame();
                    var sheetId = FileIO.ReadByte();
                    frame.sheetId = sheetIDs[sheetId];
                    frame.hitboxId = FileIO.ReadByte();
                    frame.spriteX = FileIO.ReadByte();
                    frame.spriteY = FileIO.ReadByte();
                    frame.width = FileIO.ReadByte();
                    frame.height = FileIO.ReadByte();
                    frame.pivotX = FileIO.ReadSByte();
                    frame.pivotY = FileIO.ReadSByte();
                }

                // 90 Degree (Extra rotation Frames) rotation
                if (anim.rotationStyle == ROTSTYLE.STATICFRAMES)
                    anim.frameCount >>= 1;
            }

            animFile.hitboxListOffset = hitboxCount;
            var numHitboxes = FileIO.ReadByte();
            for (int i = 0; i < numHitboxes; ++i)
            {
                var hitbox = hitboxList[hitboxCount++] = new Hitbox();
                for (int d = 0; d < HITBOX_DIR_COUNT; ++d)
                {
                    hitbox.left[d] = FileIO.ReadSByte();
                    hitbox.top[d] = FileIO.ReadSByte();
                    hitbox.right[d] = FileIO.ReadSByte();
                    hitbox.bottom[d] = FileIO.ReadSByte();
                }
            }

            FileIO.CloseFile();
        }
    }

    public static AnimationFile AddAnimationFile(string filePath)
    {
        string path = "Data/Animations/" + filePath;

        for (int a = 0; a < 0x100; ++a)
        {
            if (animationFileList[a].fileName == null)
            {
                animationFileList[a] = new AnimationFile();
                animationFileList[a].fileName = filePath;
                LoadAnimationFile(path);
                ++animationFileCount;
                return animationFileList[a];
            }
            if (animationFileList[a].fileName == filePath)
                return animationFileList[a];
        }

        return null;
    }

    public static void ClearAnimationData()
    {
        Helpers.Memset(scriptFrames, () => new SpriteFrame());
        Helpers.Memset(animFrames, () => new SpriteFrame());
        Helpers.Memset(hitboxList, () => new Hitbox());
        Helpers.Memset(animationList, () => new SpriteAnimation());
        Helpers.Memset(animationFileList, () => new AnimationFile());

        scriptFrameCount = 0;
        animFrameCount = 0;
        animationCount = 0;
        animationFileCount = 0;
        hitboxCount = 0;

        // Used for pause menu
        Drawing.LoadGIFFile("Data/Game/SystemText.gif", Drawing.TEXTMENU_SURFACE);
    }

    public static void ProcessObjectAnimation(ObjectScript objectScript, Entity entity)
    {
        var sprAnim = animationList[objectScript.animFile.animListOffset + entity.animation];
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
}
