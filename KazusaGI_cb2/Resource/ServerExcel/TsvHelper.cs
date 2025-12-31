using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.ServerExcel;

[AttributeUsage(AttributeTargets.Property)]
public sealed class TsvColumnAttribute : Attribute
{
    public string? Name { get; }
    public int? Index { get; }
    public bool Required { get; set; } = true;

    public TsvColumnAttribute(string name) => Name = name;
    public TsvColumnAttribute(int index) => Index = index;
}

public sealed class TsvParserOptions
{
    public char Separator { get; init; } = '\t';
    public Encoding Encoding { get; init; } = new UTF8Encoding(false);
    public bool HasHeader { get; init; } = true;
    public bool TrimWhitespace { get; init; } = true;
    public bool SkipEmptyLines { get; init; } = true;
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    public static TsvParserOptions Default { get; } = new();
}

internal sealed class TsvTypeMap<T> where T : new()
{
    public sealed record ColumnMap(int Index, Action<T, string?> Setter);

    private readonly ColumnMap[] _columns;

    private TsvTypeMap(ColumnMap[] columns) => _columns = columns;

    public static TsvTypeMap<T> FromHeader(IReadOnlyList<string> headers, TsvParserOptions options)
    {
        var headerLookup = headers
            .Select((h, i) => new { Name = h, Index = i })
            .ToDictionary(h => h.Name, h => h.Index, StringComparer.Ordinal);

        var cols = new List<ColumnMap>();
        var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Where(p => p.CanWrite);

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<TsvColumnAttribute>();
            int index = -1;

            if (attr != null)
            {
                if (attr.Index.HasValue)
                {
                    index = attr.Index.Value;
                }
                else if (!string.IsNullOrEmpty(attr.Name))
                {
                    if (!headerLookup.TryGetValue(attr.Name, out index))
                    {
                        if (attr.Required)
                            throw new InvalidOperationException(
                                $"Required column '{attr.Name}' not found for property '{prop.Name}'.");
                        continue;
                    }
                }
            }
            else
            {
                if (!headerLookup.TryGetValue(prop.Name, out index))
                    continue;
            }

            if (index < 0 || index >= headers.Count)
                continue;

            var setter = BuildSetter(prop, options.Culture);
            cols.Add(new ColumnMap(index, setter));
        }

        return new TsvTypeMap<T>(cols.ToArray());
    }

    private static Action<T, string?> BuildSetter(PropertyInfo prop, CultureInfo culture)
    {
        var targetParam = Expression.Parameter(typeof(T), "target");
        var valueParam = Expression.Parameter(typeof(string), "value");

        var convertedValueExpr = BuildConversionExpression(valueParam, prop.PropertyType, culture);

        var call = Expression.Call(
            targetParam,
            prop.GetSetMethod() ?? throw new InvalidOperationException($"Property {prop.Name} has no setter."),
            convertedValueExpr);

        var lambda = Expression.Lambda<Action<T, string?>>(call, targetParam, valueParam);
        return lambda.Compile();
    }

    private static Expression BuildConversionExpression(
        ParameterExpression valueParam,
        Type targetType,
        CultureInfo culture)
    {
        // Delegate actual conversion (including handling of "" and "?" and TryParse fallbacks)
        // to a strongly-typed helper so we don't need complex expression trees per type.
        var helper = typeof(TsvTypeMap<T>)
            .GetMethod(nameof(ConvertValue), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(targetType);

        return Expression.Call(helper, valueParam, Expression.Constant(culture));
    }

    private static TTarget ConvertValue<TTarget>(string? value, CultureInfo culture)
    {
        var targetType = typeof(TTarget);
        var underlying = Nullable.GetUnderlyingType(targetType);
        var nonNullType = underlying ?? targetType;

        // Treat empty / whitespace / "?" as default(TTarget) for non-string types
        if (string.IsNullOrWhiteSpace(value))
            return default!;

        if (nonNullType != typeof(string) && value == "?")
            return default!;

        // Strings: just pass through
        if (nonNullType == typeof(string))
            return (TTarget)(object)value!;

        object? converted;

        // Enums
        if (nonNullType.IsEnum)
        {
            if (System.Enum.TryParse(nonNullType, value, true, out var enumObj))
            {
                converted = enumObj;
            }
            else
            {
                return default!;
            }
        }
        // Common numeric and primitive types with TryParse
        else if (nonNullType == typeof(int))
        {
            if (int.TryParse(value, NumberStyles.Integer, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(decimal))
        {
            if (decimal.TryParse(value, NumberStyles.Number, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(double))
        {
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(float))
        {
            if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(long))
        {
            if (long.TryParse(value, NumberStyles.Integer, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(short))
        {
            if (short.TryParse(value, NumberStyles.Integer, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(byte))
        {
            if (byte.TryParse(value, NumberStyles.Integer, culture, out var v))
                converted = v;
            else
                return default!;
        }
        else if (nonNullType == typeof(bool))
        {
            if (bool.TryParse(value, out var v))
                converted = v;
            else
                return default!;
        }
        else
        {
            try
            {
                converted = Convert.ChangeType(value, nonNullType, culture);
            }
            catch
            {
                return default!;
            }
        }

        if (underlying != null)
        {
            // converted is already of the non-nullable underlying type;
            // just box it and cast to Nullable<TUnderlying>.
            return (TTarget)(object)converted!;
        }

        return (TTarget)converted!;
    }

    public T Create(string[] fields)
    {
        var obj = new T();
        foreach (var c in _columns)
        {
            if (c.Index < fields.Length)
            {
                c.Setter(obj, fields[c.Index]);
            }
        }
        return obj;
    }
}
