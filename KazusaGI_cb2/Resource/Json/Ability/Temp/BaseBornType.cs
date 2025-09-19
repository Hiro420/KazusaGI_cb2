using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseBornType
{
    [JsonProperty] public readonly OffSet? offset;
    public class OffSet
    {
        [JsonProperty] public readonly float x;
        [JsonProperty] public readonly float y;
        [JsonProperty] public readonly float z;
    }
}
