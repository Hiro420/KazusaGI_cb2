using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KazusaGI_cb2.Resource.Excel;

public class SceneExcelConfig
{
	public uint id { get; set; }
	public uint nameTextMapHash { get; set; }
	public SceneType type { get; set; }
	public string scriptData { get; set; }
	public string levelEntityConfig { get; set; }
	public List<uint> specifiedAvatarList { get; set; }
	public string comment { get; set; }
	public uint maxSpecifiedAvatarNum { get; set; }
}

public enum SceneType
{
	SCENE_DUNGEON,
	SCENE_WORLD,
	SCENE_ROOM
}