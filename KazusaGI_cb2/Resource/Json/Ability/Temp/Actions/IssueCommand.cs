using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	[JsonObject(MemberSerialization.OptIn)]
	public class IssueCommand : BaseAction
	{
		[JsonProperty]
		public string? target { get; set; }

		[JsonProperty]
		public string? commandType { get; set; }

		[JsonProperty]
		public object? commandData { get; set; }

		[JsonProperty]
		public int priority { get; set; }
	}
}