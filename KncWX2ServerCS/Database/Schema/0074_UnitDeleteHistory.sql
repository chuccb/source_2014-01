CREATE TABLE IF NOT EXISTS GDeletedNickNameHistory (
    NickName TEXT NOT NULL,
    UnitUID INTEGER NOT NULL,
    Regdate TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_GDeletedNickNameHistory_UnitUID ON GDeletedNickNameHistory(UnitUID);
