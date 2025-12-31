using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.ServerExcel;

public static class TsvReader
{
    public static List<T> ReadFile<T>(string path, TsvParserOptions? options = null)
        where T : new()
    {
        options ??= TsvParserOptions.Default;
        using var reader = new StreamReader(path, options.Encoding);
        return Read<T>(reader, options);
    }

    public static List<T> Read<T>(TextReader reader, TsvParserOptions? options = null)
        where T : new()
    {
        options ??= TsvParserOptions.Default;

        string? firstLine = reader.ReadLine();
        if (firstLine == null)
            return new List<T>();

        if (!options.HasHeader)
            throw new InvalidOperationException("This parser expects a header line.");

        var headers = Split(firstLine, options);
        var map = TsvTypeMap<T>.FromHeader(headers, options);

        var result = new List<T>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (options.SkipEmptyLines && string.IsNullOrWhiteSpace(line))
                continue;

            var fields = Split(line, options);
            if (fields.Length == 0)
                continue;

            result.Add(map.Create(fields));
        }

        return result;
    }

    private static string[] Split(string line, TsvParserOptions options)
    {
        var parts = line.Split(options.Separator);
        if (!options.TrimWhitespace)
            return parts;

        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();

        return parts;
    }
}