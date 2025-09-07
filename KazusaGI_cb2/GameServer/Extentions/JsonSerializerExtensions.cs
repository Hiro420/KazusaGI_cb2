using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer;

public static class JsonSerializerExtensions
{
	/// <summary>
	/// Serialize an object to JSON using this configured JsonSerializer.
	/// Works like JsonConvert.SerializeObject but uses the instance settings (e.g., StringEnumConverter).
	/// </summary>
	public static string SerializeObject(this JsonSerializer serializer, object value, Formatting formatting = Formatting.None)
	{
		if (serializer == null) throw new ArgumentNullException(nameof(serializer));

		var sb = new StringBuilder(256);
		using var sw = new StringWriter(sb, CultureInfo.InvariantCulture);
		using var writer = new JsonTextWriter(sw) { Formatting = formatting };

		serializer.Serialize(writer, value);
		writer.Flush();
		return sb.ToString();
	}

	/// <summary>
	/// Convenience to pretty-print.
	/// </summary>
	public static string SerializeObjectIndented(this JsonSerializer serializer, object value) =>
		SerializeObject(serializer, value, Formatting.Indented);
}