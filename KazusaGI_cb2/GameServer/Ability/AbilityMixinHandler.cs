using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Base abstract class for handling ability mixins, similar to GC's AbilityMixinHandler.
/// Each specific mixin type should inherit from this class and implement the Execute method.
/// </summary>
public abstract class AbilityMixinHandler
{
    /// <summary>
    /// Execute the mixin logic with the given parameters
    /// </summary>
    /// <param name="ability">The ability that contains this mixin</param>
    /// <param name="mixin">The mixin data containing configuration</param>
    /// <param name="abilityData">Additional ability data from the invoke</param>
    /// <param name="source">The entity that owns/executes the ability</param>
    /// <param name="target">The target entity (can be null)</param>
    /// <returns>True if the mixin was executed successfully, false otherwise</returns>
    public abstract Task<bool> ExecuteAsync(
        ConfigAbility ability, 
        BaseAbilityMixin mixin, 
        Entity source, 
        Entity? target);
}

/// <summary>
/// Attribute to mark mixin handler classes with their corresponding mixin type.
/// This is used for automatic registration of handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AbilityMixinAttribute : Attribute
{
    public Type MixinType { get; }
    
    public AbilityMixinAttribute(Type mixinType)
    {
        MixinType = mixinType;
    }
}