using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class FireMonsterBeingHitAfterImage : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
    }
}
