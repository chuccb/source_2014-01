CREATE TABLE IF NOT EXISTS GDeletedNickNameHistory (
    NickName TEXT NOT NULL,
    UnitUID INTEGER NOT NULL,
    Regdate TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_GDeletedNickNameHistory_NickName_Regdate
    ON GDeletedNickNameHistory(NickName, Regdate DESC);
