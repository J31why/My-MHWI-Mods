using Common;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Savedata;
using SharpPluginLoader.Core.SaveData;

namespace AutoItemRestore;

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

    /// <summary>Stores the pouch item into the box. Clears the slot on success.</summary>
    /// <returns>true if fully stored; false if empty, invalid count, or box is full.</returns>
    public static bool Store(UserData userdata, ref Item item, bool isAmmo)
    {
        if (item.Id == 0 || item.Count <= 0)
            return false;

        if (!TryAddToBox(userdata, item.Id, item.Count, isAmmo))
            return false;

        item.Id = 0;
        item.Count = 0;
        return true;
    }

    /// <summary>Sets the pouch slot to the given item and restocks up to count.</summary>
    /// <returns>true if the slot ends with Id == id and Count >= count; false otherwise.</returns>
    public static bool Take(UserData userdata, ref Item pouchItem, int id, int count, bool isAmmo)
    {
        if (id == 0 || count <= 0)
            return false;

        if (pouchItem.Id == id)
        {
            Restock(userdata, ref pouchItem, count, isAmmo);
            return pouchItem.Id == id && pouchItem.Count >= count;
        }

        if (pouchItem.Id != 0)
        {
            if (!Store(userdata, ref pouchItem, isAmmo))
                return false;
        }

        pouchItem.Id = id;
        pouchItem.Count = 0;

        Restock(userdata, ref pouchItem, count, isAmmo);

        if (pouchItem.Count == 0)
            pouchItem.Id = 0;

        return pouchItem.Id == id && pouchItem.Count >= count;
    }

    /// <summary>Restocks the pouch item from the box up to dst.</summary>
    /// <returns>true if item.Count >= dst; false otherwise.</returns>
    public static bool Restock(UserData userdata, ref Item item, int dst, bool isAmmo)
    {
        if (item.Id == 0 || dst <= 0) return false;
        if (item.Count >= dst) return true;

        var need = dst - item.Count;
        TryRemoveFromBox(userdata, item.Id, need, isAmmo, out var removed);
        item.Count += removed;
        return item.Count >= dst;
    }

    public static ref Item GetBoxItem(UserData userdata, int slot, bool isAmmo)
    {
        if(slot <0 || slot >= (isAmmo ? _ammoBoxSlotCount : _itemBoxSlotCount))
            throw new ArgumentOutOfRangeException(nameof(slot));
        return ref userdata.GetRef<Item>((isAmmo ? _ammoBoxAddr : _itemBoxAddr) + (slot * _itemSize));
    }

    /// <summary>Returns the slot index of the item in the box, or -1 if not found.</summary>
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

    private static void SetItemCount(ref Item item, int count)
    {
        if (count < 0) count = 0;
        item.Count = count;
        if (count == 0)
            item.Id = 0;
    }
    /// <summary>Adds the item into the box. Fails if the stack would exceed 9999 or the box is full.</summary>
    /// <returns>true if added; false if nothing was changed.</returns>
    public static bool TryAddToBox(UserData userdata, int id, int count, bool isAmmo)
    {
        if (id == 0 || count <= 0) return false;

        var slot = FindSlot(userdata, id, isAmmo);
        if (slot == -1)
        {
            slot = FindSlot(userdata, 0, isAmmo);
            if (slot == -1) return false;
        }

        ref var boxItem = ref GetBoxItem(userdata, slot, isAmmo);
       
        var dst = boxItem.Count + count;
        if (dst > 9999) return false;

        if (boxItem.Id == 0)
            boxItem.Id = id;

        SetItemCount(ref boxItem, dst);
        return true;
    }
    /// <summary>Removes up to count from the box. May remove fewer if the stack is smaller.</summary>
    /// <returns>true if at least one was removed; false if nothing was changed.</returns>
    public static bool TryRemoveFromBox(UserData userdata, int id, int count, bool isAmmo, out int removedCount)
    {
        removedCount = 0;
        if (id == 0 || count <= 0) return false;
        var slot = FindSlot(userdata, id, isAmmo);
        if (slot == -1) return false;

        ref var boxItem = ref GetBoxItem(userdata, slot, isAmmo);
        if (boxItem.Count < count)
        {
            removedCount = boxItem.Count;
            SetItemCount(ref boxItem, 0);
        }
        else
        {
            removedCount = count;
            SetItemCount(ref boxItem, boxItem.Count - count);
        }
        return true;
    }
}
