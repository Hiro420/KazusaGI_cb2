using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSceneInitFinishReq
{
    [Packet.PacketCmdId(PacketId.SceneInitFinishReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SceneInitFinishReq req = packet.GetDecodedBody<SceneInitFinishReq>();

        // Mirror hk4e: validate enter_scene_token for SceneInitFinishReq.
        if (req.EnterSceneToken != session.player!.EnterSceneToken)
        {
            session.SendPacket(new SceneInitFinishRsp
            {
                Retcode = (int)Retcode.RetEnterSceneTokenInvalid
            });
            return;
        }

        OnlinePlayerInfo onlinePlayerInfo = new OnlinePlayerInfo()
        {
            Uid = session.player!.Uid,
            Nickname = "KazusaPS",
            PlayerLevel = (uint)session.player.Level,
            AvatarId = session.player.PlayerGender == Player.Gender.Female ? (uint)10000007 : 10000005,
            CurPlayerNumInWorld = 1,
            WorldLevel = session.player.WorldLevel
        };

        WorldDataNotify worldDataNotify = new WorldDataNotify();

        worldDataNotify.WorldPropMaps.Add(1, new PropValue() { Type = 8, Ival = 8 });
        worldDataNotify.WorldPropMaps.Add(2, new PropValue() { Type = 2, Ival = 0 });

        SceneDataNotify sceneDataNotify = new SceneDataNotify();
        if (MainApp.resourceManager.SceneExcel.TryGetValue(session.player.SceneId, out var sceneExcelConfig))
        {
            sceneDataNotify.LevelConfigNameLists.Add(sceneExcelConfig.levelEntityConfig);
        }

        session.SendPacket(worldDataNotify);
        session.SendPacket(sceneDataNotify);
        session.SendPacket(new HostPlayerNotify()
        {
            HostPeerId = session.player!.PeerId,
            HostUid = session.player!.Uid
        });
        session.SendPacket(new PlayerGameTimeNotify()
        {
            GameTime = 18000,
            Uid = session.player.Uid
        });
        session.SendPacket(new SceneTimeNotify()
        {
            SceneId = session.player.SceneId,
            SceneTime = 69
        });
        session.SendPacket(new WorldPlayerInfoNotify()
        {
            PlayerInfoLists = { onlinePlayerInfo },
            PlayerUidLists = { session.player.Uid }
        });
        session.SendPacket(new ScenePlayerInfoNotify()
        {
            PlayerInfoLists =
            {
                new ScenePlayerInfo()
                {
                    Uid = session.player.Uid,
                    PeerId = session.player.PeerId,
                    Name = session.player.Name,
                    IsConnected = true,
                    SceneId = session.player.SceneId,
                    OnlinePlayerInfo = onlinePlayerInfo
                }
            }
        });



        // Scene-level entity (weather etc.) appears first on init finish.
        var sceneEntity = session.player.Scene.EntityManager.Entities.Values
            .OfType<SceneEntity>()
            .FirstOrDefault();
        if (sceneEntity != null)
        {
            var sceneAppear = new SceneEntityAppearNotify
            {
                AppearType = Protocol.VisionType.VisionReplace
            };
            sceneAppear.EntityLists.Add(sceneEntity.ToSceneEntityInfo());
            session.SendPacket(sceneAppear);
        }

        // Team entity appear mirrors hk4e VisionReplace semantics on init finish.
        var teamAppear = new SceneEntityAppearNotify()
        {
            AppearType = Protocol.VisionType.VisionReplace,
        };
        teamAppear.EntityLists.Add(session.player.GetCurrentLineup().teamEntity!.ToSceneEntityInfo());
        session.SendPacket(teamAppear);

        session.player.SendSceneTeamUpdateNotify(session);
        session.player.SendSyncTeamEntityNotify(session);
        session.player.SendPlayerEnterSceneInfoNotify(session);
        session.SendPacket(new SceneInitFinishRsp());
        session.player.Scene.isFinishInit = true;

        // Seed scene block/group and region state once init finishes so
        // region enter events (e.g. for dungeon/tower scenes) can fire
        // even before the player sends any movement.
        session.player.Scene.UpdateOnMove();

        TeamHandler.SendAvatarEquipChangeNotify(session, session.player.GetCurrentLineup().Leader!);
        TeamHandler.SendAvatarTeamUpdateNotify(session, session.player.TeamIndex, session.player.GetCurrentLineup().Avatars.Select(a => a.Guid).ToList());

		session.player!.SavePersistent();
	}
}