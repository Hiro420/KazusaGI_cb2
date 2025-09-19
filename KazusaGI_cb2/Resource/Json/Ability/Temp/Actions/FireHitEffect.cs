using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class FireHitEffect : BaseAction
    {
        [JsonProperty] public readonly string hitEntity;
        [JsonProperty] public readonly HitScene hitScene;

        public class HitScene
        {
            [JsonProperty] public readonly string defaultEffect;
            [JsonProperty] public readonly Dictionary<string, string> surfaceEffect;
        }
    }
}
