using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AttackPatterns
{
    internal class ConfigAttackSphere : BaseAttackPattern
    {
        [JsonProperty] public readonly object radius;
        [JsonProperty] public readonly bool ignoreMassive;
    }
}
