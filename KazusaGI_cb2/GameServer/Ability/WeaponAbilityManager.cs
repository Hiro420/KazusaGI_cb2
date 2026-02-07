using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

public class WeaponAbilityManager : BaseAbilityManager
{
    private WeaponEntity _weapon => (WeaponEntity)Owner;

    public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();
    private readonly Dictionary<string, Dictionary<string, float>?> _abilitySpecials = new();
    private readonly HashSet<string> _activeDynamicAbilities = new();
    private readonly Dictionary<string, HashSet<string>> _unlockedTalentParams = new();
    public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _abilitySpecials;
    public override HashSet<string> ActiveDynamicAbilities => _activeDynamicAbilities;
    public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _unlockedTalentParams;

    public WeaponAbilityManager(Entity owner) : base(owner)
    {
        InitAbilities();
    }

    public override Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
    {
        return base.HandleAbilityInvokeAsync(invoke);
    }

    private void InitAbilities()
    {
        var resourceManager = MainApp.resourceManager;
        var abilityNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (uint affixId in _weapon.GetAffixMap().Keys)
        {
            if (affixId == 0)
                continue;
            var affixConfigs = resourceManager.EquipAffixExcel.Values
                .Where(e => e.AffixId == affixId && !string.IsNullOrWhiteSpace(e.OpenConfig))
                .Select(e => e.OpenConfig!)
                .ToList();
            foreach (var abilityName in affixConfigs)
                abilityNames.Add(abilityName);
        }

        var configAbilityMap = resourceManager.ConfigAbilityMap;
        if (configAbilityMap != null)
        {
            foreach (var abilityName in abilityNames)
            {
                if (!configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
                    container?.Default is not ConfigAbility configAbility)
                    continue;
                uint hash = Utils.AbilityHash(abilityName);
                ConfigAbilityHashMap[hash] = configAbility;
                if (!_abilitySpecials.ContainsKey(configAbility.abilityName))
                    _abilitySpecials[configAbility.abilityName] = BuildAbilitySpecials(configAbility);
            }
        }

        foreach (var ability in ConfigAbilityHashMap.Values)
        {
            if (ability != null)
                AddAbilityToEntity(_weapon, ability);
        }
    }

    private static Dictionary<string, float> BuildAbilitySpecials(ConfigAbility config)
    {
        var specials = new Dictionary<string, float>();
        if (config.abilitySpecials == null)
            return specials;
        foreach (var kvp in config.abilitySpecials)
        {
            if (TryReadSpecialValue(kvp.Value, out var value))
                specials[kvp.Key] = value;
        }
        return specials;
    }

    private static bool TryReadSpecialValue(object? valueObj, out float value)
    {
        switch (valueObj)
        {
            case null:
                value = 0f;
                return false;
            case float floatValue:
                value = floatValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case double doubleValue:
                value = (float)doubleValue;
                return true;
            case decimal decimalValue:
                value = (float)decimalValue;
                return true;
            case string stringValue:
                return float.TryParse(stringValue, out value);
            default:
                value = 0f;
                return false;
        }
    }
}
