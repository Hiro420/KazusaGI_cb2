using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Resource;
using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class CreateGadget : BaseAction
{
	[JsonProperty] public readonly BaseBornType born;
	[JsonProperty] public readonly int gadgetID;
	[JsonProperty] public readonly TargetType campTargetType;
	[JsonProperty] public readonly int? campID;
	// bool byServer

	public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
	{
		System.Numerics.Vector3 bornPos = GadgetEntity.ResolveBornPosition(srcEntity, born);
		GadgetEntity gadget = new GadgetEntity(srcEntity.session, (uint)gadgetID, null, bornPos, null);
		if (targetEntity != null)
		{
			gadget.OwnerEntityId = targetEntity._EntityId;
		}
		srcEntity.session.player!.Scene.EntityManager.Add(gadget);
		await Task.CompletedTask;
	}
}