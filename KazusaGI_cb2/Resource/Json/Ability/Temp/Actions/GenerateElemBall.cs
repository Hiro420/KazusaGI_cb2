using Newtonsoft.Json;
using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class GenerateElemBall : BaseAction
    {
        [JsonProperty] public readonly int configID;
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly object ratio;
        [JsonProperty] public readonly string baseEnergy;

        public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
        {
            await Task.Yield();

            if (srcEntity is not AvatarEntity avatar)
                return;

            // For now, spawn element balls at the avatar's current
            // position/rotation. This mirrors hk4e's behavior at a
            // high level (generate energy orbs around the caster),
            // without yet reproducing the full energy math.
            var info = new AbilityActionGenerateElemBall
            {
                Pos = Session.Vector3ToVector(avatar.Position),
                Rot = Session.Vector3ToVector(avatar.Rotation),
                RoomId = 0
            };

            avatar.GenerateElemBall(info);
        }
    }
}
