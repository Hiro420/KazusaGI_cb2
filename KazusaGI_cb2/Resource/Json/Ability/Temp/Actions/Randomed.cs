using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal sealed class Randomed : BaseAction
	{
		[JsonProperty("chance")]
		private object ChanceRaw
		{
			set => Chance = value switch
			{
				float f => f,
				double d => (float)d,
				long l => l,
				int i => i,
				string s when float.TryParse(s, out var f) => f,
				_ => 0f
			};
		}

		[JsonIgnore]
		public float Chance { get; private set; }

		[JsonProperty("successActions")]
		public BaseAction[] SuccessActions { get; init; } = [];

		[JsonProperty("failActions")]
		public BaseAction[] FailActions { get; init; } = [];

		public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
		{
			// Ensure chance is sane
			var chance = Math.Clamp(Chance, 0f, 1f);

			// Random.Shared is available on modern .NET and is thread-safe.
			var roll = (float)Random.Shared.NextDouble();

			var actions = roll <= chance ? SuccessActions : FailActions;

			foreach (var action in actions)
			{
				if (action is null) continue;
				await action.Invoke(abilityName, srcEntity, targetEntity).ConfigureAwait(false);
			}
		}
	}
}
