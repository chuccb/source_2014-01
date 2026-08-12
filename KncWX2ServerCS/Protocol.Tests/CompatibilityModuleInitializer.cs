using System.Runtime.CompilerServices;

internal static class CompatibilityModuleInitializer
{
    [ModuleInitializer]
    internal static void RegisterAdditionalVectors()
    {
        EventIdCompatibilityTests.Run();
        RoomUserManagerStateParityCompatibilityTests.Run();
        BattleFieldMonsterCompatibilityTests.Run();
        HenirResultTableCompatibilityTests.Run();
    }
}
