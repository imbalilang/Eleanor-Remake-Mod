using UnityEngine;

namespace EleanorRomance
{
    public class ModEntry : IMod
    {
        private static ModLog log;
        private static bool subscribed;

        public static ModManifest Manifest { get; private set; }

        public void Init(ModManifest manifest)
        {
            Manifest = manifest;
            log = new ModLog(manifest);
            log.Log("Eleanor Romance initializing.");
            VersionGate.Refresh();
            log.Log("Game version: " + VersionGate.GameVersion + ", supported: " + VersionGate.IsSupported);
        }

        public void OnEnable()
        {
            if (!subscribed)
            {
                ModHook.OnModItemDirectoryInit += Items.Register;
                ModHook.OnGameLoadedInit += StoryDirector.OnGameLoadedInit;
                ModHook.OnNewGameStarted += StoryDirector.OnNewGameStarted;
                ModHook.OnGenerateCustomerVeryEarly += StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = true;
            }

            DebugTools.Register();
            ScenarioRegistration.RegisterIfUnlocked();
            log?.Log("Eleanor Romance enabled.");
        }

        public void OnDisable()
        {
            if (subscribed)
            {
                ModHook.OnModItemDirectoryInit -= Items.Register;
                ModHook.OnGameLoadedInit -= StoryDirector.OnGameLoadedInit;
                ModHook.OnNewGameStarted -= StoryDirector.OnNewGameStarted;
                ModHook.OnGenerateCustomerVeryEarly -= StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = false;
            }

            DebugTools.Unregister();
            ScenarioRegistration.Unregister();
            log?.Log("Eleanor Romance disabled.");
        }

        public static void Log(string message)
        {
            if (log != null)
            {
                log.Log(message);
            }
            else
            {
                Debug.Log(ModConstants.LogTag + " " + message);
            }
        }

        public static void LogError(string message)
        {
            if (log != null)
            {
                log.Error(message);
            }
            else
            {
                Debug.LogError(ModConstants.LogTag + " " + message);
            }
        }
    }
}
