using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class CameraBlurMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly Blur cameraRadialBlur;

        public class Blur
        {
            [JsonProperty] public readonly float power;
            [JsonProperty] public readonly float fadeTime;
        }
    }
}
