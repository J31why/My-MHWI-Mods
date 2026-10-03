using Common;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Savedata;
using SharpPluginLoader.Core.SaveData;

namespace AutoItemRestore;

public class AutoItemRestorePlugin : IPlugin
{
    public string Name => "AutoItemRestore";
    public string Author => "Jelly";
    private Item[]? _itemPouchSnapshot;
    private Item[]? _ammoPouchSnapshot;

    private bool _isEnabled = false;

    public void OnLoad()
    {
        Logging.Name = Name;
        _isEnabled = Box.IsAddrMatch();
        if (_isEnabled)
        {
            Logging.Info("loaded.");
            Logging.Warn("please backup your save data before using this plugin.");
        }
        else
            Logging.Error("load failed, plugin initialization error.");
    }

    public void OnQuestEnter(int questId)
    {
        if (!_isEnabled) return;
        Snapshot();
    }

    public void OnQuestLeave(int questId)
    {
        if (!_isEnabled) return;
        Restore();
    }

    private void Snapshot()
    {
        try
        {
            var userdata = UserData.GetCurrentUserData();
            _itemPouchSnapshot = userdata.ItemPouch.ToArray();
            _ammoPouchSnapshot = userdata.AmmoPouch.ToArray();
            Logging.Info("item/ammo pouch snapshot saved.");
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
        }
    }

    private void Restore()
    {
        if (_itemPouchSnapshot is null || _ammoPouchSnapshot is null)
            return;
        try
        {
            var userdata = UserData.GetCurrentUserData();
            var itempouch = userdata.ItemPouch;
            var ammopouch = userdata.AmmoPouch;
            var failed = false;
            for (int i = 0; i < _itemPouchSnapshot.Length; i++)
            {
                var snapshot = _itemPouchSnapshot[i];
                ref var item = ref itempouch[i];
                var result = RestoreSlot(userdata, ref item, snapshot, false);
                if(!result)
                    failed = true;
            }
            for (int i = 0; i < _ammoPouchSnapshot.Length; i++)
            {
                var snapshot = _ammoPouchSnapshot[i];
                ref var item = ref ammopouch[i];
                var result = RestoreSlot(userdata, ref item, snapshot, true);
                if (!result)
                    failed = true;
            }
            if (failed)
                Logging.Warn("Some items failed to restore. Item box may be lack items. Check your item pouch.");
            else
                Logging.Info("item/ammo pouch restored successfully.");
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
        }
        finally
        {
            _itemPouchSnapshot = null;
            _ammoPouchSnapshot = null;
        }
    }

    private static bool RestoreSlot(UserData userData, ref Item item, Item snapshot, bool isAmmo)
    {
        if (item.Id == 0 && snapshot.Id == 0)
            return true;
        else if (snapshot.Id == 0 && item.Id > 0)
            return Box.Store(userData, ref item, isAmmo);
        else if (snapshot.Id > 0 && snapshot.Id != item.Id)
            return Box.Take(userData, ref item, snapshot.Id, snapshot.Count, isAmmo);
        else if (snapshot.Id > 0 && snapshot.Id == item.Id)
            return Box.Restock(userData, ref item, snapshot.Count, isAmmo);
        return false;
    }
}
