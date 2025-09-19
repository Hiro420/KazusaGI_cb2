using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    internal class ConfigBornByAttachPoint : BaseBornType
    {
        [JsonProperty] public readonly string attachPointName;
    }
}
