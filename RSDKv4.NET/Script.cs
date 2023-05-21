#define RETRO_REV02

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using RSDKv4.Bytecode;
using RSDKv4.Utility;

namespace RSDKv4;

public class Script
{
    public const int SCRIPTDATA_COUNT = 0x40000;
    public const int JUMPTABLE_COUNT = 0x4000;
    public const int FUNCTION_COUNT = 0x200;

    public const int JUMPSTACK_COUNT = 0x400;
    public const int FUNCSTACK_COUNT = 0x400;
    public const int FORSTACK_COUNT = 0x400;

    public ObjectScript[] objectScriptList = new ObjectScript[Objects.OBJECT_COUNT];
    public ScriptPtr[] functionScriptList = new ScriptPtr[FUNCTION_COUNT];

    public ScriptEngine state = new ScriptEngine();

    private readonly int[] scriptData = new int[SCRIPTDATA_COUNT];
    private readonly int[] jumpTableData = new int[JUMPTABLE_COUNT];
    private readonly int[] jumpTableStack = new int[JUMPSTACK_COUNT];
    private readonly int[] functionStack = new int[FUNCSTACK_COUNT];
    private readonly int[] foreachStack = new int[FORSTACK_COUNT];

    private int scriptCodePos = 0;
    private int jumpTablePos = 0;
    private int jumpTableStackPos = 0;
    private int functionStackPos = 0;
    private int foreachStackPos = 0;

    private char[] scriptTextBuffer = new char[SCRIPTDATA_COUNT]; // this is 32K, why

    private BytecodeTranslator translator = new BytecodeTranslator(EngineRevision.Rev2);

    private Animation Animation;
    private Audio Audio;
    private Collision Collision;
    private Drawing Drawing;
    private Engine Engine;
    private FileIO FileIO;
    private Font Font;
    private Input Input;
    private Objects Objects;
    private Palette Palette;
    private SaveData SaveData;
    private Scene Scene;
    private Scene3D Scene3D;
    private Text Text;

    //
    // TODO: Function/Variable rewriter
    // Code should be able to dynamically adjust between RSDK revisions by modifying script variable
    // & function indicies on the fly at runtime. Get rid of RETRO_REV01/02/03 etc.
    //
    // This will probably just be a giant lookup table
    //

    private readonly FunctionInfo[] functions = new[]
    {
        new FunctionInfo("End", 0), // End of Script
        new FunctionInfo("Equal", 2), // Equal
        new FunctionInfo("Add", 2), // Add
        new FunctionInfo("Sub", 2), // Subtract
        new FunctionInfo("Inc", 1), // Increment
        new FunctionInfo("Dec", 1), // Decrement
        new FunctionInfo("Mul", 2), // Multiply
        new FunctionInfo("Div", 2), // Divide
        new FunctionInfo("ShR", 2), // Bit Shift Right
        new FunctionInfo("ShL", 2), // Bit Shift Left
        new FunctionInfo("And", 2), // Bitwise And
        new FunctionInfo("Or", 2), // Bitwise Or
        new FunctionInfo("Xor", 2), // Bitwise Xor
        new FunctionInfo("Mod", 2), // Mod
        new FunctionInfo("FlipSign", 1), // Flips the Sign of the value

        new FunctionInfo("CheckEqual", 2), // compare a=b, return result in CheckResult Variable
        new FunctionInfo("CheckGreater", 2), // compare a>b, return result in CheckResult Variable
        new FunctionInfo("CheckLower", 2), // compare a<b, return result in CheckResult Variable
        new FunctionInfo("CheckNotEqual", 2), // compare a!=b, return result in CheckResult Variable

        new FunctionInfo("IfEqual", 3), // compare a=b, jump if condition met
        new FunctionInfo("IfGreater", 3), // compare a>b, jump if condition met
        new FunctionInfo("IfGreaterOrEqual", 3), // compare a>=b, jump if condition met
        new FunctionInfo("IfLower", 3), // compare a<b, jump if condition met
        new FunctionInfo("IfLowerOrEqual", 3), // compare a<=b, jump if condition met
        new FunctionInfo("IfNotEqual", 3), // compare a!=b, jump if condition met
        new FunctionInfo("else", 0), // The else for an if statement
        new FunctionInfo("endif", 0), // The end if

        new FunctionInfo("WEqual", 3), // compare a=b, loop if condition met
        new FunctionInfo("WGreater", 3), // compare a>b, loop if condition met
        new FunctionInfo("WGreaterOrEqual", 3), // compare a>=b, loop if condition met
        new FunctionInfo("WLower", 3), // compare a<b, loop if condition met
        new FunctionInfo("WLowerOrEqual", 3), // compare a<=b, loop if condition met
        new FunctionInfo("WNotEqual", 3), // compare a!=b, loop if condition met
        new FunctionInfo("loop", 0), // While Loop marker

        new FunctionInfo("ForEachActive", 3), // foreach loop, iterates through object group lists only if they are active and interaction is true
        new FunctionInfo("ForEachAll", 3), // foreach loop, iterates through objects matching type
        new FunctionInfo("next", 0), // foreach loop, next marker

        new FunctionInfo("switch", 2), // Switch Statement
        new FunctionInfo("break", 0), // break
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
        new FunctionInfo("SetPaletteFadeRev0", 7),
        new FunctionInfo("SetPaletteFadeRev1", 6),
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
        new FunctionInfo("MatrixInverse", 1),
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

        new FunctionInfo("LoadFontFile", 1),
        new FunctionInfo("LoadTextFile", 2),
        new FunctionInfo("LoadTextFile", 3),
        new FunctionInfo("GetTextInfo", 5),
        new FunctionInfo("DrawText", 7),
        new FunctionInfo("GetVersionNumber", 2),

        new FunctionInfo("GetTableValue", 3),
        new FunctionInfo("SetTableValue", 3),

        new FunctionInfo("CheckCurrentStageFolder", 1),
        new FunctionInfo("Abs", 1),

        new FunctionInfo("CallNativeFunction", 1),
        new FunctionInfo("CallNativeFunction2", 3),
        new FunctionInfo("CallNativeFunction4", 5),

        new FunctionInfo("SetObjectRange", 1),
        new FunctionInfo("GetObjectValue", 3),
        new FunctionInfo("SetObjectValue", 3),
        new FunctionInfo("CopyObject", 3),
        new FunctionInfo("Print", 3),

        new FunctionInfo("CheckCameraProximity", 4),
        new FunctionInfo("SetScreenCount", 1),
        new FunctionInfo("SetScreenVertices", 5),
        new FunctionInfo("GetInputDeviceID", 2),
        new FunctionInfo("GetFilteredInputDeviceID", 4),
        new FunctionInfo("GetInputDeviceType", 2),
        new FunctionInfo("IsInputDeviceAssigned", 1),
        new FunctionInfo("AssignInputSlotToDevice", 2),
        new FunctionInfo("IsInputSlotAssigned", 1),
        new FunctionInfo("ResetInputSlotAssignments", 0),
    };

    public enum SRC
    {
        SCRIPTVAR = 1,
        SCRIPTINTCONST = 2,
        SCRIPTSTRCONST = 3
    };

    public enum VARARR
    {
        NONE = 0,
        ARRAY = 1,
        ENTNOPLUS1 = 2,
        ENTNOMINUS1 = 3
    };

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
        OBJECTFLOORSENSORLC,
        OBJECTFLOORSENSORRC,
        OBJECTCOLLISIONLEFT,
        OBJECTCOLLISIONTOP,
        OBJECTCOLLISIONRIGHT,
        OBJECTCOLLISIONBOTTOM,
        OBJECTOUTOFBOUNDSREV0,
        OBJECTOUTOFBOUNDSREV1,
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
        SCENE3DFOGCOLOR,
        SCENE3DFOGSTRENGTH,
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
        ENGINEMESSAGE,
        ENGINELANGUAGE,
        ENGINEONLINEACTIVE,
        ENGINESFXVOLUME,
        ENGINEBGMVOLUME,
        ENGINETRIALMODE,
        ENGINEDEVICETYPE,
        ENGINEPLATFORMID,
        SCREENCURRENTID,
        CAMERAENABLED,
        CAMERATARGET,
        CAMERASTYLE,
        CAMERAXPOS,
        CAMERAYPOS,
        CAMERAADJUSTY,
        HAPTICSENABLED,
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
        SETPALETTEFADEREV0,
        SETPALETTEFADEREV1,
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
        MATRIXINVERSE,
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
        LOADTEXTFONT,
        LOADTEXTFILEREV0,
        LOADTEXTFILEREV2,
        GETTEXTINFO,
        DRAWTEXT,
        GETVERSIONNUMBER,
        GETTABLEVALUE,
        SETTABLEVALUE,
        CHECKCURRENTSTAGEFOLDER,
        ABS,
        CALLNATIVEFUNCTION,
        CALLNATIVEFUNCTION2,
        CALLNATIVEFUNCTION4,
        SETOBJECTRANGE,
        GETOBJECTVALUE,
        SETOBJECTVALUE,
        COPYOBJECT,
        PRINT,
        CHECKCAMERAPROXIMITY,
        SETSCREENCOUNT,
        SETSCREENVERTICES,
        GETINPUTDEVICEID,
        GETFILTEREDINPUTDEVICEID,
        GETINPUTDEVICETYPE,
        ISINPUTDEVICEASSIGNED,
        ASSIGNINPUTSLOTTODEVICE,
        ISSLOTASSIGNED,
        RESETINPUTSLOTASSIGNMENTS,
        MAX_CNT
    }

    public void Initialize(Engine engine)
    {
        Animation = engine.Animation;
        Audio = engine.Audio;
        Collision = engine.Collision;
        Drawing = engine.Drawing;
        Engine = engine;
        FileIO = engine.FileIO;
        Font = engine.Font;
        Input = engine.Input;
        Objects = engine.Objects;
        Palette = engine.Palette;
        SaveData = engine.SaveData;
        Scene = engine.Scene;
        Scene3D = engine.Scene3D;
        Text = engine.Text;
    }

    public void LoadBytecode(int stageListID, int scriptID)
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
            case 4:
                scriptPath = "Bytecode/GlobalCode.bin";
                break;
            default:
                return;
        }

        if (FileIO.LoadFile(scriptPath, out _))
        {
            byte fileBuffer;
            int scrOffset = scriptCodePos;
            int scriptCodeCount = FileIO.ReadInt32();

            while (scriptCodeCount > 0)
            {
                fileBuffer = FileIO.ReadByte();
                int blockSize = fileBuffer & 0x7F;
                if (fileBuffer >= 0x80)
                {
                    while (blockSize > 0)
                    {
                        int data = FileIO.ReadInt32();
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
            int jumpDataCnt = FileIO.ReadInt32();

            while (jumpDataCnt > 0)
            {
                fileBuffer = FileIO.ReadByte();
                int blockSize = fileBuffer & 0x7F;
                if (fileBuffer >= 0x80)
                {
                    while (blockSize > 0)
                    {
                        int data = FileIO.ReadInt32();
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

            int scriptCount = FileIO.ReadInt16();

            int objType = scriptID;
            for (int i = 0; i < scriptCount; ++i)
            {
                objectScriptList[objType].eventUpdate.scriptCodePtr = FileIO.ReadInt32();
                objectScriptList[objType].eventDraw.scriptCodePtr = FileIO.ReadInt32();
                objectScriptList[objType++].eventStartup.scriptCodePtr = FileIO.ReadInt32();
            }

            objType = scriptID;
            for (int i = 0; i < scriptCount; ++i)
            {
                objectScriptList[objType].eventUpdate.jumpTablePtr = FileIO.ReadInt32();
                objectScriptList[objType].eventDraw.jumpTablePtr = FileIO.ReadInt32();
                objectScriptList[objType++].eventStartup.jumpTablePtr = FileIO.ReadInt32();
            }

            int functionCount = FileIO.ReadInt16();

            for (int i = 0; i < functionCount; ++i)
            {
                functionScriptList[i].scriptCodePtr = FileIO.ReadInt32();
            }

            for (int i = 0; i < functionCount; ++i)
            {
                functionScriptList[i].jumpTablePtr = FileIO.ReadInt32();
            }

            FileIO.CloseFile();
        }
    }

    public void ProcessScript(int scriptCodePtr, int jumpTablePtr, byte scriptEvent)
    {
        if (scriptData[scriptCodePtr] <= 0) return;

        bool running = true;
        int scriptDataPtr = scriptCodePtr;
        // int jumpTableDataPtr = jumpTablePtr;
        jumpTableStackPos = 0;
        functionStackPos = 0;
        foreachStackPos = 0;

        while (running)
        {
            int rawOpcode = scriptData[scriptDataPtr++];

            translator.TranslateFunction(rawOpcode, out var opcode);

            int loadStoreSize = functions[(int)opcode].opcodeSize;
            int scriptCodeOffset = scriptDataPtr;

            scriptTextBuffer[0] = '\0';
            string scriptText = "";

            // Get Values
            for (int i = 0; i < loadStoreSize; ++i)
            {
                var loadType = (SRC)scriptData[scriptDataPtr++];

                //Debug.WriteLine("SCRIPT: Get value {0}", opcodeType);

                if (loadType == SRC.SCRIPTVAR)
                {
                    int arrayVal = 0;
                    switch ((VARARR)scriptData[scriptDataPtr++])
                    {
                        case VARARR.NONE:
                            arrayVal = Objects.objectEntityPos;
                            break;
                        case VARARR.ARRAY:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = state.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOPLUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = state.arrayPosition[scriptData[scriptDataPtr++]] + Objects.objectEntityPos;
                            else
                                arrayVal = scriptData[scriptDataPtr++] + Objects.objectEntityPos;
                            break;
                        case VARARR.ENTNOMINUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos - state.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos - scriptData[scriptDataPtr++];
                            break;
                        default: break;
                    }

                    // Variables
                    var rawVariable = scriptData[scriptDataPtr++];
                    translator.TranslateVariable(rawVariable, out var variable);
                    //Debug.WriteLine("SCRIPT: Get variable {0}", variable);
                    switch (variable)
                    {
                        default: break;
                        case VAR.TEMP0:
                            state.operands[i] = state.temp[0];
                            break;
                        case VAR.TEMP1:
                            state.operands[i] = state.temp[1];
                            break;
                        case VAR.TEMP2:
                            state.operands[i] = state.temp[2];
                            break;
                        case VAR.TEMP3:
                            state.operands[i] = state.temp[3];
                            break;
                        case VAR.TEMP4:
                            state.operands[i] = state.temp[4];
                            break;
                        case VAR.TEMP5:
                            state.operands[i] = state.temp[5];
                            break;
                        case VAR.TEMP6:
                            state.operands[i] = state.temp[6];
                            break;
                        case VAR.TEMP7:
                            state.operands[i] = state.temp[7];
                            break;
                        case VAR.CHECKRESULT:
                            state.operands[i] = state.checkResult;
                            break;
                        case VAR.ARRAYPOS0:
                            state.operands[i] = state.arrayPosition[0];
                            break;
                        case VAR.ARRAYPOS1:
                            state.operands[i] = state.arrayPosition[1];
                            break;
                        case VAR.ARRAYPOS2:
                            state.operands[i] = state.arrayPosition[2];
                            break;
                        case VAR.ARRAYPOS3:
                            state.operands[i] = state.arrayPosition[3];
                            break;
                        case VAR.ARRAYPOS4:
                            state.operands[i] = state.arrayPosition[4];
                            break;
                        case VAR.ARRAYPOS5:
                            state.operands[i] = state.arrayPosition[5];
                            break;
                        case VAR.ARRAYPOS6:
                            state.operands[i] = state.arrayPosition[6];
                            break;
                        case VAR.ARRAYPOS7:
                            state.operands[i] = state.arrayPosition[7];
                            break;
                        case VAR.GLOBAL:
                            state.operands[i] = Engine.globalVariables[arrayVal];
                            break;
                        case VAR.LOCAL:
                            state.operands[i] = scriptData[arrayVal];
                            break;
                        case VAR.OBJECTENTITYPOS:
                            state.operands[i] = arrayVal;
                            break;
                        case VAR.OBJECTGROUPID:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].groupID;
                                break;
                            }
                        case VAR.OBJECTTYPE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].type;
                                break;
                            }
                        case VAR.OBJECTPROPERTYVALUE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].propertyValue;
                                break;
                            }
                        case VAR.OBJECTXPOS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].xpos;
                                break;
                            }
                        case VAR.OBJECTYPOS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].ypos;
                                break;
                            }
                        case VAR.OBJECTIXPOS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].xpos >> 16;
                                break;
                            }
                        case VAR.OBJECTIYPOS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].ypos >> 16;
                                break;
                            }
                        case VAR.OBJECTXVEL:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].xvel;
                                break;
                            }
                        case VAR.OBJECTYVEL:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].yvel;
                                break;
                            }
                        case VAR.OBJECTSPEED:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].speed;
                                break;
                            }
                        case VAR.OBJECTSTATE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].state;
                                break;
                            }
                        case VAR.OBJECTROTATION:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].rotation;
                                break;
                            }
                        case VAR.OBJECTSCALE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].scale;
                                break;
                            }
                        case VAR.OBJECTPRIORITY:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].priority;
                                break;
                            }
                        case VAR.OBJECTDRAWORDER:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].drawOrder;
                                break;
                            }
                        case VAR.OBJECTDIRECTION:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].direction;
                                break;
                            }
                        case VAR.OBJECTINKEFFECT:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].inkEffect;
                                break;
                            }
                        case VAR.OBJECTALPHA:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].alpha;
                                break;
                            }
                        case VAR.OBJECTFRAME:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].frame;
                                break;
                            }
                        case VAR.OBJECTANIMATION:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].animation;
                                break;
                            }
                        case VAR.OBJECTPREVANIMATION:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].prevAnimation;
                                break;
                            }
                        case VAR.OBJECTANIMATIONSPEED:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].animationSpeed;
                                break;
                            }
                        case VAR.OBJECTANIMATIONTIMER:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].animationTimer;
                                break;
                            }
                        case VAR.OBJECTANGLE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].angle;
                                break;
                            }
                        case VAR.OBJECTLOOKPOSX:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].lookPosX;
                                break;
                            }
                        case VAR.OBJECTLOOKPOSY:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].lookPosY;
                                break;
                            }
                        case VAR.OBJECTCOLLISIONMODE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].collisionMode;
                                break;
                            }
                        case VAR.OBJECTCOLLISIONPLANE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].collisionPlane;
                                break;
                            }
                        case VAR.OBJECTCONTROLMODE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].controlMode;
                                break;
                            }
                        case VAR.OBJECTCONTROLLOCK:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].controlLock;
                                break;
                            }
                        case VAR.OBJECTPUSHING:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].pushing;
                                break;
                            }
                        case VAR.OBJECTVISIBLE:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].visible ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTTILECOLLISIONS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].tileCollisions ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTINTERACTION:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].objectInteractions ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTGRAVITY:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].gravity ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTUP:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].up ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTDOWN:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].down ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTLEFT:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].left ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTRIGHT:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].right ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTJUMPPRESS:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].jumpPress ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTJUMPHOLD:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].jumpHold ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTSCROLLTRACKING:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].scrollTracking ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORL:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[0];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORC:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[1];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORR:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[2];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORLC:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[3];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORRC:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].floorSensors[4];
                                break;
                            }
                        case VAR.OBJECTCOLLISIONLEFT:
                            {
                                AnimationFile animFile = objectScriptList[Objects.objectEntityList[arrayVal].type].animFile;
                                Entity ent = Objects.objectEntityList[arrayVal];
                                if (animFile != null)
                                {
                                    int h = Animation.animFrames[Animation.animationList[animFile.animListOffset + ent.animation].frameListOffset + ent.frame].hitboxId;
                                    state.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].left[0];
                                }
                                else
                                {
                                    state.operands[i] = 0;
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

                                    state.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].top[0];
                                }
                                else
                                {
                                    state.operands[i] = 0;
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

                                    state.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].right[0];
                                }
                                else
                                {
                                    state.operands[i] = 0;
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

                                    state.operands[i] = Animation.hitboxList[animFile.hitboxListOffset + h].bottom[0];
                                }
                                else
                                {
                                    state.operands[i] = 0;
                                }

                                break;
                            }
                        case VAR.OBJECTOUTOFBOUNDSREV0:
                            {
                                int x = Objects.objectEntityList[arrayVal].xpos >> 16;
                                int y = Objects.objectEntityList[arrayVal].ypos >> 16;

                                int boundL = Scene.xScrollOffset - Objects.OBJECT_BORDER_X1;
                                int boundR = Scene.xScrollOffset + Objects.OBJECT_BORDER_X2;
                                int boundT = Scene.yScrollOffset - Objects.OBJECT_BORDER_Y1;
                                int boundB = Scene.yScrollOffset + Objects.OBJECT_BORDER_Y2;

                                state.operands[i] = (x <= boundL || x >= boundR || y <= boundT || y >= boundB) ? 1 : 0;
                                break;
                            }
                        case VAR.OBJECTOUTOFBOUNDSREV1:
                            {
                                int boundX1_2P = -(0x200 << 16);
                                int boundX2_2P = 0x200 << 16;
                                int boundX3_2P = -(0x180 << 16);
                                int boundX4_2P = 0x180 << 16;

                                int boundY1_2P = -(0x180 << 16);
                                int boundY2_2P = 0x180 << 16;
                                int boundY3_2P = -(0x100 << 16);
                                int boundY4_2P = 0x100 << 16;

                                Entity entPtr = Objects.objectEntityList[arrayVal];
                                int x = entPtr.xpos >> 16;
                                int y = entPtr.ypos >> 16;

                                if (entPtr.priority == PRIORITY.BOUNDS_SMALL || entPtr.priority == PRIORITY.ACTIVE_SMALL)
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

                                        state.operands[i] = (oobP1 && oobP2) ? 1 : 0;
                                    }
                                    else
                                    {
                                        int boundL = Scene.xScrollOffset - Objects.OBJECT_BORDER_X3;
                                        int boundR = Scene.xScrollOffset + Objects.OBJECT_BORDER_X4;
                                        int boundT = Scene.yScrollOffset - Objects.OBJECT_BORDER_Y3;
                                        int boundB = Scene.yScrollOffset + Objects.OBJECT_BORDER_Y4;

                                        state.operands[i] = (x <= boundL || x >= boundR || y <= boundT || y >= boundB) ? 1 : 0;
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

                                        state.operands[i] = oobP1 ? 1 : 0;
                                        state.operands[i] = oobP2 ? 1 : 0;

                                        state.operands[i] = (oobP1 && oobP2) ? 1 : 0;
                                    }
                                    else
                                    {
                                        int boundL = Scene.xScrollOffset - Objects.OBJECT_BORDER_X1;
                                        int boundR = Scene.xScrollOffset + Objects.OBJECT_BORDER_X2;
                                        int boundT = Scene.yScrollOffset - Objects.OBJECT_BORDER_Y1;
                                        int boundB = Scene.yScrollOffset + Objects.OBJECT_BORDER_Y2;

                                        state.operands[i] = (x <= boundL || x >= boundR || y <= boundT || y >= boundB) ? 1 : 0;
                                    }
                                }
                                break;
                            }
                        case VAR.OBJECTSPRITESHEET:
                            {
                                state.operands[i] = objectScriptList[Objects.objectEntityList[arrayVal].type].spriteSheetId;
                                break;
                            }
                        case VAR.OBJECTVALUE0:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[0];
                                break;
                            }
                        case VAR.OBJECTVALUE1:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[1];
                                break;
                            }
                        case VAR.OBJECTVALUE2:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[2];
                                break;
                            }
                        case VAR.OBJECTVALUE3:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[3];
                                break;
                            }
                        case VAR.OBJECTVALUE4:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[4];
                                break;
                            }
                        case VAR.OBJECTVALUE5:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[5];
                                break;
                            }
                        case VAR.OBJECTVALUE6:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[6];
                                break;
                            }
                        case VAR.OBJECTVALUE7:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[7];
                                break;
                            }
                        case VAR.OBJECTVALUE8:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[8];
                                break;
                            }
                        case VAR.OBJECTVALUE9:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[9];
                                break;
                            }
                        case VAR.OBJECTVALUE10:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[10];
                                break;
                            }
                        case VAR.OBJECTVALUE11:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[11];
                                break;
                            }
                        case VAR.OBJECTVALUE12:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[12];
                                break;
                            }
                        case VAR.OBJECTVALUE13:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[13];
                                break;
                            }
                        case VAR.OBJECTVALUE14:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[14];
                                break;
                            }
                        case VAR.OBJECTVALUE15:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[15];
                                break;
                            }
                        case VAR.OBJECTVALUE16:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[16];
                                break;
                            }
                        case VAR.OBJECTVALUE17:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[17];
                                break;
                            }
                        case VAR.OBJECTVALUE18:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[18];
                                break;
                            }
                        case VAR.OBJECTVALUE19:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[19];
                                break;
                            }
                        case VAR.OBJECTVALUE20:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[20];
                                break;
                            }
                        case VAR.OBJECTVALUE21:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[21];
                                break;
                            }
                        case VAR.OBJECTVALUE22:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[22];
                                break;
                            }
                        case VAR.OBJECTVALUE23:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[23];
                                break;
                            }
                        case VAR.OBJECTVALUE24:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[24];
                                break;
                            }
                        case VAR.OBJECTVALUE25:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[25];
                                break;
                            }
                        case VAR.OBJECTVALUE26:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[26];
                                break;
                            }
                        case VAR.OBJECTVALUE27:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[27];
                                break;
                            }
                        case VAR.OBJECTVALUE28:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[28];
                                break;
                            }
                        case VAR.OBJECTVALUE29:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[29];
                                break;
                            }
                        case VAR.OBJECTVALUE30:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[30];
                                break;
                            }
                        case VAR.OBJECTVALUE31:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[31];
                                break;
                            }
                        case VAR.OBJECTVALUE32:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[32];
                                break;
                            }
                        case VAR.OBJECTVALUE33:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[33];
                                break;
                            }
                        case VAR.OBJECTVALUE34:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[34];
                                break;
                            }
                        case VAR.OBJECTVALUE35:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[35];
                                break;
                            }
                        case VAR.OBJECTVALUE36:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[36];
                                break;
                            }
                        case VAR.OBJECTVALUE37:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[37];
                                break;
                            }
                        case VAR.OBJECTVALUE38:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[38];
                                break;
                            }
                        case VAR.OBJECTVALUE39:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[39];
                                break;
                            }
                        case VAR.OBJECTVALUE40:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[40];
                                break;
                            }
                        case VAR.OBJECTVALUE41:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[41];
                                break;
                            }
                        case VAR.OBJECTVALUE42:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[42];
                                break;
                            }
                        case VAR.OBJECTVALUE43:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[43];
                                break;
                            }
                        case VAR.OBJECTVALUE44:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[44];
                                break;
                            }
                        case VAR.OBJECTVALUE45:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[45];
                                break;
                            }
                        case VAR.OBJECTVALUE46:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[46];
                                break;
                            }
                        case VAR.OBJECTVALUE47:
                            {
                                state.operands[i] = Objects.objectEntityList[arrayVal].values[47];
                                break;
                            }
                        case VAR.STAGESTATE:
                            state.operands[i] = Scene.stageMode;
                            break;
                        case VAR.STAGEACTIVELIST:
                            state.operands[i] = Scene.activeStageList;
                            break;
                        case VAR.STAGELISTPOS:
                            state.operands[i] = Scene.stageListPosition;
                            break;
                        case VAR.STAGETIMEENABLED:
                            state.operands[i] = Scene.timeEnabled ? 1 : 0;
                            break;
                        case VAR.STAGEMILLISECONDS:
                            state.operands[i] = Scene.stageMilliseconds;
                            break;
                        case VAR.STAGESECONDS:
                            state.operands[i] = Scene.stageSeconds;
                            break;
                        case VAR.STAGEMINUTES:
                            state.operands[i] = Scene.stageMinutes;
                            break;
                        case VAR.STAGEACTNUM:
                            state.operands[i] = Scene.actId;
                            break;
                        case VAR.STAGEPAUSEENABLED:
                            state.operands[i] = Scene.pauseEnabled ? 1 : 0;
                            break;
                        case VAR.STAGELISTSIZE:
                            state.operands[i] = Engine.stageListCount[Scene.activeStageList];
                            break;
                        case VAR.STAGENEWXBOUNDARY1:
                            state.operands[i] = Scene.newXBoundary1;
                            break;
                        case VAR.STAGENEWXBOUNDARY2:
                            state.operands[i] = Scene.newXBoundary2;
                            break;
                        case VAR.STAGENEWYBOUNDARY1:
                            state.operands[i] = Scene.newYBoundary1;
                            break;
                        case VAR.STAGENEWYBOUNDARY2:
                            state.operands[i] = Scene.newYBoundary2;
                            break;
                        case VAR.STAGECURXBOUNDARY1:
                            state.operands[i] = Scene.curXBoundary1;
                            break;
                        case VAR.STAGECURXBOUNDARY2:
                            state.operands[i] = Scene.curXBoundary2;
                            break;
                        case VAR.STAGECURYBOUNDARY1:
                            state.operands[i] = Scene.curYBoundary1;
                            break;
                        case VAR.STAGECURYBOUNDARY2:
                            state.operands[i] = Scene.curYBoundary2;
                            break;
                        case VAR.STAGEDEFORMATIONDATA0:
                            state.operands[i] = Scene.bgDeformationData0[arrayVal];
                            break;
                        case VAR.STAGEDEFORMATIONDATA1:
                            state.operands[i] = Scene.bgDeformationData1[arrayVal];
                            break;
                        case VAR.STAGEDEFORMATIONDATA2:
                            state.operands[i] = Scene.bgDeformationData2[arrayVal];
                            break;
                        case VAR.STAGEDEFORMATIONDATA3:
                            state.operands[i] = Scene.bgDeformationData3[arrayVal];
                            break;
                        case VAR.STAGEWATERLEVEL:
                            state.operands[i] = Scene.waterLevel;
                            break;
                        case VAR.STAGEACTIVELAYER:
                            state.operands[i] = Scene.activeTileLayers[arrayVal];
                            break;
                        case VAR.STAGEMIDPOINT:
                            state.operands[i] = Scene.tLayerMidPoint;
                            break;
                        case VAR.STAGEPLAYERLISTPOS:
                            state.operands[i] = Objects.playerListPos;
                            break;
                        case VAR.STAGEDEBUGMODE:
                            state.operands[i] = Scene.debugMode ? 1 : 0;
                            break;
                        case VAR.STAGEENTITYPOS:
                            state.operands[i] = Objects.objectEntityPos;
                            break;
                        case VAR.SCREENCAMERAENABLED:
                            state.operands[i] = Scene.cameraEnabled ? 1 : 0;
                            break;
                        case VAR.SCREENCAMERATARGET:
                            state.operands[i] = Scene.cameraTarget;
                            break;
                        case VAR.SCREENCAMERASTYLE:
                            state.operands[i] = Scene.cameraStyle;
                            break;
                        case VAR.SCREENCAMERAX:
                            state.operands[i] = Scene.cameraXPos;
                            break;
                        case VAR.SCREENCAMERAY:
                            state.operands[i] = Scene.cameraYPos;
                            break;
                        case VAR.SCREENDRAWLISTSIZE:
                            state.operands[i] = Scene.drawListEntries[arrayVal].listSize;
                            break;
                        case VAR.SCREENXCENTER:
                            state.operands[i] = Drawing.SCREEN_CENTERX;
                            break;
                        case VAR.SCREENYCENTER:
                            state.operands[i] = Drawing.SCREEN_CENTERY;
                            break;
                        case VAR.SCREENXSIZE:
                            state.operands[i] = Drawing.SCREEN_XSIZE;
                            break;
                        case VAR.SCREENYSIZE:
                            state.operands[i] = Drawing.SCREEN_YSIZE;
                            break;
                        case VAR.SCREENXOFFSET:
                            state.operands[i] = Scene.xScrollOffset;
                            break;
                        case VAR.SCREENYOFFSET:
                            state.operands[i] = Scene.yScrollOffset;
                            break;
                        case VAR.SCREENSHAKEX:
                            state.operands[i] = Scene.cameraShakeX;
                            break;
                        case VAR.SCREENSHAKEY:
                            state.operands[i] = Scene.cameraShakeY;
                            break;
                        case VAR.SCREENADJUSTCAMERAY:
                            state.operands[i] = Scene.cameraAdjustY;
                            break;
                        case VAR.TOUCHSCREENDOWN:
                            state.operands[i] = Input.touchDown[arrayVal];
                            break;
                        case VAR.TOUCHSCREENXPOS:
                            state.operands[i] = Input.touchX[arrayVal];
                            break;
                        case VAR.TOUCHSCREENYPOS:
                            state.operands[i] = Input.touchY[arrayVal];
                            break;
                        case VAR.MUSICVOLUME:
                            state.operands[i] = Audio.masterVolume;
                            break;
                        case VAR.MUSICCURRENTTRACK:
                            state.operands[i] = Audio.trackId;
                            break;
                        case VAR.MUSICPOSITION:
                            state.operands[i] = Audio.musicPosition;
                            break;
                        case VAR.INPUTDOWNUP:
                            state.operands[i] = Input.keyDown.up ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNDOWN:
                            state.operands[i] = Input.keyDown.down ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNLEFT:
                            state.operands[i] = Input.keyDown.left ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNRIGHT:
                            state.operands[i] = Input.keyDown.right ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONA:
                            state.operands[i] = Input.keyDown.A ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONB:
                            state.operands[i] = Input.keyDown.B ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONC:
                            state.operands[i] = Input.keyDown.C ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONX:
                            state.operands[i] = Input.keyDown.X ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONY:
                            state.operands[i] = Input.keyDown.Y ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONZ:
                            state.operands[i] = Input.keyDown.Z ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONL:
                            state.operands[i] = Input.keyDown.L ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNBUTTONR:
                            state.operands[i] = Input.keyDown.R ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNSTART:
                            state.operands[i] = Input.keyDown.start ? 1 : 0;
                            break;
                        case VAR.INPUTDOWNSELECT:
                            state.operands[i] = Input.keyDown.select ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSUP:
                            state.operands[i] = Input.keyPress.up ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSDOWN:
                            state.operands[i] = Input.keyPress.down ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSLEFT:
                            state.operands[i] = Input.keyPress.left ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSRIGHT:
                            state.operands[i] = Input.keyPress.right ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONA:
                            state.operands[i] = Input.keyPress.A ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONB:
                            state.operands[i] = Input.keyPress.B ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONC:
                            state.operands[i] = Input.keyPress.C ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONX:
                            state.operands[i] = Input.keyPress.X ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONY:
                            state.operands[i] = Input.keyPress.Y ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONZ:
                            state.operands[i] = Input.keyPress.Z ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONL:
                            state.operands[i] = Input.keyPress.L ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSBUTTONR:
                            state.operands[i] = Input.keyPress.R ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSSTART:
                            state.operands[i] = Input.keyPress.start ? 1 : 0;
                            break;
                        case VAR.INPUTPRESSSELECT:
                            state.operands[i] = Input.keyPress.select ? 1 : 0;
                            break;
                        case VAR.MENU1SELECTION:
                            state.operands[i] = Text.gameMenu[0].selection1;
                            break;
                        case VAR.MENU2SELECTION:
                            state.operands[i] = Text.gameMenu[1].selection1;
                            break;
                        case VAR.TILELAYERXSIZE:
                            state.operands[i] = Scene.stageLayouts[arrayVal].xsize;
                            break;
                        case VAR.TILELAYERYSIZE:
                            state.operands[i] = Scene.stageLayouts[arrayVal].ysize;
                            break;
                        case VAR.TILELAYERTYPE:
                            state.operands[i] = Scene.stageLayouts[arrayVal].type;
                            break;
                        case VAR.TILELAYERANGLE:
                            state.operands[i] = Scene.stageLayouts[arrayVal].angle;
                            break;
                        case VAR.TILELAYERXPOS:
                            state.operands[i] = Scene.stageLayouts[arrayVal].xpos;
                            break;
                        case VAR.TILELAYERYPOS:
                            state.operands[i] = Scene.stageLayouts[arrayVal].ypos;
                            break;
                        case VAR.TILELAYERZPOS:
                            state.operands[i] = Scene.stageLayouts[arrayVal].zpos;
                            break;
                        case VAR.TILELAYERPARALLAXFACTOR:
                            state.operands[i] = Scene.stageLayouts[arrayVal].parallaxFactor;
                            break;
                        case VAR.TILELAYERSCROLLSPEED:
                            state.operands[i] = Scene.stageLayouts[arrayVal].scrollSpeed;
                            break;
                        case VAR.TILELAYERSCROLLPOS:
                            state.operands[i] = Scene.stageLayouts[arrayVal].scrollPos;
                            break;
                        case VAR.TILELAYERDEFORMATIONOFFSET:
                            state.operands[i] = Scene.stageLayouts[arrayVal].deformationOffset;
                            break;
                        case VAR.TILELAYERDEFORMATIONOFFSETW:
                            state.operands[i] = Scene.stageLayouts[arrayVal].deformationOffsetW;
                            break;
                        case VAR.HPARALLAXPARALLAXFACTOR:
                            state.operands[i] = Scene.hParallax.parallaxFactor[arrayVal];
                            break;
                        case VAR.HPARALLAXSCROLLSPEED:
                            state.operands[i] = Scene.hParallax.scrollSpeed[arrayVal];
                            break;
                        case VAR.HPARALLAXSCROLLPOS:
                            state.operands[i] = Scene.hParallax.scrollPos[arrayVal];
                            break;
                        case VAR.VPARALLAXPARALLAXFACTOR:
                            state.operands[i] = Scene.vParallax.parallaxFactor[arrayVal];
                            break;
                        case VAR.VPARALLAXSCROLLSPEED:
                            state.operands[i] = Scene.vParallax.scrollSpeed[arrayVal];
                            break;
                        case VAR.VPARALLAXSCROLLPOS:
                            state.operands[i] = Scene.vParallax.scrollPos[arrayVal];
                            break;
                        case VAR.SCENE3DVERTEXCOUNT:
                            state.operands[i] = Scene3D.vertexCount;
                            break;
                        case VAR.SCENE3DFACECOUNT:
                            state.operands[i] = Scene3D.faceCount;
                            break;
                        case VAR.SCENE3DPROJECTIONX:
                            state.operands[i] = Scene3D.projectionX;
                            break;
                        case VAR.SCENE3DPROJECTIONY:
                            state.operands[i] = Scene3D.projectionY;
                            break;
                        case VAR.SCENE3DFOGCOLOR:
                            state.operands[i] = Scene3D.fogColor;
                            break;
                        case VAR.SCENE3DFOGSTRENGTH:
                            state.operands[i] = Scene3D.fogStrength;
                            break;
                        case VAR.VERTEXBUFFERX:
                            state.operands[i] = Scene3D.vertexBuffer[arrayVal].x;
                            break;
                        case VAR.VERTEXBUFFERY:
                            state.operands[i] = Scene3D.vertexBuffer[arrayVal].y;
                            break;
                        case VAR.VERTEXBUFFERZ:
                            state.operands[i] = Scene3D.vertexBuffer[arrayVal].z;
                            break;
                        case VAR.VERTEXBUFFERU:
                            state.operands[i] = Scene3D.vertexBuffer[arrayVal].u;
                            break;
                        case VAR.VERTEXBUFFERV:
                            state.operands[i] = Scene3D.vertexBuffer[arrayVal].v;
                            break;
                        case VAR.FACEBUFFERA:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].a;
                            break;
                        case VAR.FACEBUFFERB:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].b;
                            break;
                        case VAR.FACEBUFFERC:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].c;
                            break;
                        case VAR.FACEBUFFERD:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].d;
                            break;
                        case VAR.FACEBUFFERFLAG:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].flag;
                            break;
                        case VAR.FACEBUFFERCOLOR:
                            state.operands[i] = Scene3D.faceBuffer[arrayVal].color;
                            break;
                        case VAR.SAVERAM:
                            state.operands[i] = SaveData.saveRAM[arrayVal];
                            break;
                        case VAR.ENGINESTATE:
                            state.operands[i] = Engine.engineState;
                            break;
                        case VAR.ENGINEMESSAGE:
                            state.operands[i] = Engine.message;
                            break;
                        case VAR.ENGINELANGUAGE:
                            state.operands[i] = Engine.language;
                            break;
                        case VAR.ENGINEONLINEACTIVE:
                            state.operands[i] = Engine.onlineActive ? 1 : 0;
                            break;
                        case VAR.ENGINESFXVOLUME:
                            state.operands[i] = Audio.sfxVolume;
                            break;
                        case VAR.ENGINEBGMVOLUME:
                            state.operands[i] = Audio.bgmVolume;
                            break;
                        case VAR.ENGINETRIALMODE:
                            state.operands[i] = Engine.trialMode ? 1 : 0;
                            break;
                        case VAR.ENGINEDEVICETYPE:
                            state.operands[i] = Engine.deviceType;
                            break;
                        case VAR.SCREENCURRENTID:
                            state.operands[i] = 0;
                            break;
                        case VAR.CAMERAENABLED:
                            state.operands[i] = Scene.cameraEnabled ? 1 : 0;
                            break;
                        case VAR.CAMERATARGET:
                            state.operands[i] = Scene.cameraTarget;
                            break;
                        case VAR.CAMERASTYLE:
                            state.operands[i] = Scene.cameraStyle;
                            break;
                        case VAR.CAMERAXPOS:
                            state.operands[i] = Scene.cameraXPos;
                            break;
                        case VAR.CAMERAYPOS:
                            state.operands[i] = Scene.cameraYPos;
                            break;
                        case VAR.CAMERAADJUSTY:
                            state.operands[i] = Scene.cameraAdjustY;
                            break;
                        case VAR.HAPTICSENABLED:
                            state.operands[i] = Engine.hapticsEnabled ? 1 : 0;
                            break;
                    }
                }
                else if (loadType == SRC.SCRIPTINTCONST)
                {
                    // int constant
                    state.operands[i] = scriptData[scriptDataPtr++];
                }
                else if (loadType == SRC.SCRIPTSTRCONST)
                {
                    // string constant
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
                                    scriptTextBuffer[c] = (char)(byte)scriptData[scriptDataPtr++];
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
            switch (opcode)
            {
                default: break;
                case FUNC.END:
                    running = false;
                    break;
                case FUNC.EQUAL:
                    state.operands[0] = state.operands[1];
                    break;
                case FUNC.ADD:
                    state.operands[0] += state.operands[1];
                    break;
                case FUNC.SUB:
                    state.operands[0] -= state.operands[1];
                    break;
                case FUNC.INC:
                    ++state.operands[0];
                    break;
                case FUNC.DEC:
                    --state.operands[0];
                    break;
                case FUNC.MUL:
                    state.operands[0] *= state.operands[1];
                    break;
                case FUNC.DIV:
                    state.operands[0] /= state.operands[1];
                    break;
                case FUNC.SHR:
                    state.operands[0] >>= state.operands[1];
                    break;
                case FUNC.SHL:
                    state.operands[0] <<= state.operands[1];
                    break;
                case FUNC.AND:
                    state.operands[0] &= state.operands[1];
                    break;
                case FUNC.OR:
                    state.operands[0] |= state.operands[1];
                    break;
                case FUNC.XOR:
                    state.operands[0] ^= state.operands[1];
                    break;
                case FUNC.MOD:
                    state.operands[0] %= state.operands[1];
                    break;
                case FUNC.FLIPSIGN:
                    state.operands[0] = -state.operands[0];
                    break;
                case FUNC.CHECKEQUAL:
                    state.checkResult = (state.operands[0] == state.operands[1]) ? 1 : 0;
                    loadStoreSize = 0;
                    break;
                case FUNC.CHECKGREATER:
                    state.checkResult = (state.operands[0] > state.operands[1]) ? 1 : 0;
                    loadStoreSize = 0;
                    break;
                case FUNC.CHECKLOWER:
                    state.checkResult = (state.operands[0] < state.operands[1]) ? 1 : 0;
                    loadStoreSize = 0;
                    break;
                case FUNC.CHECKNOTEQUAL:
                    state.checkResult = (state.operands[0] != state.operands[1]) ? 1 : 0;
                    loadStoreSize = 0;
                    break;
                case FUNC.IFEQUAL:
                    if (state.operands[1] != state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.IFGREATER:
                    if (state.operands[1] <= state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.IFGREATEROREQUAL:
                    if (state.operands[1] < state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.IFLOWER:
                    if (state.operands[1] >= state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.IFLOWEROREQUAL:
                    if (state.operands[1] > state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.IFNOTEQUAL:
                    if (state.operands[1] == state.operands[2])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0]];
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    loadStoreSize = 0;
                    break;
                case FUNC.ELSE:
                    loadStoreSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--] + 1];
                    break;
                case FUNC.ENDIF:
                    loadStoreSize = 0;
                    --jumpTableStackPos;
                    break;
                case FUNC.WEQUAL:
                    if (state.operands[1] == state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.WGREATER:
                    if (state.operands[1] > state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.WGREATEROREQUAL:
                    if (state.operands[1] >= state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.WLOWER:
                    if (state.operands[1] < state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.WLOWEROREQUAL:
                    if (state.operands[1] <= state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.WNOTEQUAL:
                    if (state.operands[1] != state.operands[2])
                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    else
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                    loadStoreSize = 0;
                    break;
                case FUNC.LOOP:
                    loadStoreSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--]];
                    break;
                case FUNC.FOREACHACTIVE:
                    {
                        int groupID = state.operands[1];
                        if (groupID < Objects.TYPEGROUP_COUNT)
                        {
                            int loop = foreachStack[++foreachStackPos] + 1;
                            foreachStack[foreachStackPos] = loop;
                            if (loop >= Objects.objectTypeGroupList[groupID].listSize)
                            {
                                loadStoreSize = 0;
                                foreachStack[foreachStackPos--] = -1;
                                scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                                break;
                            }
                            else
                            {
                                state.operands[2] = Objects.objectTypeGroupList[groupID].entityRefs[loop];
                                jumpTableStack[++jumpTableStackPos] = state.operands[0];
                            }
                        }
                        else
                        {
                            loadStoreSize = 0;
                            scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                        }

                        break;
                    }
                case FUNC.FOREACHALL:
                    {
                        int objType = state.operands[1];
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
                                        loadStoreSize = 0;
                                        foreachStack[foreachStackPos--] = -1;
                                        int off = jumpTableData[jumpTablePtr + state.operands[0] + 1];
                                        scriptDataPtr = scriptCodePtr + off;
                                        break;
                                    }
                                    else if (objType == Objects.objectEntityList[loop].type)
                                    {
                                        state.operands[2] = loop;
                                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
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
                                        loadStoreSize = 0;
                                        foreachStack[foreachStackPos--] = -1;
                                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                                        break;
                                    }
                                    else if (objType == Objects.objectEntityList[loop].type)
                                    {
                                        state.operands[2] = loop;
                                        jumpTableStack[++jumpTableStackPos] = state.operands[0];
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
                            loadStoreSize = 0;
                            scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 1];
                        }

                        break;
                    }
                case FUNC.NEXT:
                    loadStoreSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--]];
                    --foreachStackPos;
                    break;
                case FUNC.SWITCH:
                    jumpTableStack[++jumpTableStackPos] = state.operands[0];
                    if (state.operands[1] < jumpTableData[jumpTablePtr + state.operands[0]]
                        || state.operands[1] > jumpTableData[jumpTablePtr + state.operands[0] + 1])
                        scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + state.operands[0] + 2];
                    else
                        scriptDataPtr = scriptCodePtr
                                        + jumpTableData[jumpTablePtr + state.operands[0] + 4
                                                        + (state.operands[1] - jumpTableData[jumpTablePtr + state.operands[0]])];
                    loadStoreSize = 0;
                    break;
                case FUNC.BREAK:
                    loadStoreSize = 0;
                    scriptDataPtr = scriptCodePtr + jumpTableData[jumpTablePtr + jumpTableStack[jumpTableStackPos--] + 3];
                    break;
                case FUNC.ENDSWITCH:
                    loadStoreSize = 0;
                    --jumpTableStackPos;
                    break;
                case FUNC.RAND:
                    state.operands[0] = FastMath.Rand(state.operands[1]);
                    break;
                case FUNC.SIN:
                    {
                        state.operands[0] = FastMath.Sin512(state.operands[1]);
                        break;
                    }
                case FUNC.COS:
                    {
                        state.operands[0] = FastMath.Cos512(state.operands[1]);
                        break;
                    }
                case FUNC.SIN256:
                    {
                        state.operands[0] = FastMath.Sin256(state.operands[1]);
                        break;
                    }
                case FUNC.COS256:
                    {
                        state.operands[0] = FastMath.Cos256(state.operands[1]);
                        break;
                    }
                case FUNC.ATAN2:
                    {
                        state.operands[0] = FastMath.ArcTan(state.operands[1], state.operands[2]);
                        break;
                    }
                case FUNC.INTERPOLATE:
                    state.operands[0] =
                        (state.operands[2] * (0x100 - state.operands[3]) + state.operands[3] * state.operands[1]) >> 8;
                    break;
                case FUNC.INTERPOLATEXY:
                    state.operands[0] =
                        (state.operands[3] * (0x100 - state.operands[6]) >> 8) + ((state.operands[6] * state.operands[2]) >> 8);
                    state.operands[1] =
                        (state.operands[5] * (0x100 - state.operands[6]) >> 8) + (state.operands[6] * state.operands[4] >> 8);
                    break;
                case FUNC.LOADSPRITESHEET:
                    loadStoreSize = 0;
                    scriptInfo.spriteSheetId = Drawing.AddGraphicsFile(scriptText);
                    break;
                case FUNC.REMOVESPRITESHEET:
                    loadStoreSize = 0;
                    Drawing.RemoveGraphicsFile(scriptText, -1);
                    break;
                case FUNC.DRAWSPRITE:
                    loadStoreSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + state.operands[0]];
                    Drawing.DrawSprite((entity.xpos >> 16) - Scene.xScrollOffset + spriteFrame.pivotX, (entity.ypos >> 16) - Scene.yScrollOffset + spriteFrame.pivotY,
                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWSPRITEXY:
                    loadStoreSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + state.operands[0]];
                    Drawing.DrawSprite((state.operands[1] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                        (state.operands[2] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width, spriteFrame.height,
                        spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWSPRITESCREENXY:
                    loadStoreSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + state.operands[0]];
                    Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY, spriteFrame.width,
                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                    break;
                case FUNC.DRAWTINTRECT:
                    loadStoreSize = 0;
                    Drawing.DrawTintRectangle(state.operands[0], state.operands[1], state.operands[2], state.operands[3]);
                    break;
                case FUNC.DRAWNUMBERS:
                    {
                        loadStoreSize = 0;
                        int i = 10;
                        if (state.operands[6] != 0)
                        {
                            while (state.operands[4] > 0)
                            {
                                int frameID = state.operands[3] % i / (i / 10) + state.operands[0];
                                spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + frameID];
                                Drawing.DrawSprite(spriteFrame.pivotX + state.operands[1], spriteFrame.pivotY + state.operands[2], spriteFrame.width,
                                    spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                state.operands[1] -= state.operands[5];
                                i *= 10;
                                --state.operands[4];
                            }
                        }
                        else
                        {
                            int extra = 10;
                            if (state.operands[3] != 0)
                                extra = 10 * state.operands[3];
                            while (state.operands[4] > 0)
                            {
                                if (extra >= i)
                                {
                                    int frameID = state.operands[3] % i / (i / 10) + state.operands[0];
                                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + frameID];
                                    Drawing.DrawSprite(spriteFrame.pivotX + state.operands[1], spriteFrame.pivotY + state.operands[2], spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                }

                                state.operands[1] -= state.operands[5];
                                i *= 10;
                                --state.operands[4];
                            }
                        }

                        break;
                    }
                case FUNC.DRAWACTNAME:
                    {
                        loadStoreSize = 0;
                        int charID = 0;
                        switch (state.operands[3])
                        {
                            // Draw Mode
                            case 0: // Draw Word 1 (but aligned from the right instead of left)
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
                                        state.operands[1] -= state.operands[5] + state.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += state.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];

                                        state.operands[1] -= spriteFrame.width + state.operands[6];

                                        Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY,
                                            spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    }

                                    charID--;
                                }

                                break;

                            case 1: // Draw Word 1
                                charID = 0;

                                // Draw the first letter as a capital letter, the rest are lowercase (if scriptEng.operands[4] is true, otherwise they're all
                                // uppercase)
                                if (state.operands[4] == 1 && Scene.titleCardText[charID] != 0)
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
                                        state.operands[1] += state.operands[5] + state.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += state.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY,
                                            spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        state.operands[1] += spriteFrame.width + state.operands[6];
                                    }

                                    state.operands[0] += 26;
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
                                        state.operands[1] += state.operands[5] + state.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += state.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY,
                                            spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        state.operands[1] += spriteFrame.width + state.operands[6];
                                    }

                                    charID++;
                                }

                                break;

                            case 2: // Draw Word 2
                                charID = Scene.titleCardWord2;

                                // Draw the first letter as a capital letter, the rest are lowercase (if scriptEng.operands[4] is true, otherwise they're all
                                // uppercase)
                                if (state.operands[4] == 1 && Scene.titleCardText[charID] != 0)
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
                                        state.operands[1] += state.operands[5] + state.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += state.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY,
                                            spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        state.operands[1] += spriteFrame.width + state.operands[6];
                                    }

                                    state.operands[0] += 26;
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
                                        state.operands[1] += state.operands[5] + state.operands[6]; // spaceWidth + spacing
                                    }
                                    else
                                    {
                                        character += state.operands[0];
                                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + character];
                                        Drawing.DrawSprite(state.operands[1] + spriteFrame.pivotX, state.operands[2] + spriteFrame.pivotY,
                                            spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                        state.operands[1] += spriteFrame.width + state.operands[6];
                                    }

                                    charID++;
                                }

                                break;
                        }

                        break;
                    }
                case FUNC.DRAWMENU:
                    loadStoreSize = 0;
                    Drawing.DrawTextMenu(Text.gameMenu[state.operands[0]], state.operands[1], state.operands[2], scriptInfo.spriteSheetId);
                    break;
                case FUNC.SPRITEFRAME:
                    loadStoreSize = 0;
                    if (scriptEvent == EVENT.SETUP && Animation.scriptFrameCount < Animation.SPRITEFRAME_COUNT)
                    {
                        Animation.scriptFrames[Animation.scriptFrameCount].pivotX = state.operands[0];
                        Animation.scriptFrames[Animation.scriptFrameCount].pivotY = state.operands[1];
                        Animation.scriptFrames[Animation.scriptFrameCount].width = state.operands[2];
                        Animation.scriptFrames[Animation.scriptFrameCount].height = state.operands[3];
                        Animation.scriptFrames[Animation.scriptFrameCount].spriteX = state.operands[4];
                        Animation.scriptFrames[Animation.scriptFrameCount].spriteY = state.operands[5];
                        ++Animation.scriptFrameCount;
                    }

                    break;
                case FUNC.EDITFRAME:
                    {
                        loadStoreSize = 0;
                        spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + state.operands[0]];

                        spriteFrame.pivotX = state.operands[1];
                        spriteFrame.pivotY = state.operands[2];
                        spriteFrame.width = state.operands[3];
                        spriteFrame.height = state.operands[4];
                        spriteFrame.spriteX = state.operands[5];
                        spriteFrame.spriteY = state.operands[6];
                    }
                    break;
                case FUNC.LOADPALETTE:
                    loadStoreSize = 0;
                    Palette.LoadPalette(scriptText, state.operands[1], state.operands[2], state.operands[3], state.operands[4]);
                    break;
                case FUNC.ROTATEPALETTE:
                    loadStoreSize = 0;
                    Palette.RotatePalette(state.operands[0], (byte)state.operands[1], (byte)state.operands[2], state.operands[3] != 0);
                    break;
                case FUNC.SETSCREENFADE:
                    loadStoreSize = 0;
                    Palette.SetFade((byte)state.operands[0], (byte)state.operands[1], (byte)state.operands[2], (ushort)state.operands[3]);
                    break;
                case FUNC.SETACTIVEPALETTE:
                    loadStoreSize = 0;
                    Palette.SetActivePalette((byte)state.operands[0], state.operands[1], state.operands[2]);
                    break;
                case FUNC.SETPALETTEFADEREV0:
                    Palette.SetLimitedFade((byte)state.operands[0], (byte)state.operands[1], (byte)state.operands[2], (ushort)state.operands[3], state.operands[4],
                                   state.operands[5], state.operands[6]);
                    break;
                case FUNC.SETPALETTEFADEREV1:
                    Palette.SetPaletteFade((byte)state.operands[0], (byte)state.operands[1], (byte)state.operands[2], (ushort)state.operands[3], state.operands[4],
                        state.operands[5]);
                    break;
                case FUNC.SETPALETTEENTRY:
                    Palette.SetPaletteEntryPacked((byte)state.operands[0], (byte)state.operands[1], (uint)state.operands[2]);
                    break;
                case FUNC.GETPALETTEENTRY:
                    state.operands[2] = Palette.GetPaletteEntryPacked((byte)state.operands[0], (byte)state.operands[1]);
                    break;
                case FUNC.COPYPALETTE:
                    loadStoreSize = 0;
                    Palette.CopyPalette((byte)state.operands[0], (byte)state.operands[1], (byte)state.operands[2], (byte)state.operands[3], (ushort)state.operands[4]);
                    break;
                case FUNC.CLEARSCREEN:
                    loadStoreSize = 0;
                    Drawing.ClearScreen((byte)state.operands[0]);
                    break;
                case FUNC.DRAWSPRITEFX:
                    loadStoreSize = 0;
                    spriteFrame = Animation.scriptFrames[scriptInfo.frameListOffset + state.operands[0]];
                    switch (state.operands[1])
                    {
                        default: break;
                        case FX.SCALE:
                            Drawing.DrawScaledSprite(entity.direction, (state.operands[2] >> 16) - Scene.xScrollOffset,
                                (state.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY, entity.scale,
                                entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTATE:
                            Drawing.DrawRotatedSprite(entity.direction, (state.operands[2] >> 16) - Scene.xScrollOffset,
                                (state.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTOZOOM:
                            Drawing.DrawRotoZoomSprite(entity.direction, (state.operands[2] >> 16) - Scene.xScrollOffset,
                                (state.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                entity.scale, scriptInfo.spriteSheetId);
                            break;
                        case FX.INK:
                            switch (entity.inkEffect)
                            {
                                case INK.NONE:
                                    Drawing.DrawSprite((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.BLEND:
                                    Drawing.DrawBlendedSprite((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.ALPHA:
                                    Drawing.DrawAlphaBlendedSprite((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case INK.ADD:
                                    Drawing.DrawAdditiveBlendedSprite((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case INK.SUB:
                                    Drawing.DrawSubtractiveBlendedSprite((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                        scriptInfo.spriteSheetId);
                                    break;
                            }

                            break;
                        case FX.TINT:
                            if (entity.inkEffect == INK.ALPHA)
                            {
                                Drawing.DrawScaledTintMask(entity.direction, (state.operands[2] >> 16) - Scene.xScrollOffset,
                                    (state.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY,
                                    entity.scale, entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX,
                                    spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }
                            else
                            {
                                Drawing.DrawScaledSprite(entity.direction, (state.operands[2] >> 16) - Scene.xScrollOffset,
                                    (state.operands[3] >> 16) - Scene.yScrollOffset, -spriteFrame.pivotX, -spriteFrame.pivotY, entity.scale,
                                    entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                    scriptInfo.spriteSheetId);
                            }

                            break;
                        case FX.FLIP:
                            switch (entity.direction)
                            {
                                default:
                                case FLIP.NONE:
                                    Drawing.DrawSpriteFlipped((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.NONE, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.X:
                                    Drawing.DrawSpriteFlipped((state.operands[2] >> 16) - Scene.xScrollOffset - spriteFrame.width - spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset + spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.X, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.Y:
                                    Drawing.DrawSpriteFlipped((state.operands[2] >> 16) - Scene.xScrollOffset + spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset - spriteFrame.height - spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.Y,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.XY:
                                    Drawing.DrawSpriteFlipped((state.operands[2] >> 16) - Scene.xScrollOffset - spriteFrame.width - spriteFrame.pivotX,
                                        (state.operands[3] >> 16) - Scene.yScrollOffset - spriteFrame.height - spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.XY,
                                        scriptInfo.spriteSheetId);
                                    break;
                            }

                            break;
                    }
                    break;
                case FUNC.DRAWSPRITESCREENFX:
                    loadStoreSize = 0;
                    int v = scriptInfo.frameListOffset + state.operands[0];
                    if (v > Animation.SPRITEFRAME_COUNT) break;
                    spriteFrame = Animation.scriptFrames[v];
                    switch (state.operands[1])
                    {
                        default: break;
                        case FX.SCALE:
                            Drawing.DrawScaledSprite(entity.direction, state.operands[2], state.operands[3], -spriteFrame.pivotX, -spriteFrame.pivotY,
                                entity.scale, entity.scale, spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTATE:
                            Drawing.DrawRotatedSprite(entity.direction, state.operands[2], state.operands[3], -spriteFrame.pivotX, -spriteFrame.pivotY,
                                spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height, entity.rotation,
                                scriptInfo.spriteSheetId);
                            break;
                        case FX.ROTOZOOM:
                            Drawing.DrawRotoZoomSprite(entity.direction, state.operands[2], state.operands[3], -spriteFrame.pivotX,
                                -spriteFrame.pivotY, spriteFrame.spriteX, spriteFrame.spriteY, spriteFrame.width, spriteFrame.height,
                                entity.rotation, entity.scale, scriptInfo.spriteSheetId);
                            break;
                        case FX.INK:
                            switch (entity.inkEffect)
                            {
                                case INK.NONE:
                                    Drawing.DrawSprite(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                                    break;
                                case INK.BLEND:
                                    Drawing.DrawBlendedSprite(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case INK.ALPHA:
                                    Drawing.DrawAlphaBlendedSprite(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, entity.alpha,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case INK.ADD:
                                    Drawing.DrawAdditiveBlendedSprite(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                        entity.alpha, scriptInfo.spriteSheetId);
                                    break;
                                case INK.SUB:
                                    Drawing.DrawSubtractiveBlendedSprite(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY,
                                        entity.alpha, scriptInfo.spriteSheetId);
                                    break;
                            }

                            break;
                        case FX.TINT:
                            if (entity.inkEffect == INK.ALPHA)
                            {
                                Drawing.DrawScaledTintMask(entity.direction, state.operands[2], state.operands[3], -spriteFrame.pivotX,
                                    -spriteFrame.pivotY, entity.scale, entity.scale, spriteFrame.width, spriteFrame.height,
                                    spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }
                            else
                            {
                                Drawing.DrawScaledSprite(entity.direction, state.operands[2], state.operands[3], -spriteFrame.pivotX,
                                    -spriteFrame.pivotY, entity.scale, entity.scale, spriteFrame.width, spriteFrame.height,
                                    spriteFrame.spriteX, spriteFrame.spriteY, scriptInfo.spriteSheetId);
                            }

                            break;
                        case FX.FLIP:
                            switch (entity.direction)
                            {
                                default: break;
                                case FLIP.NONE:
                                    Drawing.DrawSpriteFlipped(state.operands[2] + spriteFrame.pivotX, state.operands[3] + spriteFrame.pivotY,
                                        spriteFrame.width, spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.NONE,
                                        scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.X:
                                    Drawing.DrawSpriteFlipped(state.operands[2] - spriteFrame.width - spriteFrame.pivotX,
                                        state.operands[3] + spriteFrame.pivotY, spriteFrame.width, spriteFrame.height,
                                        spriteFrame.spriteX, spriteFrame.spriteY, FLIP.X, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.Y:
                                    Drawing.DrawSpriteFlipped(state.operands[2] + spriteFrame.pivotX,
                                        state.operands[3] - spriteFrame.height - spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.Y, scriptInfo.spriteSheetId);
                                    break;
                                case FLIP.XY:
                                    Drawing.DrawSpriteFlipped(state.operands[2] - spriteFrame.width - spriteFrame.pivotX,
                                        state.operands[3] - spriteFrame.height - spriteFrame.pivotY, spriteFrame.width,
                                        spriteFrame.height, spriteFrame.spriteX, spriteFrame.spriteY, FLIP.XY, scriptInfo.spriteSheetId);
                                    break;
                            }

                            break;
                    }

                    break;
                case FUNC.LOADANIMATION:
                    loadStoreSize = 0;
                    scriptInfo.animFile = Animation.AddAnimationFile(scriptText);
                    break;
                case FUNC.SETUPMENU:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        Text.SetupTextMenu(menu, state.operands[1]);
                        menu.selectionCount = (byte)state.operands[2];
                        menu.alignment = state.operands[3];
                        break;
                    }
                case FUNC.ADDMENUENTRY:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        menu.entryHighlight[menu.rowCount] = state.operands[2] != 0;
                        Text.AddTextMenuEntry(menu, scriptText);
                        break;
                    }
                case FUNC.EDITMENUENTRY:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        Text.EditTextMenuEntry(menu, scriptText, state.operands[2]);
                        menu.entryHighlight[state.operands[2]] = state.operands[3] != 0;
                        break;
                    }
                case FUNC.LOADSTAGE:
                    loadStoreSize = 0;
                    Scene.stageMode = STAGEMODE.LOAD;
                    break;
                case FUNC.DRAWRECT:
                    loadStoreSize = 0;
                    Drawing.DrawRectangle(state.operands[0], state.operands[1], state.operands[2], state.operands[3], state.operands[4],
                        state.operands[5], state.operands[6], state.operands[7]);
                    break;
                case FUNC.RESETOBJECTENTITY:
                    {
                        loadStoreSize = 0;
                        Entity newEnt = Objects.objectEntityList[state.operands[0]] = new Entity();
                        newEnt.type = (byte)state.operands[1];
                        newEnt.propertyValue = (byte)state.operands[2];
                        newEnt.xpos = state.operands[3];
                        newEnt.ypos = state.operands[4];
                        newEnt.direction = FLIP.NONE;
                        newEnt.priority = PRIORITY.BOUNDS;
                        newEnt.drawOrder = 3;
                        newEnt.scale = 512;
                        newEnt.inkEffect = INK.NONE;
                        newEnt.objectInteractions = true;
                        newEnt.visible = true;
                        newEnt.tileCollisions = true;
                        break;
                    }
                case FUNC.BOXCOLLISIONTEST:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        default: break;
                        case C.TOUCH:
                            Collision.TouchCollision(Objects.objectEntityList[state.operands[1]], state.operands[2], state.operands[3], state.operands[4],
                                state.operands[5], Objects.objectEntityList[state.operands[6]], state.operands[7], state.operands[8],
                                state.operands[9], state.operands[10]);
                            break;
                        case C.BOX:
                            Collision.BoxCollision(Objects.objectEntityList[state.operands[1]], state.operands[2], state.operands[3], state.operands[4],
                                state.operands[5], Objects.objectEntityList[state.operands[6]], state.operands[7], state.operands[8],
                                state.operands[9], state.operands[10]);
                            break;
                        case C.BOX2:
                            Collision.BoxCollision2(Objects.objectEntityList[state.operands[1]], state.operands[2], state.operands[3], state.operands[4],
                                state.operands[5], Objects.objectEntityList[state.operands[6]], state.operands[7], state.operands[8],
                                state.operands[9], state.operands[10]);
                            break;
                        case C.PLATFORM:
                            Collision.PlatformCollision(Objects.objectEntityList[state.operands[1]], state.operands[2], state.operands[3],
                                state.operands[4], state.operands[5], Objects.objectEntityList[state.operands[6]],
                                state.operands[7], state.operands[8], state.operands[9], state.operands[10]);
                            break;
                    }

                    break;
                case FUNC.CREATETEMPOBJECT:
                    {
                        loadStoreSize = 0;
                        if (Objects.objectEntityList[state.arrayPosition[8]].type > 0 && ++state.arrayPosition[8] == Objects.ENTITY_COUNT)
                            state.arrayPosition[8] = Objects.TEMPENTITY_START;

                        Entity temp = Objects.objectEntityList[state.arrayPosition[8]] = new Entity();
                        temp.type = (byte)state.operands[0];
                        temp.propertyValue = (byte)state.operands[1];
                        temp.xpos = state.operands[2];
                        temp.ypos = state.operands[3];
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
                    loadStoreSize = 0;
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
                    loadStoreSize = 0;
                    Objects.ProcessObjectControl(entity);
                    break;
                case FUNC.PROCESSANIMATION:
                    loadStoreSize = 0;
                    Animation.ProcessObjectAnimation(scriptInfo, entity);
                    break;
                case FUNC.DRAWOBJECTANIMATION:
                    loadStoreSize = 0;
                    if (entity.visible)
                        Drawing.DrawObjectAnimation(scriptInfo, entity, (entity.xpos >> 16) - Scene.xScrollOffset, (entity.ypos >> 16) - Scene.yScrollOffset);
                    break;
                case FUNC.SETMUSICTRACK:
                    loadStoreSize = 0;
                    if (state.operands[2] <= 1)
                        Audio.SetMusicTrack(scriptText, (byte)state.operands[1], state.operands[2] != 0, 0);
                    else
                        Audio.SetMusicTrack(scriptText, (byte)state.operands[1], true, (uint)state.operands[2]);
                    break;
                case FUNC.PLAYMUSIC:
                    loadStoreSize = 0;
                    Audio.PlayMusic(state.operands[0], 0);
                    break;
                case FUNC.STOPMUSIC:
                    loadStoreSize = 0;
                    Audio.StopMusic(true);
                    break;
                case FUNC.PAUSEMUSIC:
                    loadStoreSize = 0;
                    Audio.PauseSound();
                    break;
                case FUNC.RESUMEMUSIC:
                    loadStoreSize = 0;
                    Audio.ResumeSound();
                    break;
                case FUNC.SWAPMUSICTRACK:
                    loadStoreSize = 0;
                    if (state.operands[2] <= 1)
                        Audio.SwapMusicTrack(scriptText, (byte)state.operands[1], 0, (int)state.operands[3]);
                    else
                        Audio.SwapMusicTrack(scriptText, (byte)state.operands[1], (uint)state.operands[2], (int)state.operands[3]);
                    break;
                case FUNC.PLAYSFX:
                    loadStoreSize = 0;
                    Audio.PlaySfx(state.operands[0], state.operands[1] != 0);
                    break;
                case FUNC.STOPSFX:
                    loadStoreSize = 0;
                    Audio.StopSfx(state.operands[0]);
                    break;
                case FUNC.SETSFXATTRIBUTES:
                    loadStoreSize = 0;
                    Audio.SetSfxAttributes(state.operands[0], state.operands[1], state.operands[2]);
                    break;
                case FUNC.OBJECTTILECOLLISION:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        default: break;
                        case CSIDE.FLOOR:
                            Collision.ObjectFloorCollision(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.LWALL:
                            Collision.ObjectLWallCollision(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.RWALL:
                            Collision.ObjectRWallCollision(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.ROOF:
                            Collision.ObjectRoofCollision(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                    }

                    break;
                case FUNC.OBJECTTILEGRIP:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        default: break;
                        case CSIDE.FLOOR:
                            Collision.ObjectFloorGrip(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.LWALL:
                            Collision.ObjectLWallGrip(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.RWALL:
                            Collision.ObjectRWallGrip(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case CSIDE.ROOF:
                            Collision.ObjectRoofGrip(state.operands[1], state.operands[2], state.operands[3]);
                            break;
                    }

                    break;
                case FUNC.NOT:
                    state.operands[0] = ~state.operands[0];
                    break;
                case FUNC.DRAW3DSCENE:
                    loadStoreSize = 0;
                    Scene3D.TransformVertexBuffer();
                    Scene3D.Sort3DDrawList();
                    Scene3D.Draw3DScene(scriptInfo.spriteSheetId);
                    break;
                case FUNC.SETIDENTITYMATRIX:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.SetIdentityMatrix(ref Scene3D.matWorld);
                            break;
                        case MAT.VIEW:
                            Scene3D.SetIdentityMatrix(ref Scene3D.matView);
                            break;
                        case MAT.TEMP:
                            Scene3D.SetIdentityMatrix(ref Scene3D.matTemp);
                            break;
                    }

                    break;
                case FUNC.MATRIXMULTIPLY:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            switch (state.operands[1])
                            {
                                case MAT.WORLD:
                                    Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matWorld);
                                    break;
                                case MAT.VIEW:
                                    Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matView);
                                    break;
                                case MAT.TEMP:
                                    Scene3D.MatrixMultiply(ref Scene3D.matWorld, ref Scene3D.matTemp);
                                    break;
                            }

                            break;
                        case MAT.VIEW:
                            switch (state.operands[1])
                            {
                                case MAT.WORLD:
                                    Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matWorld);
                                    break;
                                case MAT.VIEW:
                                    Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matView);
                                    break;
                                case MAT.TEMP:
                                    Scene3D.MatrixMultiply(ref Scene3D.matView, ref Scene3D.matTemp);
                                    break;
                            }

                            break;
                        case MAT.TEMP:
                            switch (state.operands[1])
                            {
                                case MAT.WORLD:
                                    Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matWorld);
                                    break;
                                case MAT.VIEW:
                                    Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matView);
                                    break;
                                case MAT.TEMP:
                                    Scene3D.MatrixMultiply(ref Scene3D.matTemp, ref Scene3D.matTemp);
                                    break;
                            }

                            break;
                    }

                    break;
                case FUNC.MATRIXTRANSLATEXYZ:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixTranslateXYZ(ref Scene3D.matWorld, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixTranslateXYZ(ref Scene3D.matView, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixTranslateXYZ(ref Scene3D.matTemp, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                    }

                    break;
                case FUNC.MATRIXSCALEXYZ:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixScaleXYZ(ref Scene3D.matWorld, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixScaleXYZ(ref Scene3D.matView, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixScaleXYZ(ref Scene3D.matTemp, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                    }

                    break;
                case FUNC.MATRIXROTATEX:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixRotateX(ref Scene3D.matWorld, state.operands[1]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixRotateX(ref Scene3D.matView, state.operands[1]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixRotateX(ref Scene3D.matTemp, state.operands[1]);
                            break;
                    }

                    break;
                case FUNC.MATRIXROTATEY:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixRotateY(ref Scene3D.matWorld, state.operands[1]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixRotateY(ref Scene3D.matView, state.operands[1]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixRotateY(ref Scene3D.matTemp, state.operands[1]);
                            break;
                    }

                    break;
                case FUNC.MATRIXROTATEZ:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixRotateZ(ref Scene3D.matWorld, state.operands[1]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixRotateZ(ref Scene3D.matView, state.operands[1]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixRotateZ(ref Scene3D.matTemp, state.operands[1]);
                            break;
                    }

                    break;
                case FUNC.MATRIXROTATEXYZ:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixRotateXYZ(ref Scene3D.matWorld, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixRotateXYZ(ref Scene3D.matView, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixRotateXYZ(ref Scene3D.matTemp, state.operands[1], state.operands[2], state.operands[3]);
                            break;
                    }

                    break;
                case FUNC.MATRIXINVERSE:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.MatrixInverse(ref Scene3D.matWorld);
                            break;
                        case MAT.VIEW:
                            Scene3D.MatrixInverse(ref Scene3D.matView);
                            break;
                        case MAT.TEMP:
                            Scene3D.MatrixInverse(ref Scene3D.matTemp);
                            break;
                    }

                    break;
                case FUNC.TRANSFORMVERTICES:
                    loadStoreSize = 0;
                    switch (state.operands[0])
                    {
                        case MAT.WORLD:
                            Scene3D.TransformVertices(ref Scene3D.matWorld, state.operands[1], state.operands[2]);
                            break;
                        case MAT.VIEW:
                            Scene3D.TransformVertices(ref Scene3D.matView, state.operands[1], state.operands[2]);
                            break;
                        case MAT.TEMP:
                            Scene3D.TransformVertices(ref Scene3D.matTemp, state.operands[1], state.operands[2]);
                            break;
                    }

                    break;
                case FUNC.CALLFUNCTION:
                    {
                        loadStoreSize = 0;
                        functionStack[functionStackPos++] = scriptDataPtr;
                        functionStack[functionStackPos++] = jumpTablePtr;
                        functionStack[functionStackPos++] = scriptCodePtr;
                        scriptCodePtr = functionScriptList[state.operands[0]].scriptCodePtr;
                        jumpTablePtr = functionScriptList[state.operands[0]].jumpTablePtr;
                        scriptDataPtr = scriptCodePtr;
                        break;
                    }
                case FUNC.RETURN:
                    loadStoreSize = 0;
                    if (functionStackPos == 0)
                    {
                        // event, stop running
                        running = false;
                    }
                    else
                    {
                        // function, jump out
                        scriptCodePtr = functionStack[--functionStackPos];
                        jumpTablePtr = functionStack[--functionStackPos];
                        scriptDataPtr = functionStack[--functionStackPos];
                    }

                    break;
                case FUNC.SETLAYERDEFORMATION:
                    loadStoreSize = 0;
                    Scene.SetLayerDeformation(state.operands[0], state.operands[1], state.operands[2], state.operands[3], state.operands[4],
                        state.operands[5]);
                    break;
                case FUNC.CHECKTOUCHRECT:
                    loadStoreSize = 0;
                    state.checkResult = -1;

                    for (int f = 0; f < Input.touches; ++f)
                    {
                        if (Input.touchDown[f] != 0 &&
                            Input.touchX[f] > state.operands[0] &&
                            Input.touchX[f] < state.operands[2] &&
                            Input.touchY[f] > state.operands[1] &&
                            Input.touchY[f] < state.operands[3])
                        {
                            state.checkResult = f;
                        }
                    }

                    break;
                case FUNC.GETTILELAYERENTRY:
                    state.operands[0] = Scene.stageLayouts[state.operands[1]].tiles[state.operands[2] + 0x100 * state.operands[3]];
                    break;
                case FUNC.SETTILELAYERENTRY:
                    Scene.stageLayouts[state.operands[1]].tiles[state.operands[2] + 0x100 * state.operands[3]] = (ushort)state.operands[0];
                    break;
                case FUNC.GETBIT:
                    state.operands[0] = (state.operands[1] & (1 << state.operands[2])) >> state.operands[2];
                    break;
                case FUNC.SETBIT:
                    if (state.operands[2] <= 0)
                        state.operands[0] &= ~(1 << state.operands[1]);
                    else
                        state.operands[0] |= 1 << state.operands[1];
                    break;
                case FUNC.CLEARDRAWLIST:
                    loadStoreSize = 0;
                    Scene.drawListEntries[state.operands[0]].listSize = 0;
                    break;
                case FUNC.ADDDRAWLISTENTITYREF:
                    {
                        loadStoreSize = 0;
                        Scene.drawListEntries[state.operands[0]].entityRefs[Scene.drawListEntries[state.operands[0]].listSize++] = state.operands[1];
                        break;
                    }
                case FUNC.GETDRAWLISTENTITYREF:
                    state.operands[0] = Scene.drawListEntries[state.operands[1]].entityRefs[state.operands[2]];
                    break;
                case FUNC.SETDRAWLISTENTITYREF:
                    loadStoreSize = 0;
                    Scene.drawListEntries[state.operands[1]].entityRefs[state.operands[2]] = state.operands[0];
                    break;
                case FUNC.GET16X16TILEINFO:
                    {
                        state.operands[4] = state.operands[1] >> 7;
                        state.operands[5] = state.operands[2] >> 7;

                        int tileIdx = state.operands[4] + (state.operands[5] << 8);
                        if (tileIdx < 0 || tileIdx > Scene.stageLayouts[0].tiles.Length)
                        {
                            state.operands[6] = 0;
                            state.operands[0] = 0;
                            break;
                        }

                        // This reads out of bounds in OOZ Act 1 without bounds checking.
                        state.operands[6] = Scene.stageLayouts[0].tiles[tileIdx] << 6;
                        state.operands[6] += ((state.operands[1] & 0x7F) >> 4) + 8 * ((state.operands[2] & 0x7F) >> 4);
                        int index = Scene.tiles128x128.tileIndex[state.operands[6]];
                        switch (state.operands[3])
                        {
                            case TILEINFO.INDEX:
                                state.operands[0] = Scene.tiles128x128.tileIndex[state.operands[6]];
                                break;
                            case TILEINFO.DIRECTION:
                                state.operands[0] = Scene.tiles128x128.direction[state.operands[6]];
                                break;
                            case TILEINFO.VISUALPLANE:
                                state.operands[0] = Scene.tiles128x128.visualPlane[state.operands[6]];
                                break;
                            case TILEINFO.SOLIDITYA:
                                state.operands[0] = Scene.tiles128x128.collisionFlags[0][state.operands[6]];
                                break;
                            case TILEINFO.SOLIDITYB:
                                state.operands[0] = Scene.tiles128x128.collisionFlags[1][state.operands[6]];
                                break;
                            case TILEINFO.FLAGSA:
                                state.operands[0] = Scene.collisionMasks[0].flags[index];
                                break;
                            case TILEINFO.ANGLEA:
                                state.operands[0] = (int)Scene.collisionMasks[0].angles[index];
                                break;
                            case TILEINFO.FLAGSB:
                                state.operands[0] = Scene.collisionMasks[1].flags[index];
                                break;
                            case TILEINFO.ANGLEB:
                                state.operands[0] = (int)Scene.collisionMasks[1].angles[index];
                                break;
                            default: break;
                        }

                        break;
                    }
                case FUNC.SET16X16TILEINFO:
                    {
                        state.operands[4] = state.operands[1] >> 7;
                        state.operands[5] = state.operands[2] >> 7;
                        state.operands[6] = Scene.stageLayouts[0].tiles[state.operands[4] + (state.operands[5] << 8)] << 6;
                        state.operands[6] += ((state.operands[1] & 0x7F) >> 4) + 8 * ((state.operands[2] & 0x7F) >> 4);
                        switch (state.operands[3])
                        {
                            case TILEINFO.INDEX:
                                Scene.tiles128x128.tileIndex[state.operands[6]] = (ushort)state.operands[0];
                                Scene.tiles128x128.gfxDataPos[state.operands[6]] = state.operands[0] << 8;
                                break;
                            case TILEINFO.DIRECTION:
                                Scene.tiles128x128.direction[state.operands[6]] = (byte)state.operands[0];
                                break;
                            case TILEINFO.VISUALPLANE:
                                Scene.tiles128x128.visualPlane[state.operands[6]] = (byte)state.operands[0];
                                break;
                            case TILEINFO.SOLIDITYA:
                                Scene.tiles128x128.collisionFlags[0][state.operands[6]] = (byte)state.operands[0];
                                break;
                            case TILEINFO.SOLIDITYB:
                                Scene.tiles128x128.collisionFlags[1][state.operands[6]] = (byte)state.operands[0];
                                break;
                            case TILEINFO.FLAGSA:
                                Scene.collisionMasks[1].flags[Scene.tiles128x128.tileIndex[state.operands[6]]] = (byte)state.operands[0];
                                break;
                            case TILEINFO.ANGLEA:
                                Scene.collisionMasks[1].angles[Scene.tiles128x128.tileIndex[state.operands[6]]] = (uint)state.operands[0];
                                break;
                            default: break;
                        }

                        break;
                    }
                case FUNC.COPY16X16TILE:
                    loadStoreSize = 0;
                    Drawing.Copy16x16Tile(state.operands[0], state.operands[1]);
                    break;
                case FUNC.GETANIMATIONBYNAME:
                    {
                        AnimationFile animFile = scriptInfo.animFile;
                        state.operands[0] = -1;
                        int id = 0;
                        while (state.operands[0] == -1)
                        {
                            SpriteAnimation anim = Animation.animationList[animFile.animListOffset + id];
                            if (anim != null && scriptText == anim.name)
                                state.operands[0] = id;
                            else if (++id == animFile.animCount)
                                state.operands[0] = 0;
                        }

                        break;
                    }
                case FUNC.READSAVERAM:
                    loadStoreSize = 0;
                    state.checkResult = SaveData.ReadSaveRAMData();
                    break;
                case FUNC.WRITESAVERAM:
                    loadStoreSize = 0;
                    state.checkResult = SaveData.WriteSaveRAMData();
                    break;
                case FUNC.LOADTEXTFONT:
                    {
                        loadStoreSize = 0;
                        Font.LoadFontFile(scriptText);
                        break;
                    }
                case FUNC.LOADTEXTFILEREV0:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        Text.LoadTextFile(menu, scriptText, state.operands[2] != 0 ? (byte)1 : (byte)0);
                        break;
                    }
                case FUNC.LOADTEXTFILEREV2:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        Text.LoadTextFile(menu, scriptText, 0);
                        break;
                    }
                case FUNC.GETTEXTINFO:
                    {
                        TextMenu menu = Text.gameMenu[state.operands[1]];
                        switch (state.operands[2])
                        {
                            case TEXTINFO.TEXTDATA:
                                state.operands[0] = menu.textData[menu.entryStart[state.operands[3]] + state.operands[4]];
                                break;
                            case TEXTINFO.TEXTSIZE:
                                state.operands[0] = menu.entrySize[state.operands[3]];
                                break;
                            case TEXTINFO.ROWCOUNT:
                                state.operands[0] = menu.rowCount;
                                break;
                        }

                        break;
                    }
                case FUNC.DRAWTEXT:
                    {
                        loadStoreSize = 0;
                        var textMenuSurfaceNo = scriptInfo.spriteSheetId;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        Drawing.DrawBitmapText(menu, state.operands[1], state.operands[2], state.operands[3], state.operands[4],
                                       state.operands[5], state.operands[6]);
                        break;
                    }
                case FUNC.GETVERSIONNUMBER:
                    {
                        loadStoreSize = 0;
                        TextMenu menu = Text.gameMenu[state.operands[0]];
                        menu.entryHighlight[menu.rowCount] = state.operands[1] != 0;
                        Text.AddTextMenuEntry(menu, Engine.gameVersion);
                        break;
                    }
                case FUNC.GETTABLEVALUE:
                    {
                        int arrPos = state.operands[1];
                        if (arrPos >= 0)
                        {
                            int pos = state.operands[2];
                            int arrSize = scriptData[pos];
                            if (arrPos < arrSize)
                                state.operands[0] = scriptData[pos + arrPos + 1];
                        }

                        break;
                    }
                case FUNC.SETTABLEVALUE:
                    {
                        loadStoreSize = 0;
                        int arrPos = state.operands[1];
                        if (arrPos >= 0)
                        {
                            int pos = state.operands[2];
                            int arrSize = scriptData[pos];
                            if (arrPos < arrSize)
                                scriptData[pos + arrPos + 1] = state.operands[0];
                        }

                        break;
                    }
                case FUNC.CHECKCURRENTSTAGEFOLDER:
                    loadStoreSize = 0;
                    state.checkResult = (Engine.stageList[Scene.activeStageList][Scene.stageListPosition].folder == scriptText) ? 1 : 0;
                    break;
                case FUNC.ABS:
                    {
                        state.operands[0] = Math.Abs(state.operands[0]);
                        break;
                    }
                case FUNC.CALLNATIVEFUNCTION:
                    loadStoreSize = 0;
                    if (state.operands[0] >= 0 && state.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        var func = (NativeFunction1)Engine.nativeFunctions[state.operands[0]];
                        if (func != null)
                            func();
                    }

                    break;
                case FUNC.CALLNATIVEFUNCTION2:
                    if (state.operands[0] >= 0 && state.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        if (scriptText.Length > 0)
                        {
                            var func = (NativeFunction2)Engine.nativeFunctions[state.operands[0]];
                            if (func != null)
                                func(ref state.operands[2], scriptText);
                        }
                        else
                        {
                            var func = (NativeFunction3)Engine.nativeFunctions[state.operands[0]];
                            if (func != null)
                                func(ref state.operands[1], ref state.operands[2]);
                        }
                    }

                    break;
                case FUNC.CALLNATIVEFUNCTION4:
                    if (state.operands[0] >= 0 && state.operands[0] < Engine.NATIVEFUNCTION_MAX)
                    {
                        if (scriptText.Length > 0)
                        {
                            var func = (NativeFunction4)Engine.nativeFunctions[state.operands[0]];
                            if (func != null)
                                func(ref state.operands[1], scriptText, ref state.operands[3], ref state.operands[4]);
                        }
                        else
                        {
                            var func = (NativeFunction5)Engine.nativeFunctions[state.operands[0]];
                            if (func != null)
                                func(ref state.operands[1], ref state.operands[2], ref state.operands[3], ref state.operands[4]);
                        }
                    }

                    break;
                case FUNC.SETOBJECTRANGE:
                    {
                        loadStoreSize = 0;
                        int offset = (state.operands[0] >> 1) - Drawing.SCREEN_CENTERX;
                        Objects.OBJECT_BORDER_X1 = offset + 0x80;
                        Objects.OBJECT_BORDER_X2 = state.operands[0] + 0x80 - offset;
                        Objects.OBJECT_BORDER_X3 = offset + 0x20;
                        Objects.OBJECT_BORDER_X4 = state.operands[0] + 0x20 - offset;
                        break;
                    }
                case FUNC.GETOBJECTVALUE:
                    {
                        if (state.operands[1] < 48)
                            state.operands[0] = Objects.objectEntityList[state.operands[2]].values[state.operands[1]];
                        break;
                    }
                case FUNC.SETOBJECTVALUE:
                    {
                        loadStoreSize = 0;
                        if (state.operands[1] < 48)
                            Objects.objectEntityList[state.operands[2]].values[state.operands[1]] = state.operands[0];
                        break;
                    }
                case FUNC.COPYOBJECT:
                    {
                        // dstID, srcID, count
                        for (int i = 0; i < state.operands[2]; ++i)
                            Objects.objectEntityList[state.operands[1] + i] = new Entity(Objects.objectEntityList[state.operands[0] + i]);
                        break;
                    }
                case FUNC.PRINT:
                    {
                        if (state.operands[1] != 0)
                            Debug.WriteLine(state.operands[0]);
                        else
                            Debug.WriteLine(scriptText);

                        if (state.operands[2] != 0)
                            Debug.WriteLine("");
                        break;
                    }
                case FUNC.CHECKCAMERAPROXIMITY:
                    state.checkResult = 0;

                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = pos.x
                    // scriptEng.operands[1] = pos.y
                    // scriptEng.operands[2] = range.x
                    // scriptEng.operands[3] = range.y
                    //
                    // FUNCTION NOTES:
                    // - Sets scriptEng.checkResult

                    if (state.operands[2] > 0 && state.operands[3] > 0)
                    {
                        int sx = Math.Abs(state.operands[0] - Scene.cameraXPos);
                        int sy = Math.Abs(state.operands[1] - Scene.cameraYPos);

                        if (sx < state.operands[2] && sy < state.operands[3])
                        {
                            state.checkResult = 1;
                            break;
                        }
                    }
                    else
                    {
                        if (state.operands[2] > 0)
                        {
                            int sx = Math.Abs(state.operands[0] - Scene.cameraXPos);

                            if (sx < state.operands[2])
                            {
                                state.checkResult = 1;
                                break;
                            }
                        }
                        else if (state.operands[3] > 0)
                        {
                            int sy = Math.Abs(state.operands[1] - Scene.cameraYPos);

                            if (sy < state.operands[3])
                            {
                                state.checkResult = 1;
                                break;
                            }
                        }
                    }
                    break;

                case FUNC.SETSCREENCOUNT:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = screenCount

                    break;

                case FUNC.SETSCREENVERTICES:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = startVert2P_S1
                    // scriptEng.operands[1] = startVert2P_S2
                    // scriptEng.operands[2] = startVert3P_S1
                    // scriptEng.operands[3] = startVert3P_S2
                    // scriptEng.operands[4] = startVert3P_S3

                    break;

                case FUNC.GETINPUTDEVICEID:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = deviceID
                    // scriptEng.operands[1] = inputSlot
                    //
                    // FUNCTION NOTES:
                    // - Assigns the device's id to scriptEng.operands[0]

                    break;

                case FUNC.GETFILTEREDINPUTDEVICEID:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = deviceID
                    // scriptEng.operands[1] = confirmOnly
                    // scriptEng.operands[2] = unassignedOnly
                    // scriptEng.operands[3] = maxInactiveTimer
                    //
                    // FUNCTION NOTES:
                    // - Assigns the filtered device's id to scriptEng.operands[0]

                    break;

                case FUNC.GETINPUTDEVICETYPE:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = deviceType
                    // scriptEng.operands[1] = deviceID
                    //
                    // FUNCTION NOTES:
                    // - Assigns the device's type to scriptEng.operands[0]

                    break;

                case FUNC.ISINPUTDEVICEASSIGNED:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = deviceID

                    break;

                case FUNC.ASSIGNINPUTSLOTTODEVICE:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = inputSlot
                    // scriptEng.operands[1] = deviceID

                    break;

                case FUNC.ISSLOTASSIGNED:
                    // FUNCTION PARAMS:
                    // scriptEng.operands[0] = inputSlot
                    //
                    // FUNCTION NOTES:
                    // - Sets scriptEng.checkResult

                    break;

                case FUNC.RESETINPUTSLOTASSIGNMENTS:
                    // FUNCTION PARAMS:
                    // None

                    break;
            }

            // Store Values
            if (loadStoreSize > 0)
                scriptDataPtr -= scriptDataPtr - scriptCodeOffset;
            for (int i = 0; i < loadStoreSize; ++i)
            {
                SRC storeType = (SRC)scriptData[scriptDataPtr++];

                //Debug.WriteLine("SCRIPT: Set value {0}", opcodeType);

                if (storeType == SRC.SCRIPTVAR)
                {
                    int arrayVal = 0;
                    switch ((VARARR)scriptData[scriptDataPtr++])
                    {
                        // variable
                        case VARARR.NONE:
                            arrayVal = Objects.objectEntityPos;
                            break;
                        case VARARR.ARRAY:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = state.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOPLUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos + state.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos + scriptData[scriptDataPtr++];
                            break;
                        case VARARR.ENTNOMINUS1:
                            if (scriptData[scriptDataPtr++] == 1)
                                arrayVal = Objects.objectEntityPos - state.arrayPosition[scriptData[scriptDataPtr++]];
                            else
                                arrayVal = Objects.objectEntityPos - scriptData[scriptDataPtr++];
                            break;
                        default: break;
                    }

                    var rawVariable = scriptData[scriptDataPtr++];
                    translator.TranslateVariable(rawVariable, out var variable);
                    // Debug.WriteLine("SCRIPT: Set variable {0}", variable);
                    // Variables
                    switch (variable)
                    {
                        default: break;
                        case VAR.TEMP0:
                            state.temp[0] = state.operands[i];
                            break;
                        case VAR.TEMP1:
                            state.temp[1] = state.operands[i];
                            break;
                        case VAR.TEMP2:
                            state.temp[2] = state.operands[i];
                            break;
                        case VAR.TEMP3:
                            state.temp[3] = state.operands[i];
                            break;
                        case VAR.TEMP4:
                            state.temp[4] = state.operands[i];
                            break;
                        case VAR.TEMP5:
                            state.temp[5] = state.operands[i];
                            break;
                        case VAR.TEMP6:
                            state.temp[6] = state.operands[i];
                            break;
                        case VAR.TEMP7:
                            state.temp[7] = state.operands[i];
                            break;
                        case VAR.CHECKRESULT:
                            state.checkResult = state.operands[i];
                            break;
                        case VAR.ARRAYPOS0:
                            state.arrayPosition[0] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS1:
                            state.arrayPosition[1] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS2:
                            state.arrayPosition[2] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS3:
                            state.arrayPosition[3] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS4:
                            state.arrayPosition[4] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS5:
                            state.arrayPosition[5] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS6:
                            state.arrayPosition[6] = state.operands[i];
                            break;
                        case VAR.ARRAYPOS7:
                            state.arrayPosition[7] = state.operands[i];
                            break;
                        case VAR.GLOBAL:
                            Engine.globalVariables[arrayVal] = state.operands[i];
                            break;
                        case VAR.LOCAL:
                            scriptData[arrayVal] = state.operands[i];
                            break;
                        case VAR.OBJECTENTITYPOS: break;
                        case VAR.OBJECTGROUPID:
                            {
                                Objects.objectEntityList[arrayVal].groupID = (ushort)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTTYPE:
                            {
                                Objects.objectEntityList[arrayVal].type = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTPROPERTYVALUE:
                            {
                                Objects.objectEntityList[arrayVal].propertyValue = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTXPOS:
                            {
                                Objects.objectEntityList[arrayVal].xpos = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTYPOS:
                            {
                                Objects.objectEntityList[arrayVal].ypos = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTIXPOS:
                            {
                                Objects.objectEntityList[arrayVal].xpos = state.operands[i] << 16;
                                break;
                            }
                        case VAR.OBJECTIYPOS:
                            {
                                Objects.objectEntityList[arrayVal].ypos = state.operands[i] << 16;
                                break;
                            }
                        case VAR.OBJECTXVEL:
                            {
                                Objects.objectEntityList[arrayVal].xvel = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTYVEL:
                            {
                                Objects.objectEntityList[arrayVal].yvel = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTSPEED:
                            {
                                Objects.objectEntityList[arrayVal].speed = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTSTATE:
                            {
                                Objects.objectEntityList[arrayVal].state = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTROTATION:
                            {
                                Objects.objectEntityList[arrayVal].rotation = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTSCALE:
                            {
                                Objects.objectEntityList[arrayVal].scale = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTPRIORITY:
                            {
                                Objects.objectEntityList[arrayVal].priority = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTDRAWORDER:
                            {
                                Objects.objectEntityList[arrayVal].drawOrder = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTDIRECTION:
                            {
                                Objects.objectEntityList[arrayVal].direction = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTINKEFFECT:
                            {
                                Objects.objectEntityList[arrayVal].inkEffect = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTALPHA:
                            {
                                Objects.objectEntityList[arrayVal].alpha = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTFRAME:
                            {
                                Objects.objectEntityList[arrayVal].frame = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATION:
                            {
                                Objects.objectEntityList[arrayVal].animation = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTPREVANIMATION:
                            {
                                Objects.objectEntityList[arrayVal].prevAnimation = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATIONSPEED:
                            {
                                Objects.objectEntityList[arrayVal].animationSpeed = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTANIMATIONTIMER:
                            {
                                Objects.objectEntityList[arrayVal].animationTimer = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTANGLE:
                            {
                                Objects.objectEntityList[arrayVal].angle = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTLOOKPOSX:
                            {
                                Objects.objectEntityList[arrayVal].lookPosX = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTLOOKPOSY:
                            {
                                Objects.objectEntityList[arrayVal].lookPosY = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTCOLLISIONMODE:
                            {
                                Objects.objectEntityList[arrayVal].collisionMode = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTCOLLISIONPLANE:
                            {
                                Objects.objectEntityList[arrayVal].collisionPlane = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTCONTROLMODE:
                            {
                                Objects.objectEntityList[arrayVal].controlMode = (sbyte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTCONTROLLOCK:
                            {
                                Objects.objectEntityList[arrayVal].controlLock = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTPUSHING:
                            {
                                Objects.objectEntityList[arrayVal].pushing = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVISIBLE:
                            {
                                Objects.objectEntityList[arrayVal].visible = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTTILECOLLISIONS:
                            {
                                Objects.objectEntityList[arrayVal].tileCollisions = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTINTERACTION:
                            {
                                Objects.objectEntityList[arrayVal].objectInteractions = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTGRAVITY:
                            {
                                Objects.objectEntityList[arrayVal].gravity = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTUP:
                            {
                                Objects.objectEntityList[arrayVal].up = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTDOWN:
                            {
                                Objects.objectEntityList[arrayVal].down = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTLEFT:
                            {
                                Objects.objectEntityList[arrayVal].left = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTRIGHT:
                            {
                                Objects.objectEntityList[arrayVal].right = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTJUMPPRESS:
                            {
                                Objects.objectEntityList[arrayVal].jumpPress = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTJUMPHOLD:
                            {
                                Objects.objectEntityList[arrayVal].jumpHold = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTSCROLLTRACKING:
                            {
                                Objects.objectEntityList[arrayVal].scrollTracking = state.operands[i] != 0;
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORL:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[0] = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[1] = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORR:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[2] = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORLC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[3] = (byte)state.operands[i];
                                break;
                            }
                        case VAR.OBJECTFLOORSENSORRC:
                            {
                                Objects.objectEntityList[arrayVal].floorSensors[4] = (byte)state.operands[i];
                                break;
                            }
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
                        case VAR.OBJECTOUTOFBOUNDSREV0:
                        case VAR.OBJECTOUTOFBOUNDSREV1:
                            {
                                break;
                            }
                        case VAR.OBJECTSPRITESHEET:
                            {
                                objectScriptList[Objects.objectEntityList[arrayVal].type].spriteSheetId = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE0:
                            {
                                Objects.objectEntityList[arrayVal].values[0] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE1:
                            {
                                Objects.objectEntityList[arrayVal].values[1] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE2:
                            {
                                Objects.objectEntityList[arrayVal].values[2] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE3:
                            {
                                Objects.objectEntityList[arrayVal].values[3] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE4:
                            {
                                Objects.objectEntityList[arrayVal].values[4] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE5:
                            {
                                Objects.objectEntityList[arrayVal].values[5] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE6:
                            {
                                Objects.objectEntityList[arrayVal].values[6] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE7:
                            {
                                Objects.objectEntityList[arrayVal].values[7] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE8:
                            {
                                Objects.objectEntityList[arrayVal].values[8] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE9:
                            {
                                Objects.objectEntityList[arrayVal].values[9] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE10:
                            {
                                Objects.objectEntityList[arrayVal].values[10] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE11:
                            {
                                Objects.objectEntityList[arrayVal].values[11] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE12:
                            {
                                Objects.objectEntityList[arrayVal].values[12] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE13:
                            {
                                Objects.objectEntityList[arrayVal].values[13] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE14:
                            {
                                Objects.objectEntityList[arrayVal].values[14] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE15:
                            {
                                Objects.objectEntityList[arrayVal].values[15] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE16:
                            {
                                Objects.objectEntityList[arrayVal].values[16] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE17:
                            {
                                Objects.objectEntityList[arrayVal].values[17] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE18:
                            {
                                Objects.objectEntityList[arrayVal].values[18] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE19:
                            {
                                Objects.objectEntityList[arrayVal].values[19] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE20:
                            {
                                Objects.objectEntityList[arrayVal].values[20] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE21:
                            {
                                Objects.objectEntityList[arrayVal].values[21] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE22:
                            {
                                Objects.objectEntityList[arrayVal].values[22] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE23:
                            {
                                Objects.objectEntityList[arrayVal].values[23] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE24:
                            {
                                Objects.objectEntityList[arrayVal].values[24] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE25:
                            {
                                Objects.objectEntityList[arrayVal].values[25] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE26:
                            {
                                Objects.objectEntityList[arrayVal].values[26] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE27:
                            {
                                Objects.objectEntityList[arrayVal].values[27] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE28:
                            {
                                Objects.objectEntityList[arrayVal].values[28] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE29:
                            {
                                Objects.objectEntityList[arrayVal].values[29] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE30:
                            {
                                Objects.objectEntityList[arrayVal].values[30] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE31:
                            {
                                Objects.objectEntityList[arrayVal].values[31] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE32:
                            {
                                Objects.objectEntityList[arrayVal].values[32] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE33:
                            {
                                Objects.objectEntityList[arrayVal].values[33] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE34:
                            {
                                Objects.objectEntityList[arrayVal].values[34] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE35:
                            {
                                Objects.objectEntityList[arrayVal].values[35] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE36:
                            {
                                Objects.objectEntityList[arrayVal].values[36] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE37:
                            {
                                Objects.objectEntityList[arrayVal].values[37] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE38:
                            {
                                Objects.objectEntityList[arrayVal].values[38] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE39:
                            {
                                Objects.objectEntityList[arrayVal].values[39] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE40:
                            {
                                Objects.objectEntityList[arrayVal].values[40] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE41:
                            {
                                Objects.objectEntityList[arrayVal].values[41] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE42:
                            {
                                Objects.objectEntityList[arrayVal].values[42] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE43:
                            {
                                Objects.objectEntityList[arrayVal].values[43] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE44:
                            {
                                Objects.objectEntityList[arrayVal].values[44] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE45:
                            {
                                Objects.objectEntityList[arrayVal].values[45] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE46:
                            {
                                Objects.objectEntityList[arrayVal].values[46] = state.operands[i];
                                break;
                            }
                        case VAR.OBJECTVALUE47:
                            {
                                Objects.objectEntityList[arrayVal].values[47] = state.operands[i];
                                break;
                            }
                        case VAR.STAGESTATE:
                            Scene.stageMode = state.operands[i];
                            break;
                        case VAR.STAGEACTIVELIST:
                            Scene.activeStageList = state.operands[i];
                            break;
                        case VAR.STAGELISTPOS:
                            Scene.stageListPosition = state.operands[i];
                            break;
                        case VAR.STAGETIMEENABLED:
                            Scene.timeEnabled = state.operands[i] != 0;
                            break;
                        case VAR.STAGEMILLISECONDS:
                            Scene.stageMilliseconds = state.operands[i];
                            break;
                        case VAR.STAGESECONDS:
                            Scene.stageSeconds = state.operands[i];
                            break;
                        case VAR.STAGEMINUTES:
                            Scene.stageMinutes = state.operands[i];
                            break;
                        case VAR.STAGEACTNUM:
                            Scene.actId = state.operands[i];
                            break;
                        case VAR.STAGEPAUSEENABLED:
                            Scene.pauseEnabled = state.operands[i] != 0;
                            break;
                        case VAR.STAGELISTSIZE: break;
                        case VAR.STAGENEWXBOUNDARY1:
                            Scene.newXBoundary1 = state.operands[i];
                            break;
                        case VAR.STAGENEWXBOUNDARY2:
                            Scene.newXBoundary2 = state.operands[i];
                            break;
                        case VAR.STAGENEWYBOUNDARY1:
                            Scene.newYBoundary1 = state.operands[i];
                            break;
                        case VAR.STAGENEWYBOUNDARY2:
                            Scene.newYBoundary2 = state.operands[i];
                            break;
                        case VAR.STAGECURXBOUNDARY1:
                            if (Scene.curXBoundary1 != state.operands[i])
                            {
                                Scene.curXBoundary1 = state.operands[i];
                                Scene.newXBoundary1 = state.operands[i];
                            }

                            break;
                        case VAR.STAGECURXBOUNDARY2:
                            if (Scene.curXBoundary2 != state.operands[i])
                            {
                                Scene.curXBoundary2 = state.operands[i];
                                Scene.newXBoundary2 = state.operands[i];
                            }

                            break;
                        case VAR.STAGECURYBOUNDARY1:
                            if (Scene.curYBoundary1 != state.operands[i])
                            {
                                Scene.curYBoundary1 = state.operands[i];
                                Scene.newYBoundary1 = state.operands[i];
                            }

                            break;
                        case VAR.STAGECURYBOUNDARY2:
                            if (Scene.curYBoundary2 != state.operands[i])
                            {
                                Scene.curYBoundary2 = state.operands[i];
                                Scene.newYBoundary2 = state.operands[i];
                            }

                            break;
                        case VAR.STAGEDEFORMATIONDATA0:
                            Scene.bgDeformationData0[arrayVal] = state.operands[i];
                            break;
                        case VAR.STAGEDEFORMATIONDATA1:
                            Scene.bgDeformationData1[arrayVal] = state.operands[i];
                            break;
                        case VAR.STAGEDEFORMATIONDATA2:
                            Scene.bgDeformationData2[arrayVal] = state.operands[i];
                            break;
                        case VAR.STAGEDEFORMATIONDATA3:
                            Scene.bgDeformationData3[arrayVal] = state.operands[i];
                            break;
                        case VAR.STAGEWATERLEVEL:
                            Scene.waterLevel = state.operands[i];
                            break;
                        case VAR.STAGEACTIVELAYER:
                            Scene.activeTileLayers[arrayVal] = (byte)state.operands[i];
                            break;
                        case VAR.STAGEMIDPOINT:
                            Scene.tLayerMidPoint = (byte)state.operands[i];
                            break;
                        case VAR.STAGEPLAYERLISTPOS:
                            Objects.playerListPos = state.operands[i];
                            break;
                        case VAR.STAGEDEBUGMODE:
                            Scene.debugMode = state.operands[i] != 0;
                            break;
                        case VAR.STAGEENTITYPOS:
                            Objects.objectEntityPos = state.operands[i];
                            break;
                        case VAR.SCREENCAMERAENABLED:
                            Scene.cameraEnabled = state.operands[i] != 0;
                            break;
                        case VAR.SCREENCAMERATARGET:
                            Scene.cameraTarget = state.operands[i];
                            break;
                        case VAR.SCREENCAMERASTYLE:
                            Scene.cameraStyle = state.operands[i];
                            break;
                        case VAR.SCREENCAMERAX:
                            Scene.cameraXPos = state.operands[i];
                            break;
                        case VAR.SCREENCAMERAY:
                            Scene.cameraYPos = state.operands[i];
                            break;
                        case VAR.SCREENDRAWLISTSIZE:
                            Scene.drawListEntries[arrayVal].listSize = state.operands[i];
                            break;
                        case VAR.SCREENXCENTER: break;
                        case VAR.SCREENYCENTER: break;
                        case VAR.SCREENXSIZE: break;
                        case VAR.SCREENYSIZE: break;
                        case VAR.SCREENXOFFSET:
                            Scene.xScrollOffset = state.operands[i];
                            break;
                        case VAR.SCREENYOFFSET:
                            Scene.yScrollOffset = state.operands[i];
                            break;
                        case VAR.SCREENSHAKEX:
                            Scene.cameraShakeX = state.operands[i];
                            break;
                        case VAR.SCREENSHAKEY:
                            Scene.cameraShakeY = state.operands[i];
                            break;
                        case VAR.SCREENADJUSTCAMERAY:
                            Scene.cameraAdjustY = state.operands[i];
                            break;
                        case VAR.TOUCHSCREENDOWN: break;
                        case VAR.TOUCHSCREENXPOS: break;
                        case VAR.TOUCHSCREENYPOS: break;
                        case VAR.MUSICVOLUME:
                            Audio.SetMusicVolume(state.operands[i]);
                            break;
                        case VAR.MUSICCURRENTTRACK: break;
                        case VAR.MUSICPOSITION: break;
                        case VAR.INPUTDOWNUP:
                            Input.keyDown.up = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNDOWN:
                            Input.keyDown.down = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNLEFT:
                            Input.keyDown.left = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNRIGHT:
                            Input.keyDown.right = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONA:
                            Input.keyDown.A = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONB:
                            Input.keyDown.B = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONC:
                            Input.keyDown.C = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONX:
                            Input.keyDown.X = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONY:
                            Input.keyDown.Y = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONZ:
                            Input.keyDown.Z = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONL:
                            Input.keyDown.L = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNBUTTONR:
                            Input.keyDown.R = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNSTART:
                            Input.keyDown.start = state.operands[i] != 0;
                            break;
                        case VAR.INPUTDOWNSELECT:
                            Input.keyDown.select = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSUP:
                            Input.keyPress.up = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSDOWN:
                            Input.keyPress.down = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSLEFT:
                            Input.keyPress.left = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSRIGHT:
                            Input.keyPress.right = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONA:
                            Input.keyPress.A = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONB:
                            Input.keyPress.B = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONC:
                            Input.keyPress.C = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONX:
                            Input.keyPress.X = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONY:
                            Input.keyPress.Y = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONZ:
                            Input.keyPress.Z = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONL:
                            Input.keyPress.L = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSBUTTONR:
                            Input.keyPress.R = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSSTART:
                            Input.keyPress.start = state.operands[i] != 0;
                            break;
                        case VAR.INPUTPRESSSELECT:
                            Input.keyPress.select = state.operands[i] != 0;
                            break;
                        case VAR.MENU1SELECTION:
                            Text.gameMenu[0].selection1 = state.operands[i];
                            break;
                        case VAR.MENU2SELECTION:
                            Text.gameMenu[1].selection1 = state.operands[i];
                            break;
                        case VAR.TILELAYERXSIZE:
                            Scene.stageLayouts[arrayVal].xsize = (byte)state.operands[i];
                            break;
                        case VAR.TILELAYERYSIZE:
                            Scene.stageLayouts[arrayVal].ysize = (byte)state.operands[i];
                            break;
                        case VAR.TILELAYERTYPE:
                            Scene.stageLayouts[arrayVal].type = (byte)state.operands[i];
                            break;
                        case VAR.TILELAYERANGLE:
                            {
                                int angle = state.operands[i] + 0x200;
                                if (state.operands[i] >= 0)
                                    angle = state.operands[i];
                                Scene.stageLayouts[arrayVal].angle = angle & 0x1FF;
                                break;
                            }
                        case VAR.TILELAYERXPOS:
                            Scene.stageLayouts[arrayVal].xpos = state.operands[i];
                            break;
                        case VAR.TILELAYERYPOS:
                            Scene.stageLayouts[arrayVal].ypos = state.operands[i];
                            break;
                        case VAR.TILELAYERZPOS:
                            Scene.stageLayouts[arrayVal].zpos = state.operands[i];
                            break;
                        case VAR.TILELAYERPARALLAXFACTOR:
                            Scene.stageLayouts[arrayVal].parallaxFactor = state.operands[i];
                            break;
                        case VAR.TILELAYERSCROLLSPEED:
                            Scene.stageLayouts[arrayVal].scrollSpeed = state.operands[i];
                            break;
                        case VAR.TILELAYERSCROLLPOS:
                            Scene.stageLayouts[arrayVal].scrollPos = state.operands[i];
                            break;
                        case VAR.TILELAYERDEFORMATIONOFFSET:
                            Scene.stageLayouts[arrayVal].deformationOffset = state.operands[i];
                            break;
                        case VAR.TILELAYERDEFORMATIONOFFSETW:
                            Scene.stageLayouts[arrayVal].deformationOffsetW = state.operands[i];
                            break;
                        case VAR.HPARALLAXPARALLAXFACTOR:
                            Scene.hParallax.parallaxFactor[arrayVal] = state.operands[i];
                            break;
                        case VAR.HPARALLAXSCROLLSPEED:
                            Scene.hParallax.scrollSpeed[arrayVal] = state.operands[i];
                            break;
                        case VAR.HPARALLAXSCROLLPOS:
                            Scene.hParallax.scrollPos[arrayVal] = state.operands[i];
                            break;
                        case VAR.VPARALLAXPARALLAXFACTOR:
                            Scene.vParallax.parallaxFactor[arrayVal] = state.operands[i];
                            break;
                        case VAR.VPARALLAXSCROLLSPEED:
                            Scene.vParallax.scrollSpeed[arrayVal] = state.operands[i];
                            break;
                        case VAR.VPARALLAXSCROLLPOS:
                            Scene.vParallax.scrollPos[arrayVal] = state.operands[i];
                            break;
                        case VAR.SCENE3DVERTEXCOUNT:
                            Scene3D.vertexCount = state.operands[i];
                            break;
                        case VAR.SCENE3DFACECOUNT:
                            Scene3D.faceCount = state.operands[i];
                            break;
                        case VAR.SCENE3DPROJECTIONX:
                            Scene3D.projectionX = state.operands[i];
                            break;
                        case VAR.SCENE3DPROJECTIONY:
                            Scene3D.projectionY = state.operands[i];
                            break;
                        case VAR.SCENE3DFOGCOLOR:
                            Scene3D.fogColor = state.operands[i];
                            break;
                        case VAR.SCENE3DFOGSTRENGTH:
                            Scene3D.fogStrength = state.operands[i];
                            break;
                        case VAR.VERTEXBUFFERX:
                            Scene3D.vertexBuffer[arrayVal].x = state.operands[i];
                            break;
                        case VAR.VERTEXBUFFERY:
                            Scene3D.vertexBuffer[arrayVal].y = state.operands[i];
                            break;
                        case VAR.VERTEXBUFFERZ:
                            Scene3D.vertexBuffer[arrayVal].z = state.operands[i];
                            break;
                        case VAR.VERTEXBUFFERU:
                            Scene3D.vertexBuffer[arrayVal].u = state.operands[i];
                            break;
                        case VAR.VERTEXBUFFERV:
                            Scene3D.vertexBuffer[arrayVal].v = state.operands[i];
                            break;
                        case VAR.FACEBUFFERA:
                            Scene3D.faceBuffer[arrayVal].a = state.operands[i];
                            break;
                        case VAR.FACEBUFFERB:
                            Scene3D.faceBuffer[arrayVal].b = state.operands[i];
                            break;
                        case VAR.FACEBUFFERC:
                            Scene3D.faceBuffer[arrayVal].c = state.operands[i];
                            break;
                        case VAR.FACEBUFFERD:
                            Scene3D.faceBuffer[arrayVal].d = state.operands[i];
                            break;
                        case VAR.FACEBUFFERFLAG:
                            Scene3D.faceBuffer[arrayVal].flag = (byte)state.operands[i];
                            break;
                        case VAR.FACEBUFFERCOLOR:
                            Scene3D.faceBuffer[arrayVal].color = state.operands[i];
                            break;
                        case VAR.SAVERAM:
                            SaveData.saveRAM[arrayVal] = state.operands[i];
                            break;
                        case VAR.ENGINESTATE:
                            Engine.engineState = state.operands[i];
                            break;
                        case VAR.ENGINEMESSAGE: break;
                        case VAR.ENGINELANGUAGE:
                            Engine.language = state.operands[i];
                            break;
                        case VAR.ENGINEONLINEACTIVE:
                            Engine.onlineActive = state.operands[i] != 0;
                            break;
                        case VAR.ENGINESFXVOLUME:
                            Audio.sfxVolume = state.operands[i];
                            Audio.SetGameVolumes(Audio.bgmVolume, Audio.sfxVolume);
                            break;
                        case VAR.ENGINEBGMVOLUME:
                            Audio.bgmVolume = state.operands[i];
                            Audio.SetGameVolumes(Audio.bgmVolume, Audio.sfxVolume);
                            break;
                        case VAR.ENGINETRIALMODE:
                            Engine.trialMode = state.operands[i] != 0;
                            break;
                        case VAR.ENGINEDEVICETYPE:
                            Engine.hapticsEnabled = state.operands[i] != 0;
                            break;
                        case VAR.SCREENCURRENTID: break;
                        case VAR.CAMERAENABLED:
                            if (arrayVal <= 1)
                                Scene.cameraEnabled = state.operands[i] != 0;
                            break;
                        case VAR.CAMERATARGET:
                            if (arrayVal <= 1)
                                Scene.cameraTarget = state.operands[i];
                            break;
                        case VAR.CAMERASTYLE:
                            if (arrayVal <= 1)
                                Scene.cameraStyle = state.operands[i];
                            break;
                        case VAR.CAMERAXPOS:
                            if (arrayVal <= 1)
                                Scene.cameraXPos = state.operands[i];
                            break;
                        case VAR.CAMERAYPOS:
                            if (arrayVal <= 1)
                                Scene.cameraYPos = state.operands[i];
                            break;
                        case VAR.CAMERAADJUSTY:
                            if (arrayVal <= 1)
                                Scene.cameraAdjustY = state.operands[i];
                            break;
                    }
                }
                else if (storeType == SRC.SCRIPTINTCONST)
                {
                    // int constant
                    scriptDataPtr++;
                }
                else if (storeType == SRC.SCRIPTSTRCONST)
                {
                    // string constant
                    int strLen = scriptData[scriptDataPtr++];
                    for (int c = 0; c < strLen; ++c)
                    {
                        switch (c % 4)
                        {
                            case 0: break;
                            case 1: break;
                            case 2: break;
                            case 3:
                                ++scriptDataPtr;
                                break;
                            default: break;
                        }
                    }

                    scriptDataPtr++;
                }
            }
        }
    }

    public void ClearScriptData()
    {
        translator = new BytecodeTranslator(Engine.engineRevision);

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

        Animation.ClearAnimationData();

        for (int o = 0; o < Objects.OBJECT_COUNT; ++o)
        {
            ObjectScript scriptInfo = objectScriptList[o] = new ObjectScript();
            scriptInfo.eventUpdate.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventUpdate.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.eventDraw.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventDraw.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.eventStartup.scriptCodePtr = SCRIPTDATA_COUNT - 1;
            scriptInfo.eventStartup.jumpTablePtr = JUMPTABLE_COUNT - 1;
            scriptInfo.frameListOffset = 0;
            scriptInfo.spriteSheetId = 0;
            scriptInfo.animFile = Animation.animationFileList[0];
            Objects.typeNames[o] = null;
        }

        for (int s = Engine.globalSfxCount; s < Engine.globalSfxCount + Audio.stageSfxCount; ++s)
        {
            Audio.soundEffects[s] = new SfxInfo();
        }

        for (int f = 0; f < FUNCTION_COUNT; ++f)
        {
            functionScriptList[f].scriptCodePtr = SCRIPTDATA_COUNT - 1;
            functionScriptList[f].jumpTablePtr = JUMPTABLE_COUNT - 1;
        }

        Objects.SetObjectTypeName("Blank Object", Objects.OBJ_TYPE_BLANKOBJECT);
    }

    public void ClearStacks()
    {
        Helpers.Memset(foreachStack, -1);
        Helpers.Memset(jumpTableStack, 0);
    }
}