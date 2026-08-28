using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	public class TriggerSetCastShadow : BaseAction
	{
		[JsonProperty("enable")]
		public bool Enable { get; set; }

		[JsonProperty("target")]
		public string? Target { get; set; }
	}
}