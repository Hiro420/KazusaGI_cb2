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

public class ClientGadgetEntity : GadgetEntity
{
	public uint StateBeginTime { get; private set; }
	public float Hp { get; private set; } = 1f;
	public float MaxHp { get; private set; } = 1f;
	private EvtCreateGadgetNotify gadgetNotify;

	public ClientGadgetEntity(Session session, uint gadgetId, EvtCreateGadgetNotify req, Vector3? position, Vector3? rotation, uint? entityId = null)
	: base(session, gadgetId, null, position, rotation, entityId)
	{
		gadgetNotify = req;
		OwnerEntityId = req.OwnerEntityId;
		StateBeginTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		abilityManager = new GadgetAbilityManager(this);
		abilityManager.Initialize();
	}

	protected override void BuildKindSpecific(SceneEntityInfo ret)
	{
		base.BuildKindSpecific(ret);
		ret.Gadget.ClientGadget = new ClientGadgetInfo()
		{
			CampId = gadgetExcel.campID,
			TargetEntityId = OwnerEntityId,
			Guid = gadgetNotify.Guid,
			OwnerEntityId = OwnerEntityId,
			AsyncLoad = gadgetNotify.IsAsyncLoad,
			CampType = gadgetNotify.CampType,
		};
	}

}
