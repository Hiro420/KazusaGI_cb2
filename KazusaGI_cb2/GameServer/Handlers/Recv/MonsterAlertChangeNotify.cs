using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KazusaGI_cb2.Utils.ENet;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleMonsterAlertChangeNotify
{
    [Packet.PacketCmdId(PacketId.MonsterAlertChangeNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		var req = packet.DecodeBody<MonsterAlertChangeNotify>();

		var entityManager = session.player?.Scene?.EntityManager;
		if (entityManager == null)
			return;

		var entities =
			req.MonsterEntityLists
			   .Select(eid => entityManager.TryGet(eid, out Entity? ent) ? ent : null)
			   .Where(ent => ent != null);

		foreach (var entity in entities)
		{
			if (entity is not MonsterEntity monsterEntity)
				continue;
			if (monsterEntity._monsterInfo == null)
				continue;
			ScriptArgs args = new ScriptArgs(
				(int)monsterEntity._monsterInfo.group_id, 
				(int)Lua.EventType.EVENT_MONSTER_BATTLE, 
				(int)monsterEntity._monsterInfo.config_id
			);
			// todo: executeTrigger
		}
	}

}