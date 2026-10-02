using Commmon;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Savedata;
using SharpPluginLoader.Core.SaveData;

namespace AutoItemResore;

internal static class Box
{
    private const int _itemSize = 0x10;
    private const nint _itemBoxAddr = 0x38a08;
    private const int _itemBoxSlotCount = 200;
    private const nint _ammoBoxAddr = _itemBoxAddr + _itemBoxSlotCount * _itemSize;
    private const int _ammoBoxSlotCount = 100;

    private const long _itemBoxMatchAddr = 0x14135349B;
    private static readonly byte[] _itemBoxMatchPattern = [0x48, 0x05, 0x08, 0x8a, 0x03, 0x00];

    public static bool IsAddrMatch()
    {
        try
        {
            return MemoryUtil
                .AsSpan<byte>(_itemBoxMatchAddr, _itemBoxMatchPattern.Length)
                .SequenceEqual(_itemBoxMatchPattern);
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
            return false;
        }
    }

    public static bool Store(UserData userdata, ref Item item, bool isAmmo)
    {
        if (item.Id == 0 || item.Count <= 0) return false;

        var slot = FindSlot(userdata, item.Id, isAmmo);
        if (slot == -1)
        {
            slot = FindSlot(userdata, 0, isAmmo);
            if (slot == -1) return false;
        }
        ref var boxItem = ref GetBoxItem(userdata, slot, isAmmo);
        if (boxItem.Id == 0)
            boxItem.Id = item.Id;

        SetItemCount(userdata, ref boxItem, boxItem.Count + item.Count);
        item.Id = 0;
        item.Count = 0;
        return true;
    }

    public static bool Take(UserData userdata, ref Item pounchItem, int id,int count, bool isAmmo)
    {
        if (pounchItem.Id != 0)
        {
            if (!Store(userdata, ref pounchItem, isAmmo))
            {
                Logging.Warn($"Store failed, slot not restored (box full?)");
                return false;
            }
        }
        pounchItem.Id = id;
        pounchItem.Count = 0;
        Restock(userdata, ref pounchItem, count, isAmmo);
        if(pounchItem.Count == 0)
            pounchItem.Id = 0;
        return true;
    }

    public static void Restock(UserData userdata, ref Item item, int dst, bool isAmmo)
    {
        if (item.Id == 0 || dst <= item.Count) return;

        var slot = FindSlot(userdata, item.Id, isAmmo);
        if (slot == -1) return;
        ref var boxItem = ref GetBoxItem(userdata, slot, isAmmo);
        var restockCount = dst - item.Count;
        if (boxItem.Count < restockCount)
        {
            item.Count += boxItem.Count;
            SetItemCount(userdata, ref boxItem, 0);
        }
        else
        {
            item.Count += restockCount;
            SetItemCount(userdata, ref boxItem, boxItem.Count - restockCount);
        }
    }

    public static ref Item GetBoxItem(UserData userdata, int slot, bool isAmmo)
    {
        if(slot <0 || slot >= (isAmmo ? _ammoBoxSlotCount : _itemBoxSlotCount))
            throw new ArgumentOutOfRangeException(nameof(slot));
        return ref userdata.GetRef<Item>((isAmmo ? _ammoBoxAddr : _itemBoxAddr) + (slot * _itemSize));
    }

    /// <summary>
    /// return slot index of the item in the box, if not found return -1
    /// </summary>
    /// <param name="userdata"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static int FindSlot(UserData userdata,int id, bool isAmmo)
    {
        var addr = isAmmo ? _ammoBoxAddr : _itemBoxAddr;
        var slotCount = isAmmo ? _ammoBoxSlotCount : _itemBoxSlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            var item = userdata.Get<Item>(addr + i * _itemSize);
            if (item.Id == id) return i;
        }
        return -1;
    }

    public static void SetItemCount(UserData userdata, ref Item item, int count)
    {
        if (count < 0) count = 0;
        item.Count = count;
        if (count == 0)
            item.Id = 0;
    }

    public static Dictionary<int, (int slot, Item item)> GetItemBox(UserData userdata)
    {
        var dict = new Dictionary<int, (int slot, Item item)>(200);
        for (int i = 0; i < _itemBoxSlotCount; i++)
        {
            var item = userdata.Get<Item>(_itemBoxAddr + i * _itemSize);
            if (item.Id == 0) continue;
            if (item.Id < 0 || item.Id > 3000) throw new Exception("Invalid item ID in item box");
            if (item.Count < 0 || item.Count > 9999) throw new Exception("Invalid item count in item box");
            if (dict.ContainsKey(item.Id)) throw new Exception($"Duplicate item ID {item.Id} in item box");
            dict[item.Id] = (i, item);
        }
        return dict;
    }

    public static Dictionary<int, (int slot, Item item)> GetAmmoBox(UserData userdata)
    {
        var dict = new Dictionary<int, (int slot, Item item)>(200);
        for (int i = 0; i < _ammoBoxSlotCount; i++)
        {
            var item = userdata.Get<Item>(_ammoBoxAddr + i * _itemSize);
            if (item.Id == 0) continue;
            if (item.Id < 0 || item.Id > 3000) throw new Exception("Invalid item ID in ammo box");
            if (item.Count < 0 || item.Count > 9999) throw new Exception("Invalid item count in ammo box");
            if (dict.ContainsKey(item.Id)) throw new Exception($"Duplicate item ID {item.Id} in ammo box");
            dict[item.Id] = (i, item);
        }
        return dict;
    }
}
