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
        var stringIsNullOrEmpty = typeof(string).GetMethod(
            nameof(string.IsNullOrEmpty),
            new[] { typeof(string) })!;

        var underlying = Nullable.GetUnderlyingType(targetType);
        var isNullable = underlying != null || !targetType.IsValueType;
        var nonNullType = underlying ?? targetType;

        var returnTarget = Expression.Label(targetType);
        var bodyExpressions = new List<Expression>();

        if (isNullable)
        {
            // if (string.IsNullOrEmpty(value)) return default;
            bodyExpressions.Add(
                Expression.IfThen(
                    Expression.Call(stringIsNullOrEmpty, valueParam),
                    Expression.Return(returnTarget, Expression.Default(targetType))
                )
            );
        }

        Expression converted;

        if (nonNullType == typeof(string))
        {
            converted = valueParam;
        }
        else if (nonNullType.IsEnum)
        {
            var parseEnum = typeof(Enum).GetMethod(
                nameof(Enum.Parse),
                new[] { typeof(Type), typeof(string), typeof(bool) })!;

            var callParse = Expression.Call(
                parseEnum,
                Expression.Constant(nonNullType),
                valueParam,
                Expression.Constant(true));

            converted = Expression.Convert(callParse, nonNullType);
        }
        else
        {
            var parseMethod = GetParseMethod(nonNullType);
            if (parseMethod == null)
            {
                // Convert.ChangeType(value, type, culture)
                var changeType = typeof(Convert).GetMethod(
                    nameof(Convert.ChangeType),
                    new[] { typeof(object), typeof(Type), typeof(IFormatProvider) })!;

                var call = Expression.Call(
                    changeType,
                    Expression.Convert(valueParam, typeof(object)),
                    Expression.Constant(nonNullType),
                    Expression.Constant(culture, typeof(IFormatProvider)));

                converted = Expression.Convert(call, nonNullType);
            }
            else
            {
                var call = Expression.Call(
                    parseMethod,
                    valueParam,
                    Expression.Constant(culture));

                converted = call;
            }
        }

        if (underlying != null)
        {
            converted = Expression.Convert(converted, targetType);
        }

        bodyExpressions.Add(Expression.Label(returnTarget, converted));
        return Expression.Block(bodyExpressions);
    }

    private static MethodInfo? GetParseMethod(Type type)
    {
        // Look for static T Parse(string, IFormatProvider)
        return type.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(string), typeof(IFormatProvider) });
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