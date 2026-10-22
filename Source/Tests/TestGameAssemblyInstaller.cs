using System.IO;
using System.Linq;
using System.Runtime.Loader;
using PurePatcher.Runtime;
using TestAssemblyTarget;

namespace Tests;

internal sealed class TestGameAssemblyInstaller {
    [Test]
    public void ReplacesAStillMappedAssembly() {
        var folder = Path.Combine(Path.GetTempPath(), "purepatcher-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "game.dll");
        File.Copy(typeof(BaseWithComps).Assembly.Location, path);
        var context = new AssemblyLoadContext("PurePatcher.InstallTest", isCollectible: true);
        try {
            var loaded = context.LoadFromAssemblyPath(path);
            var originalTypes = loaded.GetTypes().Select(type => type.FullName).ToArray();
            var replacement = File.ReadAllBytes(typeof(GameAssemblyInstaller).Assembly.Location);
            GameAssemblyInstaller.Install(path, replacement);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(replacement));
            Assert.That(loaded.GetTypes().Select(type => type.FullName), Is.EqualTo(originalTypes));
        } finally {
            context.Unload();
            File.Delete(path);
            GameAssemblyInstaller.CleanupPrevious(path);
            // A collectible load context can retain its mapping until a later GC.
            // Leave only a mapped backup rather than forcing collection timing in this test.
            if (!Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
        }
    }

    [Test]
    public void ReplacesAnUnmappedFileWithoutKeepingBackups() {
        var path = Path.GetTempFileName();
        try {
            File.WriteAllBytes(path, [1, 2, 3]);
            GameAssemblyInstaller.Install(path, [4, 5]);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 4, 5 }));
            var folder = Path.GetDirectoryName(path)!;
            Assert.That(Directory.EnumerateFiles(folder, Path.GetFileName(path) + ".purepatcher-*.previous"), Is.Empty);
        } finally {
            File.Delete(path);
        }
    }
}
