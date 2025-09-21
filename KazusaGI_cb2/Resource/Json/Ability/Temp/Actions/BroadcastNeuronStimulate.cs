using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class BroadcastNeuronStimulate : BaseAction
{
    [JsonProperty] public string? neuronType;
    [JsonProperty] public float? stimulateValue;
}