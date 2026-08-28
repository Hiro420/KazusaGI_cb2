namespace KazusaGI_cb2.Resource.Json.Ability;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class AbilityJsonTypeAliasAttribute : Attribute
{
	public string Alias { get; }

	public AbilityJsonTypeAliasAttribute(string alias)
	{
		Alias = alias;
	}
}
