using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetAnimatorBool : BaseAction
    {
        [JsonProperty] public readonly string boolID;
    }
}
