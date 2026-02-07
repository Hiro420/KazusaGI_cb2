using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Linq;
using KazusaGI_cb2.Resource.Json.Avatar;
using Newtonsoft.Json;
using System;
using System.IO;
using KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes;
using KazusaGI_cb2.Resource.ServerExcel;

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
