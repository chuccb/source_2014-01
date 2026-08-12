using System.Runtime.CompilerServices;

internal static class CompatibilityModuleInitializer
{
    [ModuleInitializer]
    internal static void RegisterAdditionalVectors()
    {
        RoomUserManagerStateParityCompatibilityTests.Run();
        BattleFieldMonsterCompatibilityTests.Run();
    }
}
