using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using NLua;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KazusaGI_cb2.Utils.ENet;

namespace KazusaGI_cb2.GameServer.Lua;

public class LuaManager
{
    static Logger logger = new Logger("LuaManager");

    private static bool IsRegionEventMatch(SceneTriggerLua trigger, ScriptArgs evt)
    {
        // Mirrors hk4e's Group::isRegionEventMatch semantics using our
        // ScriptArgs layout. trigger.source is the Lua source string
        // field from the trigger config.
        string sourceName = trigger.source ?? string.Empty;

        // Empty source string: default to Avatar-only region events.
        if (sourceName.Length == 0)
        {
            return evt.param2 == (int)EntityType.Avatar;
        }

        // "0" means match all entity types.
        if (sourceName == "0")
        {
            return true;
        }

        // Otherwise, treat source as numeric ProtEntityType id and
        // require evt.param2 to match it.
        if (!int.TryParse(sourceName, out int scriptEntityType))
        {
            logger.LogWarning($"[LuaManager] isRegionEventMatch: failed to parse source '{sourceName}' as int");
            return false;
        }

        if (!Enum.IsDefined(typeof(EntityType), scriptEntityType))
        {
            logger.LogWarning($"[LuaManager] isRegionEventMatch: invalid EntityType value '{scriptEntityType}' from source '{sourceName}'");
            return false;
        }

        return evt.param2 == scriptEntityType;
    }

    private static bool IsTriggerEventMatch(SceneTriggerLua trigger, ScriptArgs evt)
    {
        if (trigger == null || evt == null)
            return false;

        var evtType = evt.eventTypeAsEnum();

        // Region events delegate to isRegionEventMatch.
        if (evtType == EventType.EVENT_ENTER_REGION || evtType == EventType.EVENT_LEAVE_REGION)
        {
            return IsRegionEventMatch(trigger, evt);
        }

        string sourceName = trigger.source ?? string.Empty;
        if (sourceName.Length > 0)
        {
            string evtSource = evt.source ?? string.Empty;
            if (evtSource.Length != sourceName.Length)
                return false;

            // Exact match on source string.
            return string.Equals(evtSource, sourceName, StringComparison.Ordinal);
        }

        // No source configured on the trigger: match any event of the
        // right type.
        return true;
    }
    public static void executeTrigger(Session session, SceneTriggerLua trigger, ScriptArgs args, SceneGroupLua? group = null)
    {
        if (group == null)
        {
            group = session.player!.Scene.GetGroup(args.group_id);
        }

        if (group != null)
        {
            using (NLua.Lua groupLua = new NLua.Lua())
            {
                ScriptLib scriptLib = new(session);
                scriptLib.currentSession = session;
                var scene = session.player!.Scene;
                scriptLib.currentGroupId = (int)scene.GetGroupIdFromGroupInfo(group);
                scriptLib.currentEventArgs = args;
                // Mirror hk4e's per-trigger trigger_count_ tracking. Lua
                // should see the current (pre-increment) count via
                // ScriptLib.GetCurTriggerCount, and only after the trigger
                // fires successfully do we bump the stored counter.
                scene.BeginTriggerExecution(group, trigger);
                groupLua["ScriptLib"] = scriptLib;
                groupLua["context_"] = session;
                groupLua["evt_"] = args.toTable();

                ResourceLoader resourceLoader = MainApp.resourceManager.loader;
                string luaFile = GetCommonScriptConfigAsLua() + "\n"
                        + GetConfigEntityTypeEnumAsLua() + "\n"
                        + GetConfigEntityEnumAsLua() + "\n"
                        + File.ReadAllText(MainApp.resourceManager.GetLuaStringFromGroupId((uint)args.group_id));

                groupLua.DoString(luaFile.Replace("ScriptLib.", "ScriptLib:"));

                string luaScript;
                try
                {
                    // Mirror hk4e trigger semantics as closely as possible:
                    //  - condition & action: if condition(...) then return action(...) end
                    //  - no condition, action only: return action(...)
                    //  - condition only: return condition(...) (fallback for imperfect resources)
                    //  - neither: skip

                    bool hasCondition = !string.IsNullOrEmpty(trigger.condition);
                    bool hasAction = !string.IsNullOrEmpty(trigger.action);

                    if (!hasCondition && !hasAction)
                    {
                        logger.LogError("Trigger has neither condition nor action; skipping.");
                        return;
                    }
                    else if (!hasCondition && hasAction)
                    {
                        // Same as hk4e: unconditional action.
                        luaScript = @$"return {trigger.action}(context_, evt_)";
                    }
                    else if (hasCondition && !hasAction)
                    {
                        // hk4e generally doesn't ship condition-only triggers, but
                        // when our resources do, run the condition so any ScriptLib
                        // side effects still happen.
                        luaScript = @$"return {trigger.condition}(context_, evt_)";
                    }
                    else // hasCondition && hasAction
                    {
                        luaScript = @$"
                                    if {trigger.condition}(context_, evt_) then
                                        return {trigger.action}(context_, evt_)
                                    end
                            ";
                    }

                    groupLua.DoString(luaScript);
                        scene.EndTriggerExecution(group, trigger);
                    logger.LogSuccess($"Executed successfully LUA of type: {(EventType)trigger._event}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error occured in executing Trigger Lua {ex.Message}");
                        // On error, mirror hk4e's behavior of not
                        // advancing trigger_count for this trigger but
                        // still restoring the outer ScriptContext when
                        // nested calls return.
                        scene.AbortTriggerExecution();
                }
            }
        }
    }

    public static void executeTriggersLua(Session session, SceneGroupLua group, ScriptArgs args)
    {
        if (group == null) return;
        var player = session.player;
        var scene = player?.Scene;
        if (scene == null) return;

        var evtType = args.eventTypeAsEnum();

        foreach (SceneTriggerLua trigger in group.triggers)
        {
            if (trigger == null)
                continue;

            if (trigger._event != evtType)
                continue;

            // First, apply hk4e's Group::isTriggerEventMatch logic
            // (source / region matching).
            if (!IsTriggerEventMatch(trigger, args))
                continue;

            // Then apply the max trigger count cap from the Lua
            // trigger config (trigger_count field). 0 means
            // unlimited; positive values cap the number of times the
            // trigger is allowed to fire.
            uint maxCount = trigger.trigger_count;
            if (maxCount != 0)
            {
                int curCount = scene.GetTriggerExecutionCount(group, trigger);
                if (curCount >= maxCount)
                    continue;
            }

            executeTrigger(session, trigger, args, group);
        }
    }

    public static string GetCommonScriptConfigAsLua()
    {
        var luaScript = "RegionShape = \n" + EnumToLua<RegionShape>() + "\n";
        luaScript += "EventType = \n" + EnumToLua<EventType>() + "\n";
        luaScript += "GadgetType = " + EnumToLua<GadgetType_Lua>() + "\n";
        luaScript += "ElementType = \n" + EnumToLua<ElementType>() + "\n";
        luaScript += "GroupKillPolicy = \n" + EnumToLua<GroupKillPolicy>() + "\n";
        return luaScript;
    }

    public static string GetConfigEntityTypeEnumAsLua()
    {
        var luaScript = "EntityType = \n" + EnumToLua<EntityType>() + "\n";
        return luaScript;
    }

    public static string GetConfigEntityEnumAsLua()
    {
        var luaScript = "GadgetState = \n" + EnumToLua<GadgetState>() + "\n";
        luaScript += "GearType = \n" + EnumToLua<GearType>() + "\n";
        return luaScript;
    }

    public static string EnumToLua<T>() where T : Enum
    {
        var enumType = typeof(T);
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValues(enumType);
        var luaTable = "{\n";
        for (int i = 0; i < enumNames.Length; i++)
        {
            luaTable += $"\t{enumNames[i]} = {Convert.ToInt32(enumValues.GetValue(i))},\n";
        }
        luaTable += "}\n";
        return luaTable;
    }
}
