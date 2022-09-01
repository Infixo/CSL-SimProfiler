using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;

namespace SimProfiler
{
    public static class SimProfilerPatcher
    {
        private const string HarmonyId = "Infixo.SimProfiler";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) { Debug.Log("PatchAll: already patched!"); return; }
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Harmony.HasAnyPatches(HarmonyId))
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} methods patched ok");
                patched = true;
                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods)
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} ...method {method.Name}");
            }
            else
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, $"{HarmonyId} ERROR: methods not patched");
        }

        public static void UnpatchAll()
        {
            if (!patched) { Debug.Log("UnpatchAll: not patched!"); return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
            //Harmony.DEBUG = false;
        }
    }

    [HarmonyPatch(typeof(SimulationManager))]
    public static class SimulationManager_Patches
    {
        // private void SimulationManager::SimulationStep()
        [HarmonyPatch("SimulationStep")]
        [HarmonyPrefix]
        public static bool SimulationStep_Prefix(
            SimulationManager __instance,
            // private members that are used in the routine - start with 3 underscores
            ref bool ___m_hasActions,
            ref Queue<AsyncTaskBase> ___m_simulationActions,
            ref SimulationManager.ViewData ___m_simulationView,
            ref SimulationManager.ViewData ___m_tempView,
            ref FastList<ISimulationManager> ___m_managers,
            ref object ___m_viewLock,
            ref uint[] ___m_buildIndexHistory)
        {
            TerrainModify.BeginUpdateArea();
            try
            {
                while (___m_hasActions)
                {
                    while (!Monitor.TryEnter(___m_simulationActions, SimulationManager.SYNCHRONIZE_TIMEOUT))
                    {
                    }
                    AsyncTaskBase asyncTaskBase;
                    try
                    {
                        asyncTaskBase = ___m_simulationActions.Dequeue();
                        ___m_hasActions = ___m_simulationActions.Count != 0;
                    }
                    finally
                    {
                        Monitor.Exit(___m_simulationActions);
                    }
                    asyncTaskBase.Execute();
                }
                while (!Monitor.TryEnter(___m_viewLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
                {
                }
                try
                {
                    ___m_simulationView = ___m_tempView;
                }
                finally
                {
                    Monitor.Exit(___m_viewLock);
                }
                if (!Singleton<LoadingManager>.instance.m_simulationDataLoaded)
                {
                    return false;
                }
                int finalSimulationSpeed = __instance.FinalSimulationSpeed;
                __instance.m_ThreadingWrapper.OnBeforeSimulationTick();
                if (Singleton<LoadingManager>.instance.m_loadingComplete)
                {
                    __instance.m_currentTickIndex++;
                }
                int num = ((finalSimulationSpeed != 0) ? 1 : 0);
                for (int i = num; i <= finalSimulationSpeed; i++)
                {
                    if (i != 0)
                    {
                        if ((__instance.m_currentFrameIndex & 0xFF) == 255)
                        {
                            ___m_buildIndexHistory[(__instance.m_currentFrameIndex >> 8) & 0x1F] = __instance.m_currentBuildIndex;
                        }
                        __instance.m_ThreadingWrapper.OnBeforeSimulationFrame();
                        __instance.m_currentFrameIndex++;
                        __instance.m_metaData.m_currentDateTime += __instance.m_timePerFrame;
                        if (!__instance.m_enableDayNight && (Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.Game) != 0)
                        {
                            __instance.m_dayTimeOffsetFrames = ((SimulationManager.DAYTIME_FRAMES >> 1) - __instance.m_currentFrameIndex) & (SimulationManager.DAYTIME_FRAMES - 1);
                        }
                        __instance.m_dayTimeFrame = (__instance.m_currentFrameIndex + __instance.m_dayTimeOffsetFrames) & (SimulationManager.DAYTIME_FRAMES - 1);
                        float num2 = (float)__instance.m_dayTimeFrame * SimulationManager.DAYTIME_FRAME_TO_HOUR;
                        __instance.m_metaData.m_currentDayHour = num2;
                        bool flag = num2 < SimulationManager.SUNRISE_HOUR || num2 > SimulationManager.SUNSET_HOUR;
                        if (flag != __instance.m_isNightTime)
                        {
                            __instance.m_isNightTime = flag;
                            /* unlocking achievement via a delagate - problems with patching - off you go
                            if (!flag && Singleton<SimulationManager>.instance.m_metaData.m_disableAchievements != SimulationMetaData.MetaBool.True)
                            {
                                ThreadHelper.dispatcher.Dispatch(delegate
                                {
                                    int num3 = m_nightCount.value + 1;
                                    m_nightCount.value = num3;
                                    if (num3 == 1001)
                                    {
                                        SteamHelper.UnlockAchievement("1001Nights");
                                    }
                                });
                            }
                            */
                        }
                    }
                    SimProfiler.BeginStep();
                    for (int j = 0; j < ___m_managers.m_size; j++)
                    {
                        string name = ___m_managers.m_buffer[j].GetName();
                        SimProfiler.Begin(name);
                        ___m_managers.m_buffer[j].SimulationStep(i);
                        SimProfiler.End(name);
                    }
                    SimProfiler.EndStep();
                    if (i != 0)
                    {
                        __instance.m_ThreadingWrapper.OnAfterSimulationFrame();
                    }
                }
                __instance.m_ThreadingWrapper.OnAfterSimulationTick();
            }
            finally
            {
                TerrainModify.EndUpdateArea();
            }
            return false;
        }
    } // patcher class
} // namespace