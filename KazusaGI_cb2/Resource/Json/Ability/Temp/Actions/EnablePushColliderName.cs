using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnablePushColliderName : BaseAction
    {
        [JsonProperty] public readonly string[] pushColliderNames;
        [JsonProperty] public readonly bool setEnable;
    }
}
