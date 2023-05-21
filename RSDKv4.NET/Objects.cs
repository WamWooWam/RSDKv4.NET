using System;
using System.Collections.Generic;
using System.Diagnostics;
using RSDKv4.Native;
using RSDKv4.Utility;

namespace RSDKv4;

public class Objects
{
    public const int NATIVEENTITY_COUNT = 0x100;
    public const int ENTITY_COUNT = 0x4A0;
    public const int OBJECT_COUNT = 0x100;
    public const int TYPEGROUP_COUNT = 0x103;
    public const int TEMPENTITY_START = ENTITY_COUNT - 0x80;

    public int nativeEntityPos = 0;
    public int playerListPos = 0;

    public int[] activeEntityList = new int[NATIVEENTITY_COUNT];
    public byte[] objectRemoveFlag = new byte[NATIVEENTITY_COUNT];

    public NativeEntity[] nativeEntityBank = new NativeEntity[NATIVEENTITY_COUNT];
    public int nativeEntityCount = 0;

    public int objectEntityPos = 0;
    public int curObjectType;
    public Entity[] objectEntityList = new Entity[ENTITY_COUNT];
    public bool[] processObjectFlag = new bool[ENTITY_COUNT];
    public TypeGroupList[] objectTypeGroupList = new TypeGroupList[TYPEGROUP_COUNT];
    public string[] typeNames = new string[OBJECT_COUNT];

    public int OBJECT_BORDER_X1 = 0x80;
    public int OBJECT_BORDER_X2 = Drawing.SCREEN_XSIZE + 0x80;
    public int OBJECT_BORDER_X3 = 0x20;
    public int OBJECT_BORDER_X4 = Drawing.SCREEN_XSIZE + 0x20;

    public int OBJECT_BORDER_Y1 = 0x100;
    public int OBJECT_BORDER_Y2 = Drawing.SCREEN_YSIZE + 0x100;
    public int OBJECT_BORDER_Y3 = 0x80;
    public int OBJECT_BORDER_Y4 = Drawing.SCREEN_YSIZE + 0x80;

    public const int OBJ_TYPE_BLANKOBJECT = 0;
    public const int GROUP_ALL = 0;

    private Animation Animation;
    private Engine Engine;
    private Input Input;
    private Script Script;
    private Scene Scene;
    private NativeRenderer Renderer;

    public Objects()
    {
        Helpers.Memset(objectEntityList, () => new Entity());
        Helpers.Memset(objectTypeGroupList, () => new TypeGroupList());
    }

    public void Initialize(Engine engine)
    {
        Engine = engine;
        Animation = engine.Animation;
        Input = engine.Input;
        Script = engine.Script;
        Scene = engine.Scene;
        Renderer = engine.Renderer;
    }

    public void ProcessStartupObjects()
    {
        Animation.scriptFrameCount = 0;
        Animation.ClearAnimationData();
        Script.state.arrayPosition[8] = TEMPENTITY_START;

        OBJECT_BORDER_X1 = 0x80;
        OBJECT_BORDER_X3 = 0x20;
        OBJECT_BORDER_X2 = Drawing.SCREEN_XSIZE + 0x80;
        OBJECT_BORDER_X4 = Drawing.SCREEN_XSIZE + 0x20;

        Entity entity = objectEntityList[TEMPENTITY_START];
        objectEntityList[TEMPENTITY_START + 1].type = objectEntityList[0].type;

        Script.ClearStacks();

        for (int i = 0; i < OBJECT_COUNT; ++i)
        {
            ObjectScript scriptInfo = Script.objectScriptList[i];
            objectEntityPos = TEMPENTITY_START;
            curObjectType = i;
            scriptInfo.frameListOffset = Animation.scriptFrameCount;
            scriptInfo.spriteSheetId = 0;
            entity.type = (byte)i;

            Script.ProcessScript(scriptInfo.eventStartup.scriptCodePtr, scriptInfo.eventStartup.jumpTablePtr, EVENT.SETUP);

            scriptInfo.frameCount = Animation.scriptFrameCount - scriptInfo.frameListOffset;
        }
        entity.type = 0;
        curObjectType = 0;
    }

    public void ProcessObjects()
    {
        for (int i = 0; i < Scene.DRAWLAYER_COUNT; ++i) Scene.drawListEntries[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            processObjectFlag[objectEntityPos] = false;

            var entity = objectEntityList[objectEntityPos];
            var x = entity.xpos >> 16;
            var y = entity.ypos >> 16;

            switch (entity.priority)
            {
                case PRIORITY.BOUNDS:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < Scene.xScrollOffset + OBJECT_BORDER_X2
                                                         && y > Scene.yScrollOffset - OBJECT_BORDER_Y1 && y < Scene.yScrollOffset + OBJECT_BORDER_Y2;
                    break;

                case PRIORITY.ACTIVE:
                case PRIORITY.ALWAYS:
                case PRIORITY.ACTIVE_SMALL: processObjectFlag[objectEntityPos] = true; break;

                case PRIORITY.XBOUNDS:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < OBJECT_BORDER_X2 + Scene.xScrollOffset;
                    break;

                case PRIORITY.XBOUNDS_DESTROY:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < Scene.xScrollOffset + OBJECT_BORDER_X2;
                    if (!processObjectFlag[objectEntityPos])
                    {
                        processObjectFlag[objectEntityPos] = false;
                        entity.type = OBJ_TYPE_BLANKOBJECT;
                    }
                    break;

                case PRIORITY.INACTIVE: processObjectFlag[objectEntityPos] = false; break;
                case PRIORITY.BOUNDS_SMALL:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X3 && x < OBJECT_BORDER_X4 + Scene.xScrollOffset
                                                         && y > Scene.yScrollOffset - OBJECT_BORDER_Y3 && y < Scene.yScrollOffset + OBJECT_BORDER_Y4;
                    break;

                default: break;
            }

            if (processObjectFlag[objectEntityPos] && entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = Script.objectScriptList[entity.type];
                Script.ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder >= 0 && entity.drawOrder < Scene.DRAWLAYER_COUNT)
                    Scene.drawListEntries[entity.drawOrder].entityRefs[Scene.drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
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

    public void ProcessPausedObjects()
    {
        for (int i = 0; i < Scene.DRAWLAYER_COUNT; ++i) Scene.drawListEntries[i].listSize = 0;

        for (objectEntityPos = 0; objectEntityPos < ENTITY_COUNT; ++objectEntityPos)
        {
            Entity entity = objectEntityList[objectEntityPos];

            if (entity.priority == PRIORITY.ALWAYS && entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = Script.objectScriptList[entity.type];
                Script.ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder < Scene.DRAWLAYER_COUNT && entity.drawOrder >= 0)
                    Scene.drawListEntries[entity.drawOrder].entityRefs[Scene.drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
            }
        }
    }

    public void ProcessFrozenObjects()
    {
        for (int i = 0; i < Scene.DRAWLAYER_COUNT; ++i) Scene.drawListEntries[i].listSize = 0;

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
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < Scene.xScrollOffset + OBJECT_BORDER_X2
                                                         && y > Scene.yScrollOffset - OBJECT_BORDER_Y1 && y < Scene.yScrollOffset + OBJECT_BORDER_Y2;
                    break;

                case PRIORITY.ACTIVE:
                case PRIORITY.ALWAYS:
                case PRIORITY.ACTIVE_SMALL: processObjectFlag[objectEntityPos] = true; break;

                case PRIORITY.XBOUNDS:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < OBJECT_BORDER_X2 + Scene.xScrollOffset;
                    break;

                case PRIORITY.XBOUNDS_DESTROY:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X1 && x < Scene.xScrollOffset + OBJECT_BORDER_X2;
                    if (!processObjectFlag[objectEntityPos])
                    {
                        processObjectFlag[objectEntityPos] = false;
                        entity.type = OBJ_TYPE_BLANKOBJECT;
                    }
                    break;

                case PRIORITY.INACTIVE: processObjectFlag[objectEntityPos] = false; break;

                case PRIORITY.BOUNDS_SMALL:
                    processObjectFlag[objectEntityPos] = x > Scene.xScrollOffset - OBJECT_BORDER_X3 && x < OBJECT_BORDER_X4 + Scene.xScrollOffset
                                                         && y > Scene.yScrollOffset - OBJECT_BORDER_Y3 && y < Scene.yScrollOffset + OBJECT_BORDER_Y4;
                    break;

                default: break;
            }

            if (entity.type > OBJ_TYPE_BLANKOBJECT)
            {
                ObjectScript scriptInfo = Script.objectScriptList[entity.type];
                if (entity.priority == PRIORITY.ALWAYS)
                    Script.ProcessScript(scriptInfo.eventUpdate.scriptCodePtr, scriptInfo.eventUpdate.jumpTablePtr, EVENT.MAIN);

                if (entity.drawOrder < Scene.DRAWLAYER_COUNT && entity.drawOrder >= 0)
                    Scene.drawListEntries[entity.drawOrder].entityRefs[Scene.drawListEntries[entity.drawOrder].listSize++] = objectEntityPos;
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

    public void ProcessObjectControl(Entity player)
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

    public void SetObjectTypeName(string objectName, int objectID)
    {
        typeNames[objectID] = objectName;
        Debug.WriteLine("Set Object ({0}) name to: {1}", objectID, objectName);
    }

    public T CreateNativeObject<T>(Func<T> factory) where T : NativeEntity
    {
        NativeEntity entity;
        if (nativeEntityCount == 0)
        {
            Helpers.Memset(nativeEntityBank, (NativeEntity)null);
            entity = nativeEntityBank[0] = factory();
            activeEntityList[0] = 0;
            nativeEntityCount++;
            entity.Engine = Engine;
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
        entity.Engine = Engine;
        activeEntityList[nativeEntityCount++] = slot;
        entity.Create();
        return (T)entity;
    }

    public NativeEntity ResetNativeObject<T>(NativeEntity obj, Func<T> factory) where T : NativeEntity
    {
        int slotId = obj.slotId;
        int objId = obj.objectId;

        nativeEntityBank[slotId] = factory();
        nativeEntityBank[slotId].slotId = slotId;
        nativeEntityBank[slotId].objectId = objId;
        nativeEntityBank[slotId].Engine = Engine;
        nativeEntityBank[slotId].Create();

        return nativeEntityBank[slotId];
    }

    public NativeEntity GetNativeObject(uint objId)
    {
        if (objId >= NATIVEENTITY_COUNT)
            return null;
        else
            return nativeEntityBank[objId];
    }

    public void ClearNativeObjects()
    {
        nativeEntityCount = 0;
        nativeEntityBank = new NativeEntity[NATIVEENTITY_COUNT];
    }

    public void ProcessNativeObjects()
    {
        Renderer.ResetRenderStates();

        Renderer.BeginDraw();
        for (nativeEntityPos = 0; nativeEntityPos < nativeEntityCount; ++nativeEntityPos)
        {
            NativeEntity entity = nativeEntityBank[activeEntityList[nativeEntityPos]];
            entity.Main();
        }
        Renderer.EndDraw();
    }

    public void RemoveNativeObjects<T>() where T : NativeEntity
    {
        for (int i = nativeEntityCount - 1; i >= 0; --i)
        {
            var entity = nativeEntityBank[activeEntityList[i]];
            if (entity is T)
                RemoveNativeObject(entity);
        }
    }

    public void RemoveNativeObject(NativeEntity obj)
    {
        Array.Copy(activeEntityList, obj.objectId + 1, activeEntityList, obj.objectId, NATIVEENTITY_COUNT - (obj.objectId + 2));

        --nativeEntityCount;
        for (int i = obj.slotId; nativeEntityBank[i] != null; ++i)
            nativeEntityBank[i].objectId--;
    }
}
