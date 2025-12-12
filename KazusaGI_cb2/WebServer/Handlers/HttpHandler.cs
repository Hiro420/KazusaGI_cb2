using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Account;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.WebServer.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace KazusaGI_cb2.WebServer;

public class HttpHandler
{
    Config config = MainApp.config;
    public string GameConfig = """
    {
       "retcode":0,
       "message":"OK",
       "data":{
          "id":6,
          "game_key":"hk4e_cn",
          "client":"PC",
          "identity":"I_IDENTITY",
          "guest":true,
          "ignore_versions":"",
          "scene":"S_NORMAL",
          "name":"原神海外",
          "disable_regist":false,
          "enable_email_captcha":false,
          "thirdparty":[
             "fb",
             "tw"
          ],
          "disable_mmt":false,
          "server_guest":true,
          "thirdparty_ignore":{
             "tw":"",
             "fb":""
          },
          "enable_ps_bind_account":false,
          "thirdparty_login_configs":{
             "tw":{
                "token_type":"TK_GAME_TOKEN",
                "game_token_expires_in":604800
             },
             "fb":{
                "token_type":"TK_GAME_TOKEN",
                "game_token_expires_in":604800
             }
          }
       }
    }
    """;

    public string custom_config = """
    {
        "sdkenv": "2",
        "checkdevice": "False",
        "loadPatch": "False",
        "showexception": "False",
        "regionConfig": "pm|fk|add",
        "downloadMode": "0",
    }
    """;

    [HttpEndpoint("/mdk/shield/api/login", "POST")]
    public HttpResponse MdkShieldApiLogin(HttpListenerRequest request)
    {
		ShieldApiLoginReq apiLoginReq;

		using (var reader = new StreamReader(request.InputStream))
		{
			var body = reader.ReadToEnd();
			apiLoginReq = JsonConvert.DeserializeObject<ShieldApiLoginReq>(body)!;
		}

        // Resolve or create account in database based on provided account identifier.
        // We deliberately skip password verification here as requested.
        string accountUid = apiLoginReq.account;
        if (string.IsNullOrWhiteSpace(accountUid))
        {
            // Minimal error handling: client must provide some account identifier
            var errorRsp = new
            {
                retcode = -1,
                message = "Invalid account",
                data = (object?)null
            };
            return new JsonResponse(JsonConvert.SerializeObject(errorRsp));
        }

        // Generate a simple token for this login and persist it with the account.
        // Later, the game server will use this token in GetPlayerTokenReq.
        string token = $"{accountUid}_token"; // dummy token generation for now
		AccountRecord account = AccountManager.GetOrCreate(accountUid, token);

        // Build response payload following the original ACCOUNT_INFO shape,
        // but populated from the database-backed account.
        var rspObj = new
        {
            retcode = 0,
            message = "OK",
            data = new
            {
                account = new
                {
                    uid = account.AccountUid,
                    name = account.AccountUid,
                    email = "",
                    mobile = "",
                    is_email_verify = 1,
                    realname = "",
                    identity_card = "",
                    token = token,
                    safe_mobile = "",
                    facebook_name = "",
                    google_name = "",
                    twitter_name = "",
                    game_center_name = "",
                    apple_name = "",
                    sony_name = "",
                    tap_name = "",
                    country = "US",
                    reactivate_ticket = "",
                    area_code = "US",
                    device_grant_ticket = "",
                    steam_name = ""
                },
                device_grant_required = false,
                safe_mobile_required = false,
                realperson_required = false,
                realname_operation = "None"
            }
        };

        string json = JsonConvert.SerializeObject(rspObj);
        return new JsonResponse(json);
    }

    [HttpEndpoint("/mdk/shield/api/verify", "POST")]
    public HttpResponse MdkShieldApiVerify(HttpListenerRequest request)
    {
        ShieldApiVerifyReq apiVerifyReq;
        using (var reader = new StreamReader(request.InputStream))
        {
            var body = reader.ReadToEnd();
            apiVerifyReq = JsonConvert.DeserializeObject<ShieldApiVerifyReq>(body)!;
		}
		// Lookup account in database based on provided uid.
        AccountRecord? account = AccountManager.GetByAccountUid(apiVerifyReq.uid);
        if (account == null)
            {
            // Minimal error handling: account not found
            var errorRsp = new
            {
                retcode = -1,
                message = "Account not found",
                data = (object?)null
            };
            return new JsonResponse(JsonConvert.SerializeObject(errorRsp));
		}
		var rspObj = new
		{
			retcode = 0,
			message = "OK",
			data = new
			{
				account = new
				{
					uid = account.AccountUid,
					name = account.AccountUid,
					email = "",
					mobile = "",
					is_email_verify = 1,
					realname = "",
					identity_card = "",
					token = account.AccountToken,
					safe_mobile = "",
					facebook_name = "",
					google_name = "",
					twitter_name = "",
					game_center_name = "",
					apple_name = "",
					sony_name = "",
					tap_name = "",
					country = "US",
					reactivate_ticket = "",
					area_code = "US",
					device_grant_ticket = "",
					steam_name = ""
				},
				device_grant_required = false,
				safe_mobile_required = false,
				realperson_required = false,
				realname_operation = "None"
			}
		};
		string json = JsonConvert.SerializeObject(rspObj);
		return new JsonResponse(json);
	}

    [HttpEndpoint("/mdk/shield/api/loadConfig", "POST")]
    public HttpResponse MdkShieldApiLoadConfig(HttpListenerRequest request)
    {
        return new JsonResponse(GameConfig);
    }

    [HttpEndpoint("/admin/mi18n/bh3_usa/20190628_5d15ba66cd922/20190628_5d15ba66cd922-version.json", "GET")]
    public HttpResponse VersionJson(HttpListenerRequest request)
    {
        return new JsonResponse("{\"version\": 52}");
    }

    [HttpEndpoint("/mdk/shield/api/loginCaptcha", "POST")]
    public HttpResponse LoginCaptcha(HttpListenerRequest request)
    {
        return new JsonResponse("{\"retcode\":0,\"message\":\"OK\",\"data\":{\"protocol\":true,\"qr_enabled\":true,\"log_level\":\"INFO\"}}");
    }

    [HttpEndpoint("/log/sdk/upload", "POST")]
    public HttpResponse Upload(HttpListenerRequest request)
    {
        return new JsonResponse("");
    }

    [HttpEndpoint("/pcSdkLogin.html", "GET")]
    public HttpResponse PcSdkLogin(HttpListenerRequest request)
    {
        return new JsonResponse("");
    }

    [HttpEndpoint("/client_game_res/:data", "GET")]
    public HttpResponse ClientGameRes(HttpListenerRequest request)
    {
        return new JsonResponse("");
    }

    //[HttpEndpoint("/sdk/login", "GET")]
    //public HttpResponse SdkLogin(HttpListenerRequest request)
    //{
    //    string rsp = """
    //    {
    //       "retcode":0,
    //       "data":{
    //          "uid":"69420",
    //          "token":"kazusaaa",
    //          "email":"Kazusa@pot.moe"
    //       }
    //    }
    //    """;
    //    return new JsonResponse(rsp);
    //}

    [HttpEndpoint("/client_design_data/:data", "GET")]
    public HttpResponse ClientDesignData(HttpListenerRequest request)
    {
        return new JsonResponse("");
    }

    [HttpEndpoint("/combo/granter/api/getProtocol", "GET")]
    public HttpResponse ApiGetProtocol(HttpListenerRequest request)
    {
        string rsp = """
        {
           "retcode":0,
           "message":"OK",
           "data":{
              "modified":true,
              "protocol":{
                 "id":0,
                 "app_id":4,
                 "language":"zh-cn",
                 "user_proto":"",
                 "priv_proto":"",
                 "major":31,
                 "minimum":0,
                 "create_time":"-",
                 "teenager_proto":"kazusa",
                 "third_proto":"kazusa"
              }
           }
        }
        """;
        return new JsonResponse(rsp);
    }

    [HttpEndpoint("/combo/granter/login/login", "POST")]
    public HttpResponse ComboGranterLoginLogin(HttpListenerRequest request)
    {
        string rsp = """
        {
           "retcode":0,
           "message":"OK",
           "data":{
              "combo_id":69420,
              "open_id":69420,
              "combo_token":"kazusaaa",
              "data":"{\"guest\":true}",
              "heartbeat":false,
              "account_type":1
           }
        }
        """;
        return new JsonResponse(rsp);
    }

    [HttpEndpoint("/combo/granter/api/getConfig", "GET")]
    public HttpResponse ComboGranterGetConfig(HttpListenerRequest request)
    {
        string rsp = """
        {
           "retcode":0,
           "message":"OK",
           "data":{
              "protocol":true,
              "qr_enabled":true,
              "log_level":"DEBUG",
              "announce_url":"https://webstatic-sea.hoyoverse.com/hk4e/announcement/index.html?sdk_presentation_style=fullscreen\u0026sdk_screen_transparent=true\u0026game_biz=hk4e_global\u0026auth_appid=announcement\u0026game=hk4e#/",
              "push_alias_type":2,
              "disable_ysdk_guard":false,
              "enable_announce_pic_popup":true
           }
        }
        """;
        return new JsonResponse(rsp);
    }

    [HttpEndpoint("/query_region_list", "GET")]
    public HttpResponse QueryRegionList(HttpListenerRequest request)
    {

        QueryRegionListHttpRsp queryRegionListHttpRsp = new QueryRegionListHttpRsp()
        {
            ClientCustomConfig = custom_config,
            ClientConfig = new ClientCustomConfig()
            {
                Sdkenv = "2",
                Showexception = false
            }
        };

        queryRegionListHttpRsp.RegionLists.Add(new RegionSimpleInfo()
        {
            Name = "KazusaGI",
            Title = "KazusaGI",
            Type = "DEV_PUBLIC",
            DispatchUrl = $"http://{config.WebServer.ServerIP}:{config.WebServer.ServerPort}/query_cur_region"
        });

        return new TextResponse(Convert.ToBase64String(Packet.SerializeToByteArray(queryRegionListHttpRsp)));
    }

    [HttpEndpoint("/query_cur_region", "GET")]
    public HttpResponse QueryCurRegion(HttpListenerRequest request)
    {
        QueryCurrRegionHttpRsp queryCurrRegionHttpRsp = new QueryCurrRegionHttpRsp()
        {
            RegionInfo = new RegionInfo()
            {
                GateserverIp = config.GameServer.ServerIP,
                GateserverPort = Convert.ToUInt32(config.GameServer.ServerPort),
                ResourceUrl = $"http://{config.WebServer.ServerIP}:{config.WebServer.ServerPort}/client_game_res",
                DataUrl = $"http://{config.WebServer.ServerIP}:{config.WebServer.ServerPort}/client_design_data",
                SecretKey = new byte[0],
            },
            ClientConfig = new ClientCustomConfig()
            {
                Sdkenv = "2",
                Showexception = false,
            },
        };

        return new TextResponse(Convert.ToBase64String(Packet.SerializeToByteArray(queryCurrRegionHttpRsp)));
    }
}