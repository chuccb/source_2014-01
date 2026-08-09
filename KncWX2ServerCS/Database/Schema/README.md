# SQLite migration rules

The native server uses SQL Server/ODBC and a large collection of T-SQL stored procedures. The C# 14 port deliberately uses SQLite instead.

Migration rules:

1. Preserve table/column semantics from the SQL Server source before simplifying anything.
2. Convert `bigint` to SQLite INTEGER and C# `long`; `tinyint` to INTEGER and C# `byte`; Unicode character types to TEXT; binary types to BLOB.
3. SQL Server computed columns are represented with SQLite generated columns where the SQLite feature set can express the same deterministic expression.
4. SQL Server stored procedures are not emulated as fake SQLite procedures. Their observable behavior is reimplemented in C# repositories/services using parameterized SQL and explicit transactions.
5. Identity/sequence behavior is verified from each original procedure/table instead of applying a global AUTOINCREMENT rule.
6. Date/time representation is selected per column from its original comparison/arithmetic semantics; it is not blindly converted to TEXT.
7. Every migrated table and procedure gets a source-reference note and an integration test before the corresponding server subsystem is considered complete.
