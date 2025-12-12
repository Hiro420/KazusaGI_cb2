using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class Randomed : BaseAction
    {
        [JsonProperty] public readonly object chance;
        [JsonProperty] public readonly BaseAction[] successActions;
        [JsonProperty] public readonly BaseAction[] failActions;

		public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
		{
			await Task.Yield();

			int chance = Convert.ToInt32(this.chance);
			Random rand = new Random();

			// chance is between 0 and 1
			if (rand.NextDouble() <= chance)
			{
				// Success
				foreach (var action in successActions)
				{
					await action.Invoke(abilityName, srcEntity, targetEntity);
				}
			}
			else
			{
				// Fail
				foreach (var action in failActions)
				{
					await action.Invoke(abilityName, srcEntity, targetEntity);
				}
			}
		}
	}
}
