using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityModifierController
{
    public ConfigAbility? Ability { get; private set; }
    public ConfigAbility? AbilityData { get; private set; }
    public AbilityModifier? ModifierData { get; private set; }
    public uint InstancedModifierId { get; private set; }
    public uint OwnerEntityId { get; private set; }
    public uint ApplyEntityId { get; private set; }

    public AbilityModifierController(ConfigAbility? ability, ConfigAbility? abilityData, AbilityModifier? modifierData)
    {
        this.Ability = ability;
        this.AbilityData = abilityData;
        this.ModifierData = modifierData;
        this.InstancedModifierId = 0;
        this.OwnerEntityId = 0;
        this.ApplyEntityId = 0;
    }

    public void Initialize(uint instancedModifierId, uint ownerEntityId, uint applyEntityId)
    {
        this.InstancedModifierId = instancedModifierId;
        this.OwnerEntityId = ownerEntityId;
        this.ApplyEntityId = applyEntityId;
    }
}
