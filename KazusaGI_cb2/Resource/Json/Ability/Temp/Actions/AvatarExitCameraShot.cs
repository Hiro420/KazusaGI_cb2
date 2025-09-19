using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AvatarExitCameraShot : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
    }
}
