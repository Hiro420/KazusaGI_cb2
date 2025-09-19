using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class ResetAnimatorTrigger : BaseAction
{
    [JsonProperty] public readonly TriggerID triggerID;
}
