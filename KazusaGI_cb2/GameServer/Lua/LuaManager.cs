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
                scriptLib.currentGroupId = (int)session.player!.Scene.GetGroupIdFromGroupInfo(group);
                scriptLib.currentEventArgs = args;
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
                    logger.LogSuccess($"Executed successfully LUA of type: {(TriggerEventType)trigger._event}");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error occured in executing Trigger Lua {ex.Message}");
                }
            }
        }
    }

    public static void executeTriggersLua(Session session, SceneGroupLua group, ScriptArgs args)
    {
        if (group == null) return;
        List<SceneTriggerLua> triggers = group.triggers.FindAll(t => t._event == args.eventTypeAsEnum());

        if (triggers.Count > 0)
        {
            foreach (SceneTriggerLua trigger in triggers)
            {

                executeTrigger(session, trigger, args, group);
            }
        }
    }

    public static string GetCommonScriptConfigAsLua()
    {
        var luaScript = "RegionShape = \n" + EnumToLua<RegionShape>() + "\n";
        luaScript += "EventType = \n" + EnumToLua<TriggerEventType>() + "\n";
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
