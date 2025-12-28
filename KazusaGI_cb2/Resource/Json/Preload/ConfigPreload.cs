using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json.Preload;

public class ConfigPreload
{
	public PreloadInfo commonPreload = new();
	public Dictionary<uint, PreloadInfo> entitiesPreload = new();
}
