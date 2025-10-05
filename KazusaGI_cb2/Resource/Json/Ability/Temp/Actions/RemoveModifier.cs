using Newtonsoft.Json;
using System.Threading.Tasks;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer;
using System.Collections.Generic;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class RemoveModifier : BaseAction
    {
        [JsonProperty] public readonly string modifierName;

        public override async Task Invoke(string abilityName, Entity srcEntity, Entity targetEntity = null)
        {
            var applyEntity = targetEntity ?? srcEntity;
            if (applyEntity == null)
                return;

            var owner = srcEntity;
            var abilityManager = owner?.abilityManager as AbilityManager;
            if (abilityManager == null)
                return;

            // Find active modifiers on entity that match the modifier name and remove them.
            var active = abilityManager.GetActiveModifiers();
            var toRemove = new List<int>();
            foreach (var kv in active)
            {
                var info = kv.Value;
                if (info.ApplyEntityId == applyEntity._EntityId && info.ParentAbilityName == abilityName)
                {
                    // Attempt to match modifier name via the ability config
                    if (abilityManager.TryGetAbilityByName(info.ParentAbilityName ?? string.Empty, out var cfg))
                    {
                        var mod = cfg.GetModifierByName(modifierName);
                        if (mod != null)
                        {
                            toRemove.Add(kv.Key);
                        }
                    }
                }
            }

            foreach (var id in toRemove)
            {
                abilityManager.RemoveActiveModifierByLocalId(id);
            }
        }
    }
}
