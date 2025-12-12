using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.WebServer.Handlers;

public class ShieldApiLoginReq
{
	public string account { get; set; }
	public string password { get; set; }
	public bool is_crypto { get; set; }
}

public class ShieldApiVerifyReq
{
	public string token { get; set; }
	public string uid { get; set; }
}


public class ComboGranterLoginLoginReq
{
	public string data;
	public string app_id;
	public string channel_id;
	public string device;
	public string sign;

	public class DataInfo 
	{
		public string uid;
		public bool guest;
		public string token;
	}

	public DataInfo GetData()
	{
		return Newtonsoft.Json.JsonConvert.DeserializeObject<DataInfo>(data)!;
	}
}