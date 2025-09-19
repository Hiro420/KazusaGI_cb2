// AvatarEntity.cs (update)
using KazusaGI_cb2.GameServer.PlayerInfos;
using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.GameServer.Ability;

namespace KazusaGI_cb2.GameServer
{
	public class AvatarEntity : Entity // Maybe IDamageable in the future
	{
		public PlayerAvatar DbInfo { get; }

		public AvatarEntity(Session session, PlayerAvatar playerAvatar, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityAvatar)
		{
			DbInfo = playerAvatar;
			abilityManager = new AvatarAbilityManager(this);
		}


		protected override uint? GetLevel()
		{
			return DbInfo.Level;
		}

		protected override void BuildKindSpecific(SceneEntityInfo ret)
		{
			var asAvatarInfo = DbInfo.ToAvatarInfo();
			ret.Avatar = DbInfo.ToSceneAvatarInfo();

			foreach (var kv in asAvatarInfo.PropMaps)
				ret.PropMaps[kv.Key] = kv.Value;

			foreach (var kv in asAvatarInfo.FightPropMaps)
				ret.FightPropMaps[kv.Key] = kv.Value;
		}

		public SceneEntityInfo ToSceneEntityInfo(Session session) =>
			base.ToSceneEntityInfo(session.player!.Pos, session.player!.Rot);
	}
}
