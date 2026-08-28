namespace KazusaGI_cb2.Resource.Json.Ability;

public sealed class AbilityConfigLoadException : Exception
{
	public string FilePath { get; }
	public string? JsonPath { get; }

	public AbilityConfigLoadException(string filePath, string message, string? jsonPath = null, Exception? innerException = null)
		: base(BuildMessage(filePath, message, jsonPath), innerException)
	{
		FilePath = filePath;
		JsonPath = jsonPath;
	}

	private static string BuildMessage(string filePath, string message, string? jsonPath)
		=> string.IsNullOrEmpty(jsonPath)
			? $"Ability config load failed in '{filePath}': {message}"
			: $"Ability config load failed in '{filePath}' at '{jsonPath}': {message}";
}
