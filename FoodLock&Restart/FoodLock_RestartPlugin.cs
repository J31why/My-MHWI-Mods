using AutoItemRestore;
using Common;
using QuickQuestComplete;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Entities;
using SharpPluginLoader.Core.IO;
using SharpPluginLoader.Core.Memory;
using System.Net.NetworkInformation;
using System.Reflection;

namespace FoodLock_Restart;


public class FoodLock_RestartPlugin : IPlugin
{
    public string Name => "FoodLock&Restart";
    public string Author => "Jelly";

    private const string _foodHotkeyName = "FoodLockHotkey";
    private const string _restartHotkeyName = "RestartHotkey";

    #region quest
    private static Hook<AcceptQuest>? _acceptQuestHook;
    private delegate void AcceptQuest(nint questMgr, int save, int unk);
    private readonly nint _acceptQuestCall = new(0x141141C40);
    private readonly NativeFunction<nint, nint> _leaveQuest = new(0x141B66A70);
    private readonly NativeAction<nint> _restoreItems = new(0x141B5B470);
    private const int _questCtxStateOffset = 0x2920;
    private const int _questCtxIdOffset = 0x292c;
    private const int _questCampIndexOffset = 0x291bc;
    private static nint _questCtx = 0;
    private MethodInfo? _abandonQuestHook;
    private MethodInfo? _acceptQuestInternalHook;
    private MethodInfo? _departOnQuestHook;
    private MethodInfo? _leaveQuestHook;
    #endregion

    #region food
    private readonly record struct FoodInfo(int healthType, int staminaType, int attackType, int defenceType, int elementalType, int skill1, int skill2, int skill3)
    {
        public bool hasEaten => healthType != 0 || staminaType != 0 || attackType != 0 || defenceType != 0 || elementalType != 0 || skill1 != -1 || skill2 != -1 || skill3 != -1;
    };
    private readonly int[] _foodBaseOffset = [0x7d20, 0x18];
    private const int _healthTypeOffset = 0x8;
    private const int _staminaTypeOffset = 0xc;
    private const int _attackTypeOffset = 0x10;
    private const int _defenceTypeOffset = 0x14;
    private const int _elementalTypeOffset = 0x18;
    private const int _skillOffset = 0x1c;
    private bool _isFoodLocking;
    private FoodInfo? _foodInfo;
    private bool _waitingSwitchMap;

    #endregion

    public void OnLoad()
    {
        try
        {
            Logging.Name = Name;

            _acceptQuestHook = Hook.Create<AcceptQuest>(_acceptQuestCall, AcceptQuestHook);

            _abandonQuestHook = GetSPLQuestHook("AbandonQuestHook")!;
            _acceptQuestInternalHook = GetSPLQuestHook("AcceptQuestHook")!;
            _departOnQuestHook = GetSPLQuestHook("DepartOnQuestHook");
            _leaveQuestHook = GetSPLQuestHook("LeaveQuestHook");

            Logging.Info("loaded. \nFoodLock Hotkey: Ctrl + Alt + F4\nRestart Hotkey: Ctrl + Alt + F5");
            KeyBindings.AddKeybind(_foodHotkeyName, new Keybind<Key>(Key.F4, [Key.LeftControl, Key.LeftAlt]));
            KeyBindings.AddKeybind(_restartHotkeyName, new Keybind<Key>(Key.F5, [Key.LeftControl, Key.LeftAlt]));
        }
        catch (Exception e)
        {
            Logging.Error(e.ToString());
            throw;
        }
    }

    public void OnUnload()
    {
        _acceptQuestHook?.Dispose();
    }

    public void OnUpdate(float dt)
    {
        if (KeyBindings.IsPressed(_foodHotkeyName))
        {
            try
            {
                FoodLock();
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
        else if (KeyBindings.IsPressed(_restartHotkeyName))
        {
            try
            {
                Task.Run(Restart);
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }
        if (_waitingSwitchMap && GetMapTime() is >= 0f and <= 0.1f)
        {
            var time = GetMapTime();
            SetFood();
            _waitingSwitchMap = false;
        }
    }


    public void OnQuestLeave(int questId)
    {
        if (!_isFoodLocking) return;
        _waitingSwitchMap = true;
    }

    private static void AcceptQuestHook(nint ctx, int save, int unk)
    {
        _questCtx = ctx;
        _acceptQuestHook!.Original(ctx, save, unk);
    }

    private static MethodInfo? GetSPLQuestHook(string name)
    {
        return typeof(Quest).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
    }


    private static float? GetMapTime()
    {
        long[] addrs = [0x14500CDA0, 0x90, 0x46a0, 0x14];
        nint addr = 0;
        foreach (var offset in addrs.SkipLast(1))
        {
            addr = MemoryUtil.Read<nint>(addr + offset);
            if (addr == nint.Zero)
                return null;
        }
        return MemoryUtil.Read<float>(addr + addrs[^1]);
    }

    private FoodInfo? GetFoodInfo()
    {
        if (Player.MainPlayer is null)
            return null;
        try
        {
            nint addr = Player.MainPlayer.Instance;
            foreach (var offset in _foodBaseOffset)
                addr = MemoryUtil.Read<nint>(addr + offset);
            return new FoodInfo(
                healthType: MemoryUtil.Read<int>(addr + _healthTypeOffset),
                staminaType: MemoryUtil.Read<int>(addr + _staminaTypeOffset),
                attackType: MemoryUtil.Read<int>(addr + _attackTypeOffset),
                defenceType: MemoryUtil.Read<int>(addr + _defenceTypeOffset),
                elementalType: MemoryUtil.Read<int>(addr + _elementalTypeOffset),
                skill1: MemoryUtil.Read<int>(addr + _skillOffset),
                skill2: MemoryUtil.Read<int>(addr + _skillOffset + 4),
                skill3: MemoryUtil.Read<int>(addr + _skillOffset + 8)
            );
        }
        catch 
        {
            return null;
        }
    }


    private void SetFood()
    {
        if (_foodInfo is null || Player.MainPlayer is null) return;
        var info = _foodInfo.Value;
        nint addr = Player.MainPlayer.Instance;
        foreach (var offset in _foodBaseOffset)
            addr = MemoryUtil.Read<nint>(addr + offset);
        MemoryUtil.WriteBytes(addr + _healthTypeOffset, BitConverter.GetBytes(info.healthType));
        MemoryUtil.WriteBytes(addr + _staminaTypeOffset, BitConverter.GetBytes(info.staminaType));
        MemoryUtil.WriteBytes(addr + _attackTypeOffset, BitConverter.GetBytes(info.attackType));
        MemoryUtil.WriteBytes(addr + _defenceTypeOffset, BitConverter.GetBytes(info.defenceType));
        MemoryUtil.WriteBytes(addr + _elementalTypeOffset, BitConverter.GetBytes(info.elementalType));
        MemoryUtil.WriteBytes(addr + _skillOffset, BitConverter.GetBytes(info.skill1));
        MemoryUtil.WriteBytes(addr + _skillOffset + 4, BitConverter.GetBytes(info.skill2));
        MemoryUtil.WriteBytes(addr + _skillOffset + 8, BitConverter.GetBytes(info.skill3));
        Gui.DisplayMessage($"猫饭生效：\n{FoodSkillDic.GetName(info.skill1, true)} + {FoodSkillDic.GetName(info.skill2, true)} + {FoodSkillDic.GetName(info.skill3, true)}");
    }

    private void FoodLock()
    {
        if (!_isFoodLocking)
        {
            var info = GetFoodInfo();
            if (info is null || !info.Value.hasEaten)
            {
                Gui.DisplayMessage("先吃猫饭吧。");
                return;
            }
            _foodInfo = info;
            _isFoodLocking = true;
            Gui.DisplayMessage($"已锁定当前猫饭：\n{FoodSkillDic.GetName(info.Value.skill1,true)} + {FoodSkillDic.GetName(info.Value.skill2, true)} + {FoodSkillDic.GetName(info.Value.skill3, true)}");
        }
        else
        {
            _foodInfo=null;
            _isFoodLocking = false;
            Gui.DisplayMessage("已取消猫饭锁定。");
        }
    }

    private static (int state,int id)? ReadQuestCtxInfo()
    {
        if (_questCtx == nint.Zero)
            return null;
        var addr = MemoryUtil.Read<nint>(_questCtx);
        if (addr == nint.Zero)
            return null;
        var state = MemoryUtil.Read<int>(addr + _questCtxStateOffset);
        var id = MemoryUtil.Read<int>(addr + _questCtxIdOffset);
        return (state, id);
    }

    private static bool Reaccept()
    {
        if (Quest.QuestState != 0 || Quest.CurrentQuestId != -1)
            return false;
        var info = ReadQuestCtxInfo();
        if (info is { id: > 100 and < 70000, state: 3 })
            _acceptQuestHook!.Original(_questCtx, 0, 0);
        return true;
    }

    private async Task Restart()
    {
        if (Quest.QuestState == (uint)QuestState.InProgress
            && Quest.CurrentQuestId != -1)
        {
            var questId = Quest.CurrentQuestId;
            var campIndex = Quest.SingletonInstance.Get<int>(_questCampIndexOffset);
            _abandonQuestHook!.Invoke(null, [Quest.SingletonInstance.Instance, 0u]);
            await Task.Delay(100);
            unsafe
            {
                _restoreItems.Invoke(Quest.SingletonInstance.Instance);
            }
            _leaveQuestHook!.Invoke(null, [Quest.SingletonInstance.Instance]);
            await Task.Delay(100);
            if (Reaccept())
            {
                //_acceptQuestInternalHook!.Invoke(null,[Quest.SingletonInstance.Instance, questId, false]);
                await Task.Delay(1000);
                //Quest.SingletonInstance.Set(_questCampIndexOffset, campIndex);
                _departOnQuestHook!.Invoke(null, [Quest.SingletonInstance.Instance, false]);
            }
        }
        else if (Quest.QuestState == (uint)QuestState.Accepted && Quest.CurrentQuestId != -1)
        {
            _departOnQuestHook!.Invoke(null, [Quest.SingletonInstance.Instance, false]);
        }
        else
        {
            Reaccept();
        }
    }
}
