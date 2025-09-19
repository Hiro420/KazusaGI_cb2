using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAvatarWeaponType : BasePredicate
    {
        [JsonProperty] public readonly WeaponType[] weaponTypes;
    }
}
