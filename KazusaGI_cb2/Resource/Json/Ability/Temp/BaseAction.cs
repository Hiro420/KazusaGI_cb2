using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using System.Reflection;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAction : IInvocation
{
    /// <summary>
    /// LocalID assigned by ConfigAbility during OnBakeLoaded.
    /// Corresponds to the index in invokeSites list.
    /// </summary>
    public int LocalID { get; set; } = -1;

    public virtual async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {

    }

    /// <summary>
    /// Get all BaseAction[] fields/properties from this action.
    /// Used by _IterateSubActions to discover nested actions.
    /// </summary>
    public virtual void GetSubActions(List<BaseAction[]> outList)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = GetType();

        foreach (var field in type.GetFields(flags))
        {
            if (field.FieldType == typeof(BaseAction[]))
            {
                if (field.GetValue(this) is BaseAction[] arr && arr.Length > 0)
                {
                    outList.Add(arr);
                }
            }
        }

        foreach (var prop in type.GetProperties(flags))
        {
            if (prop.CanRead && prop.PropertyType == typeof(BaseAction[]))
            {
                if (prop.GetValue(this) is BaseAction[] arr && arr.Length > 0)
                {
                    outList.Add(arr);
                }
            }
        }
    }

    /// <summary>
    /// Check if this action requires server-side processing.
    /// Used by ResolveModifierMPBehavior.
    /// Override in specific action types that need server processing.
    /// </summary>
    public virtual bool CheckActionNeedServer()
    {
        return false;
    }
}
