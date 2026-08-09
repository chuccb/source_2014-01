CREATE TABLE IF NOT EXISTS GResurrectionStone (
    UnitUID INTEGER NOT NULL,
    Quantity INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_GResurrectionStone_UnitUID ON GResurrectionStone(UnitUID);
