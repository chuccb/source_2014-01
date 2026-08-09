CREATE TABLE IF NOT EXISTS GTitle_Mission (
    UnitUID INTEGER NOT NULL,
    TitleID INTEGER NOT NULL,
    SubMission1 INTEGER NULL,
    SubMission2 INTEGER NULL,
    SubMission3 INTEGER NULL,
    SubMission4 INTEGER NULL,
    SubMission5 INTEGER NULL
);
CREATE INDEX IF NOT EXISTS ix_GTitle_Mission_Unit_Title ON GTitle_Mission(UnitUID, TitleID);
