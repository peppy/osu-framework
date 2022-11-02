﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Wasapi;
using osu.Framework.Audio;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform.Linux.Native;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        public AudioThread()
            : base(name: "Audio")
        {
            OnNewFrame += onNewFrame;
            PreloadBass();
        }

        public override bool IsCurrent => ThreadSafety.IsAudioThread;

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsAudioThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.TasksRun,
            StatisticsCounterType.Tracks,
            StatisticsCounterType.Samples,
            StatisticsCounterType.SChannels,
            StatisticsCounterType.Components,
            StatisticsCounterType.MixChannels,
        };

        private readonly List<AudioManager> managers = new List<AudioManager>();

        private static readonly HashSet<int> initialised_devices = new HashSet<int>();

        private static readonly GlobalStatistic<double> cpu_usage = GlobalStatistics.Get<double>("Audio", "Bass CPU%");

        private long frameCount;

        private void onNewFrame()
        {
            if (frameCount++ % 1000 == 0)
                cpu_usage.Value = Bass.CPUUsage;

            lock (managers)
            {
                for (int i = 0; i < managers.Count; i++)
                {
                    var m = managers[i];
                    m.Update();
                }
            }
        }

        internal void RegisterManager(AudioManager manager)
        {
            lock (managers)
            {
                if (managers.Contains(manager))
                    throw new InvalidOperationException($"{manager} was already registered");

                managers.Add(manager);
            }
        }

        internal void UnregisterManager(AudioManager manager)
        {
            lock (managers)
                managers.Remove(manager);
        }

        protected override void OnExit()
        {
            base.OnExit();

            lock (managers)
            {
                // AudioManagers are iterated over backwards since disposal will unregister and remove them from the list.
                for (int i = managers.Count - 1; i >= 0; i--)
                {
                    var m = managers[i];

                    m.Dispose();

                    // Audio component disposal (including the AudioManager itself) is scheduled and only runs when the AudioThread updates.
                    // But the AudioThread won't run another update since it's exiting, so an update must be performed manually in order to finish the disposal.
                    m.Update();
                }

                managers.Clear();
            }

            // Safety net to ensure we have freed all devices before exiting.
            // This is mainly required for device-lost scenarios.
            // See https://github.com/ppy/osu-framework/pull/3378 for further discussion.
            foreach (int d in initialised_devices.ToArray())
                FreeDevice(d);
        }

        internal static bool InitDevice(int deviceId)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            Trace.Assert(deviceId != -1); // The real device ID should always be used, as the -1 device has special cases which are hard to work with.

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                Logger.Log($"Attempting WASAPI exclusive initialisation for device {deviceId}...");

                // http://www.un4seen.com/doc/#basswasapi/BASS_WASAPI_Init.html
                if (BassWasapi.Init(deviceId, Flags: WasapiInitFlags.Exclusive))
                {
                    Logger.Log("Success!");
                    return true;
                }

                Logger.Log($"Failed ({Bass.LastError})!");

                Logger.Log($"Attempting WASAPI raw initialisation for device {deviceId}...");

                if (BassWasapi.Init(deviceId, Flags: WasapiInitFlags.Raw))
                {
                    Logger.Log("Success!");
                    return true;
                }

                Logger.Log($"Failed ({Bass.LastError})!");
            }

            // Try to initialise the device, or request a re-initialise.
            if (Bass.Init(deviceId, Flags: (DeviceInitFlags)128)) // 128 == BASS_DEVICE_REINIT
            {
                initialised_devices.Add(deviceId);
                return true;
            }

            return false;
        }

        internal static void FreeDevice(int deviceId)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = Bass.CurrentDevice;

            if (canSelectDevice(deviceId))
            {
                Bass.CurrentDevice = deviceId;
                Bass.Free();
            }

            if (selectedDevice != deviceId && canSelectDevice(selectedDevice))
                Bass.CurrentDevice = selectedDevice;

            initialised_devices.Remove(deviceId);

            static bool canSelectDevice(int deviceId) => Bass.GetDeviceInfo(deviceId, out var deviceInfo) && deviceInfo.IsInitialized;
        }

        /// <summary>
        /// Makes BASS available to be consumed.
        /// </summary>
        internal static void PreloadBass()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
        }
    }
}
