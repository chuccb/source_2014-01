CREATE TABLE IF NOT EXISTS GItemInventorySize (
    UnitUID INTEGER NOT NULL,
    InventoryCategory INTEGER NOT NULL,
    NumSlot INTEGER NOT NULL,
    RegDate TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_GItemInventorySize_Unit_Category ON GItemInventorySize(UnitUID, InventoryCategory);
