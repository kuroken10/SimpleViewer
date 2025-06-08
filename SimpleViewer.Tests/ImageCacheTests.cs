using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleViewer.Infrastructure;

namespace SimpleViewer.Tests
{
    [TestClass]
    public class ImageCacheTests
    {
        private static readonly byte[] PngData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAOGNKoYAAAAASUVORK5CYII=");

        private string CreateTempImages(int count)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            for (int i = 0; i < count; i++)
            {
                File.WriteAllBytes(Path.Combine(dir, $"{i}.png"), PngData);
            }
            return dir;
        }

        [TestMethod]
        public async Task GetImageAsync_ReturnsImage()
        {
            var dir = CreateTempImages(2);
            var cache = new ImageCache(dir);
            cache.Prepare();

            var image = await cache.GetImageAsync();

            Assert.IsNotNull(image);
            Assert.AreEqual("0.png", cache.CurrentFileName);

            var image2 = await cache.GetImageAsync(1);
            Assert.IsNotNull(image2);
            Assert.AreEqual("1.png", cache.CurrentFileName);
        }

        [TestMethod]
        public async Task ClearCache_RemovesOldItems()
        {
            var dir = CreateTempImages(3);
            var cache = new ImageCache(dir);
            cache.Prepare();

            await cache.GetImageAsync();      // index 0
            await cache.GetImageAsync(1);     // index 1
            await cache.GetImageAsync(1);     // index 2

            cache.CacheImages(0, 0, 0);       // only current index (2)
            cache.ClearCache();

            var store = cache.DebugGetImageStore();
            Assert.IsTrue(store.ContainsKey(2));
            Assert.IsFalse(store.ContainsKey(0));
            Assert.IsFalse(store.ContainsKey(1));
        }

        [TestMethod]
        public async Task CacheImages_WithSkipCount_PopulatesFuture()
        {
            var dir = CreateTempImages(5);
            var cache = new ImageCache(dir);
            cache.Prepare();

            cache.CacheImages(0, 0, 2);

            var status = cache.DebugGetStatus();
            Assert.IsNotNull(status[0]);
            Assert.IsNotNull(status[2]);
            await status[2];
            var store = cache.DebugGetImageStore();
            Assert.IsTrue(store.ContainsKey(2));
        }
    }
}
