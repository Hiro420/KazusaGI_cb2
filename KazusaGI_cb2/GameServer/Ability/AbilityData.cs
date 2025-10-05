using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Collections.Concurrent;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityData
{
    public string? AbilityName { get; set; }
    public ConcurrentDictionary<uint, AbilityModifier>? Modifiers { get; set; }
    public bool IsDynamicAbility { get; set; }
    public Dictionary<string, float>? AbilitySpecials { get; set; }

    public AbilityData() { }
}
