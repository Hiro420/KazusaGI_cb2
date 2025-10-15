using System.Collections.Concurrent;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityManager : BaseAbilityManager
{
    private static readonly Dictionary<Type, Func<ConfigAbility, BaseAction, byte[], Entity, Entity?, Task<bool>>> actionHandlers = new();
    private static readonly Dictionary<Type, Func<ConfigAbility, BaseAbilityMixin, byte[], Entity, Entity?, Task<bool>>> mixinHandlers = new();

    public static readonly TaskFactory EventExecutor = new(TaskScheduler.Default);

    public bool AbilityInvulnerable { get; private set; } = false;

    public AbilityManager(Entity owner) : base(owner)
    {
        // nothing here for now
    }

    // Implement abstract properties from BaseAbilityManager. Use resource container where available.
    public static Dictionary<uint, ConfigAbility> ConfigAbilityHashMap;

    // Ability special tables and state
    private static Dictionary<string, Dictionary<string, float>?>? _abilitySpecials = null;
    public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _abilitySpecials ??= new();
    private static HashSet<string>? _activeDynamicAbilities = null;
    public override HashSet<string> ActiveDynamicAbilities => _activeDynamicAbilities ??= new HashSet<string>();
    private static Dictionary<string, HashSet<string>>? _unlockedTalentParams = null;
    public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _unlockedTalentParams ??= new Dictionary<string, HashSet<string>>();

    public static void RegisterHandlers()
    {
        // In this translation we rely on BaseAbilityManager registering mixin handlers via attributes
    }

    public async Task ExecuteActionAsync(ConfigAbility ability, BaseAction action, byte[] abilityData, Entity? target)
    {
        // Find handler by action runtime type
        var type = action.GetType();
        if (actionHandlers.TryGetValue(type, out var handler))
        {
            try
            {
                await handler(ability, action, abilityData, Owner, target);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error executing ability action: {ex.Message}");
            }
        }
        else
        {
            // Default behaviour: execute the action's own Invoke implementation (most actions are IInvocation)
            try
            {
                await action.Invoke(ability.abilityName, Owner, target);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error invoking action {type.Name}: {ex.Message}");
            }
        }
    }

    public async Task ExecuteMixinAsync(ConfigAbility ability, BaseAbilityMixin mixin)
    {
        await base.ExecuteMixinAsync(ability, mixin, Owner);
    }

    public void AddAbilityToEntity(Entity entity, string name)
    {
        // Try to find ConfigAbility from this manager's ConfigAbilityHashMap by matching ability name
        uint hash = Utils.AbilityHash(name);
        if (ConfigAbilityHashMap.TryGetValue(hash, out var cfg))
        {
            AddAbilityToEntity(entity, cfg);
        }
        else
        {
            logger.LogWarning($"Ability config not found in manager: {name}");
        }
    }

    public void AddAbilityToEntity(Entity entity, ConfigAbility abilityData)
    {
        uint hash = Utils.AbilityHash(abilityData.abilityName);

        // Create runtime Ability instance and attach to entity list (1-based instance id = index+1)
        var abilityInstance = new Ability((uint)(entity.InstancedAbilities.Count + 1), abilityData);
        entity.InstancedAbilities.Add(abilityInstance);

        uint instancedId = (uint)entity.InstancedAbilities.Count; // 1-based
        InstanceToAbilityHashMap[instancedId] = hash;

        logger.LogInfo($"Added ability instance {instancedId} -> {abilityData.abilityName} to entity {entity._EntityId}");
    }

    /// <summary>
    /// Helper to add an ability by name to the owning entity (useful for quick smoke tests).
    /// </summary>
    public bool TryAddAbilityByNameToOwner(string abilityName)
    {
        uint hash = Utils.AbilityHash(abilityName);
        if (ConfigAbilityHashMap.TryGetValue(hash, out var cfg))
        {
            AddAbilityToEntity(Owner, cfg);
            return true;
        }
        return false;
    }
}
