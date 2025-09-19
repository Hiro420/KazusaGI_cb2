using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AttachEffect : BaseAction
    {
        [JsonProperty] public readonly string effectPattern;
    }
}
