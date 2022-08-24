﻿using RSDKv4.Native;
using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RSDKv4;

internal class Objects
{
    public const int NATIVEENTITY_COUNT = 0x100;
    public const int ENTITY_COUNT = 0x4A0;
    public const int OBJECT_COUNT = 0x100;
    public const int TYPEGROUP_COUNT = 0x103;
    public const int TEMPENTITY_START = (ENTITY_COUNT - 0x80);

    public static int nativeEntityPos = 0;
    public static int playerListPos = 0;

    public static int[] activeEntityList = new int[NATIVEENTITY_COUNT];
    public static byte[] objectRemoveFlag = new byte[NATIVEENTITY_COUNT];
    public static NativeEntity[] objectEntityBank = new NativeEntity[NATIVEENTITY_COUNT];
    public static int nativeEntityCount;

    public static int objectEntityPos = 0;
    public static int curObjectType;
    public static Entity[] objectEntityList = new Entity[ENTITY_COUNT];
    public static bool[] processObjectFlag = new bool[ENTITY_COUNT];
    public static TypeGroupList[] objectTypeGroupList = new TypeGroupList[TYPEGROUP_COUNT];
    public static string[] typeNames = new string[OBJECT_COUNT];

    public static int OBJECT_BORDER_X1 = 0x80;
    public static int OBJECT_BORDER_X2 = Drawing.SCREEN_XSIZE + 0x80;
    public static int OBJECT_BORDER_X3 = 0x20;
    public static int OBJECT_BORDER_X4 = Drawing.SCREEN_XSIZE + 0x20;

    public static readonly int OBJECT_BORDER_Y1 = 0x100;
    public static readonly int OBJECT_BORDER_Y2 = Drawing.SCREEN_YSIZE + 0x100;
    public static readonly int OBJECT_BORDER_Y3 = 0x80;
    public static readonly int OBJECT_BORDER_Y4 = Drawing.SCREEN_YSIZE + 0x80;

    public const int OBJ_TYPE_BLANKOBJECT = 0;

    static Objects()
    {
        Helpers.Memset(objectEntityList, () => new Entity());
        Helpers.Memset(objectTypeGroupList, () => new TypeGroupList());
    }

    public static void ProcessStartupObjects()
    {
        Animation.scriptFrameCount = 0;
        Animation.ClearAnimationData();
        Script.scriptEng.arrayPosition[8] = TEMPENTITY_START;

        OBJECT_BORDER_X1 = 0x80;
        OBJECT_BORDER_X3 = 0x20;
        OBJECT_BORDER_X2 = Drawing.SCREEN_XSIZE + 0x80;
        OBJECT_BORDER_X4 = Drawing.SCREEN_XSIZE + 0x20;

        Entity entity = objectEntityList[TEMPENTITY_START];
        objectEntityList[TEMPENTITY_START + 1].type = objectEntityList[0].type;

        Helpers.Memset(Script.foreachStack, -1);
        Helpers.Memset(Script.jumpTableStack, 0);

        for (int i = 0; i < OBJECT_COUNT; ++i)
        {
            ObjectScript scriptInfo = Script.objectScriptList[i];
            objectEntityPos = TEMPENTITY_START;
            curObjectType = i;
            scriptInfo.frameListOffset = Animation.scriptFrameCount;
            scriptInfo.spriteSheetId = 0;
            entity.type = (byte)i;

            if (Script.scriptData[scriptInfo.eventStartup.scriptCodePtr] > 0)
                Script.ProcessScript(scriptInfo.eventStartup.scriptCodePtr, scriptInfo.eventStartup.jumpTablePtr, EVENT.SETUP);

            scriptInfo.frameCount = Animation.scriptFrameCount - scriptInfo.frameListOffset;
        }
        entity.type = 0;
        curObjectType = 0;
    }

    public static void ProcessObjects(int stageMode)
    {
        for (int i = 0; i < Scene.DRAWLAYER_COUNT; ++i) Scene.drawListEntries[i].entityRefs.Clear();

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            processObjectFlag[objectEntityPos] = false;

            int x = 0, y = 0;
            Entity entity = objectEntityList[objectEntityPos];
            x = entity.xpos >> 16;
            y = entity.ypos >> 16;

            var xScrollOffset = Scene.xScrollOffset;
            var yScrollOffset = Scene.yScrollOffset;

            switch (entity.priority)
            {
                case PRIORITY.ACTIVE_BOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2
                                                         && y > yScrollOffset - OBJECT_BORDER_Y1 && y < yScrollOffset + OBJECT_BORDER_Y2;
                    break;

                case PRIORITY.ACTIVE:
                case PRIORITY.ACTIVE_PAUSED:
                case PRIORITY.ACTIVE_SMALL: processObjectFlag[objectEntityPos] = true; break;

                case PRIORITY.ACTIVE_XBOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < OBJECT_BORDER_X2 + xScrollOffset;
                    break;

                case PRIORITY.ACTIVE_XBOUNDS_REMOVE:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2;
                    if (!processObjectFlag[objectEntityPos])
                    {
                        processObjectFlag[objectEntityPos] = false;
                        entity.type = OBJ_TYPE_BLANKOBJECT;
                    }
                    break;

                case PRIORITY.INACTIVE: processObjectFlag[objectEntityPos] = false; break;
                case PRIORITY.ACTIVE_BOUNDS_SMALL:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X3 && x < OBJECT_BORDER_X4 + xScrollOffset
                                                         && y > yScrollOffset - OBJECT_BORDER_Y3 && y < yScrollOffset + OBJECT_BORDER_Y4;
                    break;

                default: break;
            }

            if (processObjectFlag[objectEntityPos] && entity.type > 0)
            {
                ObjectScript scriptInfo = Script.objectScriptList[entity.type];
                if (Script.scriptData[scriptInfo.eventMain.scriptCodePtr] > 0)
                {
                    if (stageMode != STAGEMODE.FROZEN && stageMode != STAGEMODE.FROZEN_STEP || entity.priority == PRIORITY.ACTIVE_PAUSED)
                        Script.ProcessScript(scriptInfo.eventMain.scriptCodePtr, scriptInfo.eventMain.jumpTablePtr, EVENT.MAIN);
                }

                if (entity.drawOrder < Scene.DRAWLAYER_COUNT && entity.drawOrder >= 0)
                    Scene.drawListEntries[entity.drawOrder].entityRefs.Add(objectEntityPos);
            }
        }

        for (int i = 0; i < TYPEGROUP_COUNT; ++i) objectTypeGroupList[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            Entity entity = objectEntityList[objectEntityPos];
            if (processObjectFlag[objectEntityPos] && entity.objectInteractions && entity.type != 0)
            {
                // Custom Group
                if (entity.groupID >= OBJECT_COUNT)
                {
                    TypeGroupList listCustom = objectTypeGroupList[objectEntityList[objectEntityPos].groupID];
                    listCustom.entityRefs[listCustom.listSize++] = objectEntityPos;
                }

                // Type-Specific list
                TypeGroupList listType = objectTypeGroupList[objectEntityList[objectEntityPos].type];
                listType.entityRefs[listType.listSize++] = objectEntityPos;

                // All Entities list
                TypeGroupList listAll = objectTypeGroupList[0];
                listAll.entityRefs[listAll.listSize++] = objectEntityPos;
            }
        }
    }

    public static void ProcessObjectControl(Entity player)
    {
        if (player.controlMode == 0)
        {
            player.up = Input.inputDown.up;
            player.down = Input.inputDown.down;
            if (Input.inputDown.left == 0 || Input.inputDown.right == 0)
            {
                player.left = Input.inputDown.left;
                player.right = Input.inputDown.right;
            }
            else
            {
                player.left = 0;
                player.right = 0;
            }
            player.jumpHold = (Input.inputDown.C != 0 || Input.inputDown.B != 0 || Input.inputDown.A != 0) ? (byte)1 : (byte)0;
            player.jumpPress = (Input.inputPress.C != 0 || Input.inputPress.B != 0 || Input.inputPress.A != 0) ? (byte)1 : (byte)0;
        }
    }

    internal static void SetObjectTypeName(string objectName, int objectID)
    {
        typeNames[objectID] = objectName;
        Debug.WriteLine("Set Object ({0}) name to: {1}", objectID, objectName);
    }

    public static NativeEntity CreateNativeObject<T>(Func<T> factory) where T : NativeEntity
    {
        if (nativeEntityCount == 0)
        {
            Helpers.Memset(objectEntityBank, (NativeEntity)null);
            NativeEntity entity = objectEntityBank[0] = factory();
            activeEntityList[0] = 0;
            nativeEntityCount++;
            entity.Create();
            return entity;
        }
        else if (nativeEntityCount >= NATIVEENTITY_COUNT)
        {
            Debug.Assert(false);
            // TODO, probably never
            return null;
        }
        else
        {
            int slot = 0;
            for (; slot < NATIVEENTITY_COUNT; ++slot)
            {
                if (objectEntityBank[slot] == null)
                    break;
            }
            NativeEntity entity = objectEntityBank[slot] = factory();
            entity.slotId = slot;
            entity.objectId = nativeEntityCount;
            activeEntityList[nativeEntityCount++] = slot;
            entity.Create();
            return entity;
        }
    }

    public static NativeEntity ResetNativeObject<T>(NativeEntity obj, Func<T> factory) where T : NativeEntity
    {
        int slotId = obj.slotId;
        int objId = obj.objectId;

        objectEntityBank[slotId] = factory();
        objectEntityBank[slotId].slotId = slotId;
        objectEntityBank[slotId].objectId = objId;
        objectEntityBank[slotId].Create();

        return objectEntityBank[slotId];
    }

    public static void ProcessNativeObjects()
    {
        NativeRenderer.ResetRenderStates();

        NativeRenderer.BeginDraw();
        for (int i = 0; i < nativeEntityCount; i++)
            objectEntityBank[i]?.Main();
        NativeRenderer.EndDraw();
    }

    internal static void RemoveNativeObject(NativeEntity obj)
    {
        //throw new NotImplementedException();
    }
}
