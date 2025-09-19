using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class KillGadget : BaseAction
    {
        [JsonProperty] public readonly BaseSelectTargetType gadgetInfo;
    }
}
