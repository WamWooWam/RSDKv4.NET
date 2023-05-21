using System;
using System.Collections.Generic;
using System.Diagnostics;
using RSDKv4.Native;
using RSDKv4.Utility;

using static RSDKv4.Scene;
using static RSDKv4.Script;

namespace RSDKv4;

public class Objects
{
    public const int NATIVEENTITY_COUNT = 0x100;
    public const int ENTITY_COUNT = 0x4A0;
    public const int OBJECT_COUNT = 0x100;
    public const int TYPEGROUP_COUNT = 0x103;
    public const int TEMPENTITY_START = ENTITY_COUNT - 0x80;

    public static int nativeEntityPos = 0;
    public static int playerListPos = 0;

    public static int[] activeEntityList = new int[NATIVEENTITY_COUNT];
    public static byte[] objectRemoveFlag = new byte[NATIVEENTITY_COUNT];

    public static NativeEntity[] nativeEntityBank = new NativeEntity[NATIVEENTITY_COUNT];
    public static int nativeEntityCount = 0;

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

    public static int OBJECT_BORDER_Y1 = 0x100;
    public static int OBJECT_BORDER_Y2 = Drawing.SCREEN_YSIZE + 0x100;
    public static int OBJECT_BORDER_Y3 = 0x80;
    public static int OBJECT_BORDER_Y4 = Drawing.SCREEN_YSIZE + 0x80;

    public const int OBJ_TYPE_BLANKOBJECT = 0;
    public const int GROUP_ALL = 0;

    static Objects()
    {
        Helpers.Memset(objectEntityList, () => new Entity());
        Helpers.Memset(objectTypeGroupList, () => new TypeGroupList());
    }

    public static void ProcessStartupObjects()
    {
        Animation.scriptFrameCount = 0;
        Animation.ClearAnimationData();
        scriptEng.arrayPosition[8] = TEMPENTITY_START;

        OBJECT_BORDER_X1 = 0x80;
        OBJECT_BORDER_X3 = 0x20;
        OBJECT_BORDER_X2 = Drawing.SCREEN_XSIZE + 0x80;
        OBJECT_BORDER_X4 = Drawing.SCREEN_XSIZE + 0x20;

        Entity entity = objectEntityList[TEMPENTITY_START];
        objectEntityList[TEMPENTITY_START + 1].type = objectEntityList[0].type;

        ClearStacks();

        for (int i = 0; i < OBJECT_COUNT; ++i)
        {
            ObjectScript scriptInfo = objectScriptList[i];
            objectEntityPos = TEMPENTITY_START;
            curObjectType = i;
            scriptInfo.frameListOffset = Animation.scriptFrameCount;
            scriptInfo.spriteSheetId = 0;
            entity.type = (byte)i;

            ProcessScript(scriptInfo.eventStartup.scriptCodePtr, scriptInfo.eventStartup.jumpTablePtr, EVENT.SETUP);

            scriptInfo.frameCount = Animation.scriptFrameCount - scriptInfo.frameListOffset;
        }
        entity.type = 0;
        curObjectType = 0;
    }

    public static void ProcessObjects()
    {
        for (int i = 0; i < DRAWLAYER_COUNT; ++i) drawListEntries[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            processObjectFlag[objectEntityPos] = false;

            var entity = objectEntityList[objectEntityPos];
            var x = entity.xpos >> 16;
            var y = entity.ypos >> 16;

            switch (entity.priority)
            {
                case PRIORITY.BOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2
                                                         && y > yScrollOffset - OBJECT_BORDER_Y1 && y < yScrollOffset + OBJECT_BORDER_Y2;
                    break;

                case PRIORITY.ACTIVE:
                case PRIORITY.ALWAYS:
                case PRIORITY.ACTIVE_SMALL: processObjectFlag[objectEntityPos] = true; break;

                case PRIORITY.XBOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < OBJECT_BORDER_X2 + xScrollOffset;
                    break;

                case PRIORITY.XBOUNDS_DESTROY:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2;
                    if (!processObjectFlag[objectEntityPos])
                    {
                        processObjectFlag[objectEntityPos] = false;
                        entity.type = OBJ_TYPE_BLANKOBJECT;
                    }
                    break;

                case PRIORITY.INACTIVE: processObjectFlag[objectEntityPos] = false; break;
                case PRIORITY.BOUNDS_SMALL:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X3 && x < OBJECT_BORDER_X4 + xScrollOffset
                                                         && y > yScrollOffset - OBJECT_BORDER_Y3 && y < yScrollOffset + OBJECT_BORDER_Y4;
                    break;

                default: break;
            }

            if (processObjectFlag[objectEntityPos] && entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = objectScriptList[entity.type];
                ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder >= 0 && entity.drawOrder < DRAWLAYER_COUNT)
                    drawListEntries[entity.drawOrder].entityRefs[drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
            }
        }

        for (int i = 0; i < TYPEGROUP_COUNT; ++i) objectTypeGroupList[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            Entity entity = objectEntityList[objectEntityPos];
            if (processObjectFlag[objectEntityPos] && entity.objectInteractions)
            {
                // Custom Group
                if (entity.groupID >= OBJECT_COUNT)
                {
                    TypeGroupList listCustom = objectTypeGroupList[objectEntityList[objectEntityPos].groupID];
                    listCustom.entityRefs[listCustom.listSize++] = objectEntityPos;
                }

                // Type-Specific list
                TypeGroupList listType = objectTypeGroupList[objectEntityList[objectEntityPos].type];
                if (listType.listSize < ENTITY_COUNT)
                    listType.entityRefs[listType.listSize++] = objectEntityPos;

                // All Entities list
                TypeGroupList listAll = objectTypeGroupList[GROUP_ALL];
                if (listAll.listSize < ENTITY_COUNT)
                    listAll.entityRefs[listAll.listSize++] = objectEntityPos;
            }
        }
    }

    public static void ProcessPausedObjects()
    {
        for (int i = 0; i < DRAWLAYER_COUNT; ++i) drawListEntries[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            Entity entity = objectEntityList[objectEntityPos];

            if (entity.priority == PRIORITY.ALWAYS && entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = objectScriptList[entity.type];
                ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder < DRAWLAYER_COUNT && entity.drawOrder >= 0)
                    drawListEntries[entity.drawOrder].entityRefs[drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
            }
        }
    }

    public static void ProcessFrozenObjects()
    {
        for (int i = 0; i < DRAWLAYER_COUNT; ++i) drawListEntries[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            processObjectFlag[objectEntityPos] = false;
            int x = 0, y = 0;
            Entity entity = objectEntityList[objectEntityPos];
            x = entity.xpos >> 16;
            y = entity.ypos >> 16;

            switch (entity.priority)
            {
                case PRIORITY.BOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2
                                                         && y > yScrollOffset - OBJECT_BORDER_Y1 && y < yScrollOffset + OBJECT_BORDER_Y2;
                    break;

                case PRIORITY.ACTIVE:
                case PRIORITY.ALWAYS:
                case PRIORITY.ACTIVE_SMALL: processObjectFlag[objectEntityPos] = true; break;

                case PRIORITY.XBOUNDS:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < OBJECT_BORDER_X2 + xScrollOffset;
                    break;

                case PRIORITY.XBOUNDS_DESTROY:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X1 && x < xScrollOffset + OBJECT_BORDER_X2;
                    if (!processObjectFlag[objectEntityPos])
                    {
                        processObjectFlag[objectEntityPos] = false;
                        entity.type = OBJ_TYPE_BLANKOBJECT;
                    }
                    break;

                case PRIORITY.INACTIVE: processObjectFlag[objectEntityPos] = false; break;

                case PRIORITY.BOUNDS_SMALL:
                    processObjectFlag[objectEntityPos] = x > xScrollOffset - OBJECT_BORDER_X3 && x < OBJECT_BORDER_X4 + xScrollOffset
                                                         && y > yScrollOffset - OBJECT_BORDER_Y3 && y < yScrollOffset + OBJECT_BORDER_Y4;
                    break;

                default: break;
            }

            if (entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = objectScriptList[entity.type];
                if (entity.priority == PRIORITY.ALWAYS)
                    ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder < DRAWLAYER_COUNT && entity.drawOrder >= 0)
                    drawListEntries[entity.drawOrder].entityRefs[drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
            }
        }

        for (int i = 0; i < TYPEGROUP_COUNT; ++i) objectTypeGroupList[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            Entity entity = objectEntityList[objectEntityPos];
            if (processObjectFlag[objectEntityPos] && entity.objectInteractions)
            {
                // Custom Group
                if (entity.groupID >= OBJECT_COUNT)
                {
                    TypeGroupList listCustom = objectTypeGroupList[objectEntityList[objectEntityPos].groupID];
                    listCustom.entityRefs[listCustom.listSize++] = objectEntityPos;
                }
                // Type-Specific list
                TypeGroupList listType = objectTypeGroupList[objectEntityList[objectEntityPos].type];
                if (listType.listSize < ENTITY_COUNT)
                    listType.entityRefs[listType.listSize++] = objectEntityPos;

                // All Entities list
                TypeGroupList listAll = objectTypeGroupList[GROUP_ALL];
                if (listAll.listSize < ENTITY_COUNT)
                    listAll.entityRefs[listAll.listSize++] = objectEntityPos;
            }
        }
    }

    public static void ProcessObjectControl(Entity player)
    {
        if (player.controlMode == 0)
        {
            player.up = Input.keyDown.up;
            player.down = Input.keyDown.down;
            if (Input.keyDown.left || Input.keyDown.right)
            {
                player.left = Input.keyDown.left;
                player.right = Input.keyDown.right;
            }
            else
            {
                player.left = false;
                player.right = false;
            }
            player.jumpHold = Input.keyDown.C || Input.keyDown.B || Input.keyDown.A;
            player.jumpPress = Input.keyPress.C || Input.keyPress.B || Input.keyPress.A;
        }
    }

    public static void SetObjectTypeName(string objectName, int objectID)
    {
        typeNames[objectID] = objectName;
        Debug.WriteLine("Set Object ({0}) name to: {1}", objectID, objectName);
    }

    public static T CreateNativeObject<T>(Func<T> factory) where T : NativeEntity
    {
        NativeEntity entity;
        if (nativeEntityCount == 0)
        {
            Helpers.Memset(nativeEntityBank, (NativeEntity)null);
            entity = nativeEntityBank[0] = factory();
            activeEntityList[0] = 0;
            nativeEntityCount++;
            entity.Create();
            return (T)entity;
        }

        Debug.Assert(nativeEntityCount < NATIVEENTITY_COUNT);

        int slot = 0;
        for (; slot < NATIVEENTITY_COUNT; ++slot)
        {
            if (nativeEntityBank[slot] == null)
                break;
        }

        nativeEntityBank[slot] = entity = factory();
        entity.slotId = slot;
        entity.objectId = nativeEntityCount;
        activeEntityList[nativeEntityCount++] = slot;
        entity.Create();
        return (T)entity;
    }

    public static NativeEntity ResetNativeObject<T>(NativeEntity obj, Func<T> factory) where T : NativeEntity
    {
        int slotId = obj.slotId;
        int objId = obj.objectId;

        nativeEntityBank[slotId] = factory();
        nativeEntityBank[slotId].slotId = slotId;
        nativeEntityBank[slotId].objectId = objId;
        nativeEntityBank[slotId].Create();

        return nativeEntityBank[slotId];
    }

    public static NativeEntity GetNativeObject(uint objId)
    {
        if (objId >= NATIVEENTITY_COUNT)
            return null;
        else
            return nativeEntityBank[objId];
    }

    public static void ClearNativeObjects()
    {
        nativeEntityCount = 0;
        nativeEntityBank = new NativeEntity[NATIVEENTITY_COUNT];
    }

    public static void ProcessNativeObjects()
    {
        NativeRenderer.ResetRenderStates();

        NativeRenderer.BeginDraw();
        for (nativeEntityPos = 0; nativeEntityPos < nativeEntityCount; ++nativeEntityPos)
        {
            NativeEntity entity = nativeEntityBank[activeEntityList[nativeEntityPos]];
            entity.Main();
        }
        NativeRenderer.EndDraw();
    }

    public static void RemoveNativeObjects<T>() where T : NativeEntity
    {
        for (int i = nativeEntityCount - 1; i >= 0; --i)
        {
            var entity = nativeEntityBank[activeEntityList[i]];
            if (entity is T)
                RemoveNativeObject(entity);
        }
    }

    public static void RemoveNativeObject(NativeEntity obj)
    {
        Array.Copy(activeEntityList, obj.objectId + 1, activeEntityList, obj.objectId, NATIVEENTITY_COUNT - (obj.objectId + 2));

        --nativeEntityCount;
        for (int i = obj.slotId; nativeEntityBank[i] != null; ++i)
            nativeEntityBank[i].objectId--;
    }
}
