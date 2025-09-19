using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp
{
    internal abstract class BaseEventOp
    {
        [JsonProperty] public readonly Operation operation;

        public class Operation
        {
            [JsonProperty] public readonly string text;
        }
    }
}
