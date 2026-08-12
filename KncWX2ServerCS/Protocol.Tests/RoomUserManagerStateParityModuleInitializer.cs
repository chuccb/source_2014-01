using System.Runtime.CompilerServices;

internal static class RoomUserManagerStateParityModuleInitializer
{
    [ModuleInitializer]
    public static void Run()
    {
        RoomUserManagerStateParityCompatibilityTests.Run();
    }
}
