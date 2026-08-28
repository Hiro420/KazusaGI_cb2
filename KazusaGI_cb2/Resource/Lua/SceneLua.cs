using System.Numerics;

namespace KazusaGI_cb2.Resource.Excel;

public class SceneLua
{
	public SceneConfig scene_config;
	public List<int> blocks;
	public List<BlockRect> block_rects;
	public List<string> dummy_points;
	public List<string> routes_config;

	// custom, inlined
	public Dictionary<int, SceneBlockLua> scene_blocks;
}

public class SceneConfig
{
	public Vector3 begin_pos;
	public Vector3 size;
	public Vector3 born_pos;
	public Vector3 born_rot;
	public int die_y;
}

public class BlockRect
{
	public Vector3 min;
	public Vector3 max;
}