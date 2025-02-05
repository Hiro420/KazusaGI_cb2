﻿using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KazusaGI_cb2.Command.CommandManager;

namespace KazusaGI_cb2.Command.Commands;

public class Spawn
{
    [Command("spawn")]
    public class SceneCommand
    {
        static ResourceManager resourceManager = MainApp.resourceManager;
        public static void Execute(string[] args, Session? session)
        {
            if (session == null)
            {
                logger.LogError($"Please target a session first");
                return;
            }

            if (args.Length == 0 || !uint.TryParse(args[0], out uint monsterId) || !resourceManager.MonsterExcel.ContainsKey(monsterId))
            {
                logger.LogError($"Please enter a valid monster id");
                return;
            }

            MonsterExcelConfig monster = resourceManager.MonsterExcel[monsterId];

            MonsterEntity monsterEntity = new MonsterEntity(session, monsterId, null, null);
            session.entityMap.Add(monsterEntity._EntityId, monsterEntity);

            session.SendPacket(new SceneEntityAppearNotify()
            {
                AppearType = VisionType.VisionNone,
                EntityLists = { monsterEntity.ToSceneEntityInfo(session) }
            });
            EntityFightPropNotify entityFightPropNotify = new EntityFightPropNotify()
            {
                EntityId = monsterEntity._EntityId
            };
            foreach(KeyValuePair<uint,float> prop in monsterEntity.GetFightProps())
            {
                entityFightPropNotify.FightPropMaps.Add(prop.Key, prop.Value);
            }
            // session.SendPacket(entityFightPropNotify);
        }
    }
}
