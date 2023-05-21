using System.IO;
#if SILVERLIGHT
using System.IO.IsolatedStorage;
#endif

namespace RSDKv4;
public class SaveData
{
    public int[] saveRAM = new int[1024];
    public SaveGame saveGame;

    public SaveData()
    {

    }

    public void InitializeSaveRAM()
    {
        ReadSaveRAMData();
        saveGame = new SaveGame(saveRAM);
        if (!saveGame.saveInitialized)
        {
            saveGame.saveInitialized = true;
            saveGame.musVolume = 100;
            saveGame.sfxVolume = 100;
            saveGame.spindashEnabled = true;
            saveGame.boxRegion = 0;
            saveGame.vDPadSize = 64;
            saveGame.vDPadOpacity = 160;
            saveGame.vDPadX_Move = 56;
            saveGame.vDPadY_Move = 184;
            saveGame.vDPadX_Jump = -56;
            saveGame.vDPadY_Jump = 188;
            saveGame.tailsUnlocked = true;
            saveGame.knuxUnlocked = true;
            saveGame.unlockedActs = 0;
            saveGame.unlockedHPZ = true;

            WriteSaveRAMData();
        }
    }

    public int ReadSaveRAMData()
    {
        try
        {
#if SILVERLIGHT
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = storage.OpenFile("SGame.bin", FileMode.Open))
#else
            using (var stream = File.Open("SGame.bin", FileMode.Open))
#endif
            using (var binaryReader = new BinaryReader(stream))
            {
                for (int index = 0; index < saveRAM.Length; ++index)
                    saveRAM[index] = binaryReader.ReadInt32();
            }
        }
        catch { };
        return 1;
    }

    public int WriteSaveRAMData()
    {
        try
        {
#if SILVERLIGHT
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = storage.OpenFile("SGame.bin", FileMode.OpenOrCreate))
#else
            using (var stream = File.Open("SGame.bin", FileMode.OpenOrCreate))
#endif
            using (var binaryWriter = new BinaryWriter(stream))
            {
                for (int index = 0; index < saveRAM.Length; ++index)
                    binaryWriter.Write(saveRAM[index]);
            }
        }
        catch { };

        return 1;
    }
}
