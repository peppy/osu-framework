// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneGameThread : FrameworkTestScene
    {
        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        private readonly List<GameThread> registeredThreads = new List<GameThread>();

        [Test]
        public void TestCustomGameThread()
        {
            int threadActionRunCount = 0;

            AddStep("Add new thread", () =>
            {
                var thread = new GameThread(() => threadActionRunCount++, $"Test thread {registeredThreads.Count}");

                registeredThreads.Add(thread);
                gameHost.RegisterThread(thread);
            });

            AddUntilStep("wait for thread action run", () => threadActionRunCount > 10);

            AddStep("Remove thread", () =>
            {
                if (registeredThreads.Count == 0)
                    return;

                var threadToRemove = registeredThreads.First();

                gameHost.UnregisterThread(threadToRemove);
                registeredThreads.Remove(threadToRemove);
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("Remove all registered threads", () =>
            {
                foreach (var thread in registeredThreads)
                {
                    gameHost.UnregisterThread(thread);
                }

                registeredThreads.Clear();
            });
        }
    }
}
