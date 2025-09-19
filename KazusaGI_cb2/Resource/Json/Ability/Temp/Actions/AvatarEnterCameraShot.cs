using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AvatarEnterCameraShot : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly string cameraMoveCfgPath;
    }
}
