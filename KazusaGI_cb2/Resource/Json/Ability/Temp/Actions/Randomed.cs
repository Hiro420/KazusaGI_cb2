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

			float chance = Convert.ToSingle(this.chance);
			Random rand = new Random();

			// chance is between 0 and 1
			float roll = (float)rand.NextDouble();
			if (roll <= chance)
			{
				// Console.WriteLine($"Randomed action succeeded (chance: {chance} / {roll})");
				// Success
				foreach (var action in successActions)
				{
					// Console.WriteLine($"Invoking success action: {action.GetType().Name}");
					await action.Invoke(abilityName, srcEntity, targetEntity);
				}
			}
			else
			{
				// Console.WriteLine($"Randomed action failed (chance: {chance} / {roll})");
				// Fail
				foreach (var action in failActions)
				{
					await action.Invoke(abilityName, srcEntity, targetEntity);
				}
			}
		}
	}
}
