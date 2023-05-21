namespace RSDKv4;

public static class ENGINE_STATE
{
    public const int DEVMENU = 0,
        MAINGAME = 1,
        INITDEVMENU = 2,
        WAIT = 3,
        SCRIPTERROR = 4,
        INITPAUSE = 5,
        EXITPAUSE = 6,
        ENDGAME = 7,
        RESETGAME = 8;
}


public static class DEVMENU
{
    public const int MAIN = 0,
        PLAYERSEL = 1,
        STAGELISTSEL = 2,
        STAGESEL = 3,
        SCRIPTERROR = 4,
        MODMENU = 5;
}