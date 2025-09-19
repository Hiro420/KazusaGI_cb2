using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class GetFightProperty : BaseAction
    {
        [JsonProperty] public readonly TargetType fightPropSourceTarget;
        [JsonProperty] public readonly FightPropType fightProp;
        [JsonProperty] public readonly string globalValueKey;
        [JsonProperty] public readonly bool doOffStage;
    }
}
