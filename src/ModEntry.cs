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
            log.Log("Game version: " + VersionGate.GameVersion
                + " (recorded for diagnostics; version strings do not gate the story).");
        }

        public void OnEnable()
        {
            DialogueSpeakers.Enabled=true;
            DualClosingDay.Enabled=true;
            LoveA2Closing.Enabled=true;
            LoveWaterShortage.Enabled=true;
            DualShowcaseEffects.Enabled=true;
            if (!subscribed)
            {
                ModHook.OnModItemDirectoryInit += Items.Register;
                ModHook.OnGameLoadedEarly += StoryDirector.OnGameLoadedEarly;
                ModHook.OnGameLoadedLate += LoveA2Closing.OnLoaded;
                ModHook.OnGameLoadedLate += ChapterLoadedReportPatch.OnLoaded;
                // The native registration API touches the lazy PlayerStore singleton.
                // LoadGame has an existing store here, before save deserialization.
                ModHook.OnGameLoadedInit += MaidProjects.Register;
                ModHook.OnNewGameStarted += StoryDirector.OnNewGameStarted;
                // Native urgent scripted clients/events are resolved before this hook.
                ModHook.OnGenerateCustomerEarly += StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = true;
            }

            PortraitCompat.RegisterConfig();
            DebugTools.Register();
            StartingPerkRegistration.Register();
            ScavAssistance.Register();
            log?.Log("Eleanor Romance enabled.");
        }

        public void OnDisable()
        {
            DialogueSpeakers.Enabled=false;
            DualClosingDay.Enabled=false;
            LoveA2Closing.Enabled=false;
            LoveA2Closing.Reset();
            LoveWaterShortage.Enabled=false;
            StoryPresentation.Clear();
            DualShowcaseEffects.Enabled=false;
            DualShowcaseEffects.ClearMemory();
            if (subscribed)
            {
                ModHook.OnModItemDirectoryInit -= Items.Register;
                ModHook.OnGameLoadedEarly -= StoryDirector.OnGameLoadedEarly;
                ModHook.OnGameLoadedLate -= LoveA2Closing.OnLoaded;
                ModHook.OnGameLoadedLate -= ChapterLoadedReportPatch.OnLoaded;
                ModHook.OnGameLoadedInit -= MaidProjects.Register;
                ModHook.OnNewGameStarted -= StoryDirector.OnNewGameStarted;
                ModHook.OnGenerateCustomerEarly -= StoryDirector.OnGenerateCustomerVeryEarly;
                subscribed = false;
            }

            DebugTools.Unregister();
            EndingPageUI.HideRoutePage();
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
