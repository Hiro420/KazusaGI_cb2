using Newtonsoft.Json;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class LoseHP : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly object amountByTargetCurrentHPRatio;
        [JsonProperty] public readonly bool lethal;
        [JsonProperty] public readonly float? limboByTargetMaxHPRatio;

        public override async Task Invoke(string abilityName, Entity avatarr, Entity? enemyTarget = null)
        {
            if (!(avatarr is AvatarEntity avatar)) return;
            //if (!doOffStage && avatar.abilityManager.Owner.TeamManager.GetCurrentAvatarEntity() != avatar) return;

            float curHP;
            float maxHP;
            float newHP;
            switch (target)
            {
                case TargetType.None:
                case TargetType.Self:
                    curHP = avatar.DbInfo.Hp;
                    maxHP = avatar.DbInfo.MaxHp;
                    //newHP = curHP - DynamicFloatHelper.ResolveDynamicFloat(amountByTargetCurrentHPRatio, avatar, abilityName) * curHP;
                    //if (limboByTargetMaxHPRatio != null)
                    //{
                    //    newHP = Math.Max(newHP, (float)limboByTargetMaxHPRatio);
                    //}
                    //else if (newHP < 1f)
                    //{
                    //    if (lethal)
                    //    {
                    //        //await avatar.DamageAsync(curHP);
                    //        return;
                    //    }
                    //    else
                    //        newHP = 1;
                    //}
                    //await avatar.SetHealthAsync(newHP);
                    return;
            }
        }
    }
}
