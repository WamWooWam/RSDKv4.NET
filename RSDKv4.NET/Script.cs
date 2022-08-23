#define RETRO_REV02

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using RSDKv4.Utility;

namespace RSDKv4;

public struct FunctionInfo
{
    public string name;
    public int opcodeSize;

    public FunctionInfo(string name, int size)
    {
        this.name = name;
        this.opcodeSize = size;
    }
}

public class Script
{
    public const int SCRIPTDATA_COUNT = (0x40000);
    public const int JUMPTABLE_COUNT = (0x4000);
    public const int FUNCTION_COUNT = (0x200);

    public const int JUMPSTACK_COUNT = (0x400);
    public const int FUNCSTACK_COUNT = (0x400);
    public const int FORSTACK_COUNT = (0x400);

    public static ObjectScript[] objectScriptList = new ObjectScript[Objects.OBJECT_COUNT];
    public static ScriptPtr[] functionScriptList = new ScriptPtr[FUNCTION_COUNT];

    public static int[] scriptData = new int[SCRIPTDATA_COUNT];
    public static int[] jumpTableData = new int[JUMPTABLE_COUNT];
    public static int[] jumpTableStack = new int[JUMPSTACK_COUNT];
    public static int[] functionStack = new int[FUNCSTACK_COUNT];
    public static int[] foreachStack = new int[FORSTACK_COUNT];

    public static int scriptCodePos = 0;
    public static int jumpTablePos = 0;
    public static int jumpTableStackPos = 0;
    public static int functionStackPos = 0;
    public static int foreachStackPos = 0;

    public static ScriptEngine scriptEng = new ScriptEngine();
    public static char[] scriptTextBuffer = new char[0x4000];

    public static int scriptDataPos = 0;
    public static int scriptDataOffset = 0;
    public static int jumpTableDataPos = 0;
    public static int jumpTableDataOffset = 0;

    public static readonly FunctionInfo[] functions = new[] {
        new FunctionInfo("End", 0),      // End of Script
        new FunctionInfo("Equal", 2),    // Equal
        new FunctionInfo("Add", 2),      // Add
        new FunctionInfo("Sub", 2),      // Subtract
        new FunctionInfo("Inc", 1),      // Increment
        new FunctionInfo("Dec", 1),      // Decrement
        new FunctionInfo("Mul", 2),      // Multiply
        new FunctionInfo("Div", 2),      // Divide
        new FunctionInfo("ShR", 2),      // Bit Shift Right
        new FunctionInfo("ShL", 2),      // Bit Shift Left
        new FunctionInfo("And", 2),      // Bitwise And
        new FunctionInfo("Or", 2),       // Bitwise Or
        new FunctionInfo("Xor", 2),      // Bitwise Xor
        new FunctionInfo("Mod", 2),      // Mod
        new FunctionInfo("FlipSign", 1), // Flips the Sign of the value

        new FunctionInfo("CheckEqual", 2),    // compare a=b, return result in CheckResult Variable
        new FunctionInfo("CheckGreater", 2),  // compare a>b, return result in CheckResult Variable
        new FunctionInfo("CheckLower", 2),    // compare a<b, return result in CheckResult Variable
        new FunctionInfo("CheckNotEqual", 2), // compare a!=b, return result in CheckResult Variable

        new FunctionInfo("IfEqual", 3),          // compare a=b, jump if condition met
        new FunctionInfo("IfGreater", 3),        // compare a>b, jump if condition met
        new FunctionInfo("IfGreaterOrEqual", 3), // compare a>=b, jump if condition met
        new FunctionInfo("IfLower", 3),          // compare a<b, jump if condition met
        new FunctionInfo("IfLowerOrEqual", 3),   // compare a<=b, jump if condition met
        new FunctionInfo("IfNotEqual", 3),       // compare a!=b, jump if condition met
        new FunctionInfo("else", 0),             // The else for an if statement
        new FunctionInfo("endif", 0),            // The end if

        new FunctionInfo("WEqual", 3),          // compare a=b, loop if condition met
        new FunctionInfo("WGreater", 3),        // compare a>b, loop if condition met
        new FunctionInfo("WGreaterOrEqual", 3), // compare a>=b, loop if condition met
        new FunctionInfo("WLower", 3),          // compare a<b, loop if condition met
        new FunctionInfo("WLowerOrEqual", 3),   // compare a<=b, loop if condition met
        new FunctionInfo("WNotEqual", 3),       // compare a!=b, loop if condition met
        new FunctionInfo("loop", 0),            // While Loop marker

        new FunctionInfo("ForEachActive", 3), // foreach loop, iterates through object group lists only if they are active and interaction is true
        new FunctionInfo("ForEachAll", 3),    // foreach loop, iterates through objects matching type
        new FunctionInfo("next", 0),          // foreach loop, next marker

        new FunctionInfo("switch", 2),    // Switch Statement
        new FunctionInfo("break", 0),     // break
        new FunctionInfo("endswitch", 0), // endswitch

            // Math Functions
        new FunctionInfo("Rand", 2),
        new FunctionInfo("Sin", 2),
        new FunctionInfo("Cos", 2),
        new FunctionInfo("Sin256", 2),
        new FunctionInfo("Cos256", 2),
        new FunctionInfo("ATan2", 3),
        new FunctionInfo("Interpolate", 4),
        new FunctionInfo("InterpolateXY", 7),

            // Graphics Functions
        new FunctionInfo("LoadSpriteSheet", 1),
        new FunctionInfo("RemoveSpriteSheet", 1),
        new FunctionInfo("DrawSprite", 1),
        new FunctionInfo("DrawSpriteXY", 3),
        new FunctionInfo("DrawSpriteScreenXY", 3),
        new FunctionInfo("DrawTintRect", 4),
        new FunctionInfo("DrawNumbers", 7),
        new FunctionInfo("DrawActName", 7),
        new FunctionInfo("DrawMenu", 3),
        new FunctionInfo("SpriteFrame", 6),
        new FunctionInfo("EditFrame", 7),
        new FunctionInfo("LoadPalette", 5),
        new FunctionInfo("RotatePalette", 4),
        new FunctionInfo("SetScreenFade", 4),
        new FunctionInfo("SetActivePalette", 3),
#if RETRO_REV00
        new FunctionInfo("SetPaletteFade", 7),
#else
        new FunctionInfo("SetPaletteFade", 6),
#endif
        new FunctionInfo("SetPaletteEntry", 3),
        new FunctionInfo("GetPaletteEntry", 3),
        new FunctionInfo("CopyPalette", 5),
        new FunctionInfo("ClearScreen", 1),
        new FunctionInfo("DrawSpriteFX", 4),
        new FunctionInfo("DrawSpriteScreenFX", 4),

            // More Useful Stuff
        new FunctionInfo("LoadAnimation", 1),
        new FunctionInfo("SetupMenu", 4),
        new FunctionInfo("AddMenuEntry", 3),
        new FunctionInfo("EditMenuEntry", 4),
        new FunctionInfo("LoadStage", 0),
        new FunctionInfo("DrawRect", 8),
        new FunctionInfo("ResetObjectEntity", 5),
        new FunctionInfo("BoxCollisionTest", 11),
        new FunctionInfo("CreateTempObject", 4),

            // Player and Animation Functions
        new FunctionInfo("ProcessObjectMovement", 0),
        new FunctionInfo("ProcessObjectControl", 0),
        new FunctionInfo("ProcessAnimation", 0),
        new FunctionInfo("DrawObjectAnimation", 0),

            // Music
        new FunctionInfo("SetMusicTrack", 3),
        new FunctionInfo("PlayMusic", 1),
        new FunctionInfo("StopMusic", 0),
        new FunctionInfo("PauseMusic", 0),
        new FunctionInfo("ResumeMusic", 0),
        new FunctionInfo("SwapMusicTrack", 4),

            // Sound FX
        new FunctionInfo("PlaySfx", 2),
        new FunctionInfo("StopSfx", 1),
        new FunctionInfo("SetSfxAttributes", 3),

            // More Collision Stuff
        new FunctionInfo("ObjectTileCollision", 4),
        new FunctionInfo("ObjectTileGrip", 4),

            // Bitwise Not
        new FunctionInfo("Not", 1),

            // 3D Stuff
        new FunctionInfo("Draw3DScene", 0),
        new FunctionInfo("SetIdentityMatrix", 1),
        new FunctionInfo("MatrixMultiply", 2),
        new FunctionInfo("MatrixTranslateXYZ", 4),
        new FunctionInfo("MatrixScaleXYZ", 4),
        new FunctionInfo("MatrixRotateX", 2),
        new FunctionInfo("MatrixRotateY", 2),
        new FunctionInfo("MatrixRotateZ", 2),
        new FunctionInfo("MatrixRotateXYZ", 4),
#if !RETRO_REV00
        new FunctionInfo("MatrixInverse", 1),
#endif
        new FunctionInfo("TransformVertices", 3),

        new FunctionInfo("CallFunction", 1),
        new FunctionInfo("return", 0),

        new FunctionInfo("SetLayerDeformation", 6),
        new FunctionInfo("CheckTouchRect", 4),
        new FunctionInfo("GetTileLayerEntry", 4),
        new FunctionInfo("SetTileLayerEntry", 4),

        new FunctionInfo("GetBit", 3),
        new FunctionInfo("SetBit", 3),

        new FunctionInfo("ClearDrawList", 1),
        new FunctionInfo("AddDrawListEntityRef", 2),
        new FunctionInfo("GetDrawListEntityRef", 3),
        new FunctionInfo("SetDrawListEntityRef", 3),

        new FunctionInfo("Get16x16TileInfo", 4),
        new FunctionInfo("Set16x16TileInfo", 4),
        new FunctionInfo("Copy16x16Tile", 2),
        new FunctionInfo("GetAnimationByName", 2),
        new FunctionInfo("ReadSaveRAM", 0),
        new FunctionInfo("WriteSaveRAM", 0),

#if RETRO_REV00 || RETRO_REV01
        new FunctionInfo("LoadFontFile", 1),
#endif
#if RETRO_REV02
        new FunctionInfo("LoadTextFile", 2),
#else
        new FunctionInfo("LoadTextFile", 3),
#endif
        new FunctionInfo("GetTextInfo", 5),
#if RETRO_REV00 || RETRO_REV01
        new FunctionInfo("DrawText", 7),
#endif
        new FunctionInfo("GetVersionNumber", 2),

        new FunctionInfo("GetTableValue", 3),
        new FunctionInfo("SetTableValue", 3),

        new FunctionInfo("CheckCurrentStageFolder", 1),
        new FunctionInfo("Abs", 1),

        new FunctionInfo("CallNativeFunction", 1),
        new FunctionInfo("CallNativeFunction2", 3),
        new FunctionInfo("CallNativeFunction4", 5),

        new FunctionInfo("SetObjectRange", 1),
#if !RETRO_REV00 || !RETRO_REV01
        new FunctionInfo("GetObjectValue", 3),
        new FunctionInfo("SetObjectValue", 3),
        new FunctionInfo("CopyObject", 3),
#endif
        new FunctionInfo("Print", 3),
    };
    public enum ScriptVarTypes { SCRIPTVAR = 1, SCRIPTINTCONST = 2, SCRIPTSTRCONST = 3 };
    public enum VARARR { NONE = 0, ARRAY = 1, ENTNOPLUS1 = 2, ENTNOMINUS1 = 3 };
    public enum VAR
    {
        TEMP0,
        TEMP1,
        TEMP2,
        TEMP3,
        TEMP4,
        TEMP5,
        TEMP6,
        TEMP7,
        CHECKRESULT,
        ARRAYPOS0,
        ARRAYPOS1,
        ARRAYPOS2,
        ARRAYPOS3,
        ARRAYPOS4,
        ARRAYPOS5,
        ARRAYPOS6,
        ARRAYPOS7,
        GLOBAL,
        LOCAL,
        OBJECTENTITYPOS,
        OBJECTGROUPID,
        OBJECTTYPE,
        OBJECTPROPERTYVALUE,
        OBJECTXPOS,
        OBJECTYPOS,
        OBJECTIXPOS,
        OBJECTIYPOS,
        OBJECTXVEL,
        OBJECTYVEL,
        OBJECTSPEED,
        OBJECTSTATE,
        OBJECTROTATION,
        OBJECTSCALE,
        OBJECTPRIORITY,
        OBJECTDRAWORDER,
        OBJECTDIRECTION,
        OBJECTINKEFFECT,
        OBJECTALPHA,
        OBJECTFRAME,
        OBJECTANIMATION,
        OBJECTPREVANIMATION,
        OBJECTANIMATIONSPEED,
        OBJECTANIMATIONTIMER,
        OBJECTANGLE,
        OBJECTLOOKPOSX,
        OBJECTLOOKPOSY,
        OBJECTCOLLISIONMODE,
        OBJECTCOLLISIONPLANE,
        OBJECTCONTROLMODE,
        OBJECTCONTROLLOCK,
        OBJECTPUSHING,
        OBJECTVISIBLE,
        OBJECTTILECOLLISIONS,
        OBJECTINTERACTION,
        OBJECTGRAVITY,
        OBJECTUP,
        OBJECTDOWN,
        OBJECTLEFT,
        OBJECTRIGHT,
        OBJECTJUMPPRESS,
        OBJECTJUMPHOLD,
        OBJECTSCROLLTRACKING,
        OBJECTFLOORSENSORL,
        OBJECTFLOORSENSORC,
        OBJECTFLOORSENSORR,
#if !RETRO_REV00
        OBJECTFLOORSENSORLC,
        OBJECTFLOORSENSORRC,
#endif
        OBJECTCOLLISIONLEFT,
        OBJECTCOLLISIONTOP,
        OBJECTCOLLISIONRIGHT,
        OBJECTCOLLISIONBOTTOM,
        OBJECTOUTOFBOUNDS,
        OBJECTSPRITESHEET,
        OBJECTVALUE0,
        OBJECTVALUE1,
        OBJECTVALUE2,
        OBJECTVALUE3,
        OBJECTVALUE4,
        OBJECTVALUE5,
        OBJECTVALUE6,
        OBJECTVALUE7,
        OBJECTVALUE8,
        OBJECTVALUE9,
        OBJECTVALUE10,
        OBJECTVALUE11,
        OBJECTVALUE12,
        OBJECTVALUE13,
        OBJECTVALUE14,
        OBJECTVALUE15,
        OBJECTVALUE16,
        OBJECTVALUE17,
        OBJECTVALUE18,
        OBJECTVALUE19,
        OBJECTVALUE20,
        OBJECTVALUE21,
        OBJECTVALUE22,
        OBJECTVALUE23,
        OBJECTVALUE24,
        OBJECTVALUE25,
        OBJECTVALUE26,
        OBJECTVALUE27,
        OBJECTVALUE28,
        OBJECTVALUE29,
        OBJECTVALUE30,
        OBJECTVALUE31,
        OBJECTVALUE32,
        OBJECTVALUE33,
        OBJECTVALUE34,
        OBJECTVALUE35,
        OBJECTVALUE36,
        OBJECTVALUE37,
        OBJECTVALUE38,
        OBJECTVALUE39,
        OBJECTVALUE40,
        OBJECTVALUE41,
        OBJECTVALUE42,
        OBJECTVALUE43,
        OBJECTVALUE44,
        OBJECTVALUE45,
        OBJECTVALUE46,
        OBJECTVALUE47,
        STAGESTATE,
        STAGEACTIVELIST,
        STAGELISTPOS,
        STAGETIMEENABLED,
        STAGEMILLISECONDS,
        STAGESECONDS,
        STAGEMINUTES,
        STAGEACTNUM,
        STAGEPAUSEENABLED,
        STAGELISTSIZE,
        STAGENEWXBOUNDARY1,
        STAGENEWXBOUNDARY2,
        STAGENEWYBOUNDARY1,
        STAGENEWYBOUNDARY2,
        STAGECURXBOUNDARY1,
        STAGECURXBOUNDARY2,
        STAGECURYBOUNDARY1,
        STAGECURYBOUNDARY2,
        STAGEDEFORMATIONDATA0,
        STAGEDEFORMATIONDATA1,
        STAGEDEFORMATIONDATA2,
        STAGEDEFORMATIONDATA3,
        STAGEWATERLEVEL,
        STAGEACTIVELAYER,
        STAGEMIDPOINT,
        STAGEPLAYERLISTPOS,
        STAGEDEBUGMODE,
        STAGEENTITYPOS,
        SCREENCAMERAENABLED,
        SCREENCAMERATARGET,
        SCREENCAMERASTYLE,
        SCREENCAMERAX,
        SCREENCAMERAY,
        SCREENDRAWLISTSIZE,
        SCREENXCENTER,
        SCREENYCENTER,
        SCREENXSIZE,
        SCREENYSIZE,
        SCREENXOFFSET,
        SCREENYOFFSET,
        SCREENSHAKEX,
        SCREENSHAKEY,
        SCREENADJUSTCAMERAY,
        TOUCHSCREENDOWN,
        TOUCHSCREENXPOS,
        TOUCHSCREENYPOS,
        MUSICVOLUME,
        MUSICCURRENTTRACK,
        MUSICPOSITION,
        INPUTDOWNUP,
        INPUTDOWNDOWN,
        INPUTDOWNLEFT,
        INPUTDOWNRIGHT,
        INPUTDOWNBUTTONA,
        INPUTDOWNBUTTONB,
        INPUTDOWNBUTTONC,
        INPUTDOWNBUTTONX,
        INPUTDOWNBUTTONY,
        INPUTDOWNBUTTONZ,
        INPUTDOWNBUTTONL,
        INPUTDOWNBUTTONR,
        INPUTDOWNSTART,
        INPUTDOWNSELECT,
        INPUTPRESSUP,
        INPUTPRESSDOWN,
        INPUTPRESSLEFT,
        INPUTPRESSRIGHT,
        INPUTPRESSBUTTONA,
        INPUTPRESSBUTTONB,
        INPUTPRESSBUTTONC,
        INPUTPRESSBUTTONX,
        INPUTPRESSBUTTONY,
        INPUTPRESSBUTTONZ,
        INPUTPRESSBUTTONL,
        INPUTPRESSBUTTONR,
        INPUTPRESSSTART,
        INPUTPRESSSELECT,
        MENU1SELECTION,
        MENU2SELECTION,
        TILELAYERXSIZE,
        TILELAYERYSIZE,
        TILELAYERTYPE,
        TILELAYERANGLE,
        TILELAYERXPOS,
        TILELAYERYPOS,
        TILELAYERZPOS,
        TILELAYERPARALLAXFACTOR,
        TILELAYERSCROLLSPEED,
        TILELAYERSCROLLPOS,
        TILELAYERDEFORMATIONOFFSET,
        TILELAYERDEFORMATIONOFFSETW,
        HPARALLAXPARALLAXFACTOR,
        HPARALLAXSCROLLSPEED,
        HPARALLAXSCROLLPOS,
        VPARALLAXPARALLAXFACTOR,
        VPARALLAXSCROLLSPEED,
        VPARALLAXSCROLLPOS,
        SCENE3DVERTEXCOUNT,
        SCENE3DFACECOUNT,
        SCENE3DPROJECTIONX,
        SCENE3DPROJECTIONY,
#if !RETRO_REV00
        SCENE3DFOGCOLOR,
        SCENE3DFOGSTRENGTH,
#endif
        VERTEXBUFFERX,
        VERTEXBUFFERY,
        VERTEXBUFFERZ,
        VERTEXBUFFERU,
        VERTEXBUFFERV,
        FACEBUFFERA,
        FACEBUFFERB,
        FACEBUFFERC,
        FACEBUFFERD,
        FACEBUFFERFLAG,
        FACEBUFFERCOLOR,
        SAVERAM,
        ENGINESTATE,
#if RETRO_REV00
    ENGINEMESSAGE,
#endif
        ENGINELANGUAGE,
        ENGINEONLINEACTIVE,
        ENGINESFXVOLUME,
        ENGINEBGMVOLUME,
        ENGINETRIALMODE,
        ENGINEDEVICETYPE,
        //#if RETRO_USE_HAPTICS
        HAPTICSENABLED,
        //#endif
        MAX_CNT
    }
    public enum FUNC
    {
        END,
        EQUAL,
        ADD,
        SUB,
        INC,
        DEC,
        MUL,
        DIV,
        SHR,
        SHL,
        AND,
        OR,
        XOR,
        MOD,
        FLIPSIGN,
        CHECKEQUAL,
        CHECKGREATER,
        CHECKLOWER,
        CHECKNOTEQUAL,
        IFEQUAL,
        IFGREATER,
        IFGREATEROREQUAL,
        IFLOWER,
        IFLOWEROREQUAL,
        IFNOTEQUAL,
        ELSE,
        ENDIF,
        WEQUAL,
        WGREATER,
        WGREATEROREQUAL,
        WLOWER,
        WLOWEROREQUAL,
        WNOTEQUAL,
        LOOP,
        FOREACHACTIVE,
        FOREACHALL,
        NEXT,
        SWITCH,
        BREAK,
        ENDSWITCH,
        RAND,
        SIN,
        COS,
        SIN256,
        COS256,
        ATAN2,
        INTERPOLATE,
        INTERPOLATEXY,
        LOADSPRITESHEET,
        REMOVESPRITESHEET,
        DRAWSPRITE,
        DRAWSPRITEXY,
        DRAWSPRITESCREENXY,
        DRAWTINTRECT,
        DRAWNUMBERS,
        DRAWACTNAME,
        DRAWMENU,
        SPRITEFRAME,
        EDITFRAME,
        LOADPALETTE,
        ROTATEPALETTE,
        SETSCREENFADE,
        SETACTIVEPALETTE,
        SETPALETTEFADE,
        SETPALETTEENTRY,
        GETPALETTEENTRY,
        COPYPALETTE,
        CLEARSCREEN,
        DRAWSPRITEFX,
        DRAWSPRITESCREENFX,
        LOADANIMATION,
        SETUPMENU,
        ADDMENUENTRY,
        EDITMENUENTRY,
        LOADSTAGE,
        DRAWRECT,
        RESETOBJECTENTITY,
        BOXCOLLISIONTEST,
        CREATETEMPOBJECT,
        PROCESSOBJECTMOVEMENT,
        PROCESSOBJECTCONTROL,
        PROCESSANIMATION,
        DRAWOBJECTANIMATION,
        SETMUSICTRACK,
        PLAYMUSIC,
        STOPMUSIC,
        PAUSEMUSIC,
        RESUMEMUSIC,
        SWAPMUSICTRACK,
        PLAYSFX,
        STOPSFX,
        SETSFXATTRIBUTES,
        OBJECTTILECOLLISION,
        OBJECTTILEGRIP,
        NOT,
        DRAW3DSCENE,
        SETIDENTITYMATRIX,
        MATRIXMULTIPLY,
        MATRIXTRANSLATEXYZ,
        MATRIXSCALEXYZ,
        MATRIXROTATEX,
        MATRIXROTATEY,
        MATRIXROTATEZ,
        MATRIXROTATEXYZ,
#if !RETRO_REV00
        MATRIXINVERSE,
#endif
        TRANSFORMVERTICES,
        CALLFUNCTION,
        RETURN,
        SETLAYERDEFORMATION,
        CHECKTOUCHRECT,
        GETTILELAYERENTRY,
        SETTILELAYERENTRY,
        GETBIT,
        SETBIT,
        CLEARDRAWLIST,
        ADDDRAWLISTENTITYREF,
        GETDRAWLISTENTITYREF,
        SETDRAWLISTENTITYREF,
        GET16X16TILEINFO,
        SET16X16TILEINFO,
        COPY16X16TILE,
        GETANIMATIONBYNAME,
        READSAVERAM,
        WRITESAVERAM,
#if RETRO_REV00 || RETRO_REV01
    LOADTEXTFONT,
#endif
        LOADTEXTFILE,
        GETTEXTINFO,
#if RETRO_REV00 || RETRO_REV01
    DRAWTEXT,
#endif
        GETVERSIONNUMBER,
        GETTABLEVALUE,
        SETTABLEVALUE,
        CHECKCURRENTSTAGEFOLDER,
        ABS,
        CALLNATIVEFUNCTION,
        CALLNATIVEFUNCTION2,
        CALLNATIVEFUNCTION4,
        SETOBJECTRANGE,
#if !RETRO_REV00 && !RETRO_REV01
        GETOBJECTVALUE,
        SETOBJECTVALUE,
        COPYOBJECT,
#endif
        PRINT,
        MAX_CNT
    }

    public static void LoadBytecode(int stageListID, int scriptID)
    {
        string scriptPath;
        switch (stageListID)
        {
            case STAGELIST.PRESENTATION:
            case STAGELIST.REGULAR:
            case STAGELIST.BONUS:
            case STAGELIST.SPECIAL:
                scriptPath = $"Bytecode/{Engine.stageList[stageListID][Scene.stageListPosition].folder}.bin";
                break;
            case 4: scriptPath = "Bytecode/GlobalCode.bin"; break;
            default:
                return;
        }

        if (FileIO.LoadFile(scriptPath, out var info))
        {
            byte fileBuffer = 0;
            int scrOffset = scriptCodePos;
            //int* scrData = &scriptData[scriptCodePos];
            fileBuffer = FileIO.ReadByte();
            int scriptCodeCount = fileBuffer;
            fileBuffer = FileIO.ReadByte();
            scriptCodeCount += (fileBuffer << 8);
            fileBuffer = FileIO.ReadByte();
            scriptCodeCount += (fileBuffer << 16);
            fileBuffer = FileIO.ReadByte();
            scriptCodeCount += (fileBuffer << 24);

            while (scriptCodeCount > 0)
            {
                fileBuffer = FileIO.ReadByte();
                int blockSize = fileBuffer & 0x7F;
                if (fileBuffer >= 0x80)
                {
                    while (blockSize > 0)
                    {
                        fileBuffer = FileIO.ReadByte();
                        int data = fileBuffer;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 8;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 16;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 24;
                        scriptData[scrOffset] = data;
                        ++scrOffset;
                        ++scriptCodePos;
                        --scriptCodeCount;
                        --blockSize;
                    }
                }
                else
                {
                    while (blockSize > 0)
                    {
                        fileBuffer = FileIO.ReadByte();
                        scriptData[scrOffset] = fileBuffer;
                        ++scrOffset;
                        ++scriptCodePos;
                        --scriptCodeCount;
                        --blockSize;
                    }
                }
            }

            int jumpTableOffset = jumpTablePos;
            //int* jumpPtr = &jumpTableData[jumpTablePos];
            fileBuffer = FileIO.ReadByte();
            int jumpDataCnt = fileBuffer;
            fileBuffer = FileIO.ReadByte();
            jumpDataCnt += fileBuffer << 8;
            fileBuffer = FileIO.ReadByte();
            jumpDataCnt += fileBuffer << 16;
            fileBuffer = FileIO.ReadByte();
            jumpDataCnt += fileBuffer << 24;

            while (jumpDataCnt > 0)
            {
                fileBuffer = FileIO.ReadByte();
                int blockSize = fileBuffer & 0x7F;
                if (fileBuffer >= 0x80)
                {
                    while (blockSize > 0)
                    {
                        fileBuffer = FileIO.ReadByte();
                        int data = fileBuffer;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 8;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 16;
                        fileBuffer = FileIO.ReadByte();
                        data += fileBuffer << 24;
                        jumpTableData[jumpTableOffset] = data;
                        ++jumpTableOffset;
                        ++jumpTablePos;
                        --jumpDataCnt;
                        --blockSize;
                    }
                }
                else
                {
                    while (blockSize > 0)
                    {
                        fileBuffer = FileIO.ReadByte();
                        jumpTableData[jumpTableOffset] = fileBuffer;
                        ++jumpTableOffset;
                        ++jumpTablePos;
                        --jumpDataCnt;
                        --blockSize;
                    }
                }
            }
            fileBuffer = FileIO.ReadByte();
            int scriptCount = fileBuffer;
            fileBuffer = FileIO.ReadByte();
            scriptCount += fileBuffer << 8;

            int objType = scriptID;
            for (int i = 0; i < scriptCount; ++i)
            {

                fileBuffer = FileIO.ReadByte();
                int buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType].eventMain.scriptCodePtr = buf + (fileBuffer << 24);

                fileBuffer = FileIO.ReadByte();
                buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType].eventDraw.scriptCodePtr = buf + (fileBuffer << 24);

                fileBuffer = FileIO.ReadByte();
                buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType++].eventStartup.scriptCodePtr = buf + (fileBuffer << 24);
            }

            objType = scriptID;
            for (int i = 0; i < scriptCount; ++i)
            {
                fileBuffer = FileIO.ReadByte();
                int buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType].eventMain.jumpTablePtr = buf + (fileBuffer << 24);

                fileBuffer = FileIO.ReadByte();
                buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType].eventDraw.jumpTablePtr = buf + (fileBuffer << 24);

                fileBuffer = FileIO.ReadByte();
                buf = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                buf += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                objectScriptList[objType++].eventStartup.jumpTablePtr = buf + (fileBuffer << 24);
            }

            fileBuffer = FileIO.ReadByte();
            int functionCount = fileBuffer;
            fileBuffer = FileIO.ReadByte();
            functionCount += fileBuffer << 8;

            for (int i = 0; i < functionCount; ++i)
            {
                fileBuffer = FileIO.ReadByte();
                int scrPos = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                scrPos += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                scrPos += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                functionScriptList[i].scriptCodePtr = scrPos + (fileBuffer << 24);
            }

            for (int i = 0; i < functionCount; ++i)
            {
                fileBuffer = FileIO.ReadByte();
                int jmpPos = fileBuffer;
                fileBuffer = FileIO.ReadByte();
                jmpPos += (fileBuffer << 8);
                fileBuffer = FileIO.ReadByte();
                jmpPos += (fileBuffer << 16);
                fileBuffer = FileIO.ReadByte();
                functionScriptList[i].jumpTablePtr = jmpPos + (fileBuffer << 24);
            }

            FileIO.CloseFile();
        }
    }

    public static void ProcessScript(int scriptCodePtr, int jumpTablePtr, byte scriptEvent)
    {
        bool running = true;
        int scriptDataPtr = scriptCodePtr;
        // int jumpTableDataPtr = jumpTablePtr;
        jumpTableStackPos = 0;
        functionStackPos = 0;
        foreachStackPos = 0;

        while (running)
        {
            int opcode = scriptData[scriptDataPtr++];
            int opcodeSize = functions[opcode].opcodeSize;
            int scriptCodeOffset = scriptDataPtr;

            scriptTextBuffer[0] = '\0';
            string scriptText = "";

            // Get Values
            for (int i = 0; i < opcodeSize; ++i)
            {
                ScriptVarTypes opcodeType = (ScriptVarTypes)scriptData[scriptDataPtr++];

                //Debug.WriteLine("SCRIPT: Get value {0}", opcodeType);

                if (opcodeType == ScriptVarTypes.SCRIPTVAR)
                {
                    int arrayVal = 0;
                    switch ((VARARR)scriptData[scriptDataPtr++])
                    {
                        case VARARR.NONE: arrayVal = Objects.objectEntityPos; break;
                        case VARARR.ARRAY:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = scriptEng.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOPLUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = scriptEng.arrayPosition[scriptData[scriptDataPtr++]] + Objects.objectEntityPos;
                            else
                                arrayVal = scriptData[scriptDataPtr++] + Objects.objectEntityPos;
                            break;
                        case VARARR.ENTNOMINUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos - scriptEng.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos - scriptData[scriptDataPtr++];
                            break;
                        default: break;
                    }

                    // Variables
                    var variable = (VAR)scriptData[scriptDataPtr++];
                    //Debug.WriteLine("SCRIPT: Get variable {0}", variable);
                    switch (variable)
                    {
                        default: break;
                        case VAR.TEMP0: scriptEng.operands[i] = scriptEng.temp[0]; break;
                        case VAR.TEMP1: scriptEng.operands[i] = scriptEng.temp[1]; break;
                        case VAR.TEMP2: scriptEng.operands[i] = scriptEng.temp[2]; break;
                        case VAR.TEMP3: scriptEng.operands[i] = scriptEng.temp[3]; break;
                        case VAR.TEMP4: scriptEng.operands[i] = scriptEng.temp[4]; break;
                        case VAR.TEMP5: scriptEng.operands[i] = scriptEng.temp[5]; break;
                        case VAR.TEMP6: scriptEng.operands[i] = scriptEng.temp[6]; break;
                        case VAR.TEMP7: scriptEng.operands[i] = scriptEng.temp[7]; break;
                        case VAR.CHECKRESULT: scriptEng.operands[i] = scriptEng.checkResult; break;
                        case VAR.ARRAYPOS0: scriptEng.operands[i] = scriptEng.arrayPosition[0]; break;
                        case VAR.ARRAYPOS1: scriptEng.operands[i] = scriptEng.arrayPosition[1]; break;
                        case VAR.ARRAYPOS2: scriptEng.operands[i] = scriptEng.arrayPosition[2]; break;
                        case VAR.ARRAYPOS3: scriptEng.operands[i] = scriptEng.arrayPosition[3]; break;
                        case VAR.ARRAYPOS4: scriptEng.operands[i] = scriptEng.arrayPosition[4]; break;
                        case VAR.ARRAYPOS5: scriptEng.operands[i] = scriptEng.arrayPosition[5]; break;
                        case VAR.ARRAYPOS6: scriptEng.operands[i] = scriptEng.arrayPosition[6]; break;
                        case VAR.ARRAYPOS7: scriptEng.operands[i] = scriptEng.arrayPosition[7]; break;
                        case VAR.GLOBAL: scriptEng.operands[i] = Engine.globalVariables[arrayVal]; break;
                        case VAR.LOCAL: scriptEng.operands[i] = scriptData[arrayVal]; break;
                        case VAR.OBJECTENTITYPOS: scriptEng.operands[i] = arrayVal; break;
                        case VAR.OBJECTGROUPID:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].groupID;
                                break;
                            }
                        case VAR.OBJECTTYPE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].type;
                                break;
                            }
                        case VAR.OBJECTPROPERTYVALUE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].propertyValue;
                                break;
                            }
                        case VAR.OBJECTXPOS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].xpos;
                                break;
                            }
                        case VAR.OBJECTYPOS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].ypos;
                                break;
                            }
                        case VAR.OBJECTIXPOS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].xpos >> 16;
                                break;
                            }
                        case VAR.OBJECTIYPOS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].ypos >> 16;
                                break;
                            }
                        case VAR.OBJECTXVEL:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].xvel;
                                break;
                            }
                        case VAR.OBJECTYVEL:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].yvel;
                                break;
                            }
                        case VAR.OBJECTSPEED:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].speed;
                                break;
                            }
                        case VAR.OBJECTSTATE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].state;
                                break;
                            }
                        case VAR.OBJECTROTATION:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].rotation;
                                break;
                            }
                        case VAR.OBJECTSCALE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].scale;
                                break;
                            }
                        case VAR.OBJECTPRIORITY:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].priority;
                                break;
                            }
                        case VAR.OBJECTDRAWORDER:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].drawOrder;
                                break;
                            }
                        case VAR.OBJECTDIRECTION:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].direction;
                                break;
                            }
                        case VAR.OBJECTINKEFFECT:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].inkEffect;
                                break;
                            }
                        case VAR.OBJECTALPHA:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].alpha;
                                break;
                            }
                        case VAR.OBJECTFRAME:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].frame;
                                break;
                            }
                        case VAR.OBJECTANIMATION:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].animation;
                                break;
                            }
                        case VAR.OBJECTPREVANIMATION:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].prevAnimation;
                                break;
                            }
                        case VAR.OBJECTANIMATIONSPEED:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].animationSpeed;
                                break;
                            }
                        case VAR.OBJECTANIMATIONTIMER:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].animationTimer;
                                break;
                            }
                        case VAR.OBJECTANGLE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].angle;
                                break;
                            }
                        case VAR.OBJECTLOOKPOSX:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].lookPosX;
                                break;
                            }
                        case VAR.OBJECTLOOKPOSY:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].lookPosY;
                                break;
                            }
                        case VAR.OBJECTCOLLISIONMODE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].collisionMode;
                                break;
                            }
                        case VAR.OBJECTCOLLISIONPLANE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].collisionPlane;
                                break;
                            }
                        case VAR.OBJECTCONTROLMODE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].controlMode;
                                break;
                            }
                        case VAR.OBJECTCONTROLLOCK:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].controlLock;
                                break;
                            }
                        case VAR.OBJECTPUSHING:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].pushing;
                                break;
                            }
                        case VAR.OBJECTVISIBLE:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].visible ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTTILECOLLISIONS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].tileCollisions ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTINTERACTION:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].objectInteractions ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTGRAVITY:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].gravity;
                                break;
                            }
                        case VAR.OBJECTUP:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].up;
                                break;
                            }
                        case VAR.OBJECTDOWN:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].down;
                                break;
                            }
                        case VAR.OBJECTLEFT:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].left;
                                break;
                            }
                        case VAR.OBJECTRIGHT:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].right;
                                break;
                            }
                        case VAR.OBJECTJUMPPRESS:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].jumpPress;
                                break;
                            }
                        case VAR.OBJECTJUMPHOLD:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].jumpHold;
                                break;
                            }
                        case VAR.OBJECTSCROLLTRACKING:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].scrollTracking;
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORL:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[0];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORC:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[1];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORR:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[2];
                                break;
                            }
#if !RETRO_REV00
                        case VAR.OBJECTFLOORSENSORLC:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[3];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORRC:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[4];
                                break;
                            }
#endif
                        case VAR.OBJECTCOLLISIONLEFT:
                            {
                                AnimationFile animFile = objectScriptList[Objects.objectEntityList[arrayVal].type].animFile;
                                Entity ent = Objects.objectEntityList[arrayVal];
                                if (animFile != null)
                                {
                                    int h = Animation.animFrames[Animation.animationList[animFile.animListOffset + ent.animation].frameListOffset + ent.frame].hitboxId;
                                    scriptEng.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].left[0];
                                }
                                else
                                {
                                    scriptEng.operands[i] = 0;
                                }
                                break;
                            }
                        case VAR.OBJECTCOLLISIONTOP:
                            {
                                AnimationFile animFile = objectScriptList[Objects.objectEntityList[arrayVal].type].animFile;
                                Entity ent = Objects.objectEntityList[arrayVal];
                                if (animFile != null)
                                {
                                    int h = Animation.animFrames[Animation.animationList[animFile.animListOffset + ent.animation].frameListOffset + ent.frame].hitboxId;

                                    scriptEng.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].top[0];
                                }
                                else
                                {
                                    scriptEng.operands[i] = 0;
                                }
                                break;
                            }
                        case VAR.OBJECTCOLLISIONRIGHT:
                            {
                                AnimationFile animFile = objectScriptList[Objects.objectEntityList[arrayVal].type].animFile;
                                Entity ent = Objects.objectEntityList[arrayVal];
                                if (animFile != null)
                                {
                                    int h = Animation.animFrames[Animation.animationList[animFile.animListOffset + ent.animation].frameListOffset + ent.frame].hitboxId;

                                    scriptEng.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].right[0];
                                }
                                else
                                {
                                    scriptEng.operands[i] = 0;
                                }
                                break;
                            }
                        case VAR.OBJECTCOLLISIONBOTTOM:
                            {
                                AnimationFile animFile = objectScriptList[Objects.objectEntityList[arrayVal].type].animFile;
                                Entity ent = Objects.objectEntityList[arrayVal];
                                if (animFile != null)
                                {
                                    int h = Animation.animFrames[Animation.animationList[animFile.animListOffset + ent.animation].frameListOffset + ent.frame].hitboxId;

                                    scriptEng.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].bottom[0];
                                }
                                else
                                {
                                    scriptEng.operands[i] = 0;
                                }
                                break;
                            }
                        case VAR.OBJECTOUTOFBOUNDS:
                            {
#if !RETRO_REV00
                                int boundX1_2P = -(0x200 << 16);
                                int boundX2_2P = (0x200 << 16);
                                int boundX3_2P = -(0x180 << 16);
                                int boundX4_2P = (0x180 << 16);

                                int boundY1_2P = -(0x180 << 16);
                                int boundY2_2P = (0x180 << 16);
                                int boundY3_2P = -(0x100 << 16);
                                int boundY4_2P = (0x100 << 16);

                                Entity entPtr = Objects.objectEntityList[arrayVal];
                                int x = entPtr.xpos >> 16;
                                int y = entPtr.ypos >> 16;

                                if (entPtr.priority == PRIORITY.ACTIVE_BOUNDS_SMALL || entPtr.priority == PRIORITY.ACTIVE_SMALL)
                                {
                                    if (Scene.stageMode == STAGEMODE.TWOP)
                                    {
                                        x = entPtr.xpos;
                                        y = entPtr.ypos;

                                        int boundL_P1 = Objects.objectEntityList[0].xpos + boundX3_2P;
                                        int boundR_P1 = Objects.objectEntityList[0].xpos + boundX4_2P;
                                        int boundT_P1 = Objects.objectEntityList[0].ypos + boundY3_2P;
                                        int boundB_P1 = Objects.objectEntityList[0].ypos + boundY4_2P;

                                        int boundL_P2 = Objects.objectEntityList[1].xpos + boundX3_2P;
                                        int boundR_P2 = Objects.objectEntityList[1].xpos + boundX4_2P;
                                        int boundT_P2 = Objects.objectEntityList[1].ypos + boundY3_2P;
                                        int boundB_P2 = Objects.objectEntityList[1].ypos + boundY4_2P;

                                        bool oobP1 = x <= boundL_P1 || x >= boundR_P1 || y <= boundT_P1 || y >= boundB_P1;
                                        bool oobP2 = x <= boundL_P2 || x >= boundR_P2 || y <= boundT_P2 || y >= boundB_P2;

                                        scriptEng.operands[i] = (oobP1 && oobP2) ? 1 : 0;
                                    }
                                    else
                                    {
                                        int boundL = Scene.xScrollOffset - Objects.OBJECT_BORDER_X3;
                                        int boundR = Scene.xScrollOffset + Objects.OBJECT_BORDER_X4;
                                        int boundT = Scene.yScrollOffset - Objects.OBJECT_BORDER_Y3;
                                        int boundB = Scene.yScrollOffset + Objects.OBJECT_BORDER_Y4;

                                        scriptEng.operands[i] = (x <= boundL || x >= boundR || y <= boundT || y >= boundB) ? 1 : 0;
                                    }
                                }
                                else
                                {
                                    if (Scene.stageMode == STAGEMODE.TWOP)
                                    {
                                        x = entPtr.xpos;
                                        y = entPtr.ypos;

                                        int boundL_P1 = Objects.objectEntityList[0].xpos + boundX1_2P;
                                        int boundR_P1 = Objects.objectEntityList[0].xpos + boundX2_2P;
                                        int boundT_P1 = Objects.objectEntityList[0].ypos + boundY1_2P;
                                        int boundB_P1 = Objects.objectEntityList[0].ypos + boundY2_2P;

                                        int boundL_P2 = Objects.objectEntityList[1].xpos + boundX1_2P;
                                        int boundR_P2 = Objects.objectEntityList[1].xpos + boundX2_2P;
                                        int boundT_P2 = Objects.objectEntityList[1].ypos + boundY1_2P;
                                        int boundB_P2 = Objects.objectEntityList[1].ypos + boundY2_2P;

                                        bool oobP1 = x <= boundL_P1 || x >= boundR_P1 || y <= boundT_P1 || y >= boundB_P1;
                                        bool oobP2 = x <= boundL_P2 || x >= boundR_P2 || y <= boundT_P2 || y >= boundB_P2;

                                        scriptEng.operands[i] = oobP1 ? 1 : 0;
                                        scriptEng.operands[i] = oobP2 ? 1 : 0;

                                        scriptEng.operands[i] = (oobP1 && oobP2) ? 1 : 0;
                                    }
                                    else
                                    {
                                        int boundL = Scene.xScrollOffset - Objects.OBJECT_BORDER_X1;
                                        int boundR = Scene.xScrollOffset + Objects.OBJECT_BORDER_X2;
                                        int boundT = Scene.yScrollOffset - Objects.OBJECT_BORDER_Y1;
                                        int boundB = Scene.yScrollOffset + Objects.OBJECT_BORDER_Y2;

                                        scriptEng.operands[i] = (x <= boundL || x >= boundR || y <= boundT || y >= boundB) ? 1 : 0;
                                    }
                                }
#else
                                int x = Objects.objectEntityList[arrayVal].xpos >> 16;
                                int y = Objects.objectEntityList[arrayVal].ypos >> 16;

                                int boundL = Scene.xScrollOffset - OBJECT_BORDER_X1;
                                int boundR = Scene.xScrollOffset + OBJECT_BORDER_X2;
                                int boundT = Scene.yScrollOffset - OBJECT_BORDER_Y1;
                                int boundB = Scene.yScrollOffset + OBJECT_BORDER_Y2;

                                scriptEng.operands[i] = x <= boundL || x >= boundR || y <= boundT || y >= boundB;
#endif
                                break;
                            }
                        case VAR.OBJECTSPRITESHEET:
                            {
                                scriptEng.operands[i] = objectScriptList[Objects.objectEntityList[arrayVal].type].spriteSheetId;
                                break;
                            }
                        case VAR.OBJECTVALUE0:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[0];
                                break;
                            }
                        case VAR.OBJECTVALUE1:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[1];
                                break;
                            }
                        case VAR.OBJECTVALUE2:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[2];
                                break;
                            }
                        case VAR.OBJECTVALUE3:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[3];
                                break;
                            }
                        case VAR.OBJECTVALUE4:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[4];
                                break;
                            }
                        case VAR.OBJECTVALUE5:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[5];
                                break;
                            }
                        case VAR.OBJECTVALUE6:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[6];
                                break;
                            }
                        case VAR.OBJECTVALUE7:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[7];
                                break;
                            }
                        case VAR.OBJECTVALUE8:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[8];
                                break;
                            }
                        case VAR.OBJECTVALUE9:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[9];
                                break;
                            }
                        case VAR.OBJECTVALUE10:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[10];
                                break;
                            }
                        case VAR.OBJECTVALUE11:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[11];
                                break;
                            }
                        case VAR.OBJECTVALUE12:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[12];
                                break;
                            }
                        case VAR.OBJECTVALUE13:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[13];
                                break;
                            }
                        case VAR.OBJECTVALUE14:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[14];
                                break;
                            }
                        case VAR.OBJECTVALUE15:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[15];
                                break;
                            }
                        case VAR.OBJECTVALUE16:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[16];
                                break;
                            }
                        case VAR.OBJECTVALUE17:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[17];
                                break;
                            }
                        case VAR.OBJECTVALUE18:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[18];
                                break;
                            }
                        case VAR.OBJECTVALUE19:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[19];
                                break;
                            }
                        case VAR.OBJECTVALUE20:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[20];
                                break;
                            }
                        case VAR.OBJECTVALUE21:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[21];
                                break;
                            }
                        case VAR.OBJECTVALUE22:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[22];
                                break;
                            }
                        case VAR.OBJECTVALUE23:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[23];
                                break;
                            }
                        case VAR.OBJECTVALUE24:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[24];
                                break;
                            }
                        case VAR.OBJECTVALUE25:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[25];
                                break;
                            }
                        case VAR.OBJECTVALUE26:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[26];
                                break;
                            }
                        case VAR.OBJECTVALUE27:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[27];
                                break;
                            }
                        case VAR.OBJECTVALUE28:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[28];
                                break;
                            }
                        case VAR.OBJECTVALUE29:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[29];
                                break;
                            }
                        case VAR.OBJECTVALUE30:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[30];
                                break;
                            }
                        case VAR.OBJECTVALUE31:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[31];
                                break;
                            }
                        case VAR.OBJECTVALUE32:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[32];
                                break;
                            }
                        case VAR.OBJECTVALUE33:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[33];
                                break;
                            }
                        case VAR.OBJECTVALUE34:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[34];
                                break;
                            }
                        case VAR.OBJECTVALUE35:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[35];
                                break;
                            }
                        case VAR.OBJECTVALUE36:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[36];
                                break;
                            }
                        case VAR.OBJECTVALUE37:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[37];
                                break;
                            }
                        case VAR.OBJECTVALUE38:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[38];
                                break;
                            }
                        case VAR.OBJECTVALUE39:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[39];
                                break;
                            }
                        case VAR.OBJECTVALUE40:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[40];
                                break;
                            }
                        case VAR.OBJECTVALUE41:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[41];
                                break;
                            }
                        case VAR.OBJECTVALUE42:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[42];
                                break;
                            }
                        case VAR.OBJECTVALUE43:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[43];
                                break;
                            }
                        case VAR.OBJECTVALUE44:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[44];
                                break;
                            }
                        case VAR.OBJECTVALUE45:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[45];
                                break;
                            }
                        case VAR.OBJECTVALUE46:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[46];
                                break;
                            }
                        case VAR.OBJECTVALUE47:
                            {
                                scriptEng.operands[i] = Objects.objectEntityList[arrayVal].values[47];
                                break;
                            }
                        case VAR.STAGESTATE: scriptEng.operands[i] = Scene.stageMode; break;
                        case VAR.STAGEACTIVELIST: scriptEng.operands[i] = Scene.activeStageList; break;
                        case VAR.STAGELISTPOS: scriptEng.operands[i] = Scene.stageListPosition; break;
                        case VAR.STAGETIMEENABLED: scriptEng.operands[i] = Scene.timeEnabled ? 1 : 0; break;
                        case VAR.STAGEMILLISECONDS: scriptEng.operands[i] = Scene.stageMilliseconds; break;
                        case VAR.STAGESECONDS: scriptEng.operands[i] = Scene.stageSeconds; break;
                        case VAR.STAGEMINUTES: scriptEng.operands[i] = Scene.stageMinutes; break;
                        case VAR.STAGEACTNUM: scriptEng.operands[i] = Scene.actId; break;
                        case VAR.STAGEPAUSEENABLED: scriptEng.operands[i] = Scene.pauseEnabled ? 1 : 0; break;
                        case VAR.STAGELISTSIZE: scriptEng.operands[i] = Engine.stageListCount[Scene.activeStageList]; break;
                        case VAR.STAGENEWXBOUNDARY1: scriptEng.operands[i] = Scene.newXBoundary1; break;
                        case VAR.STAGENEWXBOUNDARY2: scriptEng.operands[i] = Scene.newXBoundary2; break;
                        case VAR.STAGENEWYBOUNDARY1: scriptEng.operands[i] = Scene.newYBoundary1; break;
                        case VAR.STAGENEWYBOUNDARY2: scriptEng.operands[i] = Scene.newYBoundary2; break;
                        case VAR.STAGECURXBOUNDARY1: scriptEng.operands[i] = Scene.curXBoundary1; break;
                        case VAR.STAGECURXBOUNDARY2: scriptEng.operands[i] = Scene.curXBoundary2; break;
                        case VAR.STAGECURYBOUNDARY1: scriptEng.operands[i] = Scene.curYBoundary1; break;
                        case VAR.STAGECURYBOUNDARY2: scriptEng.operands[i] = Scene.curYBoundary2; break;
                        case VAR.STAGEDEFORMATIONDATA0: scriptEng.operands[i] = Scene.bgDeformationData0[arrayVal]; break;
                        case VAR.STAGEDEFORMATIONDATA1: scriptEng.operands[i] = Scene.bgDeformationData1[arrayVal]; break;
                        case VAR.STAGEDEFORMATIONDATA2: scriptEng.operands[i] = Scene.bgDeformationData2[arrayVal]; break;
                        case VAR.STAGEDEFORMATIONDATA3: scriptEng.operands[i] = Scene.bgDeformationData3[arrayVal]; break;
                        case VAR.STAGEWATERLEVEL: scriptEng.operands[i] = Scene.waterLevel; break;
                        case VAR.STAGEACTIVELAYER: scriptEng.operands[i] = Scene.activeTileLayers[arrayVal]; break;
                        case VAR.STAGEMIDPOINT: scriptEng.operands[i] = Scene.tLayerMidPoint; break;
                        case VAR.STAGEPLAYERLISTPOS: scriptEng.operands[i] = Objects.playerListPos; break;
                        case VAR.STAGEDEBUGMODE: scriptEng.operands[i] = Scene.debugMode ? 1 : 0; break;
                        case VAR.STAGEENTITYPOS: scriptEng.operands[i] = Objects.objectEntityPos; break;
                        case VAR.SCREENCAMERAENABLED: scriptEng.operands[i] = Scene.cameraEnabled ? 1 : 0; break;
                        case VAR.SCREENCAMERATARGET: scriptEng.operands[i] = Scene.cameraTarget; break;
                        case VAR.SCREENCAMERASTYLE: scriptEng.operands[i] = Scene.cameraStyle; break;
                        case VAR.SCREENCAMERAX: scriptEng.operands[i] = Scene.cameraXPos; break;
                        case VAR.SCREENCAMERAY: scriptEng.operands[i] = Scene.cameraYPos; break;
                        case VAR.SCREENDRAWLISTSIZE: scriptEng.operands[i] = Scene.drawListEntries[arrayVal].entityRefs.Count; break;
                        case VAR.SCREENXCENTER: scriptEng.operands[i] = Renderer.SCREEN_CENTERX; break;
                        case VAR.SCREENYCENTER: scriptEng.operands[i] = Renderer.SCREEN_CENTERY; break;
                        case VAR.SCREENXSIZE: scriptEng.operands[i] = Renderer.SCREEN_XSIZE; break;
                        case VAR.SCREENYSIZE: scriptEng.operands[i] = Renderer.SCREEN_YSIZE; break;
                        case VAR.SCREENXOFFSET: scriptEng.operands[i] = Scene.xScrollOffset; break;
                        case VAR.SCREENYOFFSET: scriptEng.operands[i] = Scene.yScrollOffset; break;
                        case VAR.SCREENSHAKEX: scriptEng.operands[i] = Scene.cameraShakeX; break;
                        case VAR.SCREENSHAKEY: scriptEng.operands[i] = Scene.cameraShakeY; break;
                        case VAR.SCREENADJUSTCAMERAY: scriptEng.operands[i] = Scene.cameraAdjustY; break;
                        case VAR.TOUCHSCREENDOWN: scriptEng.operands[i] = Input.touchDown[arrayVal]; break;
                        case VAR.TOUCHSCREENXPOS: scriptEng.operands[i] = Input.touchX[arrayVal]; break;
                        case VAR.TOUCHSCREENYPOS: scriptEng.operands[i] = Input.touchY[arrayVal]; break;
                        case VAR.MUSICVOLUME: scriptEng.operands[i] = Audio.masterVolume; break;
                        case VAR.MUSICCURRENTTRACK: scriptEng.operands[i] = Audio.trackId; break;
                        case VAR.MUSICPOSITION: scriptEng.operands[i] = Audio.musicPosition; break;
                        case VAR.INPUTDOWNUP: scriptEng.operands[i] = Input.inputDown.up; break;
                        case VAR.INPUTDOWNDOWN: scriptEng.operands[i] = Input.inputDown.down; break;
                        case VAR.INPUTDOWNLEFT: scriptEng.operands[i] = Input.inputDown.left; break;
                        case VAR.INPUTDOWNRIGHT: scriptEng.operands[i] = Input.inputDown.right; break;
                        case VAR.INPUTDOWNBUTTONA: scriptEng.operands[i] = Input.inputDown.A; break;
                        case VAR.INPUTDOWNBUTTONB: scriptEng.operands[i] = Input.inputDown.B; break;
                        case VAR.INPUTDOWNBUTTONC: scriptEng.operands[i] = Input.inputDown.C; break;
                        case VAR.INPUTDOWNBUTTONX: scriptEng.operands[i] = Input.inputDown.X; break;
                        case VAR.INPUTDOWNBUTTONY: scriptEng.operands[i] = Input.inputDown.Y; break;
                        case VAR.INPUTDOWNBUTTONZ: scriptEng.operands[i] = Input.inputDown.Z; break;
                        case VAR.INPUTDOWNBUTTONL: scriptEng.operands[i] = Input.inputDown.L; break;
                        case VAR.INPUTDOWNBUTTONR: scriptEng.operands[i] = Input.inputDown.R; break;
                        case VAR.INPUTDOWNSTART: scriptEng.operands[i] = Input.inputDown.start; break;
                        case VAR.INPUTDOWNSELECT: scriptEng.operands[i] = Input.inputDown.select; break;
                        case VAR.INPUTPRESSUP: scriptEng.operands[i] = Input.inputPress.up; break;
                        case VAR.INPUTPRESSDOWN: scriptEng.operands[i] = Input.inputPress.down; break;
                        case VAR.INPUTPRESSLEFT: scriptEng.operands[i] = Input.inputPress.left; break;
                        case VAR.INPUTPRESSRIGHT: scriptEng.operands[i] = Input.inputPress.right; break;
                        case VAR.INPUTPRESSBUTTONA: scriptEng.operands[i] = Input.inputPress.A; break;
                        case VAR.INPUTPRESSBUTTONB: scriptEng.operands[i] = Input.inputPress.B; break;
                        case VAR.INPUTPRESSBUTTONC: scriptEng.operands[i] = Input.inputPress.C; break;
                        case VAR.INPUTPRESSBUTTONX: scriptEng.operands[i] = Input.inputPress.X; break;
                        case VAR.INPUTPRESSBUTTONY: scriptEng.operands[i] = Input.inputPress.Y; break;
                        case VAR.INPUTPRESSBUTTONZ: scriptEng.operands[i] = Input.inputPress.Z; break;
                        case VAR.INPUTPRESSBUTTONL: scriptEng.operands[i] = Input.inputPress.L; break;
                        case VAR.INPUTPRESSBUTTONR: scriptEng.operands[i] = Input.inputPress.R; break;
                        case VAR.INPUTPRESSSTART: scriptEng.operands[i] = Input.inputPress.start; break;
                        case VAR.INPUTPRESSSELECT: scriptEng.operands[i] = Input.inputPress.select; break;
                        case VAR.MENU1SELECTION: scriptEng.operands[i] = Text.gameMenu[0].selection1; break;
                        case VAR.MENU2SELECTION: scriptEng.operands[i] = Text.gameMenu[1].selection1; break;
                        case VAR.TILELAYERXSIZE: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].xsize; break;
                        case VAR.TILELAYERYSIZE: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].ysize; break;
                        case VAR.TILELAYERTYPE: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].type; break;
                        case VAR.TILELAYERANGLE: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].angle; break;
                        case VAR.TILELAYERXPOS: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].xpos; break;
                        case VAR.TILELAYERYPOS: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].ypos; break;
                        case VAR.TILELAYERZPOS: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].zpos; break;
                        case VAR.TILELAYERPARALLAXFACTOR: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].parallaxFactor; break;
                        case VAR.TILELAYERSCROLLSPEED: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].scrollSpeed; break;
                        case VAR.TILELAYERSCROLLPOS: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].scrollPos; break;
                        case VAR.TILELAYERDEFORMATIONOFFSET: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].deformationOffset; break;
                        case VAR.TILELAYERDEFORMATIONOFFSETW: scriptEng.operands[i] = Scene.stageLayouts[arrayVal].deformationOffsetW; break;
                        case VAR.HPARALLAXPARALLAXFACTOR: scriptEng.operands[i] = Scene.hParallax.parallaxFactor[arrayVal]; break;
                        case VAR.HPARALLAXSCROLLSPEED: scriptEng.operands[i] = Scene.hParallax.scrollSpeed[arrayVal]; break;
                        case VAR.HPARALLAXSCROLLPOS: scriptEng.operands[i] = Scene.hParallax.scrollPos[arrayVal]; break;
                        case VAR.VPARALLAXPARALLAXFACTOR: scriptEng.operands[i] = Scene.vParallax.parallaxFactor[arrayVal]; break;
                        case VAR.VPARALLAXSCROLLSPEED: scriptEng.operands[i] = Scene.vParallax.scrollSpeed[arrayVal]; break;
                        case VAR.VPARALLAXSCROLLPOS: scriptEng.operands[i] = Scene.vParallax.scrollPos[arrayVal]; break;

                        case VAR.SCENE3DVERTEXCOUNT: scriptEng.operands[i] = Scene3D.vertexCount; break;
                        case VAR.SCENE3DFACECOUNT: scriptEng.operands[i] = Scene3D.faceCount; break;
                        case VAR.SCENE3DPROJECTIONX: scriptEng.operands[i] = Scene3D.projectionX; break;
                        case VAR.SCENE3DPROJECTIONY: scriptEng.operands[i] = Scene3D.projectionY; break;
#if !RETRO_REV00
                        case VAR.SCENE3DFOGCOLOR: scriptEng.operands[i] = Scene3D.fogColor; break;
                        case VAR.SCENE3DFOGSTRENGTH: scriptEng.operands[i] = Scene3D.fogStrength; break;
#endif
                        case VAR.VERTEXBUFFERX: scriptEng.operands[i] = Scene3D.vertexBuffer[arrayVal].x; break;
                        case VAR.VERTEXBUFFERY: scriptEng.operands[i] = Scene3D.vertexBuffer[arrayVal].y; break;
                        case VAR.VERTEXBUFFERZ: scriptEng.operands[i] = Scene3D.vertexBuffer[arrayVal].z; break;
                        case VAR.VERTEXBUFFERU: scriptEng.operands[i] = Scene3D.vertexBuffer[arrayVal].u; break;
                        case VAR.VERTEXBUFFERV: scriptEng.operands[i] = Scene3D.vertexBuffer[arrayVal].v; break;
                        case VAR.FACEBUFFERA: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].a; break;
                        case VAR.FACEBUFFERB: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].b; break;
                        case VAR.FACEBUFFERC: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].c; break;
                        case VAR.FACEBUFFERD: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].d; break;
                        case VAR.FACEBUFFERFLAG: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].flag; break;
                        case VAR.FACEBUFFERCOLOR: scriptEng.operands[i] = Scene3D.faceBuffer[arrayVal].color; break;
                        case VAR.SAVERAM: scriptEng.operands[i] = SaveData.saveRAM[arrayVal]; break;
                        case VAR.ENGINESTATE: scriptEng.operands[i] = Engine.gameMode; break;
#if RETRO_REV00
                        case ScrVar.ENGINEMESSAGE: scriptEng.operands[i] = Engine.message; break;
#endif
                        case VAR.ENGINELANGUAGE: scriptEng.operands[i] = Engine.language; break;
                        case VAR.ENGINEONLINEACTIVE: scriptEng.operands[i] = Engine.onlineActive ? 1 : 0; break;
                        case VAR.ENGINESFXVOLUME: scriptEng.operands[i] = Audio.sfxVolume; break;
                        case VAR.ENGINEBGMVOLUME: scriptEng.operands[i] = Audio.bgmVolume; break;
                        case VAR.ENGINETRIALMODE: scriptEng.operands[i] = Engine.trialMode ? 1 : 0; break;
                        case VAR.ENGINEDEVICETYPE: scriptEng.operands[i] = Engine.deviceType; break;
                        case VAR.HAPTICSENABLED: scriptEng.operands[i] = Engine.hapticsEnabled ? 1 : 0; break;
                    }
                }
                else if (opcodeType == ScriptVarTypes.SCRIPTINTCONST)
                { // int constant
                    scriptEng.operands[i] = scriptData[scriptDataPtr++];
                }
                else if (opcodeType == ScriptVarTypes.SCRIPTSTRCONST)
                { // string constant
                    int strLen = scriptData[scriptDataPtr++];
                    scriptTextBuffer[strLen] = '\0';
                    for (int c = 0; c < strLen; ++c)
                    {
                        switch (c % 4)
                        {
                            case 0:
                                {
                                    scriptTextBuffer[c] = (char)(byte)(scriptData[scriptDataPtr] >> 24);
                                    break;
                                }
                            case 1:
                                {
                                    scriptTextBuffer[c] = (char)(byte)((0xFFFFFF & scriptData[scriptDataPtr]) >> 16);
                                    break;
                                }
                            case 2:
                                {
                                    scriptTextBuffer[c] = (char)(byte)((0xFFFF & scriptData[scriptDataPtr]) >> 8);
                                    break;
                                }
                            case 3:
                                {
                                    scriptTextBuffer[c] = (char)(byte)(scriptData[scriptDataPtr++]);
                                    break;
                                }
                            default: break;
                        }
                    }
                    scriptDataPtr++;

                    scriptText = new string(scriptTextBuffer, 0, strLen);
                }
            }

            ObjectScript scriptInfo = objectScriptList[Objects.objectEntityList[Objects.objectEntityPos].type];
            Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
            SpriteFrame spriteFrame = null;

            // Debug.WriteLine("SCRIPT: Function {0}", (FUNC)opcode);

            // Functions
            switch ((FUNC)opcode)
            {
                default: break;
                case FUNC.END: running = false; break;
                case FUNC.EQUAL: scriptEng.operands[0] = scriptEng.operands[1]; break;
                case FUNC.ADD: scriptEng.operands[0] += scriptEng.operands[1]; break;
                case FUNC.SUB: scriptEng.operands[0] -= scriptEng.operands[1]; break;
                case FUNC.INC: ++scriptEng.operands[0]; break;
                case FUNC.DEC: --scriptEng.operands[0]; break;
                case FUNC.MUL: scriptEng.operands[0] *= scriptEng.operands[1]; break;
                case FUNC.DIV: scriptEng.operands[0] /= scriptEng.operands[1]; break;
                case FUNC.SHR: scriptEng.operands[0] >>= scriptEng.operands[1]; break;
                case FUNC.SHL: scriptEng.operands[0] <<= scriptEng.operands[1]; break;
                case FUNC.AND: scriptEng.operands[0] &= scriptEng.operands[1]; break;
                case FUNC.OR: scriptEng.operands[0] |= scriptEng.operands[1]; break;
                case FUNC.XOR: scriptEng.operands[0] ^= scriptEng.operands[1]; break;
                case FUNC.MOD: scriptEng.operands[0] %= scriptEng.operands[1]; break;
                case FUNC.FLIPSIGN: scriptEng.operands[0] = -scriptEng.operands[0]; break;
                case FUNC.CHECKEQUAL:
                    scriptEng.checkResult = scriptEng.operands[0] == scriptEng.operands[1] ? 1 : 0;
                    opcodeSize = 0;
                    break;
                case FUNC.CHECKGREATER:
                    scriptEng.checkResult = scriptEng.operands[0] > scriptEng.operands[1] ? 1 : 0;
                    opcodeSize = 0;
                    break;
                case FUNC.CHECKLOWER:
                    scriptEng.checkResult = scriptEng.operands[0] < scriptEng.operands[1] ? 1 : 0;
                    opcodeSize = 0;
                    break;
                case FUNC.CHECKNOTEQUAL:
                    scriptEng.checkResult = scriptEng.operands[0] != scriptEng.operands[1] ? 1 : 0;
                    opcodeSize = 0;
                    break;
                case FUNC.IFEQUAL:
                    if (scriptEng.operands[1] != scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.IFGREATER:
                    if (scriptEng.operands[1] <= scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.IFGREATEROREQUAL:
                    if (scriptEng.operands[1] < scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.IFLOWER:
                    if (scriptEng.operands[1] >= scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.IFLOWEROREQUAL:
                    if (scriptEng.operands[1] > scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.IFNOTEQUAL:
                    if (scriptEng.operands[1] == scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.ELSE:
                    opcodeSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--] + 1];
                    break;
                case FUNC.ENDIF:
                    opcodeSize = 0;
                    --jumpTableStackPos;
                    break;
                case FUNC.WEQUAL:
                    if (scriptEng.operands[1] != scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.WGREATER:
                    if (scriptEng.operands[1] <= scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.WGREATEROREQUAL:
                    if (scriptEng.operands[1] < scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.WLOWER:
                    if (scriptEng.operands[1] >= scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.WLOWEROREQUAL:
                    if (scriptEng.operands[1] > scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.WNOTEQUAL:
                    if (scriptEng.operands[1] == scriptEng.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                    else
                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    opcodeSize = 0;
                    break;
                case FUNC.LOOP:
                    opcodeSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--]];
                    break;
                case FUNC.FOREACHACTIVE:
                    {
                        int groupID = scriptEng.operands[1];
                        if (groupID < Objects.TYPEGROUP_COUNT)
                        {
                            int loop = foreachStack[++foreachStackPos] + 1;
                            foreachStack[foreachStackPos] = loop;
                            if (loop >= Objects.objectTypeGroupList[groupID].listSize)
                            {
                                opcodeSize = 0;
                                foreachStack[foreachStackPos--] = -1;
                                scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                                break;
                            }
                            else
                            {
                                scriptEng.operands[2] = Objects.objectTypeGroupList[groupID].entityRefs[loop];
                                jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                            }
                        }
                        else
                        {
                            opcodeSize = 0;
                            scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                        }
                        break;
                    }
                case FUNC.FOREACHALL:
                    {
                        int objType = scriptEng.operands[1];
                        if (objType < Objects.OBJECT_COUNT)
                        {
                            int loop = foreachStack[++foreachStackPos] + 1;
                            foreachStack[foreachStackPos] = loop;

                            if (scriptEvent == EVENT.SETUP)
                            {
                                while (true)
                                {
                                    if (loop >= Objects.TEMPENTITY_START)
                                    {
                                        opcodeSize = 0;
                                        foreachStack[foreachStackPos--] = -1;
                                        int off = jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                                        scriptDataPtr = scriptCodePtr + off;
                                        break;
                                    }
                                    else if (objType == Objects.objectEntityList[loop].type)
                                    {
                                        scriptEng.operands[2] = loop;
                                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                                        break;
                                    }
                                    else
                                    {
                                        foreachStack[foreachStackPos] = ++loop;
                                    }
                                }
                            }
                            else
                            {
                                while (true)
                                {
                                    if (loop >= Objects.ENTITY_COUNT)
                                    {
                                        opcodeSize = 0;
                                        foreachStack[foreachStackPos--] = -1;
                                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                                        break;
                                    }
                                    else if (objType == Objects.objectEntityList[loop].type)
                                    {
                                        scriptEng.operands[2] = loop;
                                        jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                                        break;
                                    }
                                    else
                                    {
                                        foreachStack[foreachStackPos] = ++loop;
                                    }
                                }
                            }
                        }
                        else
                        {
                            opcodeSize = 0;
                            scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1];
                        }
                        break;
                    }
                case FUNC.NEXT:
                    opcodeSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--]];
                    --foreachStackPos;
                    break;
                case FUNC.SWITCH:
                    jumpTableStack[++jumpTableStackPos] = scriptEng.operands[0];
                    if (scriptEng.operands[1] < jumpTableData[jumpTablePtr + scriptEng.operands[0]]
                        || scriptEng.operands[1] > jumpTableData[jumpTablePtr + scriptEng.operands[0] + 1])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 2];
                    else
                        scriptDataPtr = scriptCodePtr
                                        + jumpTableData[jumpTablePtr + scriptEng.operands[0] + 4
                                                        + (scriptEng.operands[1] - jumpTableData[jumpTablePtr + scriptEng.operands[0]])];
                    opcodeSize = 0;
                    break;
                case FUNC.BREAK:
                    opcodeSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--] + 3];
                    break;
                case FUNC.ENDSWITCH:
                    opcodeSize = 0;
                    --jumpTableStackPos;
                    break;
                case FUNC.RAND: scriptEng.operands[0] = FastMath.Rand(scriptEng.operands[1]); break;
                case FUNC.SIN:
                    {
                        scriptEng.operands[0] = FastMath.Sin512(scriptEng.operands[1]);
                        break;
                    }
                case FUNC.COS:
                    {
                        scriptEng.operands[0] = FastMath.Cos512(scriptEng.operands[1]);
                        break;
                    }
                case FUNC.SIN256:
                    {
                        scriptEng.operands[0] = FastMath.Sin256(scriptEng.operands[1]);
                        break;
                    }
                case FUNC.COS256:
                    {
                        scriptEng.operands[0] = FastMath.Cos256(scriptEng.operands[1]);
                        break;
                    }
                case FUNC.ATAN2:
                    {
                        scriptEng.operands[0] = FastMath.ArcTanLookup(scriptEng.operands[1], scriptEng.operands[2]);
                        break;
                    }
                case FUNC.INTERPOLATE:
                    scriptEng.operands[0] =
                        (scriptEng.operands[2] * (0x100 - scriptEng.operands[3]) + scriptEng.operands[3] * scriptEng.operands[1]) >> 8;
                    break;
                case FUNC.INTERPOLATEXY:
                    scriptEng.operands[0] =
                        (scriptEng.operands[3] * (0x100 - scriptEng.operands[6]) >> 8) + ((scriptEng.operands[6] * scriptEng.operands[2]) >> 8);
                    scriptEng.operands[1] =
                        (scriptEng.operands[5] * (0x100 - scriptEng.operands[6]) >> 8) + (scriptEng.operands[6] * scriptEng.operands[4] >> 8);
                    break;
                case FUNC.LOADSPRITESHEET:
                    opcodeSize = 0;
                    scriptInfo.spriteSheetId = Drawing.AddGraphicsFile(scriptText);
                    break;
                case FUNC.REMOVESPRITESHEET:
                    opcodeSize = 0;
                    Drawing.RemoveGraphicsFile(scriptText, -1);
                    break;
                case FUNC.DRAWSPRITE:
                    opcodeSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + scriptEng.operands[0]];
                    Drawing.DrawSprite((entity.xpos >> 16) - Scene.xScrollOffset + spriteFrame.pivotX, (entity.ypos >> 16) - Scene.yScrollOffset + spriteFrame.pivotY,
                               spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWSPRITEXY:
                    opcodeSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + scriptEng.operands[0]];
                    Drawing.DrawSprite((scriptEng.operands[1] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                               (scriptEng.operands[2] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width, spriteFrame.height,
                               spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWSPRITESCREENXY:
                    opcodeSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + scriptEng.operands[0]];
                    Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY, spriteFrame.width,
                               spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWTINTRECT:
                    opcodeSize = 0;
                    Drawing.DrawTintRectangle(scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]);
                    break;
                case FUNC.DRAWNUMBERS:
                    {
                        opcodeSize = 0;
                        int i = 10;
                        if (scriptEng.operands[6] != 0)
                        {
                            while (scriptEng.operands[4] > 0)
                            {
                                int frameID = scriptEng.operands[3] % i / (i / 10) + scriptEng.operands[0];
                                spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + frameID];
                                Drawing.DrawSprite(spriteFrame.pivotX + scriptEng.operands[1], spriteFrame.pivotY + scriptEng.operands[2], spriteFrame.width,
                                           spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                scriptEng.operands[1] -= scriptEng.operands[5];
                                i *= 10;
                                --scriptEng.operands[4];
                            }
                        }
                        else
                        {
                            int extra = 10;
                            if (scriptEng.operands[3] != 0)
                                extra = 10 * scriptEng.operands[3];
                            while (scriptEng.operands[4] > 0)
                            {
                                if (extra >= i)
                                {
                                    int frameID = scriptEng.operands[3] % i / (i / 10) + scriptEng.operands[0];
                                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + frameID];
                                    Drawing.DrawSprite(spriteFrame.pivotX + scriptEng.operands[1], spriteFrame.pivotY + scriptEng.operands[2], spriteFrame.width,
                                               spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                }
                                scriptEng.operands[1] -= scriptEng.operands[5];
                                i *= 10;
                                --scriptEng.operands[4];
                            }
                        }
                        break;
                    }
                case FUNC.DRAWACTNAME:
                    {
                        opcodeSize = 0;
                        int charID = 0;
                        switch (scriptEng.operands[3])
                        { // Draw Mode
                            case 0:                      // Draw Word 1 (but aligned from the right instead of left)
                                charID = 0;

                                for (charID = 0; charID < Scene.titleCardText.Length - 1; ++charID)
                                {
                                    int nextChar = Scene.titleCardText[charID + 1];
                                    if (nextChar == '-' || nextChar == '\0')
                                        break;
                                }

                                while (charID >= 0)
                                {
                                    int character = Scene.titleCardText[charID];
                                    if (character == ' ')
                                        character = -1; // special space char
                                    if (character == '-')
                                        character = 0;
                                    if (character >= '0' && character <= '9')
                                        character -= 22;
                                    if (character > '9' && character < 'f')
                                        character -= 'A';

                                    if (character <= -1)
                                    {
                                        scriptEng.operands[1] -= scriptEng.operands[5] + scriptEng.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += scriptEng.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];

                                        scriptEng.operands[1] -= spriteFrame.width + scriptEng.operands[6];

                                        Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY,
                                                   spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    }
                                    charID--;
                                }
                                break;

                            case 1: // Draw Word 1
                                charID = 0;

                                // Draw the first letter as a capital letter, the rest are lowercase (if scriptEng.operands[4] is true, otherwise they're all
                                // uppercase)
                                if (scriptEng.operands[4] == 1 && Scene.titleCardText[charID] != 0)
                                {
                                    int character = Scene.titleCardText[charID];
                                    if (character == ' ')
                                        character = 0;
                                    if (character == '-')
                                        character = 0;
                                    if (character >= '0' && character <= '9')
                                        character -= 22;
                                    if (character > '9' && character < 'f')
                                        character -= 'A';

                                    if (character <= -1)
                                    {
                                        scriptEng.operands[1] += scriptEng.operands[5] + scriptEng.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += scriptEng.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY,
                                                   spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        scriptEng.operands[1] += spriteFrame.width + scriptEng.operands[6];
                                    }

                                    scriptEng.operands[0] += 26;
                                    charID++;
                                }

                                while (Scene.titleCardText[charID] != 0 && Scene.titleCardText[charID] != '-')
                                {
                                    int character = Scene.titleCardText[charID];
                                    if (character == ' ')
                                        character = 0;
                                    if (character == '-')
                                        character = 0;
                                    if (character > '/' && character < ':')
                                        character -= 22;
                                    if (character > '9' && character < 'f')
                                        character -= 'A';

                                    if (character <= -1)
                                    {
                                        scriptEng.operands[1] += scriptEng.operands[5] + scriptEng.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += scriptEng.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY,
                                                   spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        scriptEng.operands[1] += spriteFrame.width + scriptEng.operands[6];
                                    }
                                    charID++;
                                }
                                break;

                            case 2: // Draw Word 2
                                charID = Scene.titleCardWord2;

                                // Draw the first letter as a capital letter, the rest are lowercase (if scriptEng.operands[4] is true, otherwise they're all
                                // uppercase)
                                if (scriptEng.operands[4] == 1 && Scene.titleCardText[charID] != 0)
                                {
                                    int character = Scene.titleCardText[charID];
                                    if (character == ' ')
                                        character = 0;
                                    if (character == '-')
                                        character = 0;
                                    if (character >= '0' && character <= '9')
                                        character -= 22;
                                    if (character > '9' && character < 'f')
                                        character -= 'A';

                                    if (character <= -1)
                                    {
                                        scriptEng.operands[1] += scriptEng.operands[5] + scriptEng.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += scriptEng.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY,
                                                   spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        scriptEng.operands[1] += spriteFrame.width + scriptEng.operands[6];
                                    }
                                    scriptEng.operands[0] += 26;
                                    charID++;
                                }

                                while (Scene.titleCardText[charID] != 0)
                                {
                                    int character = Scene.titleCardText[charID];
                                    if (character == ' ')
                                        character = 0;
                                    if (character == '-')
                                        character = 0;
                                    if (character >= '0' && character <= '9')
                                        character -= 22;
                                    if (character > '9' && character < 'f')
                                        character -= 'A';

                                    if (character <= -1)
                                    {
                                        scriptEng.operands[1] += scriptEng.operands[5] + scriptEng.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += scriptEng.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(scriptEng.operands[1] + spriteFrame.pivotX, scriptEng.operands[2] + spriteFrame.pivotY,
                                                   spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        scriptEng.operands[1] += spriteFrame.width + scriptEng.operands[6];
                                    }
                                    charID++;
                                }
                                break;
                        }
                        break;
                    }
                case FUNC.DRAWMENU:
                    opcodeSize = 0;
                    Drawing.DrawTextMenu(Text.gameMenu[scriptEng.operands[0]], scriptEng.operands[1], scriptEng.operands[2], scriptInfo.spriteSheetId);
                    break;
                case FUNC.SPRITEFRAME:
                    opcodeSize = 0;
                    if (scriptEvent == EVENT.SETUP && Animation.scriptFrameCount < Animation.SPRITEFRAME_COUNT)
                    {
                        Animation.scriptFrames[Animation.scriptFrameCount].pivotX = scriptEng.operands[0];
                        Animation.scriptFrames[Animation.scriptFrameCount].pivotY = scriptEng.operands[1];
                        Animation.scriptFrames[Animation.scriptFrameCount].width = scriptEng.operands[2];
                        Animation.scriptFrames[Animation.scriptFrameCount].height = scriptEng.operands[3];
                        Animation.scriptFrames[Animation.scriptFrameCount].spriteX = scriptEng.operands[4];
                        Animation.scriptFrames[Animation.scriptFrameCount].spriteY = scriptEng.operands[5];
                        ++Animation.scriptFrameCount;
                    }
                    break;
                case FUNC.EDITFRAME:
                    {
                        opcodeSize = 0;
                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + scriptEng.operands[0]];

                        spriteFrame.pivotX = scriptEng.operands[1];
                        spriteFrame.pivotY = scriptEng.operands[2];
                        spriteFrame.width = scriptEng.operands[3];
                        spriteFrame.height = scriptEng.operands[4];
                        spriteFrame.spriteX = scriptEng.operands[5];
                        spriteFrame.spriteY = scriptEng.operands[6];
                    }
                    break;
                case FUNC.LOADPALETTE:
                    opcodeSize = 0;
                    Palette.LoadPalette(scriptText, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4]);
                    break;
                case FUNC.ROTATEPALETTE:
                    opcodeSize = 0;
                    Palette.RotatePalette(scriptEng.operands[0], (byte)scriptEng.operands[1], (byte)scriptEng.operands[2], scriptEng.operands[3] != 0);
                    break;
                case FUNC.SETSCREENFADE:
                    opcodeSize = 0;
                    Drawing.SetFade((byte)scriptEng.operands[0], (byte)scriptEng.operands[1], (byte)scriptEng.operands[2], (ushort)scriptEng.operands[3]);
                    break;
                case FUNC.SETACTIVEPALETTE:
                    opcodeSize = 0;
                    Palette.SetActivePalette((byte)scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2]);
                    break;
                case FUNC.SETPALETTEFADE:
#if RETRO_REV00
                    SetLimitedFade(scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                   scriptEng.operands[5], scriptEng.operands[6]);
#else
                    Palette.SetPaletteFade((byte)scriptEng.operands[0], (byte)scriptEng.operands[1], (byte)scriptEng.operands[2], (ushort)scriptEng.operands[3], scriptEng.operands[4],
                                   scriptEng.operands[5]);
#endif
                    break;
                case FUNC.SETPALETTEENTRY: Palette.SetPaletteEntryPacked((byte)scriptEng.operands[0], (byte)scriptEng.operands[1], (uint)scriptEng.operands[2]); break;
                case FUNC.GETPALETTEENTRY: scriptEng.operands[2] = Palette.GetPaletteEntryPacked((byte)scriptEng.operands[0], (byte)scriptEng.operands[1]); break;
                case FUNC.COPYPALETTE:
                    opcodeSize = 0;
                    Palette.CopyPalette((byte)scriptEng.operands[0], (byte)scriptEng.operands[1], (byte)scriptEng.operands[2], (byte)scriptEng.operands[3], (ushort)scriptEng.operands[4]);
                    break;
                case FUNC.CLEARSCREEN:
                    opcodeSize = 0;
                    Drawing.ClearScreen((byte)scriptEng.operands[0]);
                    break;
                case FUNC.DRAWSPRITEFX:
                    opcodeSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + scriptEng.operands[0]];
                    switch (scriptEng.operands[1])
                    {
                        default: break;
                        case FX.SCALE:
                            Drawing.DrawScaledSprite(entity.direction, (scriptEng.operands[2] >> 16) - Scene.xScrollOffset,
                                             (scriptEng.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY, entity.scale,
                                             entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                             scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTATE:
                            Drawing.DrawRotatedSprite(entity.direction, (scriptEng.operands[2] >> 16) - Scene.xScrollOffset,
                                              (scriptEng.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                              spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                              scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTOZOOM:
                            Drawing.DrawRotoZoomSprite(entity.direction, (scriptEng.operands[2] >> 16) - Scene.xScrollOffset,
                                               (scriptEng.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                               spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                               entity.scale, scriptInfo.spriteSheetId);
                            break;
                        case FX.INK:
                            switch (entity.inkEffect)
                            {
                                case INK.NONE:
                                    Drawing.DrawSprite((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                               (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                               spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.BLEND:
                                    Drawing.DrawBlendedSprite((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                      (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                      spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.ALPHA:
                                    Drawing.DrawAlphaBlendedSprite((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                           (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                           spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                                           scriptInfo.spriteSheetId);
                                    break;
                                case INK.ADD:
                                    Drawing.DrawAdditiveBlendedSprite((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                              (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                              spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                                              scriptInfo.spriteSheetId);
                                    break;
                                case INK.SUB:
                                    Drawing.DrawSubtractiveBlendedSprite((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                                 (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                                 spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                                                 scriptInfo.spriteSheetId);
                                    break;
                            }
                            break;
                        case FX.TINT:
                            if (entity.inkEffect == INK.ALPHA)
                            {
                                Drawing.DrawScaledTintMask(entity.direction, (scriptEng.operands[2] >> 16) - Scene.xScrollOffset,
                                                   (scriptEng.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                                   entity.scale, entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX,
                                                   spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }
                            else
                            {
                                Drawing.DrawScaledSprite(entity.direction, (scriptEng.operands[2] >> 16) - Scene.xScrollOffset,
                                                 (scriptEng.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY, entity.scale,
                                                 entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                                 scriptInfo.spriteSheetId);
                            }
                            break;
                        case FX.FLIP:
                            switch (entity.direction)
                            {
                                default:
                                case FLIP.NONE:
                                    Drawing.DrawSpriteFlipped((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                      (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                      spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.NONE, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.X:
                                    Drawing.DrawSpriteFlipped((scriptEng.operands[2] >> 16) - Scene.xScrollOffset - spriteFrame.width - spriteFrame.pivotX,
                                                      (scriptEng.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                                      spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.X, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.Y:
                                    Drawing.DrawSpriteFlipped((scriptEng.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                                      (scriptEng.operands[3] >> 16) - Scene.yScrollOffset - spriteFrame.height - spriteFrame.pivotY,
                                                      spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.Y,
                                                      scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.XY:
                                    Drawing.DrawSpriteFlipped((scriptEng.operands[2] >> 16) - Scene.xScrollOffset - spriteFrame.width - spriteFrame.pivotX,
                                                      (scriptEng.operands[3] >> 16) - Scene.yScrollOffset - spriteFrame.height - spriteFrame.pivotY,
                                                      spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.XY,
                                                      scriptInfo.spriteSheetId);
                                    break;
                            }
                            break;
                    }
                    break;
                case FUNC.DRAWSPRITESCREENFX:
                    opcodeSize = 0;
                    int v = scriptInfo.frameListOffset + scriptEng.operands[0];
                    if (v > Animation.SPRITEFRAME_COUNT) break;
                    spriteFrame = Animation.scriptFrames[v];
                    switch (scriptEng.operands[1])
                    {
                        default: break;
                        case FX.SCALE:
                            Drawing.DrawScaledSprite(entity.direction, scriptEng.operands[2], scriptEng.operands[3], -spriteFrame.pivotX, -spriteFrame.pivotY,
                                             entity.scale, entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                             scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTATE:
                            Drawing.DrawRotatedSprite(entity.direction, scriptEng.operands[2], scriptEng.operands[3], -spriteFrame.pivotX, -spriteFrame.pivotY,
                                              spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                              scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTOZOOM:
                            Drawing.DrawRotoZoomSprite(entity.direction, scriptEng.operands[2], scriptEng.operands[3], -spriteFrame.pivotX,
                                               -spriteFrame.pivotY, spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height,
                                               entity.rotation, entity.scale, scriptInfo.spriteSheetId);
                            break;
                        case FX.INK:
                            switch (entity.inkEffect)
                            {
                                case INK.NONE:
                                    Drawing.DrawSprite(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                               spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.BLEND:
                                    Drawing.DrawBlendedSprite(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                                      spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                                      scriptInfo.spriteSheetId);
                                    break;
                                case INK.ALPHA:
                                    Drawing.DrawAlphaBlendedSprite(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                                           spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                                           scriptInfo.spriteSheetId);
                                    break;
                                case INK.ADD:
                                    Drawing.DrawAdditiveBlendedSprite(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                                              spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                                              entity.alpha, scriptInfo.spriteSheetId);
                                    break;
                                case INK.SUB:
                                    Drawing.DrawSubtractiveBlendedSprite(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                                                 spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                                                 entity.alpha, scriptInfo.spriteSheetId);
                                    break;
                            }
                            break;
                        case FX.TINT:
                            if (entity.inkEffect == INK.ALPHA)
                            {
                                Drawing.DrawScaledTintMask(entity.direction, scriptEng.operands[2], scriptEng.operands[3], -spriteFrame.pivotX,
                                                   -spriteFrame.pivotY, entity.scale, entity.scale, spriteFrame.width, spriteFrame.height,
                                                   spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }
                            else
                            {
                                Drawing.DrawScaledSprite(entity.direction, scriptEng.operands[2], scriptEng.operands[3], -spriteFrame.pivotX,
                                                 -spriteFrame.pivotY, entity.scale, entity.scale, spriteFrame.width, spriteFrame.height,
                                                 spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }
                            break;
                        case FX.FLIP:
                            switch (entity.direction)
                            {
                                default:
                                case FLIP.NONE:
                                    Drawing.DrawSpriteFlipped(scriptEng.operands[2] + spriteFrame.pivotX, scriptEng.operands[3] + spriteFrame.pivotY,
                                                      spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.NONE,
                                                      scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.X:
                                    Drawing.DrawSpriteFlipped(scriptEng.operands[2] - spriteFrame.width - spriteFrame.pivotX,
                                                      scriptEng.operands[3] + spriteFrame.pivotY, spriteFrame.width, spriteFrame.height,
                                                      spriteFrame.spriteX, spriteFrame.spriteY, FLIP.X, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.Y:
                                    Drawing.DrawSpriteFlipped(scriptEng.operands[2] + spriteFrame.pivotX,
                                                      scriptEng.operands[3] - spriteFrame.height - spriteFrame.pivotY, spriteFrame.width,
                                                      spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.Y, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.XY:
                                    Drawing.DrawSpriteFlipped(scriptEng.operands[2] - spriteFrame.width - spriteFrame.pivotX,
                                                      scriptEng.operands[3] - spriteFrame.height - spriteFrame.pivotY, spriteFrame.width,
                                                      spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.XY, scriptInfo.spriteSheetId);
                                    break;
                            }
                            break;
                    }
                    break;
                case FUNC.LOADANIMATION:
                    opcodeSize = 0;
                    scriptInfo.animFile = Animation.AddAnimationFile(scriptText);
                    break;
                case FUNC.SETUPMENU:
                    {
                        opcodeSize = 0;
                        TextMenu menu = Text.gameMenu[scriptEng.operands[0]];
                        Text.SetupTextMenu(menu, scriptEng.operands[1]);
                        menu.selectionCount = (byte)scriptEng.operands[2];
                        menu.alignment = (TextMenuAlignment)scriptEng.operands[3];
                        break;
                    }
                case FUNC.ADDMENUENTRY:
                    {
                        opcodeSize = 0;
                        TextMenu menu = Text.gameMenu[scriptEng.operands[0]];
                        menu.entryHighlight[menu.rowCount] = scriptEng.operands[2] != 0;
                        Text.AddTextMenuEntry(menu, scriptText);
                        break;
                    }
                case FUNC.EDITMENUENTRY:
                    {
                        opcodeSize = 0;
                        TextMenu menu = Text.gameMenu[scriptEng.operands[0]];
                        Text.EditTextMenuEntry(menu, scriptText, scriptEng.operands[2]);
                        menu.entryHighlight[scriptEng.operands[2]] = scriptEng.operands[3] != 0;
                        break;
                    }
                case FUNC.LOADSTAGE:
                    opcodeSize = 0;
                    Scene.stageMode = STAGEMODE.LOAD;
                    break;
                case FUNC.DRAWRECT:
                    opcodeSize = 0;
                    Drawing.DrawRectangle(scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                   scriptEng.operands[5], scriptEng.operands[6], scriptEng.operands[7]);
                    break;
                case FUNC.RESETOBJECTENTITY:
                    {
                        opcodeSize = 0;
                        Entity newEnt = Objects.objectEntityList[scriptEng.operands[0]] = new Entity();
                        newEnt.type = (byte)scriptEng.operands[1];
                        newEnt.propertyValue = (byte)scriptEng.operands[2];
                        newEnt.xpos = scriptEng.operands[3];
                        newEnt.ypos = scriptEng.operands[4];
                        newEnt.direction = FLIP.NONE;
                        newEnt.priority = PRIORITY.ACTIVE_BOUNDS;
                        newEnt.drawOrder = 3;
                        newEnt.scale = 512;
                        newEnt.inkEffect = INK.NONE;
                        newEnt.objectInteractions = true;
                        newEnt.visible = true;
                        newEnt.tileCollisions = true;
                        break;
                    }
                case FUNC.BOXCOLLISIONTEST:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        default: break;
                        case C.TOUCH:
                            Collision.TouchCollision(Objects.objectEntityList[scriptEng.operands[1]], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                           scriptEng.operands[5], Objects.objectEntityList[scriptEng.operands[6]], scriptEng.operands[7], scriptEng.operands[8],
                                           scriptEng.operands[9], scriptEng.operands[10]);
                            break;
                        case C.BOX:
                            Collision.BoxCollision(Objects.objectEntityList[scriptEng.operands[1]], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                         scriptEng.operands[5], Objects.objectEntityList[scriptEng.operands[6]], scriptEng.operands[7], scriptEng.operands[8],
                                         scriptEng.operands[9], scriptEng.operands[10]);
                            break;
                        case C.BOX2:
                            Collision.BoxCollision2(Objects.objectEntityList[scriptEng.operands[1]], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                          scriptEng.operands[5], Objects.objectEntityList[scriptEng.operands[6]], scriptEng.operands[7], scriptEng.operands[8],
                                          scriptEng.operands[9], scriptEng.operands[10]);
                            break;
                        case C.PLATFORM:
                            Collision.PlatformCollision(Objects.objectEntityList[scriptEng.operands[1]], scriptEng.operands[2], scriptEng.operands[3],
                                              scriptEng.operands[4], scriptEng.operands[5], Objects.objectEntityList[scriptEng.operands[6]],
                                              scriptEng.operands[7], scriptEng.operands[8], scriptEng.operands[9], scriptEng.operands[10]);
                            break;
                    }
                    break;
                case FUNC.CREATETEMPOBJECT:
                    {
                        opcodeSize = 0;
                        if (Objects.objectEntityList[scriptEng.arrayPosition[8]].type > 0 && ++scriptEng.arrayPosition[8] == Objects.ENTITY_COUNT)
                            scriptEng.arrayPosition[8] = Objects.TEMPENTITY_START;

                        var x = scriptEng.arrayPosition[8];
                        Debug.WriteLine("CREATETEMPOBJECT SLOT: {0}", x);

                        //if (Objects.objectEntityList[scriptEng.arrayPosition[8]].type > 0)
                        //{
                        //    if (scriptEng.arrayPosition[8] < 0x80) 
                        //        scriptEng.arrayPosition[8] += Objects.TEMPENTITY_START;
                        //    if (++scriptEng.arrayPosition[8] == Objects.ENTITY_COUNT)
                        //        scriptEng.arrayPosition[8] = Objects.TEMPENTITY_START;
                        //}

                        Entity temp = Objects.objectEntityList[scriptEng.arrayPosition[8]] = new Entity();
                        temp.type = (byte)scriptEng.operands[0];
                        temp.propertyValue = (byte)scriptEng.operands[1];
                        temp.xpos = scriptEng.operands[2];
                        temp.ypos = scriptEng.operands[3];
                        temp.direction = FLIP.NONE;
                        temp.priority = PRIORITY.ACTIVE;
                        temp.drawOrder = 3;
                        temp.scale = 512;
                        temp.inkEffect = INK.NONE;
                        temp.objectInteractions = true;
                        temp.visible = true;
                        temp.tileCollisions = true;
                        break;
                    }
                case FUNC.PROCESSOBJECTMOVEMENT:
                    opcodeSize = 0;
                    if (entity.tileCollisions)
                    {
                        Collision.ProcessTileCollisions(entity);
                    }
                    else
                    {
                        entity.xpos += entity.xvel;
                        entity.ypos += entity.yvel;
                    }
                    break;
                case FUNC.PROCESSOBJECTCONTROL:
                    opcodeSize = 0;
                    Objects.ProcessObjectControl(entity);
                    break;
                case FUNC.PROCESSANIMATION:
                    opcodeSize = 0;
                    Animation.ProcessObjectAnimation(scriptInfo, entity);
                    break;
                case FUNC.DRAWOBJECTANIMATION:
                    opcodeSize = 0;
                    if (entity.visible)
                        Drawing.DrawObjectAnimation(scriptInfo, entity, (entity.xpos >> 16) - Scene.xScrollOffset, (entity.ypos >> 16) - Scene.yScrollOffset);
                    break;
                case FUNC.SETMUSICTRACK:
                    opcodeSize = 0;
                    if (scriptEng.operands[2] <= 1)
                        Audio.SetMusicTrack(scriptText, (byte)scriptEng.operands[1], scriptEng.operands[2] != 0, 0);
                    else
                        Audio.SetMusicTrack(scriptText, (byte)scriptEng.operands[1], true, (uint)scriptEng.operands[2]);
                    break;
                case FUNC.PLAYMUSIC:
                    opcodeSize = 0;
                    Audio.PlayMusic(scriptEng.operands[0], 0);
                    break;
                case FUNC.STOPMUSIC:
                    opcodeSize = 0;
                    Audio.StopMusic(true);
                    break;
                case FUNC.PAUSEMUSIC:
                    opcodeSize = 0;
                    Audio.PauseSound();
                    break;
                case FUNC.RESUMEMUSIC:
                    opcodeSize = 0;
                    Audio.ResumeSound();
                    break;
                case FUNC.SWAPMUSICTRACK:
                    opcodeSize = 0;
                    if (scriptEng.operands[2] <= 1)
                        Audio.SwapMusicTrack(scriptText, (byte)scriptEng.operands[1], 0, (int)scriptEng.operands[3]);
                    else
                        Audio.SwapMusicTrack(scriptText, (byte)scriptEng.operands[1], (uint)scriptEng.operands[2], (int)scriptEng.operands[3]);
                    break;
                case FUNC.PLAYSFX:
                    opcodeSize = 0;
                    Audio.PlaySfx(scriptEng.operands[0], scriptEng.operands[1] != 0);
                    break;
                case FUNC.STOPSFX:
                    opcodeSize = 0;
                    Audio.StopSfx(scriptEng.operands[0]);
                    break;
                case FUNC.SETSFXATTRIBUTES:
                    opcodeSize = 0;
                    Audio.SetSfxAttributes(scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2]);
                    break;
                case FUNC.OBJECTTILECOLLISION:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        default: break;
                        case CSIDE.FLOOR: Collision.ObjectFloorCollision(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.LWALL: Collision.ObjectLWallCollision(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.RWALL: Collision.ObjectRWallCollision(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.ROOF: Collision.ObjectRoofCollision(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                    }
                    break;
                case FUNC.OBJECTTILEGRIP:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        default: break;
                        case CSIDE.FLOOR: Collision.ObjectFloorGrip(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.LWALL: Collision.ObjectLWallGrip(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.RWALL: Collision.ObjectRWallGrip(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case CSIDE.ROOF: Collision.ObjectRoofGrip(scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                    }
                    break;
                case FUNC.NOT: scriptEng.operands[0] = ~scriptEng.operands[0]; break;
                case FUNC.DRAW3DSCENE:
                    opcodeSize = 0;
                    Scene3D.TransformVertexBuffer();
                    Scene3D.Sort3DDrawList();
                    Scene3D.Draw3DScene(scriptInfo.spriteSheetId);
                    break;
                case FUNC.SETIDENTITYMATRIX:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.SetIdentityMatrix(ref Scene3D.matWorld); break;
                        case MAT.VIEW: Scene3D.SetIdentityMatrix(ref Scene3D.matView); break;
                        case MAT.TEMP: Scene3D.SetIdentityMatrix(ref Scene3D.matTemp); break;
                    }
                    break;
                case FUNC.MATRIXMULTIPLY:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD:
                            switch (scriptEng.operands[1])
                            {
                                case MAT.WORLD: Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matWorld); break;
                                case MAT.VIEW: Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matView); break;
                                case MAT.TEMP: Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matTemp); break;
                            }
                            break;
                        case MAT.VIEW:
                            switch (scriptEng.operands[1])
                            {
                                case MAT.WORLD: Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matWorld); break;
                                case MAT.VIEW: Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matView); break;
                                case MAT.TEMP: Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matTemp); break;
                            }
                            break;
                        case MAT.TEMP:
                            switch (scriptEng.operands[1])
                            {
                                case MAT.WORLD: Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matWorld); break;
                                case MAT.VIEW: Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matView); break;
                                case MAT.TEMP: Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matTemp); break;
                            }
                            break;
                    }
                    break;
                case FUNC.MATRIXTRANSLATEXYZ:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixTranslateXYZ(ref Scene3D.matWorld, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.VIEW: Scene3D.MatrixTranslateXYZ(ref Scene3D.matView, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.TEMP: Scene3D.MatrixTranslateXYZ(ref Scene3D.matTemp, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                    }
                    break;
                case FUNC.MATRIXSCALEXYZ:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixScaleXYZ(ref Scene3D.matWorld, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.VIEW: Scene3D.MatrixScaleXYZ(ref Scene3D.matView, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.TEMP: Scene3D.MatrixScaleXYZ(ref Scene3D.matTemp, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                    }
                    break;
                case FUNC.MATRIXROTATEX:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixRotateX(ref Scene3D.matWorld, scriptEng.operands[1]); break;
                        case MAT.VIEW: Scene3D.MatrixRotateX(ref Scene3D.matView, scriptEng.operands[1]); break;
                        case MAT.TEMP: Scene3D.MatrixRotateX(ref Scene3D.matTemp, scriptEng.operands[1]); break;
                    }
                    break;
                case FUNC.MATRIXROTATEY:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixRotateY(ref Scene3D.matWorld, scriptEng.operands[1]); break;
                        case MAT.VIEW: Scene3D.MatrixRotateY(ref Scene3D.matView, scriptEng.operands[1]); break;
                        case MAT.TEMP: Scene3D.MatrixRotateY(ref Scene3D.matTemp, scriptEng.operands[1]); break;
                    }
                    break;
                case FUNC.MATRIXROTATEZ:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixRotateZ(ref Scene3D.matWorld, scriptEng.operands[1]); break;
                        case MAT.VIEW: Scene3D.MatrixRotateZ(ref Scene3D.matView, scriptEng.operands[1]); break;
                        case MAT.TEMP: Scene3D.MatrixRotateZ(ref Scene3D.matTemp, scriptEng.operands[1]); break;
                    }
                    break;
                case FUNC.MATRIXROTATEXYZ:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixRotateXYZ(ref Scene3D.matWorld, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.VIEW: Scene3D.MatrixRotateXYZ(ref Scene3D.matView, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                        case MAT.TEMP: Scene3D.MatrixRotateXYZ(ref Scene3D.matTemp, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]); break;
                    }
                    break;
#if !RETRO_REV00
                case FUNC.MATRIXINVERSE:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.MatrixInverse(ref Scene3D.matWorld); break;
                        case MAT.VIEW: Scene3D.MatrixInverse(ref Scene3D.matView); break;
                        case MAT.TEMP: Scene3D.MatrixInverse(ref Scene3D.matTemp); break;
                    }
                    break;
#endif
                case FUNC.TRANSFORMVERTICES:
                    opcodeSize = 0;
                    switch (scriptEng.operands[0])
                    {
                        case MAT.WORLD: Scene3D.TransformVertices(ref Scene3D.matWorld, scriptEng.operands[1], scriptEng.operands[2]); break;
                        case MAT.VIEW: Scene3D.TransformVertices(ref Scene3D.matView, scriptEng.operands[1], scriptEng.operands[2]); break;
                        case MAT.TEMP: Scene3D.TransformVertices(ref Scene3D.matTemp, scriptEng.operands[1], scriptEng.operands[2]); break;
                    }
                    break;
                case FUNC.CALLFUNCTION:
                    {
                        opcodeSize = 0;
                        functionStack[functionStackPos++] = scriptDataPtr;
                        functionStack[functionStackPos++] = jumpTablePtr;
                        functionStack[functionStackPos++] = scriptCodePtr;
                        scriptCodePtr = functionScriptList[scriptEng.operands[0]].scriptCodePtr;
                        jumpTablePtr = functionScriptList[scriptEng.operands[0]].jumpTablePtr;
                        scriptDataPtr = scriptCodePtr;
                        break;
                    }
                case FUNC.RETURN:
                    opcodeSize = 0;
                    if (functionStackPos == 0)
                    { // event, stop running
                        running = false;
                    }
                    else
                    { // function, jump out
                        scriptCodePtr = functionStack[--functionStackPos];
                        jumpTablePtr = functionStack[--functionStackPos];
                        scriptDataPtr = functionStack[--functionStackPos];
                    }
                    break;
                case FUNC.SETLAYERDEFORMATION:
                    opcodeSize = 0;
                    Scene.SetLayerDeformation(scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                                        scriptEng.operands[5]);
                    break;
                case FUNC.CHECKTOUCHRECT:
                    opcodeSize = 0; scriptEng.checkResult = -1;
#if !RETRO_USE_ORIGINAL_CODE
                    //addDebugHitbox(H_TYPE_FINGER, NULL, scriptEng.operands[0], scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3]);
#endif
                    //for (int f = 0; f < touches; ++f)
                    //{
                    //    if (Input.touchDown[f] != 0 && Input.touchX[f] > scriptEng.operands[0] && Input.touchX[f] < scriptEng.operands[2] && Input.touchY[f] > scriptEng.operands[1]
                    //        && Input.touchY[f] < scriptEng.operands[3])
                    //    {
                    //        scriptEng.checkResult = f;
                    //    }
                    //}
                    break;
                case FUNC.GETTILELAYERENTRY:
                    scriptEng.operands[0] = Scene.stageLayouts[scriptEng.operands[1]].tiles[scriptEng.operands[2] + 0x100 * scriptEng.operands[3]];
                    break;
                case FUNC.SETTILELAYERENTRY:
                    Scene.stageLayouts[scriptEng.operands[1]].tiles[scriptEng.operands[2] + 0x100 * scriptEng.operands[3]] = (ushort)scriptEng.operands[0];
                    break;
                case FUNC.GETBIT: scriptEng.operands[0] = (scriptEng.operands[1] & (1 << scriptEng.operands[2])) >> scriptEng.operands[2]; break;
                case FUNC.SETBIT:
                    if (scriptEng.operands[2] <= 0)
                        scriptEng.operands[0] &= ~(1 << scriptEng.operands[1]);
                    else
                        scriptEng.operands[0] |= 1 << scriptEng.operands[1];
                    break;
                case FUNC.CLEARDRAWLIST:
                    opcodeSize = 0;
                    Scene.drawListEntries[scriptEng.operands[0]].entityRefs.Clear();
                    break;
                case FUNC.ADDDRAWLISTENTITYREF:
                    {
                        opcodeSize = 0;
                        Scene.drawListEntries[scriptEng.operands[0]].entityRefs.Add(scriptEng.operands[1]);
                        break;
                    }
                case FUNC.GETDRAWLISTENTITYREF: scriptEng.operands[0] = Scene.drawListEntries[scriptEng.operands[1]].entityRefs[scriptEng.operands[2]]; break;
                case FUNC.SETDRAWLISTENTITYREF:
                    opcodeSize = 0;
                    Scene.drawListEntries[scriptEng.operands[1]].entityRefs[scriptEng.operands[2]] = scriptEng.operands[0];
                    break;
                case FUNC.GET16X16TILEINFO:
                    {
                        scriptEng.operands[4] = scriptEng.operands[1] >> 7;
                        scriptEng.operands[5] = scriptEng.operands[2] >> 7;
                        scriptEng.operands[6] = Scene.stageLayouts[0].tiles[scriptEng.operands[4] + (scriptEng.operands[5] << 8)] << 6;
                        scriptEng.operands[6] += ((scriptEng.operands[1] & 0x7F) >> 4) + 8 * ((scriptEng.operands[2] & 0x7F) >> 4);
                        int index = Scene.tiles128x128.tileIndex[scriptEng.operands[6]];
                        switch (scriptEng.operands[3])
                        {
                            case TILEINFO.INDEX: scriptEng.operands[0] = Scene.tiles128x128.tileIndex[scriptEng.operands[6]]; break;
                            case TILEINFO.DIRECTION: scriptEng.operands[0] = Scene.tiles128x128.direction[scriptEng.operands[6]]; break;
                            case TILEINFO.VISUALPLANE: scriptEng.operands[0] = Scene.tiles128x128.visualPlane[scriptEng.operands[6]]; break;
                            case TILEINFO.SOLIDITYA: scriptEng.operands[0] = Scene.tiles128x128.collisionFlags[0][scriptEng.operands[6]]; break;
                            case TILEINFO.SOLIDITYB: scriptEng.operands[0] = Scene.tiles128x128.collisionFlags[1][scriptEng.operands[6]]; break;
                            case TILEINFO.FLAGSA: scriptEng.operands[0] = Scene.collisionMasks[0].flags[index]; break;
                            case TILEINFO.ANGLEA: scriptEng.operands[0] = (int)Scene.collisionMasks[0].angles[index]; break;
                            case TILEINFO.FLAGSB: scriptEng.operands[0] = Scene.collisionMasks[1].flags[index]; break;
                            case TILEINFO.ANGLEB: scriptEng.operands[0] = (int)Scene.collisionMasks[1].angles[index]; break;
                            default: break;
                        }
                        break;
                    }
                case FUNC.SET16X16TILEINFO:
                    {
                        scriptEng.operands[4] = scriptEng.operands[1] >> 7;
                        scriptEng.operands[5] = scriptEng.operands[2] >> 7;
                        scriptEng.operands[6] = Scene.stageLayouts[0].tiles[scriptEng.operands[4] + (scriptEng.operands[5] << 8)] << 6;
                        scriptEng.operands[6] += ((scriptEng.operands[1] & 0x7F) >> 4) + 8 * ((scriptEng.operands[2] & 0x7F) >> 4);
                        switch (scriptEng.operands[3])
                        {
                            case TILEINFO.INDEX:
                                Scene.tiles128x128.tileIndex[scriptEng.operands[6]] = (ushort)scriptEng.operands[0];
                                Scene.tiles128x128.gfxDataPos[scriptEng.operands[6]] = scriptEng.operands[0] << 8;
                                break;
                            case TILEINFO.DIRECTION: Scene.tiles128x128.direction[scriptEng.operands[6]] = (byte)scriptEng.operands[0]; break;
                            case TILEINFO.VISUALPLANE: Scene.tiles128x128.visualPlane[scriptEng.operands[6]] = (byte)scriptEng.operands[0]; break;
                            case TILEINFO.SOLIDITYA: Scene.tiles128x128.collisionFlags[0][scriptEng.operands[6]] = (byte)scriptEng.operands[0]; break;
                            case TILEINFO.SOLIDITYB: Scene.tiles128x128.collisionFlags[1][scriptEng.operands[6]] = (byte)scriptEng.operands[0]; break;
                            case TILEINFO.FLAGSA: Scene.collisionMasks[1].flags[Scene.tiles128x128.tileIndex[scriptEng.operands[6]]] = (byte)scriptEng.operands[0]; break;
                            case TILEINFO.ANGLEA: Scene.collisionMasks[1].angles[Scene.tiles128x128.tileIndex[scriptEng.operands[6]]] = (uint)scriptEng.operands[0]; break;
                            default: break;
                        }
                        break;
                    }
                case FUNC.COPY16X16TILE:
                    opcodeSize = 0;
                    Drawing.Copy16x16Tile(scriptEng.operands[0], scriptEng.operands[1]);
                    break;
                case FUNC.GETANIMATIONBYNAME:
                    {
                        AnimationFile animFile = scriptInfo.animFile;
                        scriptEng.operands[0] = -1;
                        int id = 0;
                        while (scriptEng.operands[0] == -1)
                        {
                            SpriteAnimation anim = Animation.animationList[animFile.animListOffset + id];
                            if (anim != null && scriptText == anim.name)
                                scriptEng.operands[0] = id;
                            else if (++id == animFile.animCount)
                                scriptEng.operands[0] = 0;
                        }
                        break;
                    }
                case FUNC.READSAVERAM:
                    opcodeSize = 0;
                    scriptEng.checkResult = SaveData.ReadSaveRAMData();
                    break;
                case FUNC.WRITESAVERAM:
                    opcodeSize = 0;
                    scriptEng.checkResult = SaveData.WriteSaveRAMData();
                    break;
#if RETRO_REV00 || RETRO_REV01
            case ScrFunc.LOADTEXTFONT: {
                opcodeSize = 0;
                LoadFontFile(scriptText);
                break;
            }
#endif
                case FUNC.LOADTEXTFILE:
                    {
                        opcodeSize = 0;
                        TextMenu menu = Text.gameMenu[scriptEng.operands[0]];
#if RETRO_REV00 || RETRO_REV01
                        Text.LoadTextFile(menu, scriptText, scriptEng.operands[2] != 0);
#else
                        Text.LoadTextFile(menu, scriptText, 0);
#endif
                        break;
                    }
                case FUNC.GETTEXTINFO:
                    {
                        TextMenu menu = Text.gameMenu[scriptEng.operands[1]];
                        switch (scriptEng.operands[2])
                        {
                            case TEXTINFO.TEXTDATA:
                                scriptEng.operands[0] = menu.textData[menu.entryStart[scriptEng.operands[3]] + scriptEng.operands[4]];
                                break;
                            case TEXTINFO.TEXTSIZE: scriptEng.operands[0] = menu.entrySize[scriptEng.operands[3]]; break;
                            case TEXTINFO.ROWCOUNT: scriptEng.operands[0] = menu.rowCount; break;
                        }
                        break;
                    }
#if RETRO_REV00 || RETRO_REV01
            case ScrFunc.DRAWTEXT: {
                opcodeSize        = 0;
                textMenuSurfaceNo = scriptInfo.spriteSheetId;
                TextMenu *menu    = &gameMenu[scriptEng.operands[0]];
                DrawBitmapText(menu, scriptEng.operands[1], scriptEng.operands[2], scriptEng.operands[3], scriptEng.operands[4],
                               scriptEng.operands[5], scriptEng.operands[6]);
                break;
            }
#endif
                case FUNC.GETVERSIONNUMBER:
                    {
                        opcodeSize = 0;
                        TextMenu menu = Text.gameMenu[scriptEng.operands[0]];
                        menu.entryHighlight[menu.rowCount] = scriptEng.operands[1] != 0;
                        Text.AddTextMenuEntry(menu, Engine.gameVersion);
                        break;
                    }
                case FUNC.GETTABLEVALUE:
                    {
                        int arrPos = scriptEng.operands[1];
                        if (arrPos >= 0)
                        {
                            int pos = scriptEng.operands[2];
                            int arrSize = scriptData[pos];
                            if (arrPos < arrSize)
                                scriptEng.operands[0] = scriptData[pos + arrPos + 1];
                        }
                        break;
                    }
                case FUNC.SETTABLEVALUE:
                    {
                        opcodeSize = 0;
                        int arrPos = scriptEng.operands[1];
                        if (arrPos >= 0)
                        {
                            int pos = scriptEng.operands[2];
                            int arrSize = scriptData[pos];
                            if (arrPos < arrSize)
                                scriptData[pos + arrPos + 1] = scriptEng.operands[0];
                        }
                        break;
                    }
                case FUNC.CHECKCURRENTSTAGEFOLDER:
                    opcodeSize = 0;
                    scriptEng.checkResult = Engine.stageList[Scene.activeStageList][Scene.stageListPosition].folder == scriptText ? 1 : 0;
                    break;
                case FUNC.ABS:
                    {
                        scriptEng.operands[0] = Math.Abs(scriptEng.operands[0]);
                        break;
                    }
                case FUNC.CALLNATIVEFUNCTION:
                    opcodeSize = 0;
                    if (scriptEng.operands[0] >= 0 && scriptEng.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        var func = (NativeFunction1)Engine.nativeFunctions[scriptEng.operands[0]];
                        if (func != null)
                            func();
                    }
                    break;
                case FUNC.CALLNATIVEFUNCTION2:
                    if (scriptEng.operands[0] >= 0 && scriptEng.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        if (scriptText.Length > 0)
                        {
                            var func = (NativeFunction2)Engine.nativeFunctions[scriptEng.operands[0]];
                            if (func != null)
                                func(ref scriptEng.operands[2], scriptText);
                        }
                        else
                        {
                            var func = (NativeFunction3)Engine.nativeFunctions[scriptEng.operands[0]];
                            if (func != null)
                                func(ref scriptEng.operands[1], ref scriptEng.operands[2]);
                        }
                    }
                    break;
                case FUNC.CALLNATIVEFUNCTION4:
                    if (scriptEng.operands[0] >= 0 && scriptEng.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        if (scriptText.Length > 0)
                        {
                            var func = (NativeFunction4)Engine.nativeFunctions[scriptEng.operands[0]];
                            if (func != null)
                                func(ref scriptEng.operands[1], scriptText, ref scriptEng.operands[3], ref scriptEng.operands[4]);
                        }
                        else
                        {
                            var func = (NativeFunction5)Engine.nativeFunctions[scriptEng.operands[0]];
                            if (func != null)
                                func(ref scriptEng.operands[1], ref scriptEng.operands[2], ref scriptEng.operands[3], ref scriptEng.operands[4]);
                        }
                    }
                    break;
                case FUNC.SETOBJECTRANGE:
                    {
                        opcodeSize = 0;
                        int offset = (scriptEng.operands[0] >> 1) - Renderer.SCREEN_CENTERX;
                        Objects.OBJECT_BORDER_X1 = offset + 0x80;
                        Objects.OBJECT_BORDER_X2 = scriptEng.operands[0] + 0x80 - offset;
                        Objects.OBJECT_BORDER_X3 = offset + 0x20;
                        Objects.OBJECT_BORDER_X4 = scriptEng.operands[0] + 0x20 - offset;
                        break;
                    }
#if !RETRO_REV00 && !RETRO_REV01
                case FUNC.GETOBJECTVALUE:
                    {
                        if (scriptEng.operands[1] < 48)
                            scriptEng.operands[0] = Objects.objectEntityList[scriptEng.operands[2]].values[scriptEng.operands[1]];
                        break;
                    }
                case FUNC.SETOBJECTVALUE:
                    {
                        opcodeSize = 0;
                        if (scriptEng.operands[1] < 48)
                            Objects.objectEntityList[scriptEng.operands[2]].values[scriptEng.operands[1]] = scriptEng.operands[0];
                        break;
                    }
                case FUNC.COPYOBJECT:
                    {
                        // dstID, srcID, count
                        Entity dstList = Objects.objectEntityList[scriptEng.operands[0]];
                        Entity srcList = Objects.objectEntityList[scriptEng.operands[1]];
                        for (int i = 0; i < scriptEng.operands[2]; ++i)
                            Objects.objectEntityList[scriptEng.operands[1] + i] = Objects.objectEntityList[scriptEng.operands[0] + i];
                        break;
                    }
#endif
                case FUNC.PRINT:
                    {
                        if (scriptEng.operands[1] != 0)
                            Debug.WriteLine(scriptEng.operands[0]);
                        else
                            Debug.WriteLine(scriptText);

                        if (scriptEng.operands[2] != 0)
                            Debug.WriteLine("");
                        break;
                    }
            }

            // Set Values
            if (opcodeSize > 0)
                scriptDataPtr -= scriptDataPtr - scriptCodeOffset;
            for (int i = 0; i < opcodeSize; ++i)
            {
                ScriptVarTypes opcodeType = (ScriptVarTypes)scriptData[scriptDataPtr++];

                //Debug.WriteLine("SCRIPT: Set value {0}", opcodeType);

                if (opcodeType == ScriptVarTypes.SCRIPTVAR)
                {
                    int arrayVal = 0;
                    switch ((VARARR)scriptData[scriptDataPtr++])
                    { // variable
                        case VARARR.NONE: arrayVal = Objects.objectEntityPos; break;
                        case VARARR.ARRAY:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = scriptEng.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOPLUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos + scriptEng.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos + scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOMINUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos - scriptEng.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos - scriptData[scriptDataPtr++];
                            break;
                        default: break;
                    }

                    var variable = (VAR)scriptData[scriptDataPtr++];
                    // Debug.WriteLine("SCRIPT: Set variable {0}", variable);
                    // Variables
                    switch (variable)
                    {
                        default: break;
                        case VAR.TEMP0: scriptEng.temp[0] = scriptEng.operands[i]; break;
                        case VAR.TEMP1: scriptEng.temp[1] = scriptEng.operands[i]; break;
                        case VAR.TEMP2: scriptEng.temp[2] = scriptEng.operands[i]; break;
                        case VAR.TEMP3: scriptEng.temp[3] = scriptEng.operands[i]; break;
                        case VAR.TEMP4: scriptEng.temp[4] = scriptEng.operands[i]; break;
                        case VAR.TEMP5: scriptEng.temp[5] = scriptEng.operands[i]; break;
                        case VAR.TEMP6: scriptEng.temp[6] = scriptEng.operands[i]; break;
                        case VAR.TEMP7: scriptEng.temp[7] = scriptEng.operands[i]; break;
                        case VAR.CHECKRESULT: scriptEng.checkResult = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS0: scriptEng.arrayPosition[0] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS1: scriptEng.arrayPosition[1] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS2: scriptEng.arrayPosition[2] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS3: scriptEng.arrayPosition[3] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS4: scriptEng.arrayPosition[4] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS5: scriptEng.arrayPosition[5] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS6: scriptEng.arrayPosition[6] = scriptEng.operands[i]; break;
                        case VAR.ARRAYPOS7: scriptEng.arrayPosition[7] = scriptEng.operands[i]; break;
                        case VAR.GLOBAL: Engine.globalVariables[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.LOCAL: scriptData[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.OBJECTENTITYPOS: break;
                        case VAR.OBJECTGROUPID:
                            {
                                Objects.objectEntityList[arrayVal].groupID = (ushort)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTTYPE:
                            {
                                Objects.objectEntityList[arrayVal].type = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTPROPERTYVALUE:
                            {
                                Objects.objectEntityList[arrayVal].propertyValue = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTXPOS:
                            {
                                Objects.objectEntityList[arrayVal].xpos = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTYPOS:
                            {
                                Objects.objectEntityList[arrayVal].ypos = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTIXPOS:
                            {
                                Objects.objectEntityList[arrayVal].xpos = scriptEng.operands[i] << 16;
                                break;
                            }
                        case VAR.OBJECTIYPOS:
                            {
                                Objects.objectEntityList[arrayVal].ypos = scriptEng.operands[i] << 16;
                                break;
                            }
                        case VAR.OBJECTXVEL:
                            {
                                Objects.objectEntityList[arrayVal].xvel = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTYVEL:
                            {
                                Objects.objectEntityList[arrayVal].yvel = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTSPEED:
                            {
                                Objects.objectEntityList[arrayVal].speed = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTSTATE:
                            {
                                Objects.objectEntityList[arrayVal].state = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTROTATION:
                            {
                                Objects.objectEntityList[arrayVal].rotation = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTSCALE:
                            {
                                Objects.objectEntityList[arrayVal].scale = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTPRIORITY:
                            {
                                Objects.objectEntityList[arrayVal].priority = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTDRAWORDER:
                            {
                                Objects.objectEntityList[arrayVal].drawOrder = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTDIRECTION:
                            {
                                Objects.objectEntityList[arrayVal].direction = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTINKEFFECT:
                            {
                                Objects.objectEntityList[arrayVal].inkEffect = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTALPHA:
                            {
                                Objects.objectEntityList[arrayVal].alpha = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTFRAME:
                            {
                                Objects.objectEntityList[arrayVal].frame = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATION:
                            {
                                Objects.objectEntityList[arrayVal].animation = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTPREVANIMATION:
                            {
                                Objects.objectEntityList[arrayVal].prevAnimation = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATIONSPEED:
                            {
                                Objects.objectEntityList[arrayVal].animationSpeed = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATIONTIMER:
                            {
                                Objects.objectEntityList[arrayVal].animationTimer = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTANGLE:
                            {
                                Objects.objectEntityList[arrayVal].angle = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTLOOKPOSX:
                            {
                                Objects.objectEntityList[arrayVal].lookPosX = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTLOOKPOSY:
                            {
                                Objects.objectEntityList[arrayVal].lookPosY = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTCOLLISIONMODE:
                            {
                                Objects.objectEntityList[arrayVal].collisionMode = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTCOLLISIONPLANE:
                            {
                                Objects.objectEntityList[arrayVal].collisionPlane = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTCONTROLMODE:
                            {
                                Objects.objectEntityList[arrayVal].controlMode = (sbyte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTCONTROLLOCK:
                            {
                                Objects.objectEntityList[arrayVal].controlLock = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTPUSHING:
                            {
                                Objects.objectEntityList[arrayVal].pushing = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVISIBLE:
                            {
                                Objects.objectEntityList[arrayVal].visible = scriptEng.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTTILECOLLISIONS:
                            {
                                Objects.objectEntityList[arrayVal].tileCollisions = scriptEng.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTINTERACTION:
                            {
                                Objects.objectEntityList[arrayVal].objectInteractions = scriptEng.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTGRAVITY:
                            {
                                Objects.objectEntityList[arrayVal].gravity = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTUP:
                            {
                                Objects.objectEntityList[arrayVal].up = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTDOWN:
                            {
                                Objects.objectEntityList[arrayVal].down = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTLEFT:
                            {
                                Objects.objectEntityList[arrayVal].left = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTRIGHT:
                            {
                                Objects.objectEntityList[arrayVal].right = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTJUMPPRESS:
                            {
                                Objects.objectEntityList[arrayVal].jumpPress = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTJUMPHOLD:
                            {
                                Objects.objectEntityList[arrayVal].jumpHold = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTSCROLLTRACKING:
                            {
                                Objects.objectEntityList[arrayVal].scrollTracking = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORL:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[0] = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[1] = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORR:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[2] = (byte)scriptEng.operands[i];
                                break;
                            }
#if !RETRO_REV00
                        case VAR.OBJECTFLOORSENSORLC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[3] = (byte)scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORRC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[4] = (byte)scriptEng.operands[i];
                                break;
                            }
#endif
                        case VAR.OBJECTCOLLISIONLEFT:
                            {
                                break;
                            }
                        case VAR.OBJECTCOLLISIONTOP:
                            {
                                break;
                            }
                        case VAR.OBJECTCOLLISIONRIGHT:
                            {
                                break;
                            }
                        case VAR.OBJECTCOLLISIONBOTTOM:
                            {
                                break;
                            }
                        case VAR.OBJECTOUTOFBOUNDS:
                            {
                                break;
                            }
                        case VAR.OBJECTSPRITESHEET:
                            {
                                objectScriptList[Objects.objectEntityList[arrayVal].type].spriteSheetId = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE0:
                            {
                                Objects.objectEntityList[arrayVal].values[0] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE1:
                            {
                                Objects.objectEntityList[arrayVal].values[1] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE2:
                            {
                                Objects.objectEntityList[arrayVal].values[2] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE3:
                            {
                                Objects.objectEntityList[arrayVal].values[3] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE4:
                            {
                                Objects.objectEntityList[arrayVal].values[4] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE5:
                            {
                                Objects.objectEntityList[arrayVal].values[5] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE6:
                            {
                                Objects.objectEntityList[arrayVal].values[6] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE7:
                            {
                                Objects.objectEntityList[arrayVal].values[7] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE8:
                            {
                                Objects.objectEntityList[arrayVal].values[8] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE9:
                            {
                                Objects.objectEntityList[arrayVal].values[9] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE10:
                            {
                                Objects.objectEntityList[arrayVal].values[10] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE11:
                            {
                                Objects.objectEntityList[arrayVal].values[11] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE12:
                            {
                                Objects.objectEntityList[arrayVal].values[12] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE13:
                            {
                                Objects.objectEntityList[arrayVal].values[13] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE14:
                            {
                                Objects.objectEntityList[arrayVal].values[14] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE15:
                            {
                                Objects.objectEntityList[arrayVal].values[15] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE16:
                            {
                                Objects.objectEntityList[arrayVal].values[16] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE17:
                            {
                                Objects.objectEntityList[arrayVal].values[17] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE18:
                            {
                                Objects.objectEntityList[arrayVal].values[18] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE19:
                            {
                                Objects.objectEntityList[arrayVal].values[19] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE20:
                            {
                                Objects.objectEntityList[arrayVal].values[20] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE21:
                            {
                                Objects.objectEntityList[arrayVal].values[21] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE22:
                            {
                                Objects.objectEntityList[arrayVal].values[22] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE23:
                            {
                                Objects.objectEntityList[arrayVal].values[23] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE24:
                            {
                                Objects.objectEntityList[arrayVal].values[24] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE25:
                            {
                                Objects.objectEntityList[arrayVal].values[25] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE26:
                            {
                                Objects.objectEntityList[arrayVal].values[26] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE27:
                            {
                                Objects.objectEntityList[arrayVal].values[27] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE28:
                            {
                                Objects.objectEntityList[arrayVal].values[28] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE29:
                            {
                                Objects.objectEntityList[arrayVal].values[29] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE30:
                            {
                                Objects.objectEntityList[arrayVal].values[30] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE31:
                            {
                                Objects.objectEntityList[arrayVal].values[31] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE32:
                            {
                                Objects.objectEntityList[arrayVal].values[32] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE33:
                            {
                                Objects.objectEntityList[arrayVal].values[33] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE34:
                            {
                                Objects.objectEntityList[arrayVal].values[34] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE35:
                            {
                                Objects.objectEntityList[arrayVal].values[35] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE36:
                            {
                                Objects.objectEntityList[arrayVal].values[36] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE37:
                            {
                                Objects.objectEntityList[arrayVal].values[37] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE38:
                            {
                                Objects.objectEntityList[arrayVal].values[38] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE39:
                            {
                                Objects.objectEntityList[arrayVal].values[39] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE40:
                            {
                                Objects.objectEntityList[arrayVal].values[40] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE41:
                            {
                                Objects.objectEntityList[arrayVal].values[41] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE42:
                            {
                                Objects.objectEntityList[arrayVal].values[42] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE43:
                            {
                                Objects.objectEntityList[arrayVal].values[43] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE44:
                            {
                                Objects.objectEntityList[arrayVal].values[44] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE45:
                            {
                                Objects.objectEntityList[arrayVal].values[45] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE46:
                            {
                                Objects.objectEntityList[arrayVal].values[46] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE47:
                            {
                                Objects.objectEntityList[arrayVal].values[47] = scriptEng.operands[i];
                                break;
                            }
                        case VAR.STAGESTATE: Scene.stageMode = scriptEng.operands[i]; break;
                        case VAR.STAGEACTIVELIST: Scene.activeStageList = scriptEng.operands[i]; break;
                        case VAR.STAGELISTPOS: Scene.stageListPosition = scriptEng.operands[i]; break;
                        case VAR.STAGETIMEENABLED: Scene.timeEnabled = scriptEng.operands[i] != 0; break;
                        case VAR.STAGEMILLISECONDS: Scene.stageMilliseconds = scriptEng.operands[i]; break;
                        case VAR.STAGESECONDS: Scene.stageSeconds = scriptEng.operands[i]; break;
                        case VAR.STAGEMINUTES: Scene.stageMinutes = scriptEng.operands[i]; break;
                        case VAR.STAGEACTNUM: Scene.actId = scriptEng.operands[i]; break;
                        case VAR.STAGEPAUSEENABLED: Scene.pauseEnabled = scriptEng.operands[i] != 0; break;
                        case VAR.STAGELISTSIZE: break;
                        case VAR.STAGENEWXBOUNDARY1: Scene.newXBoundary1 = scriptEng.operands[i]; break;
                        case VAR.STAGENEWXBOUNDARY2: Scene.newXBoundary2 = scriptEng.operands[i]; break;
                        case VAR.STAGENEWYBOUNDARY1: Scene.newYBoundary1 = scriptEng.operands[i]; break;
                        case VAR.STAGENEWYBOUNDARY2: Scene.newYBoundary2 = scriptEng.operands[i]; break;
                        case VAR.STAGECURXBOUNDARY1:
                            if (Scene.curXBoundary1 != scriptEng.operands[i])
                            {
                                Scene.curXBoundary1 = scriptEng.operands[i];
                                Scene.newXBoundary1 = scriptEng.operands[i];
                            }
                            break;
                        case VAR.STAGECURXBOUNDARY2:
                            if (Scene.curXBoundary2 != scriptEng.operands[i])
                            {
                                Scene.curXBoundary2 = scriptEng.operands[i];
                                Scene.newXBoundary2 = scriptEng.operands[i];
                            }
                            break;
                        case VAR.STAGECURYBOUNDARY1:
                            if (Scene.curYBoundary1 != scriptEng.operands[i])
                            {
                                Scene.curYBoundary1 = scriptEng.operands[i];
                                Scene.newYBoundary1 = scriptEng.operands[i];
                            }
                            break;
                        case VAR.STAGECURYBOUNDARY2:
                            if (Scene.curYBoundary2 != scriptEng.operands[i])
                            {
                                Scene.curYBoundary2 = scriptEng.operands[i];
                                Scene.newYBoundary2 = scriptEng.operands[i];
                            }
                            break;
                        case VAR.STAGEDEFORMATIONDATA0: Scene.bgDeformationData0[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.STAGEDEFORMATIONDATA1: Scene.bgDeformationData1[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.STAGEDEFORMATIONDATA2: Scene.bgDeformationData2[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.STAGEDEFORMATIONDATA3: Scene.bgDeformationData3[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.STAGEWATERLEVEL: Scene.waterLevel = scriptEng.operands[i]; break;
                        case VAR.STAGEACTIVELAYER: Scene.activeTileLayers[arrayVal] = (byte)scriptEng.operands[i]; break;
                        case VAR.STAGEMIDPOINT: Scene.tLayerMidPoint = (byte)scriptEng.operands[i]; break;
                        case VAR.STAGEPLAYERLISTPOS: Objects.playerListPos = scriptEng.operands[i]; break;
                        case VAR.STAGEDEBUGMODE: Scene.debugMode = scriptEng.operands[i] != 0; break;
                        case VAR.STAGEENTITYPOS: Objects.objectEntityPos = scriptEng.operands[i]; break;
                        case VAR.SCREENCAMERAENABLED: Scene.cameraEnabled = scriptEng.operands[i] != 0; break;
                        case VAR.SCREENCAMERATARGET: Scene.cameraTarget = scriptEng.operands[i]; break;
                        case VAR.SCREENCAMERASTYLE: Scene.cameraStyle = scriptEng.operands[i]; break;
                        case VAR.SCREENCAMERAX: Scene.cameraXPos = scriptEng.operands[i]; break;
                        case VAR.SCREENCAMERAY: Scene.cameraYPos = scriptEng.operands[i]; break;
                        case VAR.SCREENDRAWLISTSIZE:
                            {
                                Debug.Assert(false);
                                //Scene.drawListEntries[arrayVal].listSize = scriptEng.operands[i];
                                break;
                            }
                        case VAR.SCREENXCENTER: break;
                        case VAR.SCREENYCENTER: break;
                        case VAR.SCREENXSIZE: break;
                        case VAR.SCREENYSIZE: break;
                        case VAR.SCREENXOFFSET: Scene.xScrollOffset = scriptEng.operands[i]; break;
                        case VAR.SCREENYOFFSET: Scene.yScrollOffset = scriptEng.operands[i]; break;
                        case VAR.SCREENSHAKEX: Scene.cameraShakeX = scriptEng.operands[i]; break;
                        case VAR.SCREENSHAKEY: Scene.cameraShakeY = scriptEng.operands[i]; break;
                        case VAR.SCREENADJUSTCAMERAY: Scene.cameraAdjustY = scriptEng.operands[i]; break;
                        case VAR.TOUCHSCREENDOWN: break;
                        case VAR.TOUCHSCREENXPOS: break;
                        case VAR.TOUCHSCREENYPOS: break;
                        case VAR.MUSICVOLUME: Audio.SetMusicVolume(scriptEng.operands[i]); break;
                        case VAR.MUSICCURRENTTRACK: break;
                        case VAR.MUSICPOSITION: break;
                        case VAR.INPUTDOWNUP: Input.inputDown.up = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNDOWN: Input.inputDown.down = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNLEFT: Input.inputDown.left = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNRIGHT: Input.inputDown.right = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONA: Input.inputDown.A = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONB: Input.inputDown.B = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONC: Input.inputDown.C = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONX: Input.inputDown.X = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONY: Input.inputDown.Y = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONZ: Input.inputDown.Z = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONL: Input.inputDown.L = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNBUTTONR: Input.inputDown.R = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNSTART: Input.inputDown.start = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTDOWNSELECT: Input.inputDown.select = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSUP: Input.inputPress.up = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSDOWN: Input.inputPress.down = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSLEFT: Input.inputPress.left = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSRIGHT: Input.inputPress.right = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONA: Input.inputPress.A = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONB: Input.inputPress.B = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONC: Input.inputPress.C = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONX: Input.inputPress.X = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONY: Input.inputPress.Y = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONZ: Input.inputPress.Z = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONL: Input.inputPress.L = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSBUTTONR: Input.inputPress.R = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSSTART: Input.inputPress.start = (byte)scriptEng.operands[i]; break;
                        case VAR.INPUTPRESSSELECT: Input.inputPress.select = (byte)scriptEng.operands[i]; break;
                        case VAR.MENU1SELECTION: Text.gameMenu[0].selection1 = scriptEng.operands[i]; break;
                        case VAR.MENU2SELECTION: Text.gameMenu[1].selection1 = scriptEng.operands[i]; break;
                        case VAR.TILELAYERXSIZE: Scene.stageLayouts[arrayVal].xsize = (byte)scriptEng.operands[i]; break;
                        case VAR.TILELAYERYSIZE: Scene.stageLayouts[arrayVal].ysize = (byte)scriptEng.operands[i]; break;
                        case VAR.TILELAYERTYPE: Scene.stageLayouts[arrayVal].type = (byte)scriptEng.operands[i]; break;
                        case VAR.TILELAYERANGLE:
                            {
                                int angle = scriptEng.operands[i] + 0x200;
                                if (scriptEng.operands[i] >= 0)
                                    angle = scriptEng.operands[i];
                                Scene.stageLayouts[arrayVal].angle = angle & 0x1FF;
                                break;
                            }
                        case VAR.TILELAYERXPOS: Scene.stageLayouts[arrayVal].xpos = scriptEng.operands[i]; break;
                        case VAR.TILELAYERYPOS: Scene.stageLayouts[arrayVal].ypos = scriptEng.operands[i]; break;
                        case VAR.TILELAYERZPOS: Scene.stageLayouts[arrayVal].zpos = scriptEng.operands[i]; break;
                        case VAR.TILELAYERPARALLAXFACTOR: Scene.stageLayouts[arrayVal].parallaxFactor = scriptEng.operands[i]; break;
                        case VAR.TILELAYERSCROLLSPEED: Scene.stageLayouts[arrayVal].scrollSpeed = scriptEng.operands[i]; break;
                        case VAR.TILELAYERSCROLLPOS: Scene.stageLayouts[arrayVal].scrollPos = scriptEng.operands[i]; break;
                        case VAR.TILELAYERDEFORMATIONOFFSET: Scene.stageLayouts[arrayVal].deformationOffset = scriptEng.operands[i]; break;
                        case VAR.TILELAYERDEFORMATIONOFFSETW: Scene.stageLayouts[arrayVal].deformationOffsetW = scriptEng.operands[i]; break;
                        case VAR.HPARALLAXPARALLAXFACTOR: Scene.hParallax.parallaxFactor[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.HPARALLAXSCROLLSPEED: Scene.hParallax.scrollSpeed[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.HPARALLAXSCROLLPOS: Scene.hParallax.scrollPos[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.VPARALLAXPARALLAXFACTOR: Scene.vParallax.parallaxFactor[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.VPARALLAXSCROLLSPEED: Scene.vParallax.scrollSpeed[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.VPARALLAXSCROLLPOS: Scene.vParallax.scrollPos[arrayVal] = scriptEng.operands[i]; break;
                        case VAR.SCENE3DVERTEXCOUNT: Scene3D.vertexCount = scriptEng.operands[i]; break;
                        case VAR.SCENE3DFACECOUNT: Scene3D.faceCount = scriptEng.operands[i]; break;
                        case VAR.SCENE3DPROJECTIONX: Scene3D.projectionX = scriptEng.operands[i]; break;
                        case VAR.SCENE3DPROJECTIONY: Scene3D.projectionY = scriptEng.operands[i]; break;
#if !RETRO_REV00
                        case VAR.SCENE3DFOGCOLOR: Scene3D.fogColor = scriptEng.operands[i]; break;
                        case VAR.SCENE3DFOGSTRENGTH: Scene3D.fogStrength = scriptEng.operands[i]; break;
#endif
                        case VAR.VERTEXBUFFERX: Scene3D.vertexBuffer[arrayVal].x = scriptEng.operands[i]; break;
                        case VAR.VERTEXBUFFERY: Scene3D.vertexBuffer[arrayVal].y = scriptEng.operands[i]; break;
                        case VAR.VERTEXBUFFERZ: Scene3D.vertexBuffer[arrayVal].z = scriptEng.operands[i]; break;
                        case VAR.VERTEXBUFFERU: Scene3D.vertexBuffer[arrayVal].u = scriptEng.operands[i]; break;
                        case VAR.VERTEXBUFFERV: Scene3D.vertexBuffer[arrayVal].v = scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERA: Scene3D.faceBuffer[arrayVal].a = scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERB: Scene3D.faceBuffer[arrayVal].b = scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERC: Scene3D.faceBuffer[arrayVal].c = scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERD: Scene3D.faceBuffer[arrayVal].d = scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERFLAG: Scene3D.faceBuffer[arrayVal].flag = (byte)scriptEng.operands[i]; break;
                        case VAR.FACEBUFFERCOLOR: Scene3D.faceBuffer[arrayVal].color = scriptEng.operands[i]; break;
                        case VAR.SAVERAM: SaveData.saveRAM[arrayVal] = (byte)scriptEng.operands[i]; break;
                        case VAR.ENGINESTATE: Engine.gameMode = scriptEng.operands[i]; break;
#if RETRO_REV00
                    case ScrVar.ENGINEMESSAGE: break;
#endif
                        case VAR.ENGINELANGUAGE: Engine.language = scriptEng.operands[i]; break;
                        case VAR.ENGINEONLINEACTIVE: Engine.onlineActive = scriptEng.operands[i] != 0; break;
                        case VAR.ENGINESFXVOLUME:
                            Audio.sfxVolume = scriptEng.operands[i];
                            Audio.SetGameVolumes(Audio.bgmVolume, Audio.sfxVolume);
                            break;
                        case VAR.ENGINEBGMVOLUME:
                            Audio.bgmVolume = scriptEng.operands[i];
                            Audio.SetGameVolumes(Audio.bgmVolume, Audio.sfxVolume);
                            break;
                        case VAR.ENGINETRIALMODE: Engine.trialMode = scriptEng.operands[i] != 0; break;
                        case VAR.ENGINEDEVICETYPE: Engine.hapticsEnabled = scriptEng.operands[i] != 0; break;
                    }
                }
                else if (opcodeType == ScriptVarTypes.SCRIPTINTCONST)
                { // int constant
                    scriptDataPtr++;
                }
                else if (opcodeType == ScriptVarTypes.SCRIPTSTRCONST)
                { // string constant
                    int strLen = scriptData[scriptDataPtr++];
                    for (int c = 0; c < strLen; ++c)
                    {
                        switch (c % 4)
                        {
                            case 0: break;
                            case 1: break;
                            case 2: break;
                            case 3: ++scriptDataPtr; break;
                            default: break;
                        }
                    }
                    scriptDataPtr++;
                }
            }
        }
    }

    public static void ClearScriptData()
    {
        Helpers.Memset(scriptData, 0);
        Helpers.Memset(jumpTableData, 0);

        Helpers.Memset(foreachStack, -1);
        Helpers.Memset(jumpTableStack, 0);
        Helpers.Memset(functionStack, 0);

        Animation.scriptFrameCount = 0;

        scriptCodePos = 0;
        jumpTablePos = 0;
        jumpTableStackPos = 0;
        functionStackPos = 0;

        scriptDataPos = 0;
        scriptDataOffset = 0;
        jumpTableDataPos = 0;
        jumpTableDataOffset = 0;

        Animation.ClearAnimationData();

        for (int o = 0; o < Objects.OBJECT_COUNT; ++o)
        {
            ObjectScript scriptInfo = objectScriptList[o] = new ObjectScript();
            scriptInfo.eventMain.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventMain.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.eventDraw.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventDraw.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.eventStartup.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventStartup.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.frameListOffset = 0;
            scriptInfo.spriteSheetId = 0;
            scriptInfo.animFile = Animation.animationFileList[0];
            Objects.typeNames[o] = null;
        }

        //for (int s = globalSFXCount; s < globalSFXCount + stageSFXCount; ++s)
        //{
        //    sfxNames[s][0] = 0;
        //}

        for (int f = 0; f < FUNCTION_COUNT; ++f)
        {
            functionScriptList[f].scriptCodePtr = SCRIPTDATA_COUNT - 1;
            functionScriptList[f].jumpTablePtr = JUMPTABLE_COUNT - 1;
        }

        Objects.SetObjectTypeName("Blank Object", 0 /*OBJ_TYPE_BLANKOBJECT*/);
    }
}
