using System;
using System.Diagnostics;

namespace RSDKv4;

[DebuggerDisplay("X = {xpos >> 16}, Y = {ypos >> 16}, Type = {RSDKv4.Objects.typeNames[type]}")]
public class Entity
{
    public Entity()
    {

    }

    public Entity(Entity other)
    {
        xpos = other.xpos;
        ypos = other.ypos;
        xvel = other.xvel;
        yvel = other.yvel;
        speed = other.speed;
        state = other.state;
        angle = other.angle;
        scale = other.scale;
        rotation = other.rotation;
        alpha = other.alpha;
        animationTimer = other.animationTimer;
        animationSpeed = other.animationSpeed;
        lookPosX = other.lookPosX;
        lookPosY = other.lookPosY;
        groupID = other.groupID;
        type = other.type;
        propertyValue = other.propertyValue;
        priority = other.priority;
        drawOrder = other.drawOrder;
        direction = other.direction;
        inkEffect = other.inkEffect;
        animation = other.animation;
        prevAnimation = other.prevAnimation;
        frame = other.frame;
        collisionMode = other.collisionMode;
        collisionPlane = other.collisionPlane;
        controlMode = other.controlMode;
        controlLock = other.controlLock;
        pushing = other.pushing;
        visible = other.visible;
        tileCollisions = other.tileCollisions;
        objectInteractions = other.objectInteractions;
        gravity = other.gravity;
        left = other.left;
        right = other.right;
        up = other.up;
        down = other.down;
        jumpPress = other.jumpPress;
        jumpHold = other.jumpHold;
        scrollTracking = other.scrollTracking;

        Array.Copy(other.values, values, 48);
        Array.Copy(other.floorSensors, floorSensors, 5);
    }

    public int xpos;
    public int ypos;
    public int xvel;
    public int yvel;
    public int speed;
    public int[] values = new int[48];
    public int state;
    public int angle;
    public int scale;
    public int rotation;
    public int alpha;
    public int animationTimer;
    public int animationSpeed;
    public int lookPosX;
    public int lookPosY;
    public ushort groupID;
    public byte type;
    public byte propertyValue;
    public byte priority;
    public int drawOrder;
    public byte direction;
    public byte inkEffect;
    public byte animation;
    public byte prevAnimation;
    public byte frame;
    public byte collisionMode;
    public byte collisionPlane;
    public sbyte controlMode;
    public byte controlLock;
    public byte pushing;
    public bool visible;
    public bool tileCollisions;
    public bool objectInteractions;
    public byte gravity;
    public byte left;
    public byte right;
    public byte up;
    public byte down;
    public byte jumpPress;
    public byte jumpHold;
    public byte scrollTracking;
    // was 3 on S1 release, but bumped up to 5 for S2
    public byte[] floorSensors = new byte[5];
}
