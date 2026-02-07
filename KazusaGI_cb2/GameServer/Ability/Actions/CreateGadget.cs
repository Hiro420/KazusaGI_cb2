using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using Newtonsoft.Json;
using ProtoBuf;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class CreateGadget : BaseAction
{
	[JsonProperty] public readonly BaseBornType born;
	[JsonProperty] public readonly int gadgetID;
	[JsonProperty] public readonly TargetType campTargetType;
	[JsonProperty] public readonly int? campID;
	[JsonProperty] public readonly bool byServer = false;

	public override async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
	{
		System.Numerics.Vector3 bornPos;
		AbilityActionCreateGadget createGadget;

		try
		{
			createGadget = Serializer.Deserialize<AbilityActionCreateGadget>(new MemoryStream(invoke.AbilityData));
		}
		catch (Exception)
		{
			srcEntity.session.c.LogError($"[CreateGadget] Failed to deserialize AbilityActionCreateGadget for ability {abilityName}");
			return;
		}

		if (byServer)
			bornPos = new System.Numerics.Vector3(createGadget.Pos.X, createGadget.Pos.Y, createGadget.Pos.Z);
		else
			bornPos = GadgetEntity.ResolveBornPosition(srcEntity, born);

		AbilityGadgetEntity gadget = new AbilityGadgetEntity(srcEntity.session, (uint)gadgetID, srcEntity, position: bornPos, rotation: null);
		if (targetEntity != null)
		{
			gadget.OwnerEntityId = targetEntity._EntityId;
		}
		SceneEntityAppearNotify appearNotify = new SceneEntityAppearNotify()
		{
			AppearType = Protocol.VisionType.VisionMeet
		};
		appearNotify.EntityLists.Add(gadget.ToSceneEntityInfo());
		srcEntity.session.SendPacket(appearNotify);
		srcEntity.session.player!.Scene.EntityManager.Add(gadget);
		await Task.CompletedTask;
	}
}