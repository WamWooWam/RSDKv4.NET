namespace RSDKv4;

public class Entity
{
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
