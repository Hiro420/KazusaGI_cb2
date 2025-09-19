using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByLocalAvatarStamina : BasePredicate
{
    [JsonProperty] public readonly LogicType logic;
    [JsonProperty] public readonly object stamina;
}
