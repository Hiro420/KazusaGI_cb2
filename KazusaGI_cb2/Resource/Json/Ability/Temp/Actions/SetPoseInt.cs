using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetPoseInt : BaseAction
    {
        [JsonProperty] public readonly string intID;
        [JsonProperty] public readonly int value;
    }
}