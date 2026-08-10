namespace KncWX2Server.Protocol;

/// <summary>Feature gates that change the native KUnitInfo wire layout.</summary>
public sealed record KUnitInfoWireOptions
{
    public bool PvpNewSystem { get; init; }
    public bool PvpSeason2 { get; init; }
    public bool BattleFieldSystem { get; init; }
    public bool ReformTheGateOfDarkness { get; init; }
    public bool LimitedDungeonPlayTimes { get; init; }
    public bool PcBangType { get; init; }
    public bool TitleDataSize { get; init; }
    public bool GuildTest { get; init; }
    public bool UnitWaitDelete { get; init; }
    public bool AddWarpButton { get; init; }
    public bool GrowUpSocket { get; init; }
    public bool ChinaSpiritEvent { get; init; }
    public bool RecruitEventQuestForNewUser { get; init; }
    public bool NewYearEvent2014 { get; init; }
    public bool GuildSkillTest { get; init; }
    public bool SkillNote { get; init; }
    public bool ExpandSlotIdDataSize { get; init; }
    public bool NewItemSystem201305 { get; init; }
    public bool GoldTicket { get; init; }
    public bool ItemState { get; init; }
    public bool RandomItemSocket { get; init; }

    public static KUnitInfoWireOptions Default { get; } = new();
}

/// <summary>Serializes the C# KUnitInfo model using the native CommonPacket field order.</summary>
public static class KUnitInfoSerializer
{
    public static bool Put(this KSerializer serializer, KStat value) =>
        serializer.Put(value.BaseHp)
        && serializer.Put(value.AtkPhysic)
        && serializer.Put(value.AtkMagic)
        && serializer.Put(value.DefPhysic)
        && serializer.Put(value.DefMagic);

    public static bool Get(this KSerializer serializer, KStat value) =>
        serializer.Get(out value.BaseHp)
        && serializer.Get(out value.AtkPhysic)
        && serializer.Get(out value.AtkMagic)
        && serializer.Get(out value.DefPhysic)
        && serializer.Get(out value.DefMagic);

    public static bool Put(this KSerializer serializer, KSkillData value) =>
        serializer.Put(value.SkillId) && serializer.Put(value.SkillLevel);

    public static bool Get(this KSerializer serializer, KSkillData value) =>
        serializer.Get(out value.SkillId) && serializer.Get(out value.SkillLevel);

    public static bool Put(this KSerializer serializer, KUnitSkillData value, KUnitInfoWireOptions options)
    {
        if (value.EquippedSkill.Length != KUnitSkillData.EquippedSkillSlotCount ||
            value.EquippedSkillSlotB.Length != KUnitSkillData.EquippedSkillSlotCount)
        {
            return false;
        }

        // Native arrays are serialized as complete ranges, not interleaved pairs.
        if (!PutSkillRange(serializer, value.EquippedSkill) ||
            !PutSkillRange(serializer, value.EquippedSkillSlotB) ||
            !serializer.PutW(value.SkillSlotBEndDate) ||
            !serializer.Put(value.SkillSlotBExpirationState) ||
            !serializer.PutVector(value.PassiveSkill, static (s, skill) => s.Put(skill)))
        {
            return false;
        }

        if (options.GuildSkillTest &&
            !serializer.PutVector(value.GuildPassiveSkill, static (s, skill) => s.Put(skill)))
        {
            return false;
        }

        return !options.SkillNote ||
               serializer.PutVector(value.SkillNote, static (s, item) => s.Put(item));
    }

    public static bool Get(this KSerializer serializer, KUnitSkillData value, KUnitInfoWireOptions options)
    {
        if (!GetSkillRange(serializer, value.EquippedSkill) ||
            !GetSkillRange(serializer, value.EquippedSkillSlotB) ||
            !serializer.GetW(out value.SkillSlotBEndDate) ||
            !serializer.Get(out value.SkillSlotBExpirationState) ||
            !serializer.GetVector(value.PassiveSkill, static s => GetSkill(s)))
        {
            return false;
        }

        if (options.GuildSkillTest &&
            !serializer.GetVector(value.GuildPassiveSkill, static s => GetSkill(s)))
        {
            return false;
        }

        return !options.SkillNote ||
               serializer.GetVector(value.SkillNote, static s => GetInt(s));
    }

    public static bool Put(this KSerializer serializer, KDungeonClearInfo value) =>
        serializer.Put(value.DungeonId)
        && serializer.Put(value.MaxScore)
        && serializer.Put(value.MaxTotalRank)
        && serializer.PutW(value.ClearTime)
        && serializer.Put(value.IsNew);

    public static bool Get(this KSerializer serializer, KDungeonClearInfo value) =>
        serializer.Get(out value.DungeonId)
        && serializer.Get(out value.MaxScore)
        && serializer.Get(out value.MaxTotalRank)
        && serializer.GetW(out value.ClearTime)
        && serializer.Get(out value.IsNew);

    public static bool Put(this KSerializer serializer, KTCClearInfo value) =>
        serializer.Put(value.TcId)
        && serializer.PutW(value.ClearTime)
        && serializer.Put(value.IsNew);

    public static bool Get(this KSerializer serializer, KTCClearInfo value) =>
        serializer.Get(out value.TcId)
        && serializer.GetW(out value.ClearTime)
        && serializer.Get(out value.IsNew);

    public static bool Put(this KSerializer serializer, KDungeonPlayInfo value) =>
        serializer.Put(value.DungeonId)
        && serializer.Put(value.PlayTimes)
        && serializer.Put(value.ClearTimes)
        && serializer.Put(value.IsNew);

    public static bool Get(this KSerializer serializer, KDungeonPlayInfo value) =>
        serializer.Get(out value.DungeonId)
        && serializer.Get(out value.PlayTimes)
        && serializer.Get(out value.ClearTimes)
        && serializer.Get(out value.IsNew);

    public static bool Put(this KSerializer serializer, KLastPositionInfo value) =>
        serializer.Put(value.MapId)
        && serializer.Put(value.LastTouchLineIndex)
        && serializer.Put(value.LastPosValue);

    public static bool Get(this KSerializer serializer, KLastPositionInfo value) =>
        serializer.Get(out value.MapId)
        && serializer.Get(out value.LastTouchLineIndex)
        && serializer.Get(out value.LastPosValue);

    public static bool Put(this KSerializer serializer, KItemAttributeEnchantInfo value) =>
        serializer.Put(value.AttribEnchant0)
        && serializer.Put(value.AttribEnchant1)
        && serializer.Put(value.AttribEnchant2);

    public static bool Get(this KSerializer serializer, KItemAttributeEnchantInfo value) =>
        serializer.Get(out value.AttribEnchant0)
        && serializer.Get(out value.AttribEnchant1)
        && serializer.Get(out value.AttribEnchant2);

    public static bool Put(this KSerializer serializer, KItemInfo value, KUnitInfoWireOptions options)
    {
        if (!serializer.Put(value.ItemId)
            || !serializer.Put(value.UsageType)
            || !serializer.Put(value.Quantity)
            || !serializer.Put(value.Endurance)
            || !serializer.Put(value.SealData)
            || !serializer.Put(value.EnchantLevel)
            || !serializer.Put(value.AttributeEnchantInfo)
            || !PutSocketVector(serializer, value.ItemSocket, options.NewItemSystem201305)
            || !serializer.Put(value.Period)
            || !serializer.PutW(value.ExpirationDate))
        {
            return false;
        }

        if (options.RandomItemSocket &&
            !PutSocketVector(serializer, value.RandomSocket, options.NewItemSystem201305))
        {
            return false;
        }

        if (options.ItemState && !serializer.Put(value.ItemState))
        {
            return false;
        }

        return !options.GoldTicket || serializer.Put(value.GoldTicketKeyUid);
    }

    public static bool Get(this KSerializer serializer, KItemInfo value, KUnitInfoWireOptions options)
    {
        if (!serializer.Get(out value.ItemId)
            || !serializer.Get(out value.UsageType)
            || !serializer.Get(out value.Quantity)
            || !serializer.Get(out value.Endurance)
            || !serializer.Get(out value.SealData)
            || !serializer.Get(out value.EnchantLevel)
            || !serializer.Get(value.AttributeEnchantInfo)
            || !GetSocketVector(serializer, value.ItemSocket, options.NewItemSystem201305)
            || !serializer.Get(out value.Period)
            || !serializer.GetW(out value.ExpirationDate))
        {
            return false;
        }

        if (options.RandomItemSocket &&
            !GetSocketVector(serializer, value.RandomSocket, options.NewItemSystem201305))
        {
            return false;
        }

        if (options.ItemState && !serializer.Get(out value.ItemState))
        {
            return false;
        }

        return !options.GoldTicket || serializer.Get(out value.GoldTicketKeyUid);
    }

    public static bool Put(this KSerializer serializer, KInventoryItemInfo value, KUnitInfoWireOptions options) =>
        serializer.Put(value.ItemUid)
        && serializer.Put(value.SlotCategory)
        && PutSlotId(serializer, value.SlotId, options.ExpandSlotIdDataSize)
        && serializer.Put(value.ItemInfo, options);

    public static bool Get(this KSerializer serializer, KInventoryItemInfo value, KUnitInfoWireOptions options) =>
        serializer.Get(out value.ItemUid)
        && serializer.Get(out value.SlotCategory)
        && GetSlotId(serializer, value, options.ExpandSlotIdDataSize)
        && serializer.Get(value.ItemInfo, options);

    public static bool Put(this KSerializer serializer, KUserGuildInfo value) =>
        serializer.Put(value.GuildUid)
        && serializer.PutW(value.GuildName)
        && serializer.Put(value.MembershipGrade)
        && serializer.Put(value.HonorPoint);

    public static bool Get(this KSerializer serializer, KUserGuildInfo value) =>
        serializer.Get(out value.GuildUid)
        && serializer.GetW(out value.GuildName)
        && serializer.Get(out value.MembershipGrade)
        && serializer.Get(out value.HonorPoint);

    public static bool Put(this KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions? options = null)
    {
        options ??= KUnitInfoWireOptions.Default;

        if (!serializer.Put(value.OwnerUserUid)
            || !serializer.Put(value.AuthLevel)
            || !serializer.Put(value.UnitUid)
            || !serializer.Put(value.KnmSerialNumber)
            || !serializer.Put(value.UnitClass)
            || !serializer.PutW(value.NickName)
            || !serializer.PutW(value.Ip)
            || !serializer.Put(value.Port)
            || !serializer.Put(value.Ed)
            || !serializer.Put(value.Level)
            || !serializer.Put(value.Exp))
        {
            return false;
        }

        if (options.PvpNewSystem)
        {
            if (!serializer.Put(value.OfficialMatchCount)
                || !serializer.Put(value.Rating)
                || !serializer.Put(value.MaxRating)
                || !serializer.Put(value.RPoint)
                || !serializer.Put(value.APoint)
                || !serializer.Put(value.IsWinBeforeMatch))
            {
                return false;
            }

            if (options.PvpSeason2 &&
                (!serializer.Put(value.Rank)
                 || !serializer.Put(value.KFactor)
                 || !serializer.Put(value.IsRedistributionUser)
                 || !serializer.Put(value.PastSeasonWin)))
            {
                return false;
            }
        }
        else if (!serializer.Put(value.PvpEmblem)
                 || !serializer.Put(value.VsPoint)
                 || !serializer.Put(value.VsPointMax))
        {
            return false;
        }

        if (!serializer.Put(value.SPoint)
            || !serializer.Put(value.CsPoint)
            || !serializer.Put(value.MaxCsPoint)
            || !serializer.PutW(value.CsPointEndDate)
            || !serializer.Put(value.NowBaseLevelExp)
            || !serializer.Put(value.NextBaseLevelExp)
            || !serializer.Put(value.StraightVictories)
            || !serializer.Put(value.Stat)
            || !serializer.Put(value.GameStat))
        {
            return false;
        }

        if (options.BattleFieldSystem)
        {
            if (!serializer.Put(value.LastPosition))
            {
                return false;
            }
        }
        else if (!serializer.Put(value.LegacyMapId)
                 || !serializer.Put(value.LegacyLastTouchLineIndex)
                 || !serializer.Put(value.LegacyLastPosValue))
        {
            return false;
        }

        // SERV_REFORM_THE_GATE_OF_DARKNESS requires KRecordBuffInfo, which is not yet modeled.
        if (options.ReformTheGateOfDarkness)
        {
            return false;
        }

        if (!serializer.Put(value.Win)
            || !serializer.Put(value.Lose)
            || !serializer.PutMap(value.DungeonClear, static (s, key) => s.Put(key), static (s, item) => s.Put(item))
            || !serializer.PutMap(value.TCClear, static (s, key) => s.Put(key), static (s, item) => s.Put(item)))
        {
            return false;
        }

        if (options.LimitedDungeonPlayTimes &&
            !serializer.PutMap(value.DungeonPlay, static (s, key) => s.Put(key), static (s, item) => s.Put(item)))
        {
            return false;
        }

        if (!serializer.PutMap(value.EquippedItem,
                static (s, key) => s.Put(key),
                (s, item) => s.Put(item, options))
            || !serializer.Put(value.UnitSkillData, options)
            || !serializer.Put(value.IsParty)
            || !serializer.Put(value.SpiritMax)
            || !serializer.Put(value.Spirit)
            || !serializer.Put(value.IsGameBang))
        {
            return false;
        }

        if (options.PcBangType && !serializer.Put(value.PcBangType))
        {
            return false;
        }

        if (options.TitleDataSize)
        {
            if (!serializer.Put(value.TitleId))
            {
                return false;
            }
        }
        else if (!serializer.Put(value.LegacyTitleId))
        {
            return false;
        }

        if (options.GuildTest && !serializer.Put(value.UserGuildInfo))
        {
            return false;
        }

        if (options.UnitWaitDelete &&
            (!serializer.PutW(value.LastDate)
             || !serializer.Put(value.Deleted)
             || !serializer.Put(value.DeleteAvailableDate)
             || !serializer.Put(value.RestoreAvailableDate)))
        {
            return false;
        }

        if (options.AddWarpButton && !serializer.Put(value.WarpVipEndDate))
        {
            return false;
        }

        if (options.GrowUpSocket &&
            (!serializer.Put(value.EventQuestClearCount) || !serializer.Put(value.ExchangeCount)))
        {
            return false;
        }

        if (options.ChinaSpiritEvent)
        {
            if (value.ChinaSpirit.Length != 6)
            {
                return false;
            }

            foreach (var spirit in value.ChinaSpirit)
            {
                if (!serializer.Put(spirit))
                {
                    return false;
                }
            }
        }

        if (options.RecruitEventQuestForNewUser && !serializer.Put(value.Recruit))
        {
            return false;
        }

        return !options.NewYearEvent2014 ||
               serializer.Put(value.OldYearMissionRewardedLevel) && serializer.Put(value.NewYearMissionStepId);
    }

    public static bool Get(this KSerializer serializer, KUnitInfo value, KUnitInfoWireOptions? options = null)
    {
        options ??= KUnitInfoWireOptions.Default;

        if (!serializer.Get(out value.OwnerUserUid)
            || !serializer.Get(out value.AuthLevel)
            || !serializer.Get(out value.UnitUid)
            || !serializer.Get(out value.KnmSerialNumber)
            || !serializer.Get(out value.UnitClass)
            || !serializer.GetW(out value.NickName)
            || !serializer.GetW(out value.Ip)
            || !serializer.Get(out value.Port)
            || !serializer.Get(out value.Ed)
            || !serializer.Get(out value.Level)
            || !serializer.Get(out value.Exp))
        {
            return false;
        }

        if (options.PvpNewSystem)
        {
            if (!serializer.Get(out value.OfficialMatchCount)
                || !serializer.Get(out value.Rating)
                || !serializer.Get(out value.MaxRating)
                || !serializer.Get(out value.RPoint)
                || !serializer.Get(out value.APoint)
                || !serializer.Get(out value.IsWinBeforeMatch))
            {
                return false;
            }

            if (options.PvpSeason2 &&
                (!serializer.Get(out value.Rank)
                 || !serializer.Get(out value.KFactor)
                 || !serializer.Get(out value.IsRedistributionUser)
                 || !serializer.Get(out value.PastSeasonWin)))
            {
                return false;
            }
        }
        else if (!serializer.Get(out value.PvpEmblem)
                 || !serializer.Get(out value.VsPoint)
                 || !serializer.Get(out value.VsPointMax))
        {
            return false;
        }

        if (!serializer.Get(out value.SPoint)
            || !serializer.Get(out value.CsPoint)
            || !serializer.Get(out value.MaxCsPoint)
            || !serializer.GetW(out value.CsPointEndDate)
            || !serializer.Get(out value.NowBaseLevelExp)
            || !serializer.Get(out value.NextBaseLevelExp)
            || !serializer.Get(out value.StraightVictories)
            || !serializer.Get(value.Stat)
            || !serializer.Get(value.GameStat))
        {
            return false;
        }

        if (options.BattleFieldSystem)
        {
            if (!serializer.Get(value.LastPosition))
            {
                return false;
            }
        }
        else if (!serializer.Get(out value.LegacyMapId)
                 || !serializer.Get(out value.LegacyLastTouchLineIndex)
                 || !serializer.Get(out value.LegacyLastPosValue))
        {
            return false;
        }

        if (options.ReformTheGateOfDarkness)
        {
            return false;
        }

        if (!serializer.Get(out value.Win)
            || !serializer.Get(out value.Lose)
            || !serializer.GetMap(value.DungeonClear, static s => GetInt(s), static s => GetDungeonClear(s))
            || !serializer.GetMap(value.TCClear, static s => GetInt(s), static s => GetTCClear(s)))
        {
            return false;
        }

        if (options.LimitedDungeonPlayTimes &&
            !serializer.GetMap(value.DungeonPlay, static s => GetInt(s), static s => GetDungeonPlay(s)))
        {
            return false;
        }

        if (!serializer.GetMap(value.EquippedItem,
                static s => GetInt(s),
                s => GetInventoryItem(s, options))
            || !serializer.Get(value.UnitSkillData, options)
            || !serializer.Get(out value.IsParty)
            || !serializer.Get(out value.SpiritMax)
            || !serializer.Get(out value.Spirit)
            || !serializer.Get(out value.IsGameBang))
        {
            return false;
        }

        if (options.PcBangType && !serializer.Get(out value.PcBangType))
        {
            return false;
        }

        if (options.TitleDataSize)
        {
            if (!serializer.Get(out value.TitleId))
            {
                return false;
            }
        }
        else if (!serializer.Get(out value.LegacyTitleId))
        {
            return false;
        }

        if (options.GuildTest && !serializer.Get(value.UserGuildInfo))
        {
            return false;
        }

        if (options.UnitWaitDelete &&
            (!serializer.GetW(out value.LastDate)
             || !serializer.Get(out value.Deleted)
             || !serializer.Get(out value.DeleteAvailableDate)
             || !serializer.Get(out value.RestoreAvailableDate)))
        {
            return false;
        }

        if (options.AddWarpButton && !serializer.Get(out value.WarpVipEndDate))
        {
            return false;
        }

        if (options.GrowUpSocket &&
            (!serializer.Get(out value.EventQuestClearCount) || !serializer.Get(out value.ExchangeCount)))
        {
            return false;
        }

        if (options.ChinaSpiritEvent)
        {
            if (value.ChinaSpirit.Length != 6)
            {
                return false;
            }

            for (var index = 0; index < value.ChinaSpirit.Length; index++)
            {
                if (!serializer.Get(out value.ChinaSpirit[index]))
                {
                    return false;
                }
            }
        }

        if (options.RecruitEventQuestForNewUser && !serializer.Get(out value.Recruit))
        {
            return false;
        }

        return !options.NewYearEvent2014 ||
               serializer.Get(out value.OldYearMissionRewardedLevel) && serializer.Get(out value.NewYearMissionStepId);
    }

    private static bool PutSkillRange(KSerializer serializer, IReadOnlyList<KSkillData> skills) =>
        skills.All(skill => serializer.Put(skill));

    private static bool GetSkillRange(KSerializer serializer, IReadOnlyList<KSkillData> skills) =>
        skills.All(skill => serializer.Get(skill));

    private static bool PutSocketVector(KSerializer serializer, IReadOnlyList<int> values, bool useInt32) =>
        useInt32
            ? serializer.PutVector(values, static (s, value) => s.Put(value))
            : serializer.PutVector(values, static (s, value) => s.Put(checked((short)value)));

    private static bool GetSocketVector(KSerializer serializer, ICollection<int> values, bool useInt32) =>
        useInt32
            ? serializer.GetVector(values, static s => GetInt(s))
            : serializer.GetVector(values, static s => GetShortAsInt(s));

    private static bool PutSlotId(KSerializer serializer, int value, bool expanded) =>
        expanded ? serializer.Put(checked((short)value)) : serializer.Put(checked((sbyte)value));

    private static bool GetSlotId(KSerializer serializer, KInventoryItemInfo value, bool expanded)
    {
        if (expanded)
        {
            if (!serializer.Get(out short slotId))
            {
                return false;
            }

            value.SlotId = slotId;
            return true;
        }

        if (!serializer.Get(out sbyte legacySlotId))
        {
            return false;
        }

        value.SlotId = legacySlotId;
        return true;
    }

    private static (bool Ok, int Value) GetInt(KSerializer serializer)
    {
        var ok = serializer.Get(out int value);
        return (ok, value);
    }

    private static (bool Ok, int Value) GetShortAsInt(KSerializer serializer)
    {
        var ok = serializer.Get(out short value);
        return (ok, value);
    }

    private static (bool Ok, KSkillData Value) GetSkill(KSerializer serializer)
    {
        var value = new KSkillData();
        return (serializer.Get(value), value);
    }

    private static (bool Ok, KDungeonClearInfo Value) GetDungeonClear(KSerializer serializer)
    {
        var value = new KDungeonClearInfo();
        return (serializer.Get(value), value);
    }

    private static (bool Ok, KTCClearInfo Value) GetTCClear(KSerializer serializer)
    {
        var value = new KTCClearInfo();
        return (serializer.Get(value), value);
    }

    private static (bool Ok, KDungeonPlayInfo Value) GetDungeonPlay(KSerializer serializer)
    {
        var value = new KDungeonPlayInfo();
        return (serializer.Get(value), value);
    }

    private static (bool Ok, KInventoryItemInfo Value) GetInventoryItem(
        KSerializer serializer,
        KUnitInfoWireOptions options)
    {
        var value = new KInventoryItemInfo();
        return (serializer.Get(value, options), value);
    }
}
