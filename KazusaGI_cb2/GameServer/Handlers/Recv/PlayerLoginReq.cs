using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePlayerLoginReq
{
    [Packet.PacketCmdId(PacketId.PlayerLoginReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        PlayerLoginReq req = packet.GetDecodedBody<PlayerLoginReq>();
        PlayerLoginRsp rsp = new PlayerLoginRsp()
        {
            GameBiz = "hk4e",
            IsUseAbilityHash = true,
            //IsNewPlayer = true,
            TargetUid = session.player!.Uid,
            AbilityHashCode = 2004869408, // todo: figure out
        };

        // If the player's position was loaded from the DB (non-zero), use it.
        // Otherwise this is likely a new account so teleport to the scene's born position.
        Vector3 dbPos = session.player.Pos;
        Vector3 bornPos = MainApp.resourceManager.SceneLuas[session.player.SceneId].scene_config.born_pos;
        Vector3 targetPos = dbPos != new Vector3() ? dbPos : bornPos;
        // TeleportToPos will update the player's Pos and persist it.
        session.player.TeleportToPos(session, targetPos, true);
        session.player.SetRot(MainApp.resourceManager.SceneLuas[session.player.SceneId].scene_config.born_rot);

        OpenStateUpdateNotify OpenStateUpdateNotify = new OpenStateUpdateNotify();

        foreach (Resource.OpenStateType i in Enum.GetValues(typeof(Resource.OpenStateType)))
        {
            if (i == OpenStateType.OPEN_STATE_TOWER_FIRST_ENTER) // OPEN_STATE_TOWER_FIRST_ENTER
            {
                OpenStateUpdateNotify.OpenStateMaps.Add(Convert.ToUInt32(i), 0);
                continue;
            }
            OpenStateUpdateNotify.OpenStateMaps.Add(Convert.ToUInt32(i), 1);
        }

        // todo: move the next 2 packets to inventory manager
        StoreWeightLimitNotify storeWeightLimitNotify = new StoreWeightLimitNotify()
        {
            StoreType = StoreType.StorePack,
            WeightLimit = UInt32.MaxValue - 1000
        };
        PlayerStoreNotify playerStoreNotify = new PlayerStoreNotify()
        {
            StoreType = StoreType.StorePack,
            WeightLimit = UInt32.MaxValue - 1000,
        };

        foreach (PlayerItem playerItem in session.player.itemDict.Values)
        {
            playerStoreNotify.ItemLists.Add(new Item()
            {
                Guid = playerItem.Guid,
                ItemId = playerItem.ItemId,
                Material = new Material()
                {
                    Count = playerItem.Count
                }
            });
        }

        foreach (PlayerWeapon playerWeapon in session.player.weaponDict.Values)
        {
            playerStoreNotify.ItemLists.Add(new Item()
            {
                Guid = playerWeapon.Guid,
                ItemId = playerWeapon.WeaponId,
                Equip = new Equip()
                {
                    Weapon = new Weapon()
                    {
                        Level = playerWeapon.Level,
                        Exp = playerWeapon.Exp,
                        PromoteLevel = playerWeapon.PromoteLevel,
                    }
                }
            });
        }

        PlayerDataNotify playerDataNotify = new PlayerDataNotify()
        {
            NickName = "KazusaGI",
            ServerTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RegionId = 1
        };

        AddPropMap(PropType.PROP_IS_SPRING_AUTO_USE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_SPRING_AUTO_USE_PERCENT, 50, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_IS_FLYABLE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_IS_TRANSFERABLE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_CUR_PERSIST_STAMINA, 24000, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_MAX_STAMINA, 24000, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_LEVEL, (uint)session.player!.Level, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_EXP, 0, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_HCOIN, 999999, playerDataNotify.PropMaps); // todo: get from inventory
        AddPropMap(PropType.PROP_IS_MP_MODE_AVAILABLE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_MP_SETTING_TYPE, (uint)MpSettingType.MpSettingNoEnter, playerDataNotify.PropMaps);

        ActivityScheduleInfoNotify activity = new ActivityScheduleInfoNotify()
        {
            ActivityScheduleLists =
            {
                new ActivityScheduleInfo()
                {
                    ActivityId = 1001,
                    BeginTime = 0,
                    EndTime = 4102415999,
                    IsOpen = true,
                    ScheduleId = 1
                }
            },
            RemainFlySeaLampNum = 30
        };
        session.SendPacket(GetWindSeed(session.player.Uid));
        session.SendPacket(OpenStateUpdateNotify);
        session.SendPacket(storeWeightLimitNotify);
        session.SendPacket(playerStoreNotify);
        session.SendPacket(playerDataNotify);
        session.player.SendAvatarDataNotify(session);
        Investigation.SendInvestigationNotify(session);
        session.player.EnterScene(session, session.player.SceneId);
        session.SendPacket(activity);
        session.SendPacket(rsp);
    }

    private static void AddPropMap(PropType propType, uint ival, Dictionary<uint, PropValue> keyValuePairs)
    {
        keyValuePairs.Add((uint)propType, new PropValue()
        {
            Type = (uint)propType,
            Ival = ival,
            Val = ival
        });
    }

    private static PlayerLuaShellNotify GetWindSeed(uint uid)
    {
        string text = $"KazusaGI | UID: {uid}";
        string startColor = "F4C4F3";
        string endColor = "FC67FA";
        string encodedName = GenerateGradientText(text, startColor, endColor);
        PlayerLuaShellNotify playerLuaShellNotify = new PlayerLuaShellNotify()
        {
            Id = 3,
            LuaShell = Encoding.UTF8.GetBytes($"CS.UnityEngine.GameObject.Find(\"/BetaWatermarkCanvas(Clone)/Panel/TxtUID\"):GetComponent(\"Text\").text=\"{encodedName}\"")
        };
        return playerLuaShellNotify;
    }

    static string ColorToHex(float r, float g, float b)
    {
        return $"{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
    }

    static (float r, float g, float b) LerpColor((float r, float g, float b) start, (float r, float g, float b) end, float t)
    {
        return (
            start.r + (end.r - start.r) * t,
            start.g + (end.g - start.g) * t,
            start.b + (end.b - start.b) * t
        );
    }

    public static string GenerateGradientText(string input, string startHex, string endHex)
    {
        (float r, float g, float b) start = (
            Convert.ToInt32(startHex.Substring(0, 2), 16) / 255f,
            Convert.ToInt32(startHex.Substring(2, 4 - 2), 16) / 255f,
            Convert.ToInt32(startHex.Substring(4, 6 - 4), 16) / 255f
        );

        (float r, float g, float b) end = (
            Convert.ToInt32(endHex.Substring(0, 2), 16) / 255f,
            Convert.ToInt32(endHex.Substring(2, 4 - 2), 16) / 255f,
            Convert.ToInt32(endHex.Substring(4, 6 - 4), 16) / 255f
        );

        var sb = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            float t = (float)i / (input.Length - 1);
            var color = LerpColor(start, end, t);
            string hex = ColorToHex(color.r, color.g, color.b);
            sb.Append($"<color=#{hex}>{input[i]}</color>");
        }

        return sb.ToString();
    }
}