using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability
{
	/// <summary>
	/// Controller for managing an active ability modifier, equivalent to Grasscutter's AbilityModifierController
	/// </summary>
	public class AbilityModifierController
	{
		public InstancedAbility? Ability { get; }
		public AbilityData? AbilityData { get; }
		public AbilityModifier? ModifierData { get; }
		
		public AbilityModifierController(InstancedAbility? ability, AbilityData? abilityData, AbilityModifier? modifierData)
		{
			Ability = ability;
			AbilityData = abilityData;
			ModifierData = modifierData;
		}
		
		public override string ToString()
		{
			return $"AbilityModifierController(Ability={Ability?.Data?.AbilityName}, ModifierData={ModifierData})";
		}
	}

	/// <summary>
	/// Represents an instanced ability, equivalent to Grasscutter's Ability class
	/// </summary>
	public class InstancedAbility
	{
		public AbilityData? Data { get; }
		public Dictionary<string, float> AbilitySpecials { get; } = new();
		
		public InstancedAbility(AbilityData? data)
		{
			Data = data;
		}
	}

	/// <summary>
	/// Represents ability data configuration, equivalent to Grasscutter's AbilityData class  
	/// </summary>
	public class AbilityData
	{
		public string? AbilityName { get; }
		public Dictionary<string, AbilityModifier>? Modifiers { get; }
		
		public AbilityData(string? abilityName, Dictionary<string, AbilityModifier>? modifiers)
		{
			AbilityName = abilityName;
			Modifiers = modifiers;
		}
	}
}