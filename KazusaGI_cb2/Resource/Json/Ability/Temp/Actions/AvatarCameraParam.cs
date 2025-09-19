using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AvatarCameraParam : BaseAction
    {
        [JsonProperty] public readonly CamParam cameraParam;

        public class CamParam
        {
            [JsonProperty] public readonly float forceRadius;
            [JsonProperty] public readonly float forceRadiusDuration;
            [JsonProperty] public readonly bool shouldKeepForceRadius;
        }
    }
}
