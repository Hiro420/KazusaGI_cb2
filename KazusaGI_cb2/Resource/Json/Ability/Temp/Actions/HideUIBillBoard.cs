using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class HideUIBillBoard : BaseAction
    {
        [JsonProperty] public readonly bool hide;
    }
}
