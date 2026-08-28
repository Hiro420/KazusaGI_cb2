using System.Numerics;

namespace KazusaGI_cb2.Resource.Excel;

public class MonsterLua
{
	public uint config_id;
	public uint monster_id;
	public uint level;
	public uint pose_id;
	public List<uint> affix = new List<uint>();
	public bool isElite;
	public Vector3 pos;
	public Vector3 rot;

	// custom fields
	public uint block_id;
	public uint group_id;
}