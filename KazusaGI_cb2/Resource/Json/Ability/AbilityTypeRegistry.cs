using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using Newtonsoft.Json;
using System.Reflection;

namespace KazusaGI_cb2.Resource.Json.Ability;

/// <summary>
/// Mirrors hk4e's separate JSON factory maps: type names are resolved inside
/// the declared polymorphic family, not through one process-wide name table.
/// This is important because different config families may legitimately use
/// the same JSON type name.
/// </summary>
internal sealed class AbilityTypeRegistry
{
	private static readonly Type[] RootTypes =
	{
		typeof(BaseConfigAbility),
		typeof(BaseAction),
		typeof(BaseAbilityMixin),
		typeof(BasePredicate),
		typeof(BaseAttackPattern),
		typeof(BaseBornType),
		typeof(BaseDirectionType),
		typeof(BaseSelectTargetType),
		typeof(BaseEventOp),
		typeof(BaseConfigTalent),
	};

	public static AbilityTypeRegistry Shared { get; } = new(typeof(AbilityTypeRegistry).Assembly);

	private readonly Dictionary<Type, Dictionary<string, Type>> _typesByRoot = new();
	private readonly Dictionary<string, Type> _globallyUniqueTypes = new(StringComparer.Ordinal);
	private readonly HashSet<string> _ambiguousGlobalNames = new(StringComparer.Ordinal);

	private AbilityTypeRegistry(Assembly assembly)
	{
		Type[] allTypes = assembly.GetTypes();
		foreach (Type root in RootTypes)
		{
			var map = new Dictionary<string, Type>(StringComparer.Ordinal);
			foreach (Type type in allTypes.Where(t => root.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface))
			{
				Register(map, type.Name, type, root);
				if (!string.IsNullOrEmpty(type.FullName))
					Register(map, type.FullName!, type, root);

				foreach (AbilityJsonTypeAliasAttribute alias in type.GetCustomAttributes<AbilityJsonTypeAliasAttribute>())
					Register(map, alias.Alias, type, root);
			}
			_typesByRoot[root] = map;
		}

		foreach (Dictionary<string, Type> map in _typesByRoot.Values)
		{
			foreach ((string name, Type type) in map)
			{
				// Full names are already unambiguous and are useful to the
				// fallback binder for object-typed legacy fields.
				if (name.Contains('.'))
				{
					_globallyUniqueTypes[name] = type;
					continue;
				}

				if (_ambiguousGlobalNames.Contains(name))
					continue;

				if (_globallyUniqueTypes.TryGetValue(name, out Type? existing) && existing != type)
				{
					_globallyUniqueTypes.Remove(name);
					_ambiguousGlobalNames.Add(name);
				}
				else
				{
					_globallyUniqueTypes[name] = type;
				}
			}
		}
	}

	public bool IsPolymorphicRoot(Type type) => RootTypes.Contains(type);

	public Type Resolve(Type declaredRoot, string jsonTypeName)
	{
		Type root = FindRoot(declaredRoot)
			?? throw new JsonSerializationException($"'{declaredRoot.FullName}' is not a registered ability JSON family.");

		string normalized = NormalizeTypeName(jsonTypeName);
		Dictionary<string, Type> map = _typesByRoot[root];
		if (map.TryGetValue(normalized, out Type? type))
			return type;

		string simpleName = SimpleName(normalized);
		if (map.TryGetValue(simpleName, out type))
			return type;

		throw new JsonSerializationException(
			$"Unknown ability JSON type '{jsonTypeName}' for family '{root.Name}'.");
	}

	public Type ResolveGlobal(string jsonTypeName)
	{
		string normalized = NormalizeTypeName(jsonTypeName);
		if (_globallyUniqueTypes.TryGetValue(normalized, out Type? type))
			return type;

		string simpleName = SimpleName(normalized);
		if (_ambiguousGlobalNames.Contains(simpleName))
			throw new JsonSerializationException(
				$"Ambiguous ability JSON type '{jsonTypeName}'. It must be deserialized through its declared config family.");

		if (_globallyUniqueTypes.TryGetValue(simpleName, out type))
			return type;

		throw new JsonSerializationException($"Unknown ability JSON type '{jsonTypeName}'.");
	}

	private Type? FindRoot(Type declaredType)
	{
		if (_typesByRoot.ContainsKey(declaredType))
			return declaredType;
		return RootTypes.FirstOrDefault(root => root.IsAssignableFrom(declaredType));
	}

	private static void Register(Dictionary<string, Type> map, string name, Type type, Type root)
	{
		if (map.TryGetValue(name, out Type? existing) && existing != type)
		{
			throw new InvalidOperationException(
				$"Duplicate ability JSON type '{name}' in family '{root.Name}': " +
				$"'{existing.FullName}' and '{type.FullName}'.");
		}
		map[name] = type;
	}

	private static string NormalizeTypeName(string typeName)
	{
		string value = typeName.Trim();
		int comma = value.IndexOf(',');
		if (comma >= 0)
			value = value[..comma].Trim();
		return value;
	}

	private static string SimpleName(string typeName)
	{
		int dot = typeName.LastIndexOf('.');
		int plus = typeName.LastIndexOf('+');
		int split = Math.Max(dot, plus);
		return split >= 0 ? typeName[(split + 1)..] : typeName;
	}
}
