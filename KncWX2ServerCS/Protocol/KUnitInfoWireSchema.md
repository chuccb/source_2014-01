# KUnitInfo wire schema

This document records the native `CommonPacket.cpp` serialization order used as the compatibility specification for the C# 14 migration.

## Core order

1. `m_iOwnerUserUID` — `UidType`
2. `m_cAuthLevel` — `char`
3. `m_nUnitUID` — `UidType`
4. `m_uiKNMSerialNum` — `u_int`
5. `m_cUnitClass` — `char`
6. `m_wstrNickName` — `std::wstring`
7. `m_wstrIP` — `std::wstring`
8. `m_usPort` — `USHORT`
9. `m_iED` — `int`
10. `m_ucLevel` — `UCHAR`
11. `m_iEXP` — `int`

## PVP feature gate

When `SERV_PVP_NEW_SYSTEM` is enabled:

- `m_iOfficialMatchCnt`
- `m_iRating`
- `m_iMaxRating`
- `m_iRPoint`
- `m_iAPoint`
- `m_bIsWinBeforeMatch`

When `SERV_2012_PVP_SEASON2` is also enabled:

- `m_cRank`
- `m_fKFactor`
- `m_bIsRedistributionUser`
- `m_iPastSeasonWin`

Otherwise the legacy PVP fields are:

- `m_iPVPEmblem`
- `m_iVSPoint`
- `m_iVSPointMax`

## Remaining core fields

- `m_iSPoint`
- `m_iCSPoint`
- `m_iMaxCSPoint`
- `m_wstrCSPointEndDate`
- `m_nNowBaseLevelEXP`
- `m_nNextBaseLevelEXP`
- `m_nStraightVictories`
- `m_kStat`
- `m_kGameStat`

## Battle-field / legacy position

`SERV_BATTLE_FIELD_SYSTEM` selects `KLastPositionInfo`.

Otherwise the native fields are:

- `m_nMapID`
- `m_ucLastTouchLineIndex`
- `m_usLastPosValue`

## Optional collections and fields

- `SERV_REFORM_THE_GATE_OF_DARKNESS`: `m_vecBuffInfo`
- `m_mapDungeonClear`
- `m_mapTCClear`
- `SERV_LIMITED_DUNGEON_PLAY_TIMES`: `m_mapDungeonPlay`
- `m_mapEquippedItem`
- `m_UnitSkillData`
- `m_bIsParty`
- `m_iSpiritMax`
- `m_iSpirit`
- `m_bIsGameBang`
- `SERV_PC_BANG_TYPE`: `m_iPcBangType`
- `SERV_TITLE_DATA_SIZE`: `m_iTitleID`, otherwise `m_sTitleID`
- `GUILD_TEST`: `m_kUserGuildInfo`
- `SERV_UNIT_WAIT_DELETE`: delete/restore state fields
- `SERV_ADD_WARP_BUTTON`: `m_trWarpVipEndData`
- `SERV_GROW_UP_SOCKET`: growth counters
- `SERV_CHINA_SPIRIT_EVENT`: six-element spirit array
- `SERV_RECRUIT_EVENT_QUEST_FOR_NEW_USER`: `m_bRecruit`
- `SERV_NEW_YEAR_EVENT_2014`: old-year level and mission step

## STL map wire format

Native `SerMap.h` delegates to `SerSTL::PutRange`. Each entry is serialized as a `std::pair`, therefore tagged mode is:

```text
Map tag
DWORD count
Pair tag
key
value
Pair tag
key
value
...
```

The C# `PutMap`/`GetMap` implementation follows this layout. Native `std::map` ordering must be represented by an ordered C# collection such as `SortedDictionary<TKey, TValue>` when byte-for-byte output is required.

## Verification rule

Do not add a C# field merely because it exists in the native class. A field is part of the wire contract only when the active native `SERIALIZE_DEFINE_PUT/GET` path includes it under the same feature gates. PUT and GET order must remain identical.
