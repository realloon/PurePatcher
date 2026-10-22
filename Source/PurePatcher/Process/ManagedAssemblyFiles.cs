using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace PurePatcher.Process;

internal static class ManagedAssemblyFiles {
    // RimWorld 1.7 ships native DLLs (including clretwrc.dll) beside managed ones.
    // Cecil requires a CLI assembly, not merely a valid Windows PE image. Do not
    // catch malformed managed images: those should still fail with their real error.
    internal static bool IsAssembly(string path) {
        using var stream = File.OpenRead(path);
        using var image = new PEReader(stream);
        return image.HasMetadata && image.PEHeaders.CorHeader != null && image.GetMetadataReader().IsAssembly;
    }
}
