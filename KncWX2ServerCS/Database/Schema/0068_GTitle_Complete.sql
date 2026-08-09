CREATE TABLE IF NOT EXISTS GTitle_Complete (
    UnitUID INTEGER NOT NULL,
    TitleID INTEGER NOT NULL,
    EndDate TEXT NULL,
    IsHang INTEGER NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_GTitle_Complete_Unit_Title ON GTitle_Complete(UnitUID, TitleID);
