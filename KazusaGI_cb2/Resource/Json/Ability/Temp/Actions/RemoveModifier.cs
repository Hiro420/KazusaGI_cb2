using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class RemoveModifier : BaseAction
    {
        [JsonProperty] public readonly string modifierName;
    }
}
