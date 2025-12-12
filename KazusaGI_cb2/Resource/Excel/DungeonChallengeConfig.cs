using System;

namespace KazusaGI_cb2.Resource.Excel;

public class DungeonChallengeConfig
{
    public uint id;
    public uint targetTextTemplateTextMapHash;
    public uint subTargetTextTemplateTextMapHash;
    public uint progressTextTemplateTextMapHash;
    public uint subProgressTextTemplateTextMapHash;
    public string challengeType = string.Empty;
    public bool noSuccessHint;
}
