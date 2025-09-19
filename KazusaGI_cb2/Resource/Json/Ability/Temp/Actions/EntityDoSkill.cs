using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EntityDoSkill : BaseAction
    {
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly bool isHold;
    }
}
