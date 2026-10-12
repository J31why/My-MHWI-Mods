using Common;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Savedata;
using SharpPluginLoader.Core.SaveData;

namespace AutoFarm;

internal static class HarvestBox
{

    private const long _fertilizerAddr = 0x103068;
    private const int _fertilizerCount = 5;
    private const long _harvestBoxAddr = _fertilizerAddr + _structureSize * _fertilizerCount;
    private const int _harvestBoxSlotCount = 50;
    private const int _structureSize = 0x10;
    private const long _harvestBoxMatchAddr = 0x14136D2A5;
    private static readonly byte[] _harvestBoxMatchPattern = [0x48, 0x8d, 0xb7, 0x68, 0x30, 0x10, 0x00];

    public static bool IsAddrMatch()
    {
        try
        {
            return MemoryUtil
                .AsSpan<byte>(_harvestBoxMatchAddr, _harvestBoxMatchPattern.Length)
                .SequenceEqual(_harvestBoxMatchPattern);
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
            return false;
        }
    }

    public static bool Harvest()
    {
        var userdata = UserData.GetCurrentUserData();
        var success = true;
        for (int i = 0; i < _harvestBoxSlotCount; i++)
        {
            ref var slot = ref userdata.GetRef<Item>((nint)(_harvestBoxAddr + i * _structureSize));
            if (slot.Id <= 0) continue;
            var result = Box.Store(userdata, ref slot,out _);
            if (!result) 
                success = false;
        }
        return success;
    }

    public static bool ExtendFertilizer()
    {
        var userdata = UserData.GetCurrentUserData();
        ref var rp = ref userdata.GetRef<int>(0x98);
        if (rp < 300) return false;
        if (!IsNeedFertilizer(userdata))
            return true;

        for (int i = 0; i < _fertilizerCount; i++)
        {
            ref var fertilizer = ref userdata.GetRef<Item>((nint)(_fertilizerAddr + i * _structureSize));
            if (fertilizer.Id <= 0)
                continue;
            var dst = Math.Min(9, fertilizer.Count + 5);
            fertilizer.Count = dst;
        }
        rp -= 300;
        return true;
    }

    public static bool IsNeedFertilizer(UserData data)
    {
        for (int i = 0; i < _fertilizerCount; i++)
        {
            var fertilizer = data.GetRef<Item>((nint)(_fertilizerAddr + i * _structureSize));
            if (fertilizer.Id != 0 && fertilizer.Count <= 4)
                return true;
        }
        return false;
    }

}
