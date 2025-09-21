using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetPoseBool : BaseAction
    {
        [JsonProperty] public readonly string boolID;
        [JsonProperty] public readonly bool value;
    }
}