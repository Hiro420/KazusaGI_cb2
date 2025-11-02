using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetActivityInfoReq
{
    [Packet.PacketCmdId(PacketId.GetActivityInfoReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        GetActivityInfoReq req = packet.GetDecodedBody<GetActivityInfoReq>();
        GetActivityInfoRsp rsp = new GetActivityInfoRsp()
        {
            ActivityInfoLists =
            {
                new ActivityInfo()
                {
                    ActivityId = 2011001,
                    BeginTime = 0,
                    EndTime = 4294967295,
                    ActivityType = 1, // IGJMCEAJEIP.ACTIVITY_SEA_LAMP
                    ScheduleId = 1,
                    SamLampInfo = new SeaLampActivityDetailInfo()
                    {
                        PhaseId = 4,
                        Contribution = 69420,
                        Days = 7,
                        Progress = 100,
                        TakenContributionRewardLists = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                        TakenPhaseRewardLists = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                        Factor = 12345
                    }
                }
            }
        };
        session.SendPacket(rsp);
    }
}