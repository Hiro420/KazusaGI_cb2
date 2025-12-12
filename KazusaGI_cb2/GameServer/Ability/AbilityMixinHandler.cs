using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        byte[] abilityData, 
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

/// <summary>
/// Registry that discovers and caches ability mixin handlers by their
/// associated config mixin type via the <see cref="AbilityMixinAttribute"/>.
/// </summary>
public static class AbilityMixinHandlerRegistry
{
    private static readonly Dictionary<Type, AbilityMixinHandler> Handlers;

    static AbilityMixinHandlerRegistry()
    {
        Handlers = new Dictionary<Type, AbilityMixinHandler>();

        try
        {
            Type handlerBaseType = typeof(AbilityMixinHandler);
            Type attributeType = typeof(AbilityMixinAttribute);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || !handlerBaseType.IsAssignableFrom(type))
                        continue;

                    var attr = type.GetCustomAttribute<AbilityMixinAttribute>();
                    if (attr?.MixinType == null)
                        continue;

                    if (Handlers.ContainsKey(attr.MixinType))
                        continue;

                    if (Activator.CreateInstance(type) is AbilityMixinHandler handler)
                    {
                        Handlers[attr.MixinType] = handler;
                    }
                }
            }
        }
        catch
        {
            // If reflection fails for any reason, leave the registry empty.
        }
    }

    public static AbilityMixinHandler? GetHandlerForMixin(BaseAbilityMixin mixin)
    {
        if (mixin == null)
            return null;

        Handlers.TryGetValue(mixin.GetType(), out var handler);
        return handler;
    }
}