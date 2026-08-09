CREATE TABLE IF NOT EXISTS GoodsObjectList (
    ItemUID INTEGER PRIMARY KEY AUTOINCREMENT,
    OwnerLogin TEXT NOT NULL,
    BuyerLogin TEXT NOT NULL,
    ItemID INTEGER NOT NULL,
    RegDate TEXT NOT NULL,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    Period INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_GoodsObjectList_Owner_Item ON GoodsObjectList(OwnerLogin, ItemID, Period);

CREATE TABLE IF NOT EXISTS DurationItemObjectList (
    DurationItemUID INTEGER PRIMARY KEY AUTOINCREMENT,
    OwnerLogin TEXT NOT NULL,
    BuyerLogin TEXT NOT NULL,
    GoodsID INTEGER NOT NULL,
    Duration INTEGER NOT NULL,
    RegDate TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_DurationItemObjectList_Owner_Goods ON DurationItemObjectList(OwnerLogin, GoodsID);
