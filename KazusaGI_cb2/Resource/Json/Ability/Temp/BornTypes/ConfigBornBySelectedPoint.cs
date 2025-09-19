using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    internal class ConfigBornBySelectedPoint : BaseBornType
    {
        [JsonProperty] public readonly bool onGround;
    }
}
