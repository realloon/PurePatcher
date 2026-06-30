using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace PurePatcher.Process;

public class ModifiableAssembly {
    private string FriendlyName { get; }

    public Assembly? SourceAssembly { get; set; }
    public AssemblyDefinition AsmDefinition { get; }
    public ModuleDefinition ModuleDefinition => AsmDefinition.MainModule;

    public bool ProcessAttributes { get; set; }
    public bool NeedsReload => _needsReload || Modified;
    public bool Modified { get; set; }
    public bool AllowPatches { get; set; } = true;

    public byte[]? Bytes { get; private set; }

    private byte[]? RawBytes { get; }

    private bool _needsReload;

    public override string ToString() => FriendlyName;

    public ModifiableAssembly(string friendlyName, Assembly sourceAssembly,
        IAssemblyResolver resolver) {
        FriendlyName = friendlyName;
        SourceAssembly = sourceAssembly;

        RawBytes = UnsafeAssembly.GetRawData(sourceAssembly);
        AsmDefinition = AssemblyDefinition.ReadAssembly(
            new MemoryStream(RawBytes),
            new ReaderParameters {
                AssemblyResolver = resolver
            });
    }

    public ModifiableAssembly(string friendlyName, string path, IAssemblyResolver resolver) {
        FriendlyName = friendlyName;
        AsmDefinition = AssemblyDefinition.ReadAssembly(
            path,
            new ReaderParameters { AssemblyResolver = resolver, InMemory = true }
        );
    }

    public void SerializeToByteArray() {
        if (RawBytes != null && !Modified) {
            Logger.Verbose($"Assembly not modified, skipping serialization: {FriendlyName}");
            Bytes = RawBytes;
            return;
        }

        Logger.Verbose($"Serializing: {FriendlyName}");
        var stream = new MemoryStream();
        AsmDefinition.Write(stream);
        Bytes = stream.ToArray();
    }

    public void SetSourceRefOnly() {
        Logger.Verbose($"Setting refonly: {FriendlyName}");
        UnsafeAssembly.SetReflectionOnly(SourceAssembly!, true);
    }

    public void SetNeedsReload() {
        _needsReload = true;
    }
}