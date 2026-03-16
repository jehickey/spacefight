#if UNITY_EDITOR
using UnityEditor;

//This ensures that all running jobs started by Icosphere get cleaned up


[InitializeOnLoad]
public static class IcosphereEditorCleanupHook
{
    static IcosphereEditorCleanupHook()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
    }

    static void OnBeforeReload()
    {
        Shapes.Icosphere.Cleanup();
    }
}
#endif
