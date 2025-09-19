using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableHDMesh : BaseAction
    {
        [JsonProperty] public readonly bool enable;
        [JsonProperty] public readonly string hdMeshKey;
    }
}
