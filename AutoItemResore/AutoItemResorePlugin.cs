using Commmon;
using Iced.Intel;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.IO;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Savedata;
using SharpPluginLoader.Core.SaveData;

namespace AutoItemResore;

public class AutoItemResorePlugin : IPlugin
{
    public string Name => "AutoItemResore";
    public string Author => "Jelly";
    private Item[]? _itemPouchSnapshot;
    private Item[]? _ammoPouchSnapshot;


    private bool _isEnabled = false;

    public void OnLoad()
    {
        Logging.Name = Name;
        _isEnabled = Box.IsAddrMatch();
        if (_isEnabled)
            Logging.Info("loaded.");
        else
            Logging.Error("load failed, plugin initialization error.");
    }

    public void OnQuestEnter(int questId)
    {
        if (!_isEnabled) return;
        SnapshotPouches();
    }

    public void OnQuestLeave(int questId)
    {
        if (!_isEnabled) return;
        RestorePouches();
    }

    private void SnapshotPouches()
    {
        try
        {
            var userdata = UserData.GetCurrentUserData();
            _itemPouchSnapshot = userdata.ItemPouch.ToArray();
            _ammoPouchSnapshot = userdata.AmmoPouch.ToArray();
            Logging.Info("items saved.");
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
        }
    }

    private void RestorePouches()
    {
        if (_itemPouchSnapshot is null || _ammoPouchSnapshot is null)
            return;
        try
        {
            var userdata = UserData.GetCurrentUserData();
            var itempouch = userdata.ItemPouch;
            var ammopouch = userdata.AmmoPouch;
            for (int i = 0; i < _itemPouchSnapshot.Length; i++)
            {
                ref var item = ref itempouch[i];
                var snapshot = _itemPouchSnapshot[i];
                RestoreSlot(userdata, ref item, snapshot, false);
            }
            for (int i = 0; i < _ammoPouchSnapshot.Length; i++)
            {
                ref var item = ref ammopouch[i];
                var snapshot = _ammoPouchSnapshot[i];
                RestoreSlot(userdata, ref item, snapshot, true);
            }
            Logging.Info("items restored.");
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

    private static void RestoreSlot(UserData userData, ref Item item, Item snapshot, bool isAmmo)
    {
        if(snapshot.Id != item.Id)
            Box.Take(userData, ref item, snapshot.Id, snapshot.Count, isAmmo);
        else
            Box.Restock(userData, ref item, snapshot.Count, isAmmo);
    }

}
