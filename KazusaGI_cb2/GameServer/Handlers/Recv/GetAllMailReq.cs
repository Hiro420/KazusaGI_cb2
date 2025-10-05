using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetAllMailReq
{
    [Packet.PacketCmdId(PacketId.GetAllMailReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        GetAllMailReq req = packet.GetDecodedBody<GetAllMailReq>();
        GetAllMailRsp rsp = new GetAllMailRsp();
        rsp.MailLists.Add(new MailData()
        {
            MailId = 1,
            MailTextContent = new MailTextContent()
            {
                Title = "Welcome to KazusaGI",
                Content = "Hope you have fun, don't forget to leave a star on GitHub if you enjoy.\n"
                            + "Please remember, the server is still work in progress and many things may not work as intended.\n"
                            + "If you find any bugs, feel free to open an issue on GitHub or open a pull request for a fix",
                Sender = "Hiro420"
            },
            //
            SendTime = 0,
            ExpireTime = 1999999999,
            Importance = 0,
            IsRead = true,
            IsAttachmentGot = true,
        });
        session.SendPacket(rsp);
    }
}