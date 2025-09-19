using Newtonsoft.Json;
using KazusaGI_cb2.Resource;
using Newtonsoft.Json.Converters;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class TriggerWeatherMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly WeatherTriggerType type;
        [JsonProperty] public readonly string weatherPattern;
        [JsonProperty] public readonly float transDuration;
        [JsonProperty] public readonly float duration;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum WeatherTriggerType
    {
		Area = 0,
		Weather = 1,
		Skill = 2
	}
}
