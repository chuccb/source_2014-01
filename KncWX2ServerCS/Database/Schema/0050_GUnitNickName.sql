CREATE TABLE IF NOT EXISTS GUnitNickName (
    UnitUID INTEGER NOT NULL,
    NickName TEXT NULL,
    RegDate TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_GUnitNickName_NickName ON GUnitNickName(NickName);
CREATE INDEX IF NOT EXISTS ix_GUnitNickName_UnitUID ON GUnitNickName(UnitUID);
