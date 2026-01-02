using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer;

public class NpcEntity : Entity
{
	public NpcLua? _npcInfo;
	public uint _npcId;

	public NpcEntity(Session session, uint npcId, NpcLua? npcInfo, Vector3? position = null, Vector3? rotation = null)
		: base(session, position, rotation, ProtEntityType.ProtEntityNpc)
	{
		_npcId = npcId;
		_npcInfo = npcInfo;
		Position = _npcInfo?.pos ?? Position;
	}

	protected override void BuildKindSpecific(SceneEntityInfo ret)
	{
		ret.Npc = new SceneNpcInfo
		{
			NpcId = _npcId,
			RoomId = 0,
			ParentQuestId = 0,
			BlockId = _npcInfo?.block_id ?? 0
		};
	}
}
