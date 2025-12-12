// AvatarEntity.cs (update)
using KazusaGI_cb2.GameServer.PlayerInfos;
using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource;
using System.Linq;
using System.Text.RegularExpressions;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

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
			InitAbilityStuff();
			abilityManager.Initialize();
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

		public void GenerateElemBallByAbility(GenerateElemBall info)
		{
			// Default to an elementless particle.
			int itemId = info.configID != 0 ? info.configID : 2024;

			// Generate 2 particles by default. todo: use info.ratio and info.baseEnergy?
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

			int gadgetId = MainApp.resourceManager.MaterialExcel.Values.FirstOrDefault(m => m.id == itemId)?.gadgetId is uint gid ? (int)gid : 70610008; // no element by default
			if (MainApp.resourceManager.MaterialExcel.TryGetValue((uint)itemId, out MaterialExcelConfig? materialExcel))
			{
				gadgetId = (int)materialExcel.gadgetId;
			}

			session.player.Scene.GenerateParticles(
				gadgetId, 
				amount, 
				Session.Vector3ToVector(session.player!.Pos),
				Session.Vector3ToVector(session.player!.Rot)
			);
		}

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
		
		/// <summary>
		/// Initialize ability system for this avatar entity
		/// </summary>
		public void InitAbilityStuff()
		{
			var resourceManager = MainApp.resourceManager;
			string name = $"Avatar_{DbInfo.AvatarName}";
			int CurDepotId = (int)DbInfo.SkillDepotId;

			// Build AbilityConfigMap using TargetAbilities from ConfigAvatarMap
			List<ConfigAbilityContainer> configContainerList = new();
			
			if (resourceManager.ConfigAvatarMap.TryGetValue($"ConfigAvatar_{DbInfo.AvatarName}", out var configAvatar))
			{
				foreach (TargetAbility targetAbility in configAvatar.abilities)
				{
					if (!resourceManager.ConfigAbilityMap.TryGetValue(targetAbility.abilityName, out ConfigAbilityContainer? container))
					{
						session.c.LogError($"Avatar ability {targetAbility.abilityName} not found in binoutput");
						continue;
					}
					configContainerList.Add(container);
				}
			}

			if (configContainerList.Count > 0)
			{
				DbInfo.AbilityConfigMap.TryAdd((int)DbInfo.SkillDepotId, configContainerList.ToArray());
			}

			var dictionary1 = resourceManager.AvatarSkillExcel.Where(w => DbInfo.avatarSkillDepotExcel.skills.Contains(w.Key) || 
				DbInfo.avatarSkillDepotExcel.subSkills.Contains(w.Key) || DbInfo.avatarSkillDepotExcel.energySkill == w.Key)
				.ToDictionary(x => x.Key, x => x.Value);

			// Populate SkillData with skills organized by depot ID
			if (!DbInfo.SkillData.ContainsKey((int)DbInfo.SkillDepotId))
			{
				DbInfo.SkillData[(int)DbInfo.SkillDepotId] = new SortedList<uint, AvatarSkillExcelConfig>();
			}
			
			foreach (var skill in dictionary1)
			{
				DbInfo.SkillData[CurDepotId][skill.Key] = skill.Value;
			}

			var talentList = resourceManager.AvatarTalentExcel.Where(w => DbInfo.avatarSkillDepotExcel.talents.Contains((uint)w.Value.talentId));
			foreach (var i in talentList)
			{
				DbInfo.TalentData[CurDepotId][i.Key] = i.Value;
			}

			var dict1 = resourceManager.ProudSkillExcel.Where(w => DbInfo.avatarSkillDepotExcel.inherentProudSkillOpens.Exists(y => y.proudSkillGroupId == w.Value.proudSkillGroupId)).ToDictionary(x => x.Key, x => x.Value);

			foreach (var i in dict1)
			{
				DbInfo.ProudSkillData[CurDepotId].TryAdd(i.Key, i.Value);
			}

			foreach (var skilldata in dictionary1.Values)
			{
				var proudData = resourceManager.ProudSkillExcel.Where(w => w.Value.proudSkillGroupId == skilldata.proudSkillGroupId);
				foreach (var proud in proudData)
				{
					DbInfo.ProudSkillData[CurDepotId][proud.Key] = proud.Value;
				}
			}

			if (resourceManager.AvatarTalentConfigDataMap.TryGetValue($"ConfigTalent_{Regex.Replace(name, "Avatar_", "")}", out Dictionary<string, BaseConfigTalent[]>? configTalents))
				DbInfo.ConfigTalentMap[(int)DbInfo.SkillDepotId] = configTalents;

			Dictionary<uint, ConfigAbility> abilityHashMap = new();

			// add abilityGroup abilities (if player skill depot ability group)
			if (resourceManager.ConfigAvatarMap.TryGetValue($"ConfigAvatar_{DbInfo.AvatarName}", out var configAvatarForHash) && 
			    DbInfo.AbilityConfigMap.ContainsKey((int)DbInfo.SkillDepotId))
			{
				foreach (TargetAbility ability in configAvatarForHash.abilities)
				{
					ConfigAbility? config = null;
					foreach (var container in DbInfo.AbilityConfigMap[(int)DbInfo.SkillDepotId])
					{
						if (container.Default is ConfigAbility konfig && konfig.abilityName == ability.abilityName)
						{
							config = konfig;
							break;
						}
					}
					if (config == null) continue;
					abilityHashMap[(uint)Ability.Utils.AbilityHash(ability.abilityName)] = config;
				}
			}
			
			if (abilityHashMap.Count > 0)
			{
				DbInfo.AbilityHashMap.TryAdd((int)DbInfo.SkillDepotId, abilityHashMap);
			}
		}
	}
}
