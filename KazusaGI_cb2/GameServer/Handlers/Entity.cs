using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Entity
{
    [Packet.PacketCmdId(PacketId.QueryPathReq)]
    public static void HandleQueryPathReq(Session session, Packet packet)
    {

        QueryPathReq req = packet.GetDecodedBody<QueryPathReq>();
        QueryPathRsp rsp = new QueryPathRsp();
        if (req.DestinationPos.Count > 0)
        {
            rsp.Corners.Add(req.SourcePos);
            rsp.Corners.Add(req.DestinationPos[0]);
            rsp.QueryId = req.QueryId;
            rsp.QueryStatus = QueryPathRsp.PathStatusType.StatusSucc;
        }
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.SeeMonsterReq)]
    public static void HandleSeeMonsterReq(Session session, Packet packet)
    {
        SeeMonsterReq req = packet.GetDecodedBody<SeeMonsterReq>();
        SeeMonsterRsp rsp = new SeeMonsterRsp();
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.EntityForceSyncReq)]
    public static void HandleEntityForceSyncReq(Session session, Packet packet)
    {
        EntityForceSyncReq req = packet.GetDecodedBody<EntityForceSyncReq>();
        EntityForceSyncRsp rsp = new EntityForceSyncRsp()
        {
            EntityId = req.EntityId,
            SceneTime = req.SceneTime
        };
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.ExecuteGadgetLuaReq)]
    public static void HandleExecuteGadgetLuaReq(Session session, Packet packet)
    {
        ExecuteGadgetLuaReq req = packet.GetDecodedBody<ExecuteGadgetLuaReq>();
        // todo: handle
        //GameServer.Entity? targetEntity = session.player!.Scene.FindEntityByEntityId(req.SourceEntityId);
        //if (targetEntity == null)
        //{
        //    session.c.LogWarning($"[FUCKED EXECUTE GADGET LUA] Entity {req.SourceEntityId} not found for ExecuteGadgetLuaReq");
        //    return;
        //}
        //GadgetEntity gadget = (GameServer.GadgetEntity)targetEntity;
        session.SendPacket(new ExecuteGadgetLuaRsp());
    }

    // QuestCreateEntityReq
    [Packet.PacketCmdId(PacketId.QuestCreateEntityReq)]
    public static void HandleQuestCreateEntityReq(Session session, Packet packet)
    {
        // maybe later for quests ???
    }


}