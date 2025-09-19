using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ActCameraRadialBlur : BaseAction
    {
        [JsonProperty] public readonly Blur cameraRadialBlur;

        public class Blur
        {
            [JsonProperty] public readonly float power;
            [JsonProperty] public readonly float blurTime;
            [JsonProperty] public readonly float fadeTime;
        }
    }
}
