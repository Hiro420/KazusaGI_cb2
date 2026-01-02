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
		_gadgetId = gadgetId;
		level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
		gadgetExcel = MainApp.resourceManager.GadgetExcel[gadgetId];
		serverExcelConfig = MainApp.resourceManager.ServerGadgetRows.First(i => i.Id == gadgetId)!;
		isEnableInteract = gadgetExcel.isInteractive;
		gadgetNotify = req;
		OwnerEntityId = req.OwnerEntityId;

		if (_gadgetLua != null && _gadgetLua.owner != 0)
		{
			// Find owner gadget Id from lua config
			var ownerGadget = session.player.Scene.EntityManager.Entities.Values
				.OfType<GadgetEntity>()
				.FirstOrDefault(e => e._gadgetLua != null
					&& e._gadgetLua.group_id == _gadgetLua.group_id
					&& e._gadgetLua.config_id == _gadgetLua.owner);
		}

		if (
			!MainApp.resourceManager.ConfigGadgetMap.TryGetValue(gadgetExcel.jsonName, out configGadget) || configGadget == null)
		{
			// should not happen
			if (!string.IsNullOrEmpty(gadgetExcel.jsonName))
				session.c.LogWarning($"{gadgetExcel.jsonName} does not exist in binoutput");
		}
		else
		{
			var combatdata = configGadget.combat;
			if (combatdata != null && combatdata.property != null)
			{
				MaxHp = combatdata.property.HP;
				Hp = MaxHp;
			}
		}

		// Initialize state begin time similarly to Gadget::state_begin_time_
		StateBeginTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		abilityManager = new GadgetAbilityManager(this);
		InitAbilityStuff();
		abilityManager.Initialize();
	}

	protected override void BuildKindSpecific(SceneEntityInfo ret)
	{
		ret.Name = gadgetExcel.jsonName;

		// Determine born type similarly to hk4e's Gadget::toClient:
		// 1) Prefer the script-configured born_type if present.
		// 2) If none, derive from GadgetExcelConfig.type.
		var bornType = _gadgetLua?.born_type ?? GadgetBornType.GadgetBornNone;
		if (bornType == GadgetBornType.GadgetBornNone)
		{
			var type = gadgetExcel.type;
			// In hk4e, when born_type_ is 0:
			//   if (type <= 24) born_type = (type >= 23) ? 1 : 0; // IN_AIR for AirflowField/SpeedupField
			//   else if (type == 34) born_type = 6;               // GROUND for EnvAnimal
			if (type <= GadgetType_Excel.SpeedupField)
			{
				if (type >= GadgetType_Excel.AirflowField)
					bornType = GadgetBornType.GadgetBornInAir;
			}
			else if (type == GadgetType_Excel.EnvAnimal)
			{
				bornType = GadgetBornType.GadgetBornGround;
			}
		}

		var info = new SceneGadgetInfo
		{
			AuthorityPeerId = session.player!.PeerId,
			GadgetState = (uint)state,
			IsEnableInteract = isEnableInteract,
			ConfigId = _gadgetLua?.config_id ?? 0,
			GroupId = _gadgetLua?.group_id ?? 0,
			GadgetId = _gadgetId,
			BornType = bornType,
			GadgetType = (uint)(_gadgetLua?.type ?? 0)
		};

		// showcutscene in gadget lua directly maps to SceneGadgetInfo.IsShowCutscene.
		info.IsShowCutscene = _gadgetLua?.showcutscene ?? false;

		info.ClientGadget = new ClientGadgetInfo()
		{
			CampId = gadgetExcel.campID,
			TargetEntityId = OwnerEntityId,
			Guid = gadgetNotify.Guid,
			OwnerEntityId = OwnerEntityId,
			AsyncLoad = gadgetNotify.IsAsyncLoad,
			CampType = gadgetNotify.CampType,
		};

		ret.Gadget = info;
	}

}
