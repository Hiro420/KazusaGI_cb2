using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ExecuteGadgetLua : BaseAction
	{
		[JsonProperty] public readonly string param1;
		[JsonProperty] public readonly string param2;
	}
}
