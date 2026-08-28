using KazusaGI_cb2.Protocol;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

public class AbilityGadgetEntity : GadgetEntity
{
	public uint StateBeginTime { get; private set; }
	public float Hp { get; private set; } = 1f;
	public float MaxHp { get; private set; } = 1f;

	public AbilityGadgetEntity(Session session, uint gadgetId, Entity srcEntity, Vector3? position, Vector3? rotation, uint? entityId = null)
	: base(session, gadgetId, null, position, rotation, entityId)
	{
		OwnerEntityId = srcEntity._EntityId;
		StateBeginTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	protected override void BuildKindSpecific(SceneEntityInfo ret)
	{
		base.BuildKindSpecific(ret);
		ret.Gadget.AbilityGadget = new AbilityGadgetInfo()
		{
			CampId = gadgetExcel.campID,
			//CampTargetType = ???, todo: from CampExcelConfigData
			TargetEntityId = OwnerEntityId
		};
	}

}
