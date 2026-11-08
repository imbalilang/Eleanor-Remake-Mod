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
            DialogueSpeakers.Enabled=true;
            if (!subscribed)
            {
                ModHook.OnModItemDirectoryInit += Items.Register;
                ModHook.OnGameLoadedEarly += StoryDirector.OnGameLoadedEarly;
                ModHook.OnGameLoadedInit += Items.BeginLoad;
                // The native registration API touches the lazy PlayerStore singleton.
                // LoadGame has an existing store here, before save deserialization.
                ModHook.OnGameLoadedInit += MaidProjects.Register;
                ModHook.OnGameLoadedEarly += Items.RepairLoadedShapes;
                ModHook.OnNewGameStarted += StoryDirector.OnNewGameStarted;
                // Native urgent scripted clients/events are resolved before this hook.
                ModHook.OnGenerateCustomerEarly += StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = true;
            }

            DebugTools.Register();
            StartingPerkRegistration.Register();
            ScavAssistance.Register();
            log?.Log("Eleanor Romance enabled.");
        }

        public void OnDisable()
        {
            DialogueSpeakers.Enabled=false;
            if (subscribed)
            {
                ModHook.OnModItemDirectoryInit -= Items.Register;
                ModHook.OnGameLoadedEarly -= StoryDirector.OnGameLoadedEarly;
                ModHook.OnGameLoadedInit -= Items.BeginLoad;
                ModHook.OnGameLoadedInit -= MaidProjects.Register;
                ModHook.OnGameLoadedEarly -= Items.RepairLoadedShapes;
                ModHook.OnNewGameStarted -= StoryDirector.OnNewGameStarted;
                ModHook.OnGenerateCustomerEarly -= StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = false;
            }

            DebugTools.Unregister();
            Items.BeginLoad();
            StartingPerkRegistration.Unregister();
            ScavAssistance.Unregister();
            MaidProjects.Unregister();
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
