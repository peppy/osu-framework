using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using osu.Framework.IO.Stores;
using SixLabors.Memory;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class SimpleBenchmark
    {
        public SimpleBenchmark()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = ArrayPoolMemoryAllocator.CreateWithMinimalPooling();
        }

        public void BenchmarkNoCaching()
        {
            using (var resources = new ResourceStore<byte[]>())
            {
                resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));

                using (var store = new GlyphStore(resources, @"Fonts/OpenSans/OpenSans"))
                {
                    runForStore(store);
                }
            }
        }

        [Benchmark]
        public void BenchmarkTimedExpiryCache()
        {
            using (var resources = new ResourceStore<byte[]>())
            {
                resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));

                using (var store = new GlyphStore(resources, @"Fonts/OpenSans/OpenSans"))
                {
                    store.LoadFontAsync().Wait();
                    runForStore(store);
                }
            }
        }

        public void BenchmarkNoCachingTrimmed()
        {
            using (var resources = new ResourceStore<byte[]>())
            {
                resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));

                using (var store = new GlyphStore(resources, @"Fonts/OpenSans/OpenSans2"))
                {
                    store.LoadFontAsync().Wait();
                    runForStore(store);
                }
            }
        }

        [Benchmark]
        public void BenchmarkTimedExpiryCacheTrimmed()
        {
            using (var resources = new ResourceStore<byte[]>())
            {
                resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));

                using (var store = new GlyphStore(resources, @"Fonts/OpenSans/OpenSans2"))
                {
                    store.LoadFontAsync().Wait();
                    runForStore(store);
                }
            }
        }

        private static void runForStore(GlyphStore store)
        {
            store.LoadFontAsync().Wait();

            for (int i = 0; i < 10; i++)
            {
                using (var upload = store.Get("a"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("b"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("c"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("d"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("e"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("f"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("g"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("h"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("a"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("b"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("c"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("d"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("e"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("f"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("g"))
                    Trace.Assert(upload.Data != null);
                using (var upload = store.Get("h"))
                    Trace.Assert(upload.Data != null);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SimpleBenchmark>();
        }
    }
}
