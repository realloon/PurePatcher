using System.Reflection;
using Mono.Cecil;

namespace PurePatcher.Process;

public class ModifiableAssembly(string friendlyName, string path, IAssemblyResolver resolver) {
    private string FriendlyName { get; } = friendlyName;

    public string SourcePath { get; } = path;
    public Assembly? SourceAssembly { get; set; }

    public AssemblyDefinition AsmDefinition { get; } = AssemblyDefinition.ReadAssembly(
        path,
        new ReaderParameters { AssemblyResolver = resolver, InMemory = true }
    );

    public ModuleDefinition ModuleDefinition => AsmDefinition.MainModule;

    public bool ProcessAttributes { get; set; }
    public bool Modified { get; set; }
    public bool AllowPatches { get; set; } = true;

    public byte[]? Bytes { get; private set; }

    public override string ToString() => FriendlyName;

    public void SerializeToByteArray() {
        Logger.Verbose($"Serializing: {FriendlyName}");
        var stream = new MemoryStream();
        AsmDefinition.Write(stream);
        Bytes = stream.ToArray();
    }
}