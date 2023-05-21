﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static RSDKv4.Script;

namespace RSDKv4.Bytecode;

internal static class BytecodeRev3
{
    public static readonly VAR[] variables = new[]
    {
        VAR.TEMP0,
        VAR.TEMP1,
        VAR.TEMP2,
        VAR.TEMP3,
        VAR.TEMP4,
        VAR.TEMP5,
        VAR.TEMP6,
        VAR.TEMP7,
        VAR.CHECKRESULT,
        VAR.ARRAYPOS0,
        VAR.ARRAYPOS1,
        VAR.ARRAYPOS2,
        VAR.ARRAYPOS3,
        VAR.ARRAYPOS4,
        VAR.ARRAYPOS5,
        VAR.ARRAYPOS6,
        VAR.ARRAYPOS7,
        VAR.GLOBAL,
        VAR.LOCAL,
        VAR.OBJECTENTITYPOS,
        VAR.OBJECTGROUPID,
        VAR.OBJECTTYPE,
        VAR.OBJECTPROPERTYVALUE,
        VAR.OBJECTXPOS,
        VAR.OBJECTYPOS,
        VAR.OBJECTIXPOS,
        VAR.OBJECTIYPOS,
        VAR.OBJECTXVEL,
        VAR.OBJECTYVEL,
        VAR.OBJECTSPEED,
        VAR.OBJECTSTATE,
        VAR.OBJECTROTATION,
        VAR.OBJECTSCALE,
        VAR.OBJECTPRIORITY,
        VAR.OBJECTDRAWORDER,
        VAR.OBJECTDIRECTION,
        VAR.OBJECTINKEFFECT,
        VAR.OBJECTALPHA,
        VAR.OBJECTFRAME,
        VAR.OBJECTANIMATION,
        VAR.OBJECTPREVANIMATION,
        VAR.OBJECTANIMATIONSPEED,
        VAR.OBJECTANIMATIONTIMER,
        VAR.OBJECTANGLE,
        VAR.OBJECTLOOKPOSX,
        VAR.OBJECTLOOKPOSY,
        VAR.OBJECTCOLLISIONMODE,
        VAR.OBJECTCOLLISIONPLANE,
        VAR.OBJECTCONTROLMODE,
        VAR.OBJECTCONTROLLOCK,
        VAR.OBJECTPUSHING,
        VAR.OBJECTVISIBLE,
        VAR.OBJECTTILECOLLISIONS,
        VAR.OBJECTINTERACTION,
        VAR.OBJECTGRAVITY,
        VAR.OBJECTUP,
        VAR.OBJECTDOWN,
        VAR.OBJECTLEFT,
        VAR.OBJECTRIGHT,
        VAR.OBJECTJUMPPRESS,
        VAR.OBJECTJUMPHOLD,
        VAR.OBJECTSCROLLTRACKING,
        VAR.OBJECTFLOORSENSORL,
        VAR.OBJECTFLOORSENSORC,
        VAR.OBJECTFLOORSENSORR,
        VAR.OBJECTFLOORSENSORLC,
        VAR.OBJECTFLOORSENSORRC,
        VAR.OBJECTCOLLISIONLEFT,
        VAR.OBJECTCOLLISIONTOP,
        VAR.OBJECTCOLLISIONRIGHT,
        VAR.OBJECTCOLLISIONBOTTOM,
        VAR.OBJECTOUTOFBOUNDSREV0,
        VAR.OBJECTSPRITESHEET,
        VAR.OBJECTVALUE0,
        VAR.OBJECTVALUE1,
        VAR.OBJECTVALUE2,
        VAR.OBJECTVALUE3,
        VAR.OBJECTVALUE4,
        VAR.OBJECTVALUE5,
        VAR.OBJECTVALUE6,
        VAR.OBJECTVALUE7,
        VAR.OBJECTVALUE8,
        VAR.OBJECTVALUE9,
        VAR.OBJECTVALUE10,
        VAR.OBJECTVALUE11,
        VAR.OBJECTVALUE12,
        VAR.OBJECTVALUE13,
        VAR.OBJECTVALUE14,
        VAR.OBJECTVALUE15,
        VAR.OBJECTVALUE16,
        VAR.OBJECTVALUE17,
        VAR.OBJECTVALUE18,
        VAR.OBJECTVALUE19,
        VAR.OBJECTVALUE20,
        VAR.OBJECTVALUE21,
        VAR.OBJECTVALUE22,
        VAR.OBJECTVALUE23,
        VAR.OBJECTVALUE24,
        VAR.OBJECTVALUE25,
        VAR.OBJECTVALUE26,
        VAR.OBJECTVALUE27,
        VAR.OBJECTVALUE28,
        VAR.OBJECTVALUE29,
        VAR.OBJECTVALUE30,
        VAR.OBJECTVALUE31,
        VAR.OBJECTVALUE32,
        VAR.OBJECTVALUE33,
        VAR.OBJECTVALUE34,
        VAR.OBJECTVALUE35,
        VAR.OBJECTVALUE36,
        VAR.OBJECTVALUE37,
        VAR.OBJECTVALUE38,
        VAR.OBJECTVALUE39,
        VAR.OBJECTVALUE40,
        VAR.OBJECTVALUE41,
        VAR.OBJECTVALUE42,
        VAR.OBJECTVALUE43,
        VAR.OBJECTVALUE44,
        VAR.OBJECTVALUE45,
        VAR.OBJECTVALUE46,
        VAR.OBJECTVALUE47,
        VAR.STAGESTATE,
        VAR.STAGEACTIVELIST,
        VAR.STAGELISTPOS,
        VAR.STAGETIMEENABLED,
        VAR.STAGEMILLISECONDS,
        VAR.STAGESECONDS,
        VAR.STAGEMINUTES,
        VAR.STAGEACTNUM,
        VAR.STAGEPAUSEENABLED,
        VAR.STAGELISTSIZE,
        VAR.STAGENEWXBOUNDARY1,
        VAR.STAGENEWXBOUNDARY2,
        VAR.STAGENEWYBOUNDARY1,
        VAR.STAGENEWYBOUNDARY2,
        VAR.STAGECURXBOUNDARY1,
        VAR.STAGECURXBOUNDARY2,
        VAR.STAGECURYBOUNDARY1,
        VAR.STAGECURYBOUNDARY2,
        VAR.STAGEDEFORMATIONDATA0,
        VAR.STAGEDEFORMATIONDATA1,
        VAR.STAGEDEFORMATIONDATA2,
        VAR.STAGEDEFORMATIONDATA3,
        VAR.STAGEWATERLEVEL,
        VAR.STAGEACTIVELAYER,
        VAR.STAGEMIDPOINT,
        VAR.STAGEPLAYERLISTPOS,
        VAR.STAGEDEBUGMODE,
        VAR.STAGEENTITYPOS,
        VAR.SCREENCAMERAENABLED,
        VAR.SCREENCAMERATARGET,
        VAR.SCREENCAMERASTYLE,
        VAR.SCREENCAMERAX,
        VAR.SCREENCAMERAY,
        VAR.SCREENDRAWLISTSIZE,
        VAR.SCREENXCENTER,
        VAR.SCREENYCENTER,
        VAR.SCREENXSIZE,
        VAR.SCREENYSIZE,
        VAR.SCREENXOFFSET,
        VAR.SCREENYOFFSET,
        VAR.SCREENSHAKEX,
        VAR.SCREENSHAKEY,
        VAR.SCREENADJUSTCAMERAY,
        VAR.TOUCHSCREENDOWN,
        VAR.TOUCHSCREENXPOS,
        VAR.TOUCHSCREENYPOS,
        VAR.MUSICVOLUME,
        VAR.MUSICCURRENTTRACK,
        VAR.MUSICPOSITION,
        VAR.INPUTDOWNUP,
        VAR.INPUTDOWNDOWN,
        VAR.INPUTDOWNLEFT,
        VAR.INPUTDOWNRIGHT,
        VAR.INPUTDOWNBUTTONA,
        VAR.INPUTDOWNBUTTONB,
        VAR.INPUTDOWNBUTTONC,
        VAR.INPUTDOWNBUTTONX,
        VAR.INPUTDOWNBUTTONY,
        VAR.INPUTDOWNBUTTONZ,
        VAR.INPUTDOWNBUTTONL,
        VAR.INPUTDOWNBUTTONR,
        VAR.INPUTDOWNSTART,
        VAR.INPUTDOWNSELECT,
        VAR.INPUTPRESSUP,
        VAR.INPUTPRESSDOWN,
        VAR.INPUTPRESSLEFT,
        VAR.INPUTPRESSRIGHT,
        VAR.INPUTPRESSBUTTONA,
        VAR.INPUTPRESSBUTTONB,
        VAR.INPUTPRESSBUTTONC,
        VAR.INPUTPRESSBUTTONX,
        VAR.INPUTPRESSBUTTONY,
        VAR.INPUTPRESSBUTTONZ,
        VAR.INPUTPRESSBUTTONL,
        VAR.INPUTPRESSBUTTONR,
        VAR.INPUTPRESSSTART,
        VAR.INPUTPRESSSELECT,
        VAR.MENU1SELECTION,
        VAR.MENU2SELECTION,
        VAR.TILELAYERXSIZE,
        VAR.TILELAYERYSIZE,
        VAR.TILELAYERTYPE,
        VAR.TILELAYERANGLE,
        VAR.TILELAYERXPOS,
        VAR.TILELAYERYPOS,
        VAR.TILELAYERZPOS,
        VAR.TILELAYERPARALLAXFACTOR,
        VAR.TILELAYERSCROLLSPEED,
        VAR.TILELAYERSCROLLPOS,
        VAR.TILELAYERDEFORMATIONOFFSET,
        VAR.TILELAYERDEFORMATIONOFFSETW,
        VAR.HPARALLAXPARALLAXFACTOR,
        VAR.HPARALLAXSCROLLSPEED,
        VAR.HPARALLAXSCROLLPOS,
        VAR.VPARALLAXPARALLAXFACTOR,
        VAR.VPARALLAXSCROLLSPEED,
        VAR.VPARALLAXSCROLLPOS,
        VAR.SCENE3DVERTEXCOUNT,
        VAR.SCENE3DFACECOUNT,
        VAR.SCENE3DPROJECTIONX,
        VAR.SCENE3DPROJECTIONY,
        VAR.SCENE3DFOGCOLOR,
        VAR.SCENE3DFOGSTRENGTH,
        VAR.VERTEXBUFFERX,
        VAR.VERTEXBUFFERY,
        VAR.VERTEXBUFFERZ,
        VAR.VERTEXBUFFERU,
        VAR.VERTEXBUFFERV,
        VAR.FACEBUFFERA,
        VAR.FACEBUFFERB,
        VAR.FACEBUFFERC,
        VAR.FACEBUFFERD,
        VAR.FACEBUFFERFLAG,
        VAR.FACEBUFFERCOLOR,
        VAR.SAVERAM,
        VAR.ENGINESTATE,
        VAR.ENGINELANGUAGE,
        VAR.ENGINEONLINEACTIVE,
        VAR.ENGINESFXVOLUME,
        VAR.ENGINEBGMVOLUME,
        VAR.ENGINETRIALMODE,
        VAR.ENGINEDEVICETYPE,
        VAR.SCREENCURRENTID,
        VAR.CAMERAENABLED,
        VAR.CAMERATARGET,
        VAR.CAMERASTYLE,
        VAR.CAMERAXPOS,
        VAR.CAMERAYPOS,
        VAR.CAMERAADJUSTY,
        VAR.HAPTICSENABLED,
        VAR.MAX_CNT
    };

    public static readonly FUNC[] functions = new[]
    {
        FUNC.END,
        FUNC.EQUAL,
        FUNC.ADD,
        FUNC.SUB,
        FUNC.INC,
        FUNC.DEC,
        FUNC.MUL,
        FUNC.DIV,
        FUNC.SHR,
        FUNC.SHL,
        FUNC.AND,
        FUNC.OR,
        FUNC.XOR,
        FUNC.MOD,
        FUNC.FLIPSIGN,
        FUNC.CHECKEQUAL,
        FUNC.CHECKGREATER,
        FUNC.CHECKLOWER,
        FUNC.CHECKNOTEQUAL,
        FUNC.IFEQUAL,
        FUNC.IFGREATER,
        FUNC.IFGREATEROREQUAL,
        FUNC.IFLOWER,
        FUNC.IFLOWEROREQUAL,
        FUNC.IFNOTEQUAL,
        FUNC.ELSE,
        FUNC.ENDIF,
        FUNC.WEQUAL,
        FUNC.WGREATER,
        FUNC.WGREATEROREQUAL,
        FUNC.WLOWER,
        FUNC.WLOWEROREQUAL,
        FUNC.WNOTEQUAL,
        FUNC.LOOP,
        FUNC.FOREACHACTIVE,
        FUNC.FOREACHALL,
        FUNC.NEXT,
        FUNC.SWITCH,
        FUNC.BREAK,
        FUNC.ENDSWITCH,
        FUNC.RAND,
        FUNC.SIN,
        FUNC.COS,
        FUNC.SIN256,
        FUNC.COS256,
        FUNC.ATAN2,
        FUNC.INTERPOLATE,
        FUNC.INTERPOLATEXY,
        FUNC.LOADSPRITESHEET,
        FUNC.REMOVESPRITESHEET,
        FUNC.DRAWSPRITE,
        FUNC.DRAWSPRITEXY,
        FUNC.DRAWSPRITESCREENXY,
        FUNC.DRAWTINTRECT,
        FUNC.DRAWNUMBERS,
        FUNC.DRAWACTNAME,
        FUNC.DRAWMENU,
        FUNC.SPRITEFRAME,
        FUNC.EDITFRAME,
        FUNC.LOADPALETTE,
        FUNC.ROTATEPALETTE,
        FUNC.SETSCREENFADE,
        FUNC.SETACTIVEPALETTE,
        FUNC.SETPALETTEFADEREV1,
        FUNC.SETPALETTEENTRY,
        FUNC.GETPALETTEENTRY,
        FUNC.COPYPALETTE,
        FUNC.CLEARSCREEN,
        FUNC.DRAWSPRITEFX,
        FUNC.DRAWSPRITESCREENFX,
        FUNC.LOADANIMATION,
        FUNC.SETUPMENU,
        FUNC.ADDMENUENTRY,
        FUNC.EDITMENUENTRY,
        FUNC.LOADSTAGE,
        FUNC.DRAWRECT,
        FUNC.RESETOBJECTENTITY,
        FUNC.BOXCOLLISIONTEST,
        FUNC.CREATETEMPOBJECT,
        FUNC.PROCESSOBJECTMOVEMENT,
        FUNC.PROCESSOBJECTCONTROL,
        FUNC.PROCESSANIMATION,
        FUNC.DRAWOBJECTANIMATION,
        FUNC.SETMUSICTRACK,
        FUNC.PLAYMUSIC,
        FUNC.STOPMUSIC,
        FUNC.PAUSEMUSIC,
        FUNC.RESUMEMUSIC,
        FUNC.SWAPMUSICTRACK,
        FUNC.PLAYSFX,
        FUNC.STOPSFX,
        FUNC.SETSFXATTRIBUTES,
        FUNC.OBJECTTILECOLLISION,
        FUNC.OBJECTTILEGRIP,
        FUNC.NOT,
        FUNC.DRAW3DSCENE,
        FUNC.SETIDENTITYMATRIX,
        FUNC.MATRIXMULTIPLY,
        FUNC.MATRIXTRANSLATEXYZ,
        FUNC.MATRIXSCALEXYZ,
        FUNC.MATRIXROTATEX,
        FUNC.MATRIXROTATEY,
        FUNC.MATRIXROTATEZ,
        FUNC.MATRIXROTATEXYZ,
        FUNC.MATRIXINVERSE,
        FUNC.TRANSFORMVERTICES,
        FUNC.CALLFUNCTION,
        FUNC.RETURN,
        FUNC.SETLAYERDEFORMATION,
        FUNC.CHECKTOUCHRECT,
        FUNC.GETTILELAYERENTRY,
        FUNC.SETTILELAYERENTRY,
        FUNC.GETBIT,
        FUNC.SETBIT,
        FUNC.CLEARDRAWLIST,
        FUNC.ADDDRAWLISTENTITYREF,
        FUNC.GETDRAWLISTENTITYREF,
        FUNC.SETDRAWLISTENTITYREF,
        FUNC.GET16X16TILEINFO,
        FUNC.SET16X16TILEINFO,
        FUNC.COPY16X16TILE,
        FUNC.GETANIMATIONBYNAME,
        FUNC.READSAVERAM,
        FUNC.WRITESAVERAM,
        FUNC.LOADTEXTFILEREV2,
        FUNC.GETTEXTINFO,
        FUNC.GETVERSIONNUMBER,
        FUNC.GETTABLEVALUE,
        FUNC.SETTABLEVALUE,
        FUNC.CHECKCURRENTSTAGEFOLDER,
        FUNC.ABS,
        FUNC.CALLNATIVEFUNCTION,
        FUNC.CALLNATIVEFUNCTION2,
        FUNC.CALLNATIVEFUNCTION4,
        FUNC.SETOBJECTRANGE,
        FUNC.GETOBJECTVALUE,
        FUNC.SETOBJECTVALUE,
        FUNC.COPYOBJECT,
        FUNC.PRINT,
        FUNC.CHECKCAMERAPROXIMITY,
        FUNC.SETSCREENCOUNT,
        FUNC.SETSCREENVERTICES,
        FUNC.GETINPUTDEVICEID,
        FUNC.GETFILTEREDINPUTDEVICEID,
        FUNC.GETINPUTDEVICETYPE,
        FUNC.ISINPUTDEVICEASSIGNED,
        FUNC.ASSIGNINPUTSLOTTODEVICE,
        FUNC.ISSLOTASSIGNED,
        FUNC.RESETINPUTSLOTASSIGNMENTS,
        FUNC.MAX_CNT
    };
}
