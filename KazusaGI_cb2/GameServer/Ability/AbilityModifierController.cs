using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityModifierController
{
    public Ability Ability { get; private set; }
    public ConfigAbility AbilityData { get; private set; }
    public ConfigAbilityModifier ModifierData { get; private set; }
    
    public AbilityModifierController(Ability ability, ConfigAbility abilityData, ConfigAbilityModifier modifierData)
    {
        Ability = ability;
        AbilityData = abilityData;
        ModifierData = modifierData;
    }
}