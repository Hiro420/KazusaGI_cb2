using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAttackPattern
{
    [JsonProperty] public readonly TriggerType triggerType;
    [JsonProperty] public readonly BaseBornType born;
}
