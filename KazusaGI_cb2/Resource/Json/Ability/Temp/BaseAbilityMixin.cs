using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using System.Reflection;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// Base type for all ability mixins. In hk4e, mixins participate in the same
/// invokeSites list as actions; here we treat each mixin instance as an
/// IInvocation that ConfigAbility will append to invokeSites.
/// </summary>
public abstract class BaseAbilityMixin : IInvocation
{
    /// <summary>
    /// LocalID assigned by ConfigAbility during OnBakeLoaded.
    /// Corresponds to the index in invokeSites list.
    /// </summary>
    public int LocalID { get; set; } = -1;

    public virtual async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        await Task.Yield();
    }

    /// <summary>
    /// Get all BaseAction[] fields/properties from this mixin.
    /// Used by iteration methods to discover nested actions.
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
    /// Check if this mixin requires server-side processing.
    /// Used by ResolveModifierMPBehavior.
    /// Override in specific mixin types that need server processing.
    /// </summary>
    public virtual bool NeedServer()
    {
        return false;
    }
}
