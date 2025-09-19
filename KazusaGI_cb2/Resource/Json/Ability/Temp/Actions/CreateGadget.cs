using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class CreateGadget : BaseAction
    {
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly int gadgetID;
        [JsonProperty] public readonly TargetType campTargetType;
    }
}
