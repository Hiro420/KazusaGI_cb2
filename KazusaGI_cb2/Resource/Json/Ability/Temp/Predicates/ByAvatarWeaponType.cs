using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
	internal class ByAvatarWeaponType : BasePredicate
	{
		[JsonProperty] public readonly WeaponType[] weaponTypes;
	}
}
