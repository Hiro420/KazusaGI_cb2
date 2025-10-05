using System.Collections.Generic;
using System.Linq;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability
{
    public class Ability
    {
        public uint InstancedId { get; private set; }
        public ConfigAbility Data { get; private set; }
        public Dictionary<uint, IInvocation> LocalIdToInvocationMap { get; } = new();
        public List<AbilityModifierController> Modifiers { get; } = new();

        public Ability(uint instancedId, ConfigAbility data)
        {
            InstancedId = instancedId;
            Data = data;
            if (data.LocalIdToInvocationMap != null)
            {
                foreach (var kv in data.LocalIdToInvocationMap)
                    LocalIdToInvocationMap[kv.Key] = kv.Value;
            }
        }

        public void AttachInvocation(uint localId, IInvocation invocation)
        {
            LocalIdToInvocationMap[localId] = invocation;
        }

        public void AddModifierController(AbilityModifierController controller)
        {
            Modifiers.Add(controller);
        }

        public bool RemoveModifierByLocalId(int localId)
        {
            var m = Modifiers.FirstOrDefault(x => x.InstancedModifierId == localId || (int?)x.InstancedModifierId == localId);
            if (m != null)
            {
                Modifiers.Remove(m);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            LocalIdToInvocationMap.Clear();
            Modifiers.Clear();
        }
    }
}