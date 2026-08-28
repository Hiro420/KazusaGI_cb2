using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class ResetAnimatorTrigger : BaseAction
{
	[JsonProperty] public readonly TriggerID triggerID;
}
