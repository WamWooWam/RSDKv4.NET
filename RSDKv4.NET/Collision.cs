﻿using RSDKv4.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSDKv4;
internal class Collision
{
    public static CollisionSensor[] sensors = new CollisionSensor[7];

    public static int collisionLeft = 0;
    public static int collisionTop = 0;
    public static int collisionRight = 0;
    public static int collisionBottom = 0;
    public static int collisionTolerance = 0;

    static Collision()
    {
        Helpers.Memset(sensors, () => new CollisionSensor());
    }

    public static Hitbox GetHitbox(Entity entity)
    {
        AnimationFile thisAnim = Script.objectScriptList[entity.type].animFile;
        return Animation.hitboxList[thisAnim.hitboxListOffset
                           + Animation.animFrames[Animation.animationList[thisAnim.animListOffset + entity.animation].frameListOffset + entity.frame].hitboxId];
    }

    public static void FindFloorPosition(Entity player, CollisionSensor sensor, int startY)
    {
        int c = 0;
        int angle = sensor.angle;
        int tsm1 = (Scene.TILE_SIZE - 1);
        for (int i = 0; i < Scene.TILE_SIZE * 3; i += Scene.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = sensor.xpos >> 16;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = (sensor.ypos >> 16) - Scene.TILE_SIZE + i;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile += tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.LRB
                        && Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= 0x40)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF);
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= 0x40)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = (XPos & 15) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].roofMasks[c] <= -0x40)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].roofMasks[c] <= -0x40)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if ((Math.Abs(sensor.angle - angle) > 0x20) && (Math.Abs(sensor.angle - 0x100 - angle) > 0x20)
                            && (Math.Abs(sensor.angle + 0x100 - angle) > 0x20))
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                            sensor.angle = angle;
                            i = Scene.TILE_SIZE * 3;
                        }
                        else if (sensor.ypos - startY > collisionTolerance || sensor.ypos - startY < -collisionTolerance)
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }
    public static void FindLWallPosition(Entity player, CollisionSensor sensor, int startX)
    {
        int c = 0;
        int angle = sensor.angle;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = (sensor.xpos >> 16) - Drawing.TILE_SIZE + i;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = sensor.ypos >> 16;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile = tile + tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].lWallMasks[c] >= 0x40)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = ((int)((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF00) >> 8));
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].rWallMasks[c] <= -0x40)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF0000) >> 16));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].lWallMasks[c] >= 0x40)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF00) >> 8));
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].rWallMasks[c] <= -0x40)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF0000) >> 16));
                                    break;
                                }
                        }
                    }
                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if (Math.Abs(angle - sensor.angle) > 0x20)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                            sensor.angle = angle;
                            i = Drawing.TILE_SIZE * 3;
                        }
                        else if (sensor.xpos - startX > collisionTolerance || sensor.xpos - startX < -collisionTolerance)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }
    public static void FindRoofPosition(Entity player, CollisionSensor sensor, int startY)
    {
        int c = 0;
        int angle = sensor.angle;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = sensor.xpos >> 16;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = (sensor.ypos >> 16) + Drawing.TILE_SIZE - i;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile = tile + tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].roofMasks[c] <= -0x40)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24);
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].roofMasks[c] <= -0x40)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= 0x40)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (byte)(0x180 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= 0x40)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if (Math.Abs(sensor.angle - angle) <= 0x20)
                        {
                            if (sensor.ypos - startY > collisionTolerance || sensor.ypos - startY < -collisionTolerance)
                            {
                                sensor.ypos = startY << 16;
                                sensor.collided = false;
                            }
                        }
                        else
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                            sensor.angle = angle;
                            i = Drawing.TILE_SIZE * 3;
                        }
                    }
                }
            }
        }
    }
    public static void FindRWallPosition(Entity player, CollisionSensor sensor, int startX)
    {
        int c;
        int angle = sensor.angle;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = (sensor.xpos >> 16) + Drawing.TILE_SIZE - i;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = sensor.ypos >> 16;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile = tile + tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].rWallMasks[c] <= -0x40)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = (byte)((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF0000) >> 16);
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].lWallMasks[c] >= 0x40)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF00) >> 8));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].rWallMasks[c] <= -0x40)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF0000) >> 16));
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if (Scene.collisionMasks[player.collisionPlane].lWallMasks[c] >= 0x40)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF00) >> 8));
                                    break;
                                }
                        }
                    }
                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if (Math.Abs(sensor.angle - angle) > 0x20)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                            sensor.angle = angle;
                            i = Drawing.TILE_SIZE * 3;
                        }
                        else if (sensor.xpos - startX > collisionTolerance || sensor.xpos - startX < -collisionTolerance)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }

    public static void FloorCollision(Entity player, CollisionSensor sensor)
    {
        int c;
        int startY = sensor.ypos >> 16;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = sensor.xpos >> 16;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = (sensor.ypos >> 16) - Drawing.TILE_SIZE + i;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile += tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.LRB
                        && Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) <= Scene.collisionMasks[player.collisionPlane].floorMasks[c] - Drawing.TILE_SIZE + i
                                        || Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= tsm1)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF);
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) <= Scene.collisionMasks[player.collisionPlane].floorMasks[c] - Drawing.TILE_SIZE + i
                                        || Scene.collisionMasks[player.collisionPlane].floorMasks[c] >= tsm1)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) <= tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    byte cAngle = (byte)((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24);
                                    sensor.angle = (byte)(0x180 - cAngle);
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) <= tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if (sensor.ypos - startY > (Drawing.TILE_SIZE - 2))
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                        }
                        else if (sensor.ypos - startY < -(Drawing.TILE_SIZE + 1))
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }
    public static void LWallCollision(Entity player, CollisionSensor sensor)
    {
        int c;
        int startX = sensor.xpos >> 16;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = (sensor.xpos >> 16) - Drawing.TILE_SIZE + i;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = sensor.ypos >> 16;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile += tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.TOP
                        && Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) <= Scene.collisionMasks[player.collisionPlane].lWallMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) <= tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) <= Scene.collisionMasks[player.collisionPlane].lWallMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) <= tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] - Drawing.TILE_SIZE + i)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.xpos - startX > tsm1)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                        else if (sensor.xpos - startX < -tsm1)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }
    public static void RoofCollision(Entity player, CollisionSensor sensor)
    {
        int c;
        int startY = sensor.ypos >> 16;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = sensor.xpos >> 16;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = (sensor.ypos >> 16) + Drawing.TILE_SIZE - i;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile += tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.TOP
                        && Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) >= Scene.collisionMasks[player.collisionPlane].roofMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = ((int)((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) >= Scene.collisionMasks[player.collisionPlane].roofMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.ypos = Scene.collisionMasks[player.collisionPlane].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x100 - ((Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF000000) >> 24));
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) >= tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = (int)(0x180 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (XPos & tsm1) + (tileIndex << 4);
                                    if ((YPos & tsm1) >= tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.ypos = tsm1 - Scene.collisionMasks[player.collisionPlane].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                    sensor.collided = true;
                                    sensor.angle = 0x100 - (byte)(0x180 - (Scene.collisionMasks[player.collisionPlane].angles[tileIndex] & 0xFF));
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.angle < 0)
                            sensor.angle += 0x100;

                        if (sensor.angle >= 0x100)
                            sensor.angle -= 0x100;

                        if (sensor.ypos - startY > (tsm1 - 1))
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                        }
                        else if (sensor.ypos - startY < -(tsm1 - 1))
                        {
                            sensor.ypos = startY << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }
    public static void RWallCollision(Entity player, CollisionSensor sensor)
    {
        int c;
        int startX = sensor.xpos >> 16;
        int tsm1 = (Drawing.TILE_SIZE - 1);
        for (int i = 0; i < Drawing.TILE_SIZE * 3; i += Drawing.TILE_SIZE)
        {
            if (!sensor.collided)
            {
                int XPos = (sensor.xpos >> 16) + Drawing.TILE_SIZE - i;
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int YPos = sensor.ypos >> 16;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                if (XPos > -1 && YPos > -1)
                {
                    int tile = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
                    tile += tileX + (tileY << 3);
                    int tileIndex = Scene.tiles128x128.tileIndex[tile];
                    if (Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] != SOLID.TOP
                        && Scene.tiles128x128.collisionFlags[player.collisionPlane][tile] < SOLID.NONE)
                    {
                        switch (Scene.tiles128x128.direction[tile])
                        {
                            case FLIP.NONE:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) >= Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.X:
                                {
                                    c = (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) >= tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.Y:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) >= Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.xpos = Scene.collisionMasks[player.collisionPlane].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                            case FLIP.XY:
                                {
                                    c = tsm1 - (YPos & tsm1) + (tileIndex << 4);
                                    if ((XPos & tsm1) >= tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + Drawing.TILE_SIZE - i)
                                        break;

                                    sensor.xpos = tsm1 - Scene.collisionMasks[player.collisionPlane].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                    sensor.collided = true;
                                    break;
                                }
                        }
                    }

                    if (sensor.collided)
                    {
                        if (sensor.xpos - startX > tsm1)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                        else if (sensor.xpos - startX < -tsm1)
                        {
                            sensor.xpos = startX << 16;
                            sensor.collided = false;
                        }
                    }
                }
            }
        }
    }

    public static void ProcessAirCollision(Entity entity)
    {
        Hitbox playerHitbox = GetHitbox(entity);
        collisionLeft = playerHitbox.left[0];
        collisionTop = playerHitbox.top[0];
        collisionRight = playerHitbox.right[0];
        collisionBottom = playerHitbox.bottom[0];

        byte movingDown = 0;
        byte movingUp = 0;
        byte movingLeft = 0;
        byte movingRight = 0;

        if (entity.xvel < 0)
        {
            movingRight = 0;
        }
        else
        {
            movingRight = 1;
            sensors[0].ypos = entity.ypos + 0x40000;
            sensors[0].collided = false;
            sensors[0].xpos = entity.xpos + (collisionRight << 16);
        }
        if (entity.xvel > 0)
        {
            movingLeft = 0;
        }
        else
        {
            movingLeft = 1;
            sensors[1].ypos = entity.ypos + 0x40000;
            sensors[1].collided = false;
            sensors[1].xpos = entity.xpos + ((collisionLeft - 1) << 16);
        }
        sensors[2].xpos = entity.xpos + (playerHitbox.left[1] << 16);
        sensors[3].xpos = entity.xpos + (playerHitbox.right[1] << 16);
        sensors[2].collided = false;
        sensors[3].collided = false;
        sensors[4].xpos = sensors[2].xpos;
        sensors[5].xpos = sensors[3].xpos;
        sensors[4].collided = false;
        sensors[5].collided = false;
        if (entity.yvel < 0)
        {
            movingDown = 0;
        }
        else
        {
            movingDown = 1;
            sensors[2].ypos = entity.ypos + (collisionBottom << 16);
            sensors[3].ypos = entity.ypos + (collisionBottom << 16);
        }

        if (Math.Abs(entity.xvel) > 0x10000 || entity.yvel < 0)
        {
            movingUp = 1;
            sensors[4].ypos = entity.ypos + ((collisionTop - 1) << 16);
            sensors[5].ypos = entity.ypos + ((collisionTop - 1) << 16);
        }

        int cnt = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel) ? (Math.Abs(entity.yvel) >> 19) + 1 : (Math.Abs(entity.xvel) >> 19) + 1);
        int XVel = entity.xvel / cnt;
        int YVel = entity.yvel / cnt;
        int XVel2 = entity.xvel - XVel * (cnt - 1);
        int YVel2 = entity.yvel - YVel * (cnt - 1);
        while (cnt > 0)
        {
            if (cnt < 2)
            {
                XVel = XVel2;
                YVel = YVel2;
            }
            cnt--;

            if (movingRight == 1)
            {
                sensors[0].xpos += XVel;
                sensors[0].ypos += YVel;
                LWallCollision(entity, sensors[0]);
                if (sensors[0].collided)
                {
                    movingRight = 2;
                }
                else if (entity.xvel < 0x20000)
                {
                    sensors[0].ypos -= 0x80000;
                    LWallCollision(entity, sensors[0]);
                    if (sensors[0].collided)
                        movingRight = 2;
                    sensors[0].ypos += 0x80000;
                }
            }

            if (movingLeft == 1)
            {
                sensors[1].xpos += XVel;
                sensors[1].ypos += YVel;
                RWallCollision(entity, sensors[1]);
                if (sensors[1].collided)
                {
                    movingLeft = 2;
                }
                else if (entity.xvel > -0x20000)
                {
                    sensors[1].ypos -= 0x80000;
                    RWallCollision(entity, sensors[1]);
                    if (sensors[1].collided)
                        movingLeft = 2;
                    sensors[1].ypos += 0x80000;
                }
            }

            if (movingRight == 2)
            {
                entity.xvel = 0;
                entity.speed = 0;
                entity.xpos = (sensors[0].xpos - collisionRight) << 16;
                sensors[2].xpos = entity.xpos + ((collisionLeft + 1) << 16);
                sensors[3].xpos = entity.xpos + ((collisionRight - 2) << 16);
                sensors[4].xpos = sensors[2].xpos;
                sensors[5].xpos = sensors[3].xpos;
                XVel = 0;
                XVel2 = 0;
                movingRight = 3;
            }

            if (movingLeft == 2)
            {
                entity.xvel = 0;
                entity.speed = 0;
                entity.xpos = (sensors[1].xpos - collisionLeft + 1) << 16;
                sensors[2].xpos = entity.xpos + ((collisionLeft + 1) << 16);
                sensors[3].xpos = entity.xpos + ((collisionRight - 2) << 16);
                sensors[4].xpos = sensors[2].xpos;
                sensors[5].xpos = sensors[3].xpos;
                XVel = 0;
                XVel2 = 0;
                movingLeft = 3;
            }

            if (movingDown == 1)
            {
                for (int i = 2; i < 4; i++)
                {
                    if (!sensors[i].collided)
                    {
                        sensors[i].xpos += XVel;
                        sensors[i].ypos += YVel;
                        FloorCollision(entity, sensors[i]);
                    }
                }
                if (sensors[2].collided || sensors[3].collided)
                {
                    movingDown = 2;
                    cnt = 0;
                }
            }

            if (movingUp == 1)
            {
                for (int i = 4; i < 6; i++)
                {
                    if (!sensors[i].collided)
                    {
                        sensors[i].xpos += XVel;
                        sensors[i].ypos += YVel;
                        RoofCollision(entity, sensors[i]);
                    }
                }
                if (sensors[4].collided || sensors[5].collided)
                {
                    movingUp = 2;
                    cnt = 0;
                }
            }
        }

        if (movingRight < 2 && movingLeft < 2)
            entity.xpos = entity.xpos + entity.xvel;

        if (movingUp < 2 && movingDown < 2)
        {
            entity.ypos = entity.ypos + entity.yvel;
            return;
        }

        if (movingDown == 2)
        {
            entity.gravity = 0;
            if (sensors[2].collided && sensors[3].collided)
            {
                if (sensors[2].ypos >= sensors[3].ypos)
                {
                    entity.ypos = (sensors[3].ypos - collisionBottom) << 16;
                    entity.angle = sensors[3].angle;
                }
                else
                {
                    entity.ypos = (sensors[2].ypos - collisionBottom) << 16;
                    entity.angle = sensors[2].angle;
                }
            }
            else if (sensors[2].collided)
            {
                entity.ypos = (sensors[2].ypos - collisionBottom) << 16;
                entity.angle = sensors[2].angle;
            }
            else if (sensors[3].collided)
            {
                entity.ypos = (sensors[3].ypos - collisionBottom) << 16;
                entity.angle = sensors[3].angle;
            }
            if (entity.angle > 0xA0 && entity.angle < 0xE0 && entity.collisionMode != CMODE.LWALL)
            {
                entity.collisionMode = CMODE.LWALL;
                entity.xpos -= 0x40000;
            }
            if (entity.angle > 0x20 && entity.angle < 0x60 && entity.collisionMode != CMODE.RWALL)
            {
                entity.collisionMode = CMODE.RWALL;
                entity.xpos += 0x40000;
            }
            if (entity.angle < 0x20 || entity.angle > 0xE0)
            {
                entity.controlLock = 0;
            }
            entity.rotation = entity.angle << 1;

            int speed = 0;
            if (entity.down != 0)
            {
                if (entity.angle < 128)
                {
                    if (entity.angle < 16)
                    {
                        speed = entity.xvel;
                    }
                    else if (entity.angle >= 32)
                    {
                        speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel) ? entity.yvel + entity.yvel / 12 : entity.xvel);
                    }
                    else
                    {
                        speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel >> 1) ? (entity.yvel + entity.yvel / 12) >> 1 : entity.xvel);
                    }
                }
                else if (entity.angle > 240)
                {
                    speed = entity.xvel;
                }
                else if (entity.angle <= 224)
                {
                    speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel) ? -(entity.yvel + entity.yvel / 12) : entity.xvel);
                }
                else
                {
                    speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel >> 1) ? -((entity.yvel + entity.yvel / 12) >> 1) : entity.xvel);
                }
            }
            else if (entity.angle < 0x80)
            {
                if (entity.angle < 0x10)
                {
                    speed = entity.xvel;
                }
                else if (entity.angle >= 0x20)
                {
                    speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel) ? entity.yvel : entity.xvel);
                }
                else
                {
                    speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel >> 1) ? entity.yvel >> 1 : entity.xvel);
                }
            }
            else if (entity.angle > 0xF0)
            {
                speed = entity.xvel;
            }
            else if (entity.angle <= 0xE0)
            {
                speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel) ? -entity.yvel : entity.xvel);
            }
            else
            {
                speed = (Math.Abs(entity.xvel) <= Math.Abs(entity.yvel >> 1) ? -(entity.yvel >> 1) : entity.xvel);
            }

            if (speed < -0x180000)
                speed = -0x180000;
            if (speed > 0x180000)
                speed = 0x180000;
            entity.speed = speed;
            entity.yvel = 0;
            Script.scriptEng.checkResult = 1;
        }

        if (movingUp == 2)
        {
            int sensorAngle = 0;
            if (sensors[4].collided && sensors[5].collided)
            {
                if (sensors[4].ypos <= sensors[5].ypos)
                {
                    entity.ypos = (sensors[5].ypos - collisionTop + 1) << 16;
                    sensorAngle = sensors[5].angle;
                }
                else
                {
                    entity.ypos = (sensors[4].ypos - collisionTop + 1) << 16;
                    sensorAngle = sensors[4].angle;
                }
            }
            else if (sensors[4].collided)
            {
                entity.ypos = (sensors[4].ypos - collisionTop + 1) << 16;
                sensorAngle = sensors[4].angle;
            }
            else if (sensors[5].collided)
            {
                entity.ypos = (sensors[5].ypos - collisionTop + 1) << 16;
                sensorAngle = sensors[5].angle;
            }
            sensorAngle &= 0xFF;

            int angle = FastMath.ArcTanLookup(entity.xvel, entity.yvel);
            if (sensorAngle > 0x40 && sensorAngle < 0x62 && angle > 0xA0 && angle < 0xC2)
            {
                entity.gravity = 0;
                entity.angle = sensorAngle;
                entity.rotation = entity.angle << 1;
                entity.collisionMode = CMODE.RWALL;
                entity.xpos += 0x40000;
                entity.ypos -= 0x20000;
                if (entity.angle <= 0x60)
                    entity.speed = entity.yvel;
                else
                    entity.speed = entity.yvel >> 1;
            }
            if (sensorAngle > 0x9E && sensorAngle < 0xC0 && angle > 0xBE && angle < 0xE0)
            {
                entity.gravity = 0;
                entity.angle = sensorAngle;
                entity.rotation = entity.angle << 1;
                entity.collisionMode = CMODE.LWALL;
                entity.xpos -= 0x40000;
                entity.ypos -= 0x20000;
                if (entity.angle >= 0xA0)
                    entity.speed = -entity.yvel;
                else
                    entity.speed = -entity.yvel >> 1;
            }
            if (entity.yvel < 0)
                entity.yvel = 0;
            Script.scriptEng.checkResult = 2;
        }
    }
    public static void ProcessPathGrip(Entity entity)
    {
        int cosValue256;
        int sinValue256;
        sensors[4].xpos = entity.xpos;
        sensors[4].ypos = entity.ypos;
        for (int i = 0; i < 7; ++i)
        {
            sensors[i].angle = entity.angle;
            sensors[i].collided = false;
        }
        SetPathGripSensors(entity);
        int absSpeed = Math.Abs(entity.speed);
        int checkDist = absSpeed >> 18;
        absSpeed &= 0x3FFFF;
        byte cMode = entity.collisionMode;

        while (checkDist > -1)
        {
            if (checkDist >= 1)
            {
                cosValue256 = FastMath.cosVal256[entity.angle] << 10;
                sinValue256 = FastMath.sinVal256[entity.angle] << 10;
                checkDist--;
            }
            else
            {
                cosValue256 = absSpeed * FastMath.cosVal256[entity.angle] >> 8;
                sinValue256 = absSpeed * FastMath.sinVal256[entity.angle] >> 8;
                checkDist = -1;
            }

            if (entity.speed < 0)
            {
                cosValue256 = -cosValue256;
                sinValue256 = -sinValue256;
            }

            sensors[0].collided = false;
            sensors[1].collided = false;
            sensors[2].collided = false;
#if !RETRO_REV00
            sensors[5].collided = false;
            sensors[6].collided = false;
#endif
            sensors[4].xpos += cosValue256;
            sensors[4].ypos += sinValue256;
            int tileDistance = -1;

            switch (entity.collisionMode)
            {
                case CMODE.FLOOR:
                    {
                        sensors[3].xpos += cosValue256;
                        sensors[3].ypos += sinValue256;

                        if (entity.speed > 0)
                        {
                            LWallCollision(entity, sensors[3]);
                            if (sensors[3].collided)
                            {
                                sensors[2].xpos = (sensors[3].xpos - 2) << 16;
                            }
                        }

                        if (entity.speed < 0)
                        {
                            RWallCollision(entity, sensors[3]);
                            if (sensors[3].collided)
                            {
                                sensors[0].xpos = (sensors[3].xpos + 2) << 16;
                            }
                        }

                        if (sensors[3].collided)
                        {
                            cosValue256 = 0;
                            checkDist = -1;
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            sensors[i].xpos += cosValue256;
                            sensors[i].ypos += sinValue256;
                            FindFloorPosition(entity, sensors[i], sensors[i].ypos >> 16);
                        }

#if !RETRO_REV00
                        for (int i = 5; i < 7; i++)
                        {
                            sensors[i].xpos += cosValue256;
                            sensors[i].ypos += sinValue256;
                            FindFloorPosition(entity, sensors[i], sensors[i].ypos >> 16);
                        }
#endif

                        tileDistance = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (tileDistance > -1)
                            {
                                if (sensors[i].collided)
                                {
                                    if (sensors[i].ypos < sensors[tileDistance].ypos)
                                        tileDistance = i;

                                    if (sensors[i].ypos == sensors[tileDistance].ypos && (sensors[i].angle < 0x08 || sensors[i].angle > 0xF8))
                                        tileDistance = i;
                                }
                            }
                            else if (sensors[i].collided)
                                tileDistance = i;
                        }

                        if (tileDistance <= -1)
                        {
                            checkDist = -1;
                        }
                        else
                        {
                            sensors[0].ypos = sensors[tileDistance].ypos << 16;
                            sensors[0].angle = sensors[tileDistance].angle;
                            sensors[1].ypos = sensors[0].ypos;
                            sensors[1].angle = sensors[0].angle;
                            sensors[2].ypos = sensors[0].ypos;
                            sensors[2].angle = sensors[0].angle;
                            sensors[3].ypos = sensors[0].ypos - 0x40000;
                            sensors[3].angle = sensors[0].angle;
                            sensors[4].xpos = sensors[1].xpos;
                            sensors[4].ypos = sensors[0].ypos - (collisionBottom << 16);
                        }

                        if (sensors[0].angle < 0xDE && sensors[0].angle > 0x80)
                            entity.collisionMode = CMODE.LWALL;
                        if (sensors[0].angle > 0x22 && sensors[0].angle < 0x80)
                            entity.collisionMode = CMODE.RWALL;
                        break;
                    }
                case CMODE.LWALL:
                    {
                        sensors[3].xpos += cosValue256;
                        sensors[3].ypos += sinValue256;

                        if (entity.speed > 0)
                            RoofCollision(entity, sensors[3]);

                        if (entity.speed < 0)
                            FloorCollision(entity, sensors[3]);

                        if (sensors[3].collided)
                        {
                            sinValue256 = 0;
                            checkDist = -1;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            sensors[i].xpos += cosValue256;
                            sensors[i].ypos += sinValue256;
                            FindLWallPosition(entity, sensors[i], sensors[i].xpos >> 16);
                        }

                        tileDistance = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (tileDistance > -1)
                            {
                                if (sensors[i].xpos < sensors[tileDistance].xpos && sensors[i].collided)
                                {
                                    tileDistance = i;
                                }
                            }
                            else if (sensors[i].collided)
                            {
                                tileDistance = i;
                            }
                        }

                        if (tileDistance <= -1)
                        {
                            checkDist = -1;
                        }
                        else
                        {
                            sensors[0].xpos = sensors[tileDistance].xpos << 16;
                            sensors[0].angle = sensors[tileDistance].angle;
                            sensors[1].xpos = sensors[0].xpos;
                            sensors[1].angle = sensors[0].angle;
                            sensors[2].xpos = sensors[0].xpos;
                            sensors[2].angle = sensors[0].angle;
                            sensors[4].ypos = sensors[1].ypos;
                            sensors[4].xpos = sensors[1].xpos - (collisionRight << 16);
                        }

                        if (sensors[0].angle > 0xE2)
                            entity.collisionMode = CMODE.FLOOR;
                        if (sensors[0].angle < 0x9E)
                            entity.collisionMode = CMODE.ROOF;
                        break;
                    }
                case CMODE.ROOF:
                    {
                        sensors[3].xpos += cosValue256;
                        sensors[3].ypos += sinValue256;

                        if (entity.speed > 0)
                            RWallCollision(entity, sensors[3]);

                        if (entity.speed < 0)
                            LWallCollision(entity, sensors[3]);

                        if (sensors[3].collided)
                        {
                            cosValue256 = 0;
                            checkDist = -1;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            sensors[i].xpos += cosValue256;
                            sensors[i].ypos += sinValue256;
                            FindRoofPosition(entity, sensors[i], sensors[i].ypos >> 16);
                        }

                        tileDistance = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (tileDistance > -1)
                            {
                                if (sensors[i].ypos > sensors[tileDistance].ypos && sensors[i].collided)
                                {
                                    tileDistance = i;
                                }
                            }
                            else if (sensors[i].collided)
                            {
                                tileDistance = i;
                            }
                        }

                        if (tileDistance <= -1)
                        {
                            checkDist = -1;
                        }
                        else
                        {
                            sensors[0].ypos = sensors[tileDistance].ypos << 16;
                            sensors[0].angle = sensors[tileDistance].angle;
                            sensors[1].ypos = sensors[0].ypos;
                            sensors[1].angle = sensors[0].angle;
                            sensors[2].ypos = sensors[0].ypos;
                            sensors[2].angle = sensors[0].angle;
                            sensors[3].ypos = sensors[0].ypos + 0x40000;
                            sensors[3].angle = sensors[0].angle;
                            sensors[4].xpos = sensors[1].xpos;
                            sensors[4].ypos = sensors[0].ypos - ((collisionTop - 1) << 16);
                        }

                        if (sensors[0].angle > 0xA2)
                            entity.collisionMode = CMODE.LWALL;
                        if (sensors[0].angle < 0x5E)
                            entity.collisionMode = CMODE.RWALL;
                        break;
                    }
                case CMODE.RWALL:
                    {
                        sensors[3].xpos += cosValue256;
                        sensors[3].ypos += sinValue256;

                        if (entity.speed > 0)
                            FloorCollision(entity, sensors[3]);

                        if (entity.speed < 0)
                            RoofCollision(entity, sensors[3]);

                        if (sensors[3].collided)
                        {
                            sinValue256 = 0;
                            checkDist = -1;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            sensors[i].xpos += cosValue256;
                            sensors[i].ypos += sinValue256;
                            FindRWallPosition(entity, sensors[i], sensors[i].xpos >> 16);
                        }

                        tileDistance = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (tileDistance > -1)
                            {
                                if (sensors[i].xpos > sensors[tileDistance].xpos && sensors[i].collided)
                                {
                                    tileDistance = i;
                                }
                            }
                            else if (sensors[i].collided)
                            {
                                tileDistance = i;
                            }
                        }

                        if (tileDistance <= -1)
                        {
                            checkDist = -1;
                        }
                        else
                        {
                            sensors[0].xpos = sensors[tileDistance].xpos << 16;
                            sensors[0].angle = sensors[tileDistance].angle;
                            sensors[1].xpos = sensors[0].xpos;
                            sensors[1].angle = sensors[0].angle;
                            sensors[2].xpos = sensors[0].xpos;
                            sensors[2].angle = sensors[0].angle;
                            sensors[4].ypos = sensors[1].ypos;
                            sensors[4].xpos = sensors[1].xpos - ((collisionLeft - 1) << 16);
                        }

                        if (sensors[0].angle < 0x1E)
                            entity.collisionMode = CMODE.FLOOR;
                        if (sensors[0].angle > 0x62)
                            entity.collisionMode = CMODE.ROOF;
                        break;
                    }
            }
            if (tileDistance != -1)
                entity.angle = sensors[0].angle;

            if (!sensors[3].collided)
                SetPathGripSensors(entity);
            else
                checkDist = -2;
        }

        switch (cMode)
        {
            case CMODE.FLOOR:
                {
                    if (sensors[0].collided || sensors[1].collided || sensors[2].collided)
                    {
                        entity.angle = sensors[0].angle;
                        entity.rotation = entity.angle << 1;
                        entity.floorSensors[0] = (byte)(sensors[0].collided ? 1 : 0);
                        entity.floorSensors[1] = (byte)(sensors[1].collided ? 1 : 0);
                        entity.floorSensors[2] = (byte)(sensors[2].collided ? 1 : 0);
#if RETRO_REV00
                entity.floorSensors[3] = sensors[5].collided;
                entity.floorSensors[4] = sensors[6].collided;
#endif
                        if (!sensors[3].collided)
                        {
                            entity.pushing = 0;
                            entity.xpos = sensors[4].xpos;
                        }
                        else
                        {
                            if (entity.speed > 0)
                                entity.xpos = (sensors[3].xpos - collisionRight) << 16;

                            if (entity.speed < 0)
                                entity.xpos = (sensors[3].xpos - collisionLeft + 1) << 16;

                            entity.speed = 0;
                            if ((entity.left != 0 || entity.right != 0) && entity.pushing < 2)
                                entity.pushing++;
                        }
                        entity.ypos = sensors[4].ypos;
                    }
                    else
                    {
                        entity.gravity = 1;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.xvel = FastMath.cosVal256[entity.angle] * entity.speed >> 8;
                        entity.yvel = FastMath.sinVal256[entity.angle] * entity.speed >> 8;
                        if (entity.yvel < -0x100000)
                            entity.yvel = -0x100000;

                        if (entity.yvel > 0x100000)
                            entity.yvel = 0x100000;

                        entity.speed = entity.xvel;
                        entity.angle = 0;
                        if (!sensors[3].collided)
                        {
                            entity.pushing = 0;
                            entity.xpos += entity.xvel;
                        }
                        else
                        {
                            if (entity.speed > 0)
                                entity.xpos = (sensors[3].xpos - collisionRight) << 16;
                            if (entity.speed < 0)
                                entity.xpos = (sensors[3].xpos - collisionLeft + 1) << 16;

                            entity.speed = 0;
                            if ((entity.left != 0 || entity.right != 0) && entity.pushing < 2)
                                entity.pushing++;
                        }
                        entity.ypos += entity.yvel;
                    }
                    break;
                }
            case CMODE.LWALL:
                {
                    if (!sensors[0].collided && !sensors[1].collided && !sensors[2].collided)
                    {
                        entity.gravity = 1;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.xvel = FastMath.cosVal256[entity.angle] * entity.speed >> 8;
                        entity.yvel = FastMath.sinVal256[entity.angle] * entity.speed >> 8;
                        if (entity.yvel < -0x100000)
                        {
                            entity.yvel = -0x100000;
                        }
                        if (entity.yvel > 0x100000)
                        {
                            entity.yvel = 0x100000;
                        }
                        entity.speed = entity.xvel;
                        entity.angle = 0;
                    }
                    else if (entity.speed >= 0x28000 || entity.speed <= -0x28000 || entity.controlLock != 0)
                    {
                        entity.angle = sensors[0].angle;
                        entity.rotation = entity.angle << 1;
                    }
                    else
                    {
                        entity.gravity = 1;
                        entity.angle = 0;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.speed = entity.xvel;
                        entity.controlLock = 30;
                    }
                    if (!sensors[3].collided)
                    {
                        entity.ypos = sensors[4].ypos;
                    }
                    else
                    {
                        if (entity.speed > 0)
                            entity.ypos = (sensors[3].ypos - collisionTop) << 16;

                        if (entity.speed < 0)
                            entity.ypos = (sensors[3].ypos - collisionBottom) << 16;

                        entity.speed = 0;
                    }
                    entity.xpos = sensors[4].xpos;
                    break;
                }
            case CMODE.ROOF:
                {
                    if (!sensors[0].collided && !sensors[1].collided && !sensors[2].collided)
                    {
                        entity.gravity = 1;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.xvel = FastMath.cosVal256[entity.angle] * entity.speed >> 8;
                        entity.yvel = FastMath.sinVal256[entity.angle] * entity.speed >> 8;
                        entity.floorSensors[0] = 0;
                        entity.floorSensors[1] = 0;
                        entity.floorSensors[2] = 0;
                        if (entity.yvel < -0x100000)
                            entity.yvel = -0x100000;

                        if (entity.yvel > 0x100000)
                            entity.yvel = 0x100000;

                        entity.angle = 0;
                        entity.speed = entity.xvel;
                        if (!sensors[3].collided)
                        {
                            entity.xpos = entity.xpos + entity.xvel;
                        }
                        else
                        {
                            if (entity.speed > 0)
                                entity.xpos = (sensors[3].xpos - collisionRight) << 16;

                            if (entity.speed < 0)
                                entity.xpos = (sensors[3].xpos - collisionLeft + 1) << 16;

                            entity.speed = 0;
                        }
                    }
                    else if (entity.speed <= -0x28000 || entity.speed >= 0x28000)
                    {
                        entity.angle = sensors[0].angle;
                        entity.rotation = entity.angle << 1;
                        if (!sensors[3].collided)
                        {
                            entity.xpos = sensors[4].xpos;
                        }
                        else
                        {
                            if (entity.speed < 0)
                                entity.xpos = (sensors[3].xpos - collisionRight) << 16;

                            if (entity.speed > 0)
                                entity.xpos = (sensors[3].xpos - collisionLeft + 1) << 16;
                            entity.speed = 0;
                        }
                    }
                    else
                    {
                        entity.gravity = 1;
                        entity.angle = 0;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.speed = entity.xvel;
                        entity.floorSensors[0] = 0;
                        entity.floorSensors[1] = 0;
                        entity.floorSensors[2] = 0;
                        if (!sensors[3].collided)
                        {
                            entity.xpos = entity.xpos + entity.xvel;
                        }
                        else
                        {
                            if (entity.speed > 0)
                                entity.xpos = (sensors[3].xpos - collisionRight) << 16;

                            if (entity.speed < 0)
                                entity.xpos = (sensors[3].xpos - collisionLeft + 1) << 16;
                            entity.speed = 0;
                        }
                    }
                    entity.ypos = sensors[4].ypos;
                    break;
                }
            case CMODE.RWALL:
                {
                    if (!sensors[0].collided && !sensors[1].collided && !sensors[2].collided)
                    {
                        entity.gravity = 1;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.xvel = FastMath.cosVal256[entity.angle] * entity.speed >> 8;
                        entity.yvel = FastMath.sinVal256[entity.angle] * entity.speed >> 8;
                        if (entity.yvel < -0x100000)
                            entity.yvel = -0x100000;

                        if (entity.yvel > 0x100000)
                            entity.yvel = 0x100000;

                        entity.speed = entity.xvel;
                        entity.angle = 0;
                    }
                    else if (entity.speed <= -0x28000 || entity.speed >= 0x28000 || entity.controlLock != 0)
                    {
                        entity.angle = sensors[0].angle;
                        entity.rotation = entity.angle << 1;
                    }
                    else
                    {
                        entity.gravity = 1;
                        entity.angle = 0;
                        entity.collisionMode = CMODE.FLOOR;
                        entity.speed = entity.xvel;
                        entity.controlLock = 30;
                    }
                    if (!sensors[3].collided)
                    {
                        entity.ypos = sensors[4].ypos;
                    }
                    else
                    {
                        if (entity.speed > 0)
                            entity.ypos = (sensors[3].ypos - collisionBottom) << 16;

                        if (entity.speed < 0)
                            entity.ypos = (sensors[3].ypos - collisionTop + 1) << 16;

                        entity.speed = 0;
                    }
                    entity.xpos = sensors[4].xpos;
                    break;
                }
            default: break;
        }
    }

    public static void SetPathGripSensors(Entity player)
    {
        Hitbox playerHitbox = GetHitbox(player);

        switch (player.collisionMode)
        {
            case CMODE.FLOOR:
                {
                    collisionLeft = playerHitbox.left[0];
                    collisionTop = playerHitbox.top[0];
                    collisionRight = playerHitbox.right[0];
                    collisionBottom = playerHitbox.bottom[0];
                    sensors[0].ypos = sensors[4].ypos + (collisionBottom << 16);
                    sensors[1].ypos = sensors[0].ypos;
                    sensors[2].ypos = sensors[0].ypos;
                    sensors[3].ypos = sensors[4].ypos + 0x40000;
#if !RETRO_REV00
                    sensors[5].ypos = sensors[0].ypos;
                    sensors[6].ypos = sensors[0].ypos;
#endif

                    sensors[0].xpos = sensors[4].xpos + ((playerHitbox.left[1] - 1) << 16);
                    sensors[1].xpos = sensors[4].xpos;
                    sensors[2].xpos = sensors[4].xpos + (playerHitbox.right[1] << 16);
#if !RETRO_REV00
                    sensors[5].xpos = sensors[4].xpos + (playerHitbox.left[1] << 15);
                    sensors[6].xpos = sensors[4].xpos + (playerHitbox.right[1] << 15);
#endif
                    if (player.speed > 0)
                    {
                        sensors[3].xpos = sensors[4].xpos + ((collisionRight + 1) << 16);
                    }
                    else
                    {
                        sensors[3].xpos = sensors[4].xpos + ((collisionLeft - 1) << 16);
                    }
                    return;
                }
            case CMODE.LWALL:
                {
                    collisionLeft = playerHitbox.left[2];
                    collisionTop = playerHitbox.top[2];
                    collisionRight = playerHitbox.right[2];
                    collisionBottom = playerHitbox.bottom[2];
                    sensors[0].xpos = sensors[4].xpos + (collisionRight << 16);
                    sensors[1].xpos = sensors[0].xpos;
                    sensors[2].xpos = sensors[0].xpos;
                    sensors[3].xpos = sensors[4].xpos + 0x40000;
                    sensors[0].ypos = sensors[4].ypos + ((playerHitbox.top[3] - 1) << 16);
                    sensors[1].ypos = sensors[4].ypos;
                    sensors[2].ypos = sensors[4].ypos + (playerHitbox.bottom[3] << 16);
                    if (player.speed > 0)
                    {
                        sensors[3].ypos = sensors[4].ypos + (collisionTop << 16);
                    }
                    else
                    {
                        sensors[3].ypos = sensors[4].ypos + ((collisionBottom - 1) << 16);
                    }
                    return;
                }
            case CMODE.ROOF:
                {
                    collisionLeft = playerHitbox.left[4];
                    collisionTop = playerHitbox.top[4];
                    collisionRight = playerHitbox.right[4];
                    collisionBottom = playerHitbox.bottom[4];
                    sensors[0].ypos = sensors[4].ypos + ((collisionTop - 1) << 16);
                    sensors[1].ypos = sensors[0].ypos;
                    sensors[2].ypos = sensors[0].ypos;
                    sensors[3].ypos = sensors[4].ypos - 0x40000;
                    sensors[0].xpos = sensors[4].xpos + ((playerHitbox.left[5] - 1) << 16);
                    sensors[1].xpos = sensors[4].xpos;
                    sensors[2].xpos = sensors[4].xpos + (playerHitbox.right[5] << 16);
                    if (player.speed < 0)
                    {
                        sensors[3].xpos = sensors[4].xpos + ((collisionRight + 1) << 16);
                    }
                    else
                    {
                        sensors[3].xpos = sensors[4].xpos + ((collisionLeft - 1) << 16);
                    }
                    return;
                }
            case CMODE.RWALL:
                {
                    collisionLeft = playerHitbox.left[6];
                    collisionTop = playerHitbox.top[6];
                    collisionRight = playerHitbox.right[6];
                    collisionBottom = playerHitbox.bottom[6];
                    sensors[0].xpos = sensors[4].xpos + ((collisionLeft - 1) << 16);
                    sensors[1].xpos = sensors[0].xpos;
                    sensors[2].xpos = sensors[0].xpos;
                    sensors[3].xpos = sensors[4].xpos - 0x40000;
                    sensors[0].ypos = sensors[4].ypos + ((playerHitbox.top[7] - 1) << 16);
                    sensors[1].ypos = sensors[4].ypos;
                    sensors[2].ypos = sensors[4].ypos + (playerHitbox.bottom[7] << 16);
                    if (player.speed > 0)
                    {
                        sensors[3].ypos = sensors[4].ypos + (collisionBottom << 16);
                    }
                    else
                    {
                        sensors[3].ypos = sensors[4].ypos + ((collisionTop - 1) << 16);
                    }
                    return;
                }
            default: return;
        }
    }

    public static void ProcessTileCollisions(Entity player)
    {
        player.floorSensors[0] = 0;
        player.floorSensors[1] = 0;
        player.floorSensors[2] = 0;
        player.floorSensors[3] = 0;
        player.floorSensors[4] = 0;

        Script.scriptEng.checkResult = 0;

        collisionTolerance = 15;
        if (player.speed < 0x60000)
            collisionTolerance = (sbyte)player.angle == 0 ? 8 : 15;

        if (player.gravity == 1)
            ProcessAirCollision(player);
        else
            ProcessPathGrip(player);
    }

    public static void ObjectFloorCollision(int xOffset, int yOffset, int cPath)
    {
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int c = 0;
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7)
        {
            int chunkX = XPos >> 7;
            int tileX = (XPos & 0x7F) >> 4;
            int chunkY = YPos >> 7;
            int tileY = (YPos & 0x7F) >> 4;
            int chunk = (Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6) + tileX + (tileY << 3);
            int tileIndex = Scene.tiles128x128.tileIndex[chunk];
            if (Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.LRB && Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.NONE)
            {
                switch (Scene.tiles128x128.direction[chunk])
                {
                    case 0:
                        {
                            c = (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) <= Scene.collisionMasks[cPath].floorMasks[c])
                            {
                                break;
                            }
                            YPos = Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 1:
                        {
                            c = 15 - (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) <= Scene.collisionMasks[cPath].floorMasks[c])
                            {
                                break;
                            }
                            YPos = Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 2:
                        {
                            c = (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) <= 15 - Scene.collisionMasks[cPath].roofMasks[c])
                            {
                                break;
                            }
                            YPos = 15 - Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 3:
                        {
                            c = 15 - (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) <= 15 - Scene.collisionMasks[cPath].roofMasks[c])
                            {
                                break;
                            }
                            YPos = 15 - Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                }
            }
            if (Script.scriptEng.checkResult != 0)
            {
                entity.ypos = (YPos - yOffset) << 16;
            }
        }
    }
    public static void ObjectLWallCollision(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7)
        {
            int chunkX = XPos >> 7;
            int tileX = (XPos & 0x7F) >> 4;
            int chunkY = YPos >> 7;
            int tileY = (YPos & 0x7F) >> 4;
            int chunk = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
            chunk = chunk + tileX + (tileY << 3);
            int tileIndex = Scene.tiles128x128.tileIndex[chunk];
            if (Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.TOP && Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
            {
                switch (Scene.tiles128x128.direction[chunk])
                {
                    case 0:
                        {
                            c = (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) <= Scene.collisionMasks[cPath].lWallMasks[c])
                            {
                                break;
                            }
                            XPos = Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 1:
                        {
                            c = (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) <= 15 - Scene.collisionMasks[cPath].rWallMasks[c])
                            {
                                break;
                            }
                            XPos = 15 - Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 2:
                        {
                            c = 15 - (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) <= Scene.collisionMasks[cPath].lWallMasks[c])
                            {
                                break;
                            }
                            XPos = Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 3:
                        {
                            c = 15 - (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) <= 15 - Scene.collisionMasks[cPath].rWallMasks[c])
                            {
                                break;
                            }
                            XPos = 15 - Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                }
            }
            if (Script.scriptEng.checkResult != 0)
            {
                entity.xpos = (XPos - xOffset) << 16;
            }
        }
    }
    public static void ObjectRoofCollision(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7)
        {
            int chunkX = XPos >> 7;
            int tileX = (XPos & 0x7F) >> 4;
            int chunkY = YPos >> 7;
            int tileY = (YPos & 0x7F) >> 4;
            int chunk = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
            chunk = chunk + tileX + (tileY << 3);
            int tileIndex = Scene.tiles128x128.tileIndex[chunk];
            if (Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.TOP && Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
            {
                switch (Scene.tiles128x128.direction[chunk])
                {
                    case 0:
                        {
                            c = (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) >= Scene.collisionMasks[cPath].roofMasks[c])
                            {
                                break;
                            }
                            YPos = Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 1:
                        {
                            c = 15 - (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) >= Scene.collisionMasks[cPath].roofMasks[c])
                            {
                                break;
                            }
                            YPos = Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 2:
                        {
                            c = (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) >= 15 - Scene.collisionMasks[cPath].floorMasks[c])
                            {
                                break;
                            }
                            YPos = 15 - Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 3:
                        {
                            c = 15 - (XPos & 15) + (tileIndex << 4);
                            if ((YPos & 15) >= 15 - Scene.collisionMasks[cPath].floorMasks[c])
                            {
                                break;
                            }
                            YPos = 15 - Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                }
            }
            if (Script.scriptEng.checkResult != 0)
            {
                entity.ypos = (YPos - yOffset) << 16;
            }
        }
    }
    public static void ObjectRWallCollision(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7)
        {
            int chunkX = XPos >> 7;
            int tileX = (XPos & 0x7F) >> 4;
            int chunkY = YPos >> 7;
            int tileY = (YPos & 0x7F) >> 4;
            int chunk = Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6;
            chunk = chunk + tileX + (tileY << 3);
            int tileIndex = Scene.tiles128x128.tileIndex[chunk];
            if (Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.TOP && Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
            {
                switch (Scene.tiles128x128.direction[chunk])
                {
                    case 0:
                        {
                            c = (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) >= Scene.collisionMasks[cPath].rWallMasks[c])
                            {
                                break;
                            }
                            XPos = Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 1:
                        {
                            c = (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) >= 15 - Scene.collisionMasks[cPath].lWallMasks[c])
                            {
                                break;
                            }
                            XPos = 15 - Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 2:
                        {
                            c = 15 - (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) >= Scene.collisionMasks[cPath].rWallMasks[c])
                            {
                                break;
                            }
                            XPos = Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                    case 3:
                        {
                            c = 15 - (YPos & 15) + (tileIndex << 4);
                            if ((XPos & 15) >= 15 - Scene.collisionMasks[cPath].lWallMasks[c])
                            {
                                break;
                            }
                            XPos = 15 - Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                            Script.scriptEng.checkResult = 1;
                            break;
                        }
                }
            }
            if (Script.scriptEng.checkResult != 0)
            {
                entity.xpos = (XPos - xOffset) << 16;
            }
        }
    }

    public static void ObjectFloorGrip(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        int chunkX = YPos;
        YPos = YPos - 16;
        for (int i = 3; i > 0; i--)
        {
            if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7 && Script.scriptEng.checkResult == 0)
            {
                chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                int chunk = (Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6) + tileX + (tileY << 3);
                int tileIndex = Scene.tiles128x128.tileIndex[chunk];
                if (Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.LRB && Scene.tiles128x128.collisionFlags[cPath][chunk] != SOLID.NONE)
                {
                    switch (Scene.tiles128x128.direction[chunk])
                    {
                        case 0:
                            {
                                c = (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].floorMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.ypos = Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 1:
                            {
                                c = 15 - (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].floorMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.ypos = Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 2:
                            {
                                c = (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].roofMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.ypos = 15 - Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 3:
                            {
                                c = 15 - (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].roofMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.ypos = 15 - Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                    }
                }
            }
            YPos += 16;
        }

        if (Script.scriptEng.checkResult != 0)
        {
            if (Math.Abs(entity.ypos - chunkX) < 16)
            {
                entity.ypos = (entity.ypos - yOffset) << 16;
                return;
            }
            entity.ypos = (chunkX - yOffset) << 16;
            Script.scriptEng.checkResult = 0;
        }
    }
    public static void ObjectLWallGrip(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        int startX = XPos;
        XPos = XPos - 16;
        for (int i = 3; i > 0; i--)
        {
            if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7 && Script.scriptEng.checkResult == 0)
            {
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                int chunk = (Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6) + tileX + (tileY << 3);
                int tileIndex = Scene.tiles128x128.tileIndex[chunk];
                if (Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
                {
                    switch (Scene.tiles128x128.direction[chunk])
                    {
                        case 0:
                            {
                                c = (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].lWallMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.xpos = Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 1:
                            {
                                c = (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].rWallMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.xpos = 15 - Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 2:
                            {
                                c = 15 - (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].lWallMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.xpos = Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 3:
                            {
                                c = 15 - (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].rWallMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.xpos = 15 - Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                    }
                }
            }
            XPos += 16;
        }
        if (Script.scriptEng.checkResult != 0)
        {
            if (Math.Abs(entity.xpos - startX) < 16)
            {
                entity.xpos = (entity.xpos - xOffset) << 16;
                return;
            }
            entity.xpos = (startX - xOffset) << 16;
            Script.scriptEng.checkResult = 0;
        }
    }
    public static void ObjectRoofGrip(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 1;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        int startY = YPos;
        YPos = YPos + 16;
        for (int i = 3; i > 0; i--)
        {
            if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7 && Script.scriptEng.checkResult == 0)
            {
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                int chunk = (Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6) + tileX + (tileY << 3);
                int tileIndex = Scene.tiles128x128.tileIndex[chunk];
                if (Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
                {
                    switch (Scene.tiles128x128.direction[chunk])
                    {
                        case 0:
                            {
                                c = (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].roofMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.ypos = Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 1:
                            {
                                c = 15 - (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].roofMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.ypos = Scene.collisionMasks[cPath].roofMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 2:
                            {
                                c = (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].floorMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.ypos = 15 - Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 3:
                            {
                                c = 15 - (XPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].floorMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.ypos = 15 - Scene.collisionMasks[cPath].floorMasks[c] + (chunkY << 7) + (tileY << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                    }
                }
            }
            YPos -= 16;
        }
        if (Script.scriptEng.checkResult != 0)
        {
            if (Math.Abs(entity.ypos - startY) < 16)
            {
                entity.ypos = (entity.ypos - yOffset) << 16;
                return;
            }
            entity.ypos = (startY - yOffset) << 16;
            Script.scriptEng.checkResult = 0;
        }
    }
    public static void ObjectRWallGrip(int xOffset, int yOffset, int cPath)
    {
        int c;
        Script.scriptEng.checkResult = 0;
        Entity entity = Objects.objectEntityList[Objects.objectEntityPos];
        int XPos = (entity.xpos >> 16) + xOffset;
        int YPos = (entity.ypos >> 16) + yOffset;
        int startX = XPos;
        XPos = XPos + 16;
        for (int i = 3; i > 0; i--)
        {
            if (XPos > 0 && XPos < Scene.stageLayouts[0].xsize << 7 && YPos > 0 && YPos < Scene.stageLayouts[0].ysize << 7 && Script.scriptEng.checkResult == 0)
            {
                int chunkX = XPos >> 7;
                int tileX = (XPos & 0x7F) >> 4;
                int chunkY = YPos >> 7;
                int tileY = (YPos & 0x7F) >> 4;
                int chunk = (Scene.stageLayouts[0].tiles[chunkX + (chunkY << 8)] << 6) + tileX + (tileY << 3);
                int tileIndex = Scene.tiles128x128.tileIndex[chunk];
                if (Scene.tiles128x128.collisionFlags[cPath][chunk] < SOLID.NONE)
                {
                    switch (Scene.tiles128x128.direction[chunk])
                    {
                        case 0:
                            {
                                c = (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].rWallMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.xpos = Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 1:
                            {
                                c = (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].lWallMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.xpos = 15 - Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 2:
                            {
                                c = 15 - (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].rWallMasks[c] <= -64)
                                {
                                    break;
                                }
                                entity.xpos = Scene.collisionMasks[cPath].rWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                        case 3:
                            {
                                c = 15 - (YPos & 15) + (tileIndex << 4);
                                if (Scene.collisionMasks[cPath].lWallMasks[c] >= 64)
                                {
                                    break;
                                }
                                entity.xpos = 15 - Scene.collisionMasks[cPath].lWallMasks[c] + (chunkX << 7) + (tileX << 4);
                                Script.scriptEng.checkResult = 1;
                                break;
                            }
                    }
                }
            }
            XPos -= 16;
        }
        if (Script.scriptEng.checkResult != 0)
        {
            if (Math.Abs(entity.xpos - startX) < 16)
            {
                entity.xpos = (entity.xpos - xOffset) << 16;
                return;
            }
            entity.xpos = (startX - xOffset) << 16;
            Script.scriptEng.checkResult = 0;
        }
    }


    public static void TouchCollision(Entity thisEntity, int thisLeft, int thisTop, int thisRight, int thisBottom, Entity otherEntity, int otherLeft, int otherTop,
                        int otherRight, int otherBottom)
    {
        Hitbox thisHitbox = GetHitbox(thisEntity);
        Hitbox otherHitbox = GetHitbox(otherEntity);

        if (thisLeft == 0x10000)
            thisLeft = thisHitbox.left[0];

        if (thisTop == 0x10000)
            thisTop = thisHitbox.top[0];

        if (thisRight == 0x10000)
            thisRight = thisHitbox.right[0];

        if (thisBottom == 0x10000)
            thisBottom = thisHitbox.bottom[0];

        if (otherLeft == 0x10000)
            otherLeft = otherHitbox.left[0];

        if (otherTop == 0x10000)
            otherTop = otherHitbox.top[0];

        if (otherRight == 0x10000)
            otherRight = otherHitbox.right[0];

        if (otherBottom == 0x10000)
            otherBottom = otherHitbox.bottom[0];

#if !RETRO_USE_ORIGINAL_CODE
        //int thisHitboxID = 0;
        //int otherHitboxID = 0;
        //if (showHitboxes)
        //{
        //    thisHitboxID = addDebugHitbox(H_TYPE_TOUCH, thisEntity, thisLeft, thisTop, thisRight, thisBottom);
        //    otherHitboxID = addDebugHitbox(H_TYPE_TOUCH, otherEntity, otherLeft, otherTop, otherRight, otherBottom);
        //}
#endif

        thisLeft += thisEntity.xpos >> 16;
        thisTop += thisEntity.ypos >> 16;
        thisRight += thisEntity.xpos >> 16;
        thisBottom += thisEntity.ypos >> 16;

        otherLeft += otherEntity.xpos >> 16;
        otherTop += otherEntity.ypos >> 16;
        otherRight += otherEntity.xpos >> 16;
        otherBottom += otherEntity.ypos >> 16;

        Script.scriptEng.checkResult = (otherRight > thisLeft && otherLeft < thisRight && otherBottom > thisTop && otherTop < thisBottom) ? 1 : 0;

#if !RETRO_USE_ORIGINAL_CODE
        //if (showHitboxes)
        //{
        //    if (thisHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[thisHitboxID].collision |= 1;
        //    if (otherHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[otherHitboxID].collision |= 1;
        //}
#endif
    }
    public static void BoxCollision(Entity thisEntity, int thisLeft, int thisTop, int thisRight, int thisBottom, Entity otherEntity, int otherLeft, int otherTop,
                      int otherRight, int otherBottom)
    {
        Hitbox thisHitbox = GetHitbox(thisEntity);
        Hitbox otherHitbox = GetHitbox(otherEntity);

        if (thisLeft == 0x10000)
            thisLeft = thisHitbox.left[0];

        if (thisTop == 0x10000)
            thisTop = thisHitbox.top[0];

        if (thisRight == 0x10000)
            thisRight = thisHitbox.right[0];

        if (thisBottom == 0x10000)
            thisBottom = thisHitbox.bottom[0];

        if (otherLeft == 0x10000)
            otherLeft = otherHitbox.left[0];

        if (otherTop == 0x10000)
            otherTop = otherHitbox.top[0];

        if (otherRight == 0x10000)
            otherRight = otherHitbox.right[0];

        if (otherBottom == 0x10000)
            otherBottom = otherHitbox.bottom[0];

#if !RETRO_USE_ORIGINAL_CODE
        //int thisHitboxID = 0;
        //int otherHitboxID = 0;
        //if (showHitboxes)
        //{
        //    thisHitboxID = addDebugHitbox(H_TYPE_BOX, thisEntity, thisLeft, thisTop, thisRight, thisBottom);
        //    otherHitboxID = addDebugHitbox(H_TYPE_BOX, otherEntity, otherLeft, otherTop, otherRight, otherBottom);
        //}
#endif

        thisLeft += thisEntity.xpos >> 16;
        thisTop += thisEntity.ypos >> 16;
        thisRight += thisEntity.xpos >> 16;
        thisBottom += thisEntity.ypos >> 16;

        thisLeft <<= 16;
        thisTop <<= 16;
        thisRight <<= 16;
        thisBottom <<= 16;

        otherLeft <<= 16;
        otherTop <<= 16;
        otherRight <<= 16;
        otherBottom <<= 16;

        Script.scriptEng.checkResult = 0;

        int rx = otherEntity.xpos >> 16 << 16;
        int ry = otherEntity.ypos >> 16 << 16;

        int xDif = otherEntity.xpos - thisRight;
        if (thisEntity.xpos > otherEntity.xpos)
            xDif = thisLeft - otherEntity.xpos;
        int yDif = thisTop - otherEntity.ypos;
        if (thisEntity.ypos <= otherEntity.ypos)
            yDif = otherEntity.ypos - thisBottom;

        if (xDif <= yDif && Math.Abs(otherEntity.xvel) >> 1 <= Math.Abs(otherEntity.yvel))
        {
            sensors[0].collided = false;
            sensors[1].collided = false;
            sensors[2].collided = false;
            sensors[3].collided = false;
            sensors[4].collided = false;
            sensors[0].xpos = rx + otherLeft + 0x20000;
            sensors[1].xpos = rx;
            sensors[2].xpos = rx + otherRight - 0x20000;
            sensors[3].xpos = (sensors[0].xpos + rx) >> 1;
            sensors[4].xpos = (sensors[2].xpos + rx) >> 1;

            sensors[0].ypos = ry + otherBottom;

            if (otherEntity.yvel >= 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (thisLeft < sensors[i].xpos && thisRight > sensors[i].xpos && thisTop <= sensors[0].ypos
                        && thisTop > otherEntity.ypos - otherEntity.yvel)
                    {
                        sensors[i].collided = true;
                        otherEntity.floorSensors[i] = 1;
                    }
                }
            }

            if (sensors[0].collided || sensors[1].collided || sensors[2].collided)
            {
                if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                {
                    otherEntity.xvel = 0;
                    otherEntity.speed = 0;
                }
                otherEntity.ypos = thisTop - otherBottom;
                otherEntity.gravity = 0;
                otherEntity.yvel = 0;
                otherEntity.angle = 0;
                otherEntity.rotation = 0;
                otherEntity.controlLock = 0;
                Script.scriptEng.checkResult = 1;
            }
            else
            {
                sensors[0].collided = false;
                sensors[1].collided = false;
                sensors[0].xpos = rx + otherLeft + 0x20000;
                sensors[1].xpos = rx + otherRight - 0x20000;

                sensors[0].ypos = ry + otherTop;

                for (int i = 0; i < 2; ++i)
                {
                    if (thisLeft < sensors[1].xpos && thisRight > sensors[0].xpos && thisBottom > sensors[0].ypos
                        && thisBottom < otherEntity.ypos - otherEntity.yvel)
                    {
                        sensors[i].collided = true;
                    }
                }

                if (sensors[1].collided || sensors[0].collided)
                {
                    if (otherEntity.gravity == 1)
                        otherEntity.ypos = thisBottom - otherTop;

                    if (otherEntity.yvel <= 0)
                        otherEntity.yvel = 0;
                    Script.scriptEng.checkResult = 4;
                }
                else
                {
                    sensors[0].collided = false;
                    sensors[1].collided = false;
                    sensors[0].xpos = rx + otherRight;

                    sensors[0].ypos = ry + otherTop + 0x20000;
                    sensors[1].ypos = ry + otherBottom - 0x20000;
                    for (int i = 0; i < 2; ++i)
                    {
                        if (thisLeft <= sensors[0].xpos && thisLeft > otherEntity.xpos - otherEntity.xvel && thisTop < sensors[1].ypos
                            && thisBottom > sensors[0].ypos)
                        {
                            sensors[i].collided = true;
                        }
                    }

                    if (sensors[1].collided || sensors[0].collided)
                    {
                        otherEntity.xpos = thisLeft - otherRight;
                        if (otherEntity.xvel > 0)
                        {
                            if (otherEntity.direction == 0)
                                otherEntity.pushing = 2;

                            otherEntity.xvel = 0;
                            if (otherEntity.collisionMode != 0 || otherEntity.left == 0)
                                otherEntity.speed = 0;
                            else
                                otherEntity.speed = -0x8000;
                        }
                        Script.scriptEng.checkResult = 2;
                    }
                    else
                    {
                        sensors[0].collided = false;
                        sensors[1].collided = false;
                        sensors[0].xpos = rx + otherLeft;

                        sensors[0].ypos = ry + otherTop + 0x20000;
                        sensors[1].ypos = ry + otherBottom - 0x20000;
                        for (int i = 0; i < 2; ++i)
                        {
                            if (thisRight > sensors[0].xpos && thisRight < otherEntity.xpos - otherEntity.xvel && thisTop < sensors[1].ypos
                                && thisBottom > sensors[0].ypos)
                            {
                                sensors[i].collided = true;
                            }
                        }

                        if (sensors[1].collided || sensors[0].collided)
                        {
                            otherEntity.xpos = thisRight - otherLeft;
                            if (otherEntity.xvel < 0)
                            {
                                if (otherEntity.direction == FLIP.X)
                                    otherEntity.pushing = 2;

                                if (otherEntity.xvel < -0x10000)
                                    otherEntity.xpos += 0x8000;

                                otherEntity.xvel = 0;
                                if (otherEntity.collisionMode != 0 || otherEntity.right == 0)
                                    otherEntity.speed = 0;
                                else
                                    otherEntity.speed = 0x8000;
                            }
                            Script.scriptEng.checkResult = 3;
                        }
                    }
                }
            }
        }
        else
        {
            sensors[0].collided = false;
            sensors[1].collided = false;
            sensors[0].xpos = rx + otherRight;

            sensors[0].ypos = ry + otherTop + 0x20000;
            sensors[1].ypos = ry + otherBottom - 0x20000;
            for (int i = 0; i < 2; ++i)
            {
                if (thisLeft <= sensors[0].xpos && thisLeft > otherEntity.xpos - otherEntity.xvel && thisTop < sensors[1].ypos
                    && thisBottom > sensors[0].ypos)
                {
                    sensors[i].collided = true;
                }
            }
            if (sensors[1].collided || sensors[0].collided)
            {
                otherEntity.xpos = thisLeft - otherRight;
                if (otherEntity.xvel > 0)
                {
                    if (otherEntity.direction == 0)
                        otherEntity.pushing = 2;

                    otherEntity.xvel = 0;
                    if (otherEntity.collisionMode != 0 || otherEntity.left == 0)
                        otherEntity.speed = 0;
                    else
                        otherEntity.speed = -0x8000;
                }
                Script.scriptEng.checkResult = 2;
            }
            else
            {
                sensors[0].collided = false;
                sensors[1].collided = false;
                sensors[0].xpos = rx + otherLeft;

                sensors[0].ypos = ry + otherTop + 0x20000;
                sensors[1].ypos = ry + otherBottom - 0x20000;
                for (int i = 0; i < 2; ++i)
                {
                    if (thisRight > sensors[0].xpos && thisRight < otherEntity.xpos - otherEntity.xvel && thisTop < sensors[1].ypos
                        && thisBottom > sensors[0].ypos)
                    {
                        sensors[i].collided = true;
                    }
                }

                if (sensors[0].collided || sensors[1].collided)
                {
                    otherEntity.xpos = thisRight - otherLeft;
                    if (otherEntity.xvel < 0)
                    {
                        if (otherEntity.direction == FLIP.X)
                            otherEntity.pushing = 2;

                        if (otherEntity.xvel < -0x10000)
                            otherEntity.xpos += 0x8000;

                        otherEntity.xvel = 0;
                        if (otherEntity.collisionMode != 0 || otherEntity.right == 0)
                            otherEntity.speed = 0;
                        else
                            otherEntity.speed = 0x8000;
                    }
                    Script.scriptEng.checkResult = 3;
                }
                else
                {
                    sensors[0].collided = false;
                    sensors[1].collided = false;
                    sensors[2].collided = false;
                    sensors[3].collided = false;
                    sensors[4].collided = false;
                    sensors[0].xpos = rx + otherLeft + 0x20000;
                    sensors[1].xpos = rx;
                    sensors[2].xpos = rx + otherRight - 0x20000;
                    sensors[3].xpos = (sensors[0].xpos + rx) >> 1;
                    sensors[4].xpos = (sensors[2].xpos + rx) >> 1;

                    sensors[0].ypos = ry + otherBottom;
                    if (otherEntity.yvel >= 0)
                    {
                        for (int i = 0; i < 5; ++i)
                        {
                            if (thisLeft < sensors[i].xpos && thisRight > sensors[i].xpos && thisTop <= sensors[0].ypos
                                && thisTop > otherEntity.ypos - otherEntity.yvel)
                            {
                                sensors[i].collided = true;
                                otherEntity.floorSensors[i] = 1;
                            }
                        }
                    }
                    if (sensors[2].collided || sensors[1].collided || sensors[0].collided)
                    {
                        if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                        {
                            otherEntity.xvel = 0;
                            otherEntity.speed = 0;
                        }
                        otherEntity.ypos = thisTop - otherBottom;
                        otherEntity.gravity = 0;
                        otherEntity.yvel = 0;
                        otherEntity.angle = 0;
                        otherEntity.rotation = 0;
                        otherEntity.controlLock = 0;
                        Script.scriptEng.checkResult = 1;
                    }
                    else
                    {
                        sensors[0].collided = false;
                        sensors[1].collided = false;
                        sensors[0].xpos = rx + otherLeft + 0x20000;
                        sensors[1].xpos = rx + otherRight - 0x20000;
                        sensors[0].ypos = ry + otherTop;

                        for (int i = 0; i < 2; ++i)
                        {
                            if (thisLeft < sensors[1].xpos && thisRight > sensors[0].xpos && thisBottom > sensors[0].ypos
                                && thisBottom < otherEntity.ypos - otherEntity.yvel)
                            {
                                sensors[i].collided = true;
                            }
                        }

                        if (sensors[1].collided || sensors[0].collided)
                        {
                            if (otherEntity.gravity == 1)
                                otherEntity.ypos = thisBottom - otherTop;

                            if (otherEntity.yvel <= 0)
                                otherEntity.yvel = 0;
                            Script.scriptEng.checkResult = 4;
                        }
                    }
                }
            }
        }

#if !RETRO_USE_ORIGINAL_CODE
        //if (showHitboxes)
        //{
        //    if (thisHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[thisHitboxID].collision |= 1 << (scriptEng.checkResult - 1);
        //    if (otherHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[otherHitboxID].collision |= 1 << (4 - Script.scriptEng.checkResult);
        //}
#endif
    }
    public static void BoxCollision2(Entity thisEntity, int thisLeft, int thisTop, int thisRight, int thisBottom, Entity otherEntity, int otherLeft, int otherTop,
                       int otherRight, int otherBottom)
    {
        Hitbox thisHitbox = GetHitbox(thisEntity);
        Hitbox otherHitbox = GetHitbox(otherEntity);

        if (thisLeft == 0x10000)
            thisLeft = thisHitbox.left[0];

        if (thisTop == 0x10000)
            thisTop = thisHitbox.top[0];

        if (thisRight == 0x10000)
            thisRight = thisHitbox.right[0];

        if (thisBottom == 0x10000)
            thisBottom = thisHitbox.bottom[0];

        if (otherLeft == 0x10000)
            otherLeft = otherHitbox.left[0];

        if (otherTop == 0x10000)
            otherTop = otherHitbox.top[0];

        if (otherRight == 0x10000)
            otherRight = otherHitbox.right[0];

        if (otherBottom == 0x10000)
            otherBottom = otherHitbox.bottom[0];

#if !RETRO_USE_ORIGINAL_CODE
        //int thisHitboxID = 0;
        //int otherHitboxID = 0;
        //if (showHitboxes)
        //{
        //    thisHitboxID = addDebugHitbox(H_TYPE_BOX, thisEntity, thisLeft, thisTop, thisRight, thisBottom);
        //    otherHitboxID = addDebugHitbox(H_TYPE_BOX, otherEntity, otherLeft, otherTop, otherRight, otherBottom);
        //}
#endif

        thisLeft += thisEntity.xpos >> 16;
        thisTop += thisEntity.ypos >> 16;
        thisRight += thisEntity.xpos >> 16;
        thisBottom += thisEntity.ypos >> 16;

        thisLeft <<= 16;
        thisTop <<= 16;
        thisRight <<= 16;
        thisBottom <<= 16;

        otherLeft <<= 16;
        otherTop <<= 16;
        otherRight <<= 16;
        otherBottom <<= 16;

        Script.scriptEng.checkResult = 0;

        int rx = otherEntity.xpos >> 16 << 16;
        int ry = otherEntity.ypos >> 16 << 16;

        int xDif = thisLeft - rx;
        if (thisEntity.xpos <= rx)
            xDif = rx - thisRight;
        int yDif = thisTop - ry;
        if (thisEntity.ypos <= ry)
            yDif = ry - thisBottom;

        if (xDif <= yDif)
        {
            sensors[0].collided = false;
            sensors[1].collided = false;
            sensors[2].collided = false;
            sensors[0].xpos = rx + otherLeft + 0x20000;
            sensors[1].xpos = rx;
            sensors[2].xpos = rx + otherRight - 0x20000;

            sensors[0].ypos = ry + otherBottom;

            if (otherEntity.yvel >= 0)
            {
                // this should prolly be using all 5 sensors, but this was unused in S2 so it was prolly forgotten about
                for (int i = 0; i < 3; ++i)
                {
                    if (thisLeft < sensors[i].xpos && thisRight > sensors[i].xpos && thisTop <= sensors[0].ypos && thisEntity.ypos > sensors[0].ypos)
                    {
                        sensors[i].collided = true;
                        otherEntity.floorSensors[i] = 1;
                    }
                }
            }

            if (sensors[0].collided || sensors[1].collided || sensors[2].collided)
            {
                if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                {
                    otherEntity.xvel = 0;
                    otherEntity.speed = 0;
                }
                otherEntity.ypos = thisTop - otherBottom;
                otherEntity.gravity = 0;
                otherEntity.yvel = 0;
                otherEntity.angle = 0;
                otherEntity.rotation = 0;
                otherEntity.controlLock = 0;
                Script.scriptEng.checkResult = 1;
            }
            else
            {
                sensors[0].collided = false;
                sensors[1].collided = false;
                sensors[0].xpos = rx + otherLeft + 0x20000;
                sensors[1].xpos = rx + otherRight - 0x20000;

                sensors[0].ypos = ry + otherTop;

                for (int i = 0; i < 2; ++i)
                {
                    if (thisLeft < sensors[1].xpos && thisRight > sensors[0].xpos && thisBottom > sensors[0].ypos && thisEntity.ypos < sensors[0].ypos)
                    {
                        sensors[i].collided = true;
                    }
                }

                if (sensors[1].collided || sensors[0].collided)
                {
                    if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                    {
                        otherEntity.xvel = 0;
                        otherEntity.speed = 0;
                    }

                    otherEntity.ypos = thisBottom - otherTop;
                    if (otherEntity.yvel < 0)
                        otherEntity.yvel = 0;
                    Script.scriptEng.checkResult = 4;
                }
                else
                {
                    sensors[0].collided = false;
                    sensors[1].collided = false;
                    sensors[0].xpos = rx + otherRight;

                    sensors[0].ypos = ry + otherTop + 0x20000;
                    sensors[1].ypos = ry + otherBottom - 0x20000;
                    for (int i = 0; i < 2; ++i)
                    {
                        if (thisLeft <= sensors[0].xpos && thisEntity.xpos > sensors[0].xpos && thisTop < sensors[1].ypos
                            && thisBottom > sensors[0].ypos)
                        {
                            sensors[i].collided = true;
                        }
                    }

                    if (sensors[1].collided || sensors[0].collided)
                    {
                        otherEntity.xpos = thisLeft - otherRight;
                        if (otherEntity.xvel > 0)
                        {
                            if (otherEntity.direction == 0)
                                otherEntity.pushing = 2;

                            otherEntity.xvel = 0;
                            otherEntity.speed = 0;
                        }
                        Script.scriptEng.checkResult = 2;
                    }
                    else
                    {
                        sensors[0].collided = false;
                        sensors[1].collided = false;
                        sensors[0].xpos = rx + otherLeft;

                        sensors[0].ypos = ry + otherTop + 0x20000;
                        sensors[1].ypos = ry + otherBottom - 0x20000;
                        for (int i = 0; i < 2; ++i)
                        {
                            if (thisRight > sensors[0].xpos && thisEntity.xpos < sensors[0].xpos && thisTop < sensors[1].ypos
                                && thisBottom > sensors[0].ypos)
                            {
                                sensors[i].collided = true;
                            }
                        }

                        if (sensors[1].collided || sensors[0].collided)
                        {
                            otherEntity.xpos = thisRight - otherLeft;
                            if (otherEntity.xvel < 0)
                            {
                                if (otherEntity.direction == FLIP.X)
                                    otherEntity.pushing = 2;

                                if (otherEntity.xvel < -0x10000)
                                    otherEntity.xpos += 0x8000;

                                otherEntity.xvel = 0;
                                otherEntity.speed = 0;
                            }
                            Script.scriptEng.checkResult = 3;
                        }
                    }
                }
            }
        }
        else
        {
            sensors[0].collided = false;
            sensors[1].collided = false;
            sensors[0].xpos = rx + otherRight;

            sensors[0].ypos = ry + otherTop + 0x20000;
            sensors[1].ypos = ry + otherBottom - 0x20000;
            for (int i = 0; i < 2; ++i)
            {
                if (thisLeft <= sensors[0].xpos && thisEntity.xpos > sensors[0].xpos && thisTop < sensors[1].ypos && thisBottom > sensors[0].ypos)
                {
                    sensors[i].collided = true;
                }
            }
            if (sensors[1].collided || sensors[0].collided)
            {
                otherEntity.xpos = thisLeft - otherRight;
                if (otherEntity.xvel > 0)
                {
                    if (otherEntity.direction == 0)
                        otherEntity.pushing = 2;

                    otherEntity.xvel = 0;
                    otherEntity.speed = 0;
                }
                Script.scriptEng.checkResult = 2;
            }
            else
            {
                sensors[0].collided = false;
                sensors[1].collided = false;
                sensors[0].xpos = rx + otherLeft;

                sensors[0].ypos = ry + otherTop + 0x20000;
                sensors[1].ypos = ry + otherBottom - 0x20000;
                for (int i = 0; i < 2; ++i)
                {
                    if (thisRight > sensors[0].xpos && thisEntity.xpos < sensors[0].xpos && thisTop < sensors[1].ypos && thisBottom > sensors[0].ypos)
                    {
                        sensors[i].collided = true;
                    }
                }

                if (sensors[0].collided || sensors[1].collided)
                {
                    otherEntity.xpos = thisRight - otherLeft;
                    if (otherEntity.xvel < 0)
                    {
                        if (otherEntity.direction == FLIP.X)
                            otherEntity.pushing = 2;

                        if (otherEntity.xvel < -0x10000)
                            otherEntity.xpos += 0x8000;

                        otherEntity.xvel = 0;
                        otherEntity.speed = 0;
                    }
                    Script.scriptEng.checkResult = 3;
                }
                else
                {
                    sensors[0].collided = false;
                    sensors[1].collided = false;
                    sensors[2].collided = false;
                    sensors[0].xpos = rx + otherLeft + 0x20000;
                    sensors[1].xpos = rx;
                    sensors[2].xpos = rx + otherRight - 0x20000;

                    sensors[0].ypos = ry + otherBottom;
                    if (otherEntity.yvel >= 0)
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            if (thisLeft < sensors[i].xpos && thisRight > sensors[i].xpos && thisTop <= sensors[0].ypos
                                && thisEntity.ypos > sensors[0].ypos)
                            {
                                sensors[i].collided = true;
                                otherEntity.floorSensors[i] = 1;
                            }
                        }
                    }

                    if (sensors[0].collided || sensors[1].collided || sensors[2].collided)
                    {
                        if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                        {
                            otherEntity.xvel = 0;
                            otherEntity.speed = 0;
                        }
                        otherEntity.ypos = thisTop - otherBottom;
                        otherEntity.gravity = 0;
                        otherEntity.yvel = 0;
                        otherEntity.angle = 0;
                        otherEntity.rotation = 0;
                        otherEntity.controlLock = 0;
                        Script.scriptEng.checkResult = 1;
                    }
                    else
                    {
                        sensors[0].collided = false;
                        sensors[1].collided = false;
                        sensors[0].xpos = rx + otherLeft + 0x20000;
                        sensors[1].xpos = rx + otherRight - 0x20000;

                        sensors[0].ypos = ry + otherTop;

                        for (int i = 0; i < 2; ++i)
                        {
                            if (thisLeft < sensors[1].xpos && thisRight > sensors[0].xpos && thisBottom > sensors[0].ypos
                                && thisEntity.ypos < sensors[0].ypos)
                            {
                                sensors[i].collided = true;
                            }
                        }

                        if (sensors[1].collided || sensors[0].collided)
                        {
                            if (otherEntity.gravity == 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
                            {
                                otherEntity.xvel = 0;
                                otherEntity.speed = 0;
                            }

                            otherEntity.ypos = thisBottom - otherTop;

                            if (otherEntity.yvel < 0)
                                otherEntity.yvel = 0;
                            Script.scriptEng.checkResult = 4;
                        }
                    }
                }
            }
        }

#if !RETRO_USE_ORIGINAL_CODE
        //if (showHitboxes)
        //{
        //    if (thisHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[thisHitboxID].collision |= 1 << (scriptEng.checkResult - 1);
        //    if (otherHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[otherHitboxID].collision |= 1 << (4 - Script.scriptEng.checkResult);
        //}
#endif
    }

    public static void PlatformCollision(Entity thisEntity, int thisLeft, int thisTop, int thisRight, int thisBottom, Entity otherEntity, int otherLeft, int otherTop,
                           int otherRight, int otherBottom)
    {
        Script.scriptEng.checkResult = 0;

        Hitbox thisHitbox = GetHitbox(thisEntity);
        Hitbox otherHitbox = GetHitbox(otherEntity);

        if (thisLeft == 0x10000)
            thisLeft = thisHitbox.left[0];

        if (thisTop == 0x10000)
            thisTop = thisHitbox.top[0];

        if (thisRight == 0x10000)
            thisRight = thisHitbox.right[0];

        if (thisBottom == 0x10000)
            thisBottom = thisHitbox.bottom[0];

        if (otherLeft == 0x10000)
            otherLeft = otherHitbox.left[0];

        if (otherTop == 0x10000)
            otherTop = otherHitbox.top[0];

        if (otherRight == 0x10000)
            otherRight = otherHitbox.right[0];

        if (otherBottom == 0x10000)
            otherBottom = otherHitbox.bottom[0];

#if !RETRO_USE_ORIGINAL_CODE
        //int thisHitboxID = 0;
        //int otherHitboxID = 0;
        //if (showHitboxes)
        //{
        //    thisHitboxID = addDebugHitbox(H_TYPE_PLAT, thisEntity, thisLeft, thisTop, thisRight, thisBottom);
        //    otherHitboxID = addDebugHitbox(H_TYPE_PLAT, otherEntity, otherLeft, otherTop, otherRight, otherBottom);
        //}
#endif

        thisLeft += thisEntity.xpos >> 16;
        thisTop += thisEntity.ypos >> 16;
        thisRight += thisEntity.xpos >> 16;
        thisBottom += thisEntity.ypos >> 16;

        thisLeft <<= 16;
        thisTop <<= 16;
        thisRight <<= 16;
        thisBottom <<= 16;

        sensors[0].collided = false;
        sensors[1].collided = false;
        sensors[2].collided = false;

        int rx = otherEntity.xpos >> 16 << 16;
        int ry = otherEntity.ypos >> 16 << 16;

        sensors[0].xpos = rx + (otherLeft << 16);
        sensors[1].xpos = rx;
        sensors[2].xpos = rx + (otherRight << 16);
        sensors[3].xpos = (rx + sensors[0].xpos) >> 1;
        sensors[4].xpos = (sensors[2].xpos + rx) >> 1;

        sensors[0].ypos = (otherBottom << 16) + ry;

        for (int i = 0; i < 5; ++i)
        {
            if (thisLeft < sensors[i].xpos && thisRight > sensors[i].xpos && thisTop - 1 <= sensors[0].ypos && thisBottom > sensors[0].ypos
                && otherEntity.yvel >= 0)
            {
                sensors[i].collided = true;
                otherEntity.floorSensors[i] = 1;
            }
        }

        if (sensors[0].collided || sensors[1].collided || sensors[2].collided)
        {
            if (otherEntity.gravity != 0 && (otherEntity.collisionMode == CMODE.RWALL || otherEntity.collisionMode == CMODE.LWALL))
            {
                otherEntity.xvel = 0;
                otherEntity.speed = 0;
            }
            otherEntity.ypos = thisTop - (otherBottom << 16);
            otherEntity.gravity = 0;
            otherEntity.yvel = 0;
            otherEntity.angle = 0;
            otherEntity.rotation = 0;
            otherEntity.controlLock = 0;
            Script.scriptEng.checkResult = 1;
        }

#if !RETRO_USE_ORIGINAL_CODE
        //if (showHitboxes)
        //{
        //    if (thisHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[thisHitboxID].collision |= 1 << 0;
        //    if (otherHitboxID >= 0 && Script.scriptEng.checkResult)
        //        debugHitboxList[otherHitboxID].collision |= 1 << 3;
        //}
#endif
    }

}