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
