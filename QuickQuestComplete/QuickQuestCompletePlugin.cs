using Commmon;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.IO;
using System.Reflection;

namespace QuickQuestComplete;

public class QuickQuestCompletePlugin : IPlugin
{
    public string Name => "QuickQuestComplete";
    public string Author => "Jelly";

    private const string _hotkeyName = "QuickQuestComplete.Hotkey";
    private DateTime _lastTryTime = DateTime.MinValue;
    private bool _initialized = false;
    private Delegate? _endQuestFunc;
    private object? _completeReason;

    public void OnLoad()
    {
        Logging.Name = Name;
        var success = InitializeEndQuest();
        if (!success)
        {
            Logging.Error("load failed.");
            return;
        }
        _initialized = true;
        Logging.Info("loaded. Hotkey: Ctrl + Alt + Home");
        KeyBindings.AddKeybind(_hotkeyName, new Keybind<Key>(Key.Home, [Key.LeftControl, Key.LeftAlt]));
    }
    
    private bool InitializeEndQuest()
    {
        var questType = typeof(Quest);

        var endQuestHook = questType
            .GetField("_endQuestHook", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null);

        if (endQuestHook?.GetType().GetProperty("Original")?.GetValue(endQuestHook)
            is not Delegate originalEndQuest)
        {
            Logging.Error("Failed to find _endQuestHook or Original.");
            return false;
        }

        var reasonType = questType.GetNestedType("QuestEndReason", BindingFlags.NonPublic);
        if (reasonType is null)
        {
            Logging.Error("Failed to find QuestEndReason.");
            return false;
        }

        if (!Enum.TryParse(reasonType, "Complete", out var reason) || reason is null)
        {
            Logging.Error("Failed to parse QuestEndReason.Complete.");
            return false;
        }

        _endQuestFunc = originalEndQuest;
        _completeReason = reason;
        return true;
    }

    public void OnUpdate(float dt)
    {
        if (_initialized && KeyBindings.IsPressed(_hotkeyName) && DateTime.Now - _lastTryTime > TimeSpan.FromSeconds(1))
        {
            _lastTryTime = DateTime.Now;
            try
            {
                EndQuest();
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
            
        }
    }

    private void EndQuest()
    {
        var questId = Quest.CurrentQuestId;
        if (questId < 0) return;
        var questState = (QuestState)Quest.QuestState; 
        ref var questEndTimer = ref Quest.QuestEndTimer;
        if (questState == QuestState.Completed
            && questEndTimer.Time > 0f 
            && questEndTimer.Active 
            && !questEndTimer.Ended())
        {
            questEndTimer.SetToEnd();
            Logging.Info($"skipped quest end countdown : id {questId}");
            return;
        }
        if (questState != QuestState.InProgress) return;
        var objs = Quest.Objectives;
        var changed = false;
        foreach (ref QuestTargetData obj in objs)
        {
            if (obj.Type is QuestTargetType.None || obj.Count == obj.RequiredCount) continue;
            obj.Count = obj.RequiredCount;
            changed = true;
        }
        if (!changed) return;
        _endQuestFunc!.DynamicInvoke(Quest.SingletonInstance.Instance, true, IntPtr.Zero, _completeReason);
        Logging.Info($"forced quest complete : id {questId}");
    }

}
