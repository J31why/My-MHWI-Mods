using Common;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Savedata;

namespace AutoFarm;

public class AutoFarmPlugin : IPlugin
{
    public string Name => "AutoFarm";
    public string Author => "Jelly";
    private static bool _isEnabled = false;
    public void OnLoad()
    {
        Logging.Name = Name;

        if (!Box.IsAddrMatch() || !HarvestBox.IsAddrMatch())
        {
            Logging.Error("load failed.");
            return;
        }
        _isEnabled = true;
        Logging.Info("loaded.");
    }

    public void OnQuestLeave(int questId)
    {
        try
        {
            HandleHarvest();
        
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
        }
    }
    private static void HandleHarvest()
    {
        if (!_isEnabled) return;
        HarvestBox.ExtendFertilizer();
        var result = HarvestBox.Harvest();
        if (result)
            Logging.Info("Harvest successful.");
        else
        {
            Logging.Warn("Harvest failed. Please check whether your Item box is full.");
        }
        var userdata = UserData.GetCurrentUserData();
        var index = Box.FindSlotIndex(userdata, Box.ItemCategory.Item, 234); //Great Hornfly
        if (index != -1)
        {
            ref var slot = ref Box.GetSlot(userdata, Box.ItemCategory.Item, index);
            Box.Store(userdata,ref slot,out _);
            slot.Id = 0;
            slot.Count = 0;
        }
    }
}
