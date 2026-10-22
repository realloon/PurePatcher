using System.IO;
using System.Reflection.PortableExecutable;
using PurePatcher.Process;
using TestAssemblyTarget;

namespace Tests;

internal sealed class TestManagedAssemblyFiles {
    [Test]
    public void AcceptsManagedAssembly() {
        Assert.That(ManagedAssemblyFiles.IsAssembly(typeof(BaseWithComps).Assembly.Location), Is.True);
    }

    [Test]
    public void AcceptsCoreLibrary() {
        Assert.That(ManagedAssemblyFiles.IsAssembly(typeof(object).Assembly.Location), Is.True);
    }

    [Test]
    public void SkipsNativePeWithoutCliMetadata() {
        // Turn our managed test fixture into a native PE by clearing only the CLI
        // data directory. No game installation or platform-specific DLL is required.
        var bytes = File.ReadAllBytes(typeof(BaseWithComps).Assembly.Location);
        using (var reader = new PEReader(new MemoryStream(bytes))) {
            var headers = reader.PEHeaders;
            var dataDirectories = headers.PEHeaderStartOffset +
                                  (headers.PEHeader!.Magic == PEMagic.PE32Plus ? 112 : 96);
            Array.Clear(bytes, dataDirectories + 14 * 8, 8);
        }
        var path = Path.GetTempFileName();
        try {
            File.WriteAllBytes(path, bytes);
            using (var reader = new PEReader(File.OpenRead(path))) {
                Assert.That(reader.HasMetadata, Is.False);
                Assert.That(reader.PEHeaders.CorHeader, Is.Null);
            }
            Assert.That(ManagedAssemblyFiles.IsAssembly(path), Is.False);
        } finally {
            File.Delete(path);
        }
    }

    [Test]
    public void MalformedImageStillFails() {
        var path = Path.GetTempFileName();
        try {
            File.WriteAllBytes(path, [1, 2, 3, 4]);
            Assert.Throws<BadImageFormatException>(() => ManagedAssemblyFiles.IsAssembly(path));
        } finally {
            File.Delete(path);
        }
    }
}
