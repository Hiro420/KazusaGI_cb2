using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AttachModifier : BaseAction
    {
        [JsonProperty] public readonly string modifierName;
    }
}
