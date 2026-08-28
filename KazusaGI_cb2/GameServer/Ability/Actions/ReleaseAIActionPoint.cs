using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	public class ReleaseAIActionPoint : BaseAction
	{
		[JsonProperty("actionPoint")]
		public string? ActionPoint { get; set; }

		[JsonProperty("target")]
		public string? Target { get; set; }
	}
}