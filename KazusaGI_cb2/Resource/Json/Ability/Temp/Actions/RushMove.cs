using Newtonsoft.Json;
using System.Numerics;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class RushMove : BaseAction
    {
        [JsonProperty] public readonly float animatorStateIDs;
        [JsonProperty] public readonly float cdReduce;
        [JsonProperty] public readonly float velocity;
        [JsonProperty] public readonly Vector3 velocityForce;
        [JsonProperty] public readonly bool checkAnimatorStateOnExitOnly;
    }
}