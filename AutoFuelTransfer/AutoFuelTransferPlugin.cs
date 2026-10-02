using Common;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Memory;
using SharpPluginLoader.Core.Savedata;

namespace AutoFuelTransfer;


public class AutoFuelTransferPlugin : IPlugin
{
    public string Name => "AutoFuelTransfer";
    public string Author => "Jelly";
    private bool _isEnabled = true;
    private const long _maxStoredFuel = 3_000_000;
    private const nint _naturalFuelAddr = 0x102FDC;
    private const nint _storedFuelAddr = _naturalFuelAddr + 4;
    private readonly byte[] _naturalFuelAddFuncMatchPattern = [0x4C, 0x8D, 0x86, 0xDC, 0x2F, 0x10, 0x00];
    private const long _naturalFuelAddFuncMatchAddr = 0x141349FC4;
    public void OnLoad()
    {
        Logging.Name = Name;
        _isEnabled = IsAddrMatch();
        if (_isEnabled) 
            Logging.Info("loaded.");
        else
            Logging.Error("load failed, plugin initialization error.");
    }

    private bool IsAddrMatch()
    {
        try
        {
            return MemoryUtil
                .AsSpan<byte>(_naturalFuelAddFuncMatchAddr, _naturalFuelAddFuncMatchPattern.Length)
                .SequenceEqual(_naturalFuelAddFuncMatchPattern);
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
            return false;
        }
        
    }

    public void OnQuestLeave(int questId)
    {
        if (!_isEnabled) return;
        try
        {
            TransferFuel();
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
        }
    }

    private void TransferFuel()
    {
        if (!_isEnabled) 
            return;
        var userdata = UserData.GetCurrentUserData();
        if (userdata is null)
            return;
        var naturalFuel = userdata.Get<int>(_naturalFuelAddr);
        var storedFuel = userdata.Get<int>(_storedFuelAddr);
        var targetFuel = storedFuel + naturalFuel;
        if (naturalFuel <= 0 || targetFuel > _maxStoredFuel)
            return;

        userdata.Set(_naturalFuelAddr, 0);
        userdata.Set(_storedFuelAddr, targetFuel);
        Logging.Info($"natural fuel: {naturalFuel} -> 0 | stored fuel: {storedFuel} -> {targetFuel}");
    }
}
