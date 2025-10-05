using Newtonsoft.Json;
using System.Threading.Tasks;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ApplyModifier : BaseAction
    {
        [JsonProperty] public readonly string? target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly BaseSelectTargetType? otherTargets;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly BasePredicate[]? predicates;

        // When an ability action is invoked at runtime, this method will create and attach
        // the modifier according to the server-side BaseAbilityManager semantics.
        public override async Task Invoke(string abilityName, Entity srcEntity, Entity targetEntity = null)
        {
            //// Resolve target: prefer explicit apply target if present on the invocation's Ability (Set via AbilityInvokeEntry.ApplyEntityId path), else use provided targetEntity
            //var applyEntity = targetEntity ?? srcEntity;

            //if (applyEntity == null)
            //    return;

            //// Find the config ability instance on the source entity (owner). If none, try to add by name.
            //var owner = srcEntity;
            //var abilityManager = owner?.abilityManager as AbilityManager;
            //if (abilityManager == null)
            //    return;

            //// Try to get or create the ability instance by name
            //if (!abilityManager.TryGetAbilityByName(abilityName, out var ability))
            //{
            //    // Try adding the ability to owner if missing
            //    var added = abilityManager.TryAddAbilityByNameToOwner(abilityName);
            //    if (!added)
            //        return;
            //    // retrieve newly added ability
            //    abilityManager.TryGetAbilityByName(abilityName, out ability);
            //}

            //// Create a new instanced modifier controller
            //var modController = new AbilityModifierController(null, null, null);

            //// Generate a new instanced modifier id
            //var instancedModifierId = AbilityLocalIdGenerator.GenerateInstancedModifierId();

            //modController.Initialize(instancedModifierId, owner._EntityId, applyEntity._EntityId);
            //modController.Ability = ability;
            //modController.ModifierData = ability.Data.GetModifierByName(modifierName);

            //// Attach to ability and to the applied entity
            //ability.AddModifierController(modController);

            //// Attach to applyEntity's instanced modifiers map
            //lock (applyEntity.InstancedModifiers)
            //{
            //    applyEntity.InstancedModifiers[instancedModifierId] = modController;
            //}

            //// Add ActiveModifierInfo tracking in the manager
            //abilityManager.AddActiveModifierForEntity(applyEntity, ability, modController, modifierName);

            //// Execute onAdded actions if the modifier config has them
            //var modifierConfig = modController.ModifierData;
            //if (modifierConfig != null && modifierConfig.onAdded != null)
            //{
            //    foreach (var a in modifierConfig.onAdded)
            //    {
            //        await abilityManager.ExecuteActionAsync(ability.Data, a, null, applyEntity);
            //    }
            //}
        }
    }
}
