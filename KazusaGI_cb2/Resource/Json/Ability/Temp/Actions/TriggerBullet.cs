using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerBullet : BaseAction
    {
        [JsonProperty] public readonly int bulletID;
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly BaseDirectionType direction;
    }
}
