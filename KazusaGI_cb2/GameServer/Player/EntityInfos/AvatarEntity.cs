// AvatarEntity.cs (update)
using KazusaGI_cb2.GameServer.PlayerInfos;
using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Excel;

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

		public override void GenerateElemBall(AbilityActionGenerateElemBall info)
		{
			// Default to an elementless particle.
			int itemId = 2024;

			// Generate 2 particles by default.
			int amount = 2;

			int avatarId = (int)this.DbInfo.AvatarId;

			var skillDepotData = this.DbInfo.avatarSkillDepotExcel;

			// Determine how many particles we need to create for this avatar.
			amount = this.getBallCountForAvatar(avatarId);

			// Determine the avatar's element, and based on that the ID of the
			// particles we have to generate.
			if (skillDepotData != null)
			{
				Resource.ElementType element = skillDepotData.Element != null ? skillDepotData.Element.Type : Resource.ElementType.None;
				itemId = this.getBallIdForElement(element);
			}

			int gadgetId = 70610008; // no element by default
			if (MainApp.resourceManager.MaterialExcel.TryGetValue((uint)itemId, out MaterialExcelConfig? materialExcel))
			{
				gadgetId = (int)materialExcel.gadgetId;
			}

			session.player.Scene.GenerateParticles(gadgetId, amount, info.Pos, info.Rot);
		}

		private int getBallCountForAvatar(int avatarId)
		{
			return 2; // todo: read from ability configs
		}

		private int getBallIdForElement(Resource.ElementType element)
		{
			return element switch
			{
				Resource.ElementType.Fire => 2017,
				Resource.ElementType.Water => 2018,
				Resource.ElementType.Grass => 2019,
				Resource.ElementType.Electric => 2020,
				Resource.ElementType.Wind => 2021,
				Resource.ElementType.Ice => 2022,
				Resource.ElementType.Rock => 2023,
				_ => 2024
			};
		}
	}
}
