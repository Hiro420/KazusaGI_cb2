using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class FireAISoundEvent : BaseAction
    {
        [JsonProperty] public readonly float volume;
    }
}
