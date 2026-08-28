using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SetPoseFloat : BaseAction
	{
		[JsonProperty]
		public string? poseFloatName { get; set; }

		[JsonProperty]
		public object value { get; set; }

		[JsonProperty]
		public string? target { get; set; }
	}
}