namespace KazusaGI_cb2.Resource.Json.Preload;

public class ConfigPreload
{
	public PreloadInfo commonPreload = new();
	public Dictionary<uint, PreloadInfo> entitiesPreload = new();
}
