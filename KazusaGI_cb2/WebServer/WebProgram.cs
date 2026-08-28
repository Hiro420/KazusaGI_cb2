using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace KazusaGI_cb2.WebServer;

// Attribute to map HTTP endpoints to methods

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HttpEndpointAttribute : Attribute
{
	public string PathPattern { get; }
	public string Method { get; }
	public readonly Regex _regexPattern;

	public HttpEndpointAttribute(string pathPattern, string method = "GET")
	{
		PathPattern = pathPattern;
		Method = method.ToUpper();

		// Convert route pattern to regex, e.g., "/client_game_res/:param" → "^/client_game_res/([^/]+)$"
		string regexPattern = "^" + Regex.Replace(pathPattern, @":([^/]+)", "(?<$1>[^/]+)") + "$";
		_regexPattern = new Regex(regexPattern, RegexOptions.Compiled);
	}

	public bool IsMatch(string url, out Match match)
	{
		match = _regexPattern.Match(url);
		return match.Success;
	}
}


// Class to construct HTTP responses
public abstract class HttpResponse
{
	public abstract byte[] GetBytes();
	public abstract string ContentType { get; }
}

public class JsonResponse : HttpResponse
{
	private readonly string _json;

	public JsonResponse(string json)
	{
		object jsonVerified = JsonConvert.DeserializeObject(json)!;
		_json = JsonConvert.SerializeObject(jsonVerified);
	}

	public override byte[] GetBytes()
	{
		return Encoding.UTF8.GetBytes(_json);
	}

	public override string ContentType => "application/json";
}

public class TextResponse : HttpResponse
{
	private readonly string _data;

	public TextResponse(string data)
	{
		_data = data;
	}

	public override byte[] GetBytes()
	{
		return Encoding.UTF8.GetBytes(_data);
	}

	public override string ContentType => "text/html; charset=UTF-8";
}

public class GzipJsonResponse : HttpResponse
{
	private readonly string _json;

	public GzipJsonResponse(string json)
	{
		_json = json;
	}

	public override byte[] GetBytes()
	{
		using (var memoryStream = new MemoryStream())
		{
			using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				var jsonBytes = Encoding.UTF8.GetBytes(_json);
				gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
			}
			return memoryStream.ToArray();
		}
	}

	public override string ContentType => "application/json; charset=utf-8";
}

public class WebProgram
{
	public static void Main(string address, int port)
	{
		Logger logger = new("WebServer");
		string url = $"http://{address}:{port}/";
		logger.LogInfo($"Starting server at {url}");

		HttpHandler handler = new HttpHandler();
		HttpListener listener = new HttpListener();
		listener.Prefixes.Add(url);
		listener.Start();

		logger.LogSuccess($"WebServer is listening on {url}...", true);

		// Dynamically map endpoints using reflection
		var endpointMethods = handler.GetType().GetMethods(
			BindingFlags.Public | BindingFlags.Instance
		);

		while (true)
		{
			HttpListenerContext context = listener.GetContext();
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;

			HttpResponse? httpResponse = null;
			bool handled = false;

			logger.LogInfo($"Handling {request.Url} [{request.HttpMethod}]...");

			foreach (var method in endpointMethods)
			{
				var attribute = method.GetCustomAttribute<HttpEndpointAttribute>();
				if (attribute != null && attribute.IsMatch(request.Url!.AbsolutePath.Split("?").First(), out var match))
				{
					try
					{
						var parameters = new List<object> { request };

						// Extract named parameters from the URL
						foreach (var groupName in attribute._regexPattern.GetGroupNames().Skip(1)) // Skip group "0" (entire match)
						{
							parameters.Add(match.Groups[groupName].Value);
						}

						httpResponse = (HttpResponse)method.Invoke(handler, parameters.ToArray())!;
						handled = true;
						break;
					}
					catch (Exception ex)
					{
						logger.LogError($"Error invoking handler: {ex.Message} for {method.Name}\n{ex.InnerException}");
					}
				}
			}

			if (!handled)
			{
				response.StatusCode = 404;
				httpResponse = new JsonResponse("{\"error\": \"Not Found\"}");
			}
			else
			{
				response.StatusCode = 200;
			}

			if (httpResponse != null)
			{
				response.ContentType = httpResponse.ContentType;
				byte[] buffer = httpResponse.GetBytes();
				response.ContentLength64 = buffer.Length;
				if (httpResponse.GetType() == typeof(GzipJsonResponse))
				{
					response.Headers.Add("Content-Encoding", "gzip");
				}
				response.OutputStream.Write(buffer, 0, buffer.Length);
			}

			response.OutputStream.Close();
		}
	}
}