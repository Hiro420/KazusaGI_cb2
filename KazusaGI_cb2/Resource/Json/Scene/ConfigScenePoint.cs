using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json.Scene;

public class ConfigScenePoint
{
    [JsonPropertyName("$type")]
    public string TransPointType;
    public ScenePointType Type;
    public List<uint> cutsceneList;
    public List<uint> dungeonIds;
    public List<uint> dungeonRandomList;
    public Vector3 size;
    public Vector3 pos;
    public Vector3 rot;
    public Vector3 tranPos;
    public Vector3 tranRot;
    public uint gadgetId;
    public uint npcId;
    public uint entryPointId;
    public uint showLevel;
    public ushort areaId;
    public string alias;
    public string titleTextID;
    public bool unlocked;
    public bool hideOnMap;
    public bool up;
    public float scale;
    public float velocity;
    public float maxSpringVolume;
}
