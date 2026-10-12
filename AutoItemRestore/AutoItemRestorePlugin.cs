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
                if (!result)
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
            {
                var msg = "Some items failed to restore.\n Item box may be lack items. Check your item pouch.";
                Gui.DisplayMessage(msg, true);
                Logging.Warn(msg);
            }
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

        if (snapshot.Id == 0 && item.Id > 0)
            return Box.Store(userData, ref item, out _);

        if (snapshot.Id > 0)
        {
           
            if (item.Id > 0 && item.Id != snapshot.Id)
            {
                if (!Box.Store(userData, ref item, out _))
                    return false;
            }
            

            if (item.Id == 0)
            {
                if (!Box.Take(userData, ref item, snapshot.Id, snapshot.Count, out var taken))
                    return false;
                return taken == snapshot.Count;
            }

            if (item.Id == snapshot.Id)
            {
                if (item.Count >= snapshot.Count)
                    return true;
                if (!Box.Restock(userData, ref item, snapshot.Count, out var restocked))
                    return false;
                return item.Count == snapshot.Count;
            }
        }
        return false;
    }

}
