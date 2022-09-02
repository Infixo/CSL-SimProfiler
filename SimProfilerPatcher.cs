using System;
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
    /* 
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
                            // unlocking achievement via a delagate - problems with patching - off you go
                            //if (!flag && Singleton<SimulationManager>.instance.m_metaData.m_disableAchievements != SimulationMetaData.MetaBool.True)
                            //{
                            //    ThreadHelper.dispatcher.Dispatch(delegate
                            //    {
                            //        int num3 = m_nightCount.value + 1;
                            //        m_nightCount.value = num3;
                            //        if (num3 == 1001)
                            //        {
                            //            SteamHelper.UnlockAchievement("1001Nights");
                            //        }
                            //    });
                            //}
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
*/
    // measuring VehicleManager performance

    [HarmonyPatch(typeof(VehicleManager))]
    public static class VehicleManager_Patches
    {
        // protected override void SimulationStepImpl(int subStep)
        [HarmonyPrefix, HarmonyPatch("SimulationStepImpl")]
        public static bool SimulationStepImpl_Prefix(VehicleManager __instance, int subStep)
        {
            if (subStep != 0) SimProfiler.Begin(40);

            // original code
            if (__instance.m_parkedUpdated)
            {
                int num = __instance.m_updatedParked.Length;
                for (int i = 0; i < num; i++)
                {
                    ulong num2 = __instance.m_updatedParked[i];
                    if (num2 == 0)
                    {
                        continue;
                    }
                    __instance.m_updatedParked[i] = 0uL;
                    for (int j = 0; j < 64; j++)
                    {
                        if ((num2 & (ulong)(1L << j)) != 0)
                        {
                            ushort num3 = (ushort)((i << 6) | j);
                            VehicleInfo info = __instance.m_parkedVehicles.m_buffer[num3].Info;
                            __instance.m_parkedVehicles.m_buffer[num3].m_flags &= 65531;
                            //SimProfiler.Begin(">VM-parked");
                            info.m_vehicleAI.UpdateParkedVehicle(num3, ref __instance.m_parkedVehicles.m_buffer[num3]);
                            //SimProfiler.End(">VM-parked");
                        }
                    }
                }
                __instance.m_parkedUpdated = false;
            }
            if (subStep == 0)
            {
                return false;
            }
            SimulationManager simulationManager = Singleton<SimulationManager>.instance;
            Vector3 physicsLodRefPos = simulationManager.m_simulationView.m_position + simulationManager.m_simulationView.m_direction * 1000f;
            /* trolleybus only
            for (int k = 0; k < 16384; k++)
            {
                Vehicle.Flags flags = __instance.m_vehicles.m_buffer[k].m_flags;
                if ((flags & Vehicle.Flags.Created) != 0 && __instance.m_vehicles.m_buffer[k].m_leadingVehicle == 0)
                {
                    VehicleInfo info2 = __instance.m_vehicles.m_buffer[k].Info;
                    SimProfiler.Begin(">VM-extra-step");
                    info2.m_vehicleAI.ExtraSimulationStep((ushort)k, ref __instance.m_vehicles.m_buffer[k]);
                    SimProfiler.End(">VM-extra-step");
                }
            }
            */
            int num4 = (int)(simulationManager.m_currentFrameIndex & 0xF);
            int num5 = num4 * 1024;
            int num6 = (num4 + 1) * 1024 - 1;
            for (int l = num5; l <= num6; l++)
            {
                Vehicle.Flags flags2 = __instance.m_vehicles.m_buffer[l].m_flags;
                if ((flags2 & Vehicle.Flags.Created) != 0 && __instance.m_vehicles.m_buffer[l].m_leadingVehicle == 0)
                {
                    VehicleInfo info3 = __instance.m_vehicles.m_buffer[l].Info;
                    //SimProfiler.Begin(41);
                    info3.m_vehicleAI.ExtraSimulationStep((ushort)l, ref __instance.m_vehicles.m_buffer[l]);
                    //SimProfiler.End(41);
                    //SimProfiler.Begin(42);
                    info3.m_vehicleAI.SimulationStep((ushort)l, ref __instance.m_vehicles.m_buffer[l], physicsLodRefPos);
                    //  SimProfiler.End(42);
                }
            }
            if ((simulationManager.m_currentFrameIndex & 0xFF) == 0)
            {
                uint num7 = __instance.m_maxTrafficFlow / 100u;
                if (num7 == 0)
                {
                    num7 = 1u;
                }
                uint num8 = __instance.m_totalTrafficFlow / num7;
                if (num8 > 100)
                {
                    num8 = 100u;
                }
                __instance.m_lastTrafficFlow = num8;
                __instance.m_totalTrafficFlow = 0u;
                __instance.m_maxTrafficFlow = 0u;
                //SimProfiler.Begin(">VM-stats");
                StatisticsManager statisticsManager = Singleton<StatisticsManager>.instance;
                StatisticBase statisticBase = statisticsManager.Acquire<StatisticInt32>(StatisticType.TrafficFlow);
                statisticBase.Set((int)num8);
                //SimProfiler.End(">VM-stats");
            }
            // end of original code

            SimProfiler.End(40);
            return false;
        }
    }
/*
    // VehicleAI
    [HarmonyPatch(typeof(VehicleAI))]
    public static class VehicleAI_Patches
    {
        //UpdateParkedVehicle - this is called for all parked vehicles if there is a need to update them; can be triggered by a Building, TransportLine and NetSegment
        [HarmonyPrefix, HarmonyPatch("UpdateParkedVehicle")]
        public static bool UpdateParkedVehicle_Prefix()
        {
            SimProfiler.Begin(">VM-parked");
            return true;
        }
        [HarmonyPostfix, HarmonyPatch("UpdateParkedVehicle")]
        public static void UpdateParkedVehicle_Postfix()
        {
            SimProfiler.End(">VM-parked");
        }
        //ExtraSimulationStep - this is only for trolleybuses
        [HarmonyPrefix, HarmonyPatch("ExtraSimulationStep")]
        public static bool ExtraSimulationStep_Prefix()
        {
            SimProfiler.Begin(">VM-extra-step");
            return true;
        }
        [HarmonyPostfix, HarmonyPatch("ExtraSimulationStep")]
        public static void ExtraSimulationStep_Postfix()
        {
            SimProfiler.End(">VM-extra-step");
        }
        //SimulationStep - this is called every 16 frames for 1024 vehicles (!)
        [HarmonyPrefix, HarmonyPatch("SimulationStep")]
        [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
        public static bool SimulationStep_Prefix()
        {
            SimProfiler.Begin(">VM-sim-step");
            return true;
        }
        [HarmonyPostfix, HarmonyPatch("SimulationStep")]
        [HarmonyPatch(new Type[] { typeof(ushort), typeof(Vehicle), typeof(Vector3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
        public static void SimulationStep_Postfix()
        {
            SimProfiler.End(">VM-sim-step");
        }
    }
*/

} // namespace