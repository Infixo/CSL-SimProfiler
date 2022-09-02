using CitiesHarmony.API;
using ICities;

// Make sure that "using HarmonyLib;" does not appear here!
// Only reference HarmonyLib in code that runs when Harmony is ready (DoOnHarmonyReady, IsHarmonyInstalled)
/*
 * Make sure that there are no references to HarmonyLib in your IUserMod implementation.
 * Otherwise the mod could not be loaded if CitiesHarmony is not subscribed. Instead,
 * it is recommended to keep HarmonyLib-related code (such as calls to PatchAll and UnpatchAll)
 * in a separate static Patcher class.
 * */

namespace SimProfiler
{
    public class SimProfilerMod : IUserMod, ILoadingExtension
    {
        public string Name => "Simulation Profiler";
        public string Description => "Gathers data about performance of simulation managers";

        /*
        public class CommercialBuildingAIFixed : CommercialBuildingAI
        {
            protected new void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
            {
                base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
            }
        }
        */

        public void OnEnabled()
        {
            //HarmonyHelper.EnsureHarmonyInstalled();
            /*
             * Will invoke the passed action when Harmony 2.x is ready to use.
             * This hook should be called from IUserMod.OnEnabled. If the Harmony
             * mod is not installed, this hook will attempt to auto-subscribe to it. */
            HarmonyHelper.DoOnHarmonyReady(() => SimProfilerPatcher.PatchAll());
            //if (HarmonyHelper.IsHarmonyInstalled) HappinessFixPatcher.PatchAll();
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) SimProfilerPatcher.UnpatchAll();
            /* Returns true is Harmony is ready to be used. When queried, this hook will not attempt
             * to auto-subscribe to the Harmony workshop item. Use this hook for all kinds of unpatching,
             * applying patches in the LoadingExtension or while the simulation is running. */
        }

        // called when level loading begins
        public void OnCreated(ILoading loading)
        {
            //throw new System.NotImplementedException();
        }

        // called when level is loaded
        public void OnLevelLoaded(LoadMode mode)
        {
            //throw new System.NotImplementedException();
            // logic to start measurements
            SimProfiler.StartMeasuring();
        }

        // called when unloading begins
        public void OnLevelUnloading()
        {
            //throw new System.NotImplementedException();
            // logic to stop measurements and wrte to a file
            SimProfiler.StopMeasuring();
        }

        // called when unloading finished
        public void OnReleased()
        {
            //throw new System.NotImplementedException();
        }

    }
}
