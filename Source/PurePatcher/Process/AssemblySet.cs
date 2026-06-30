using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;

namespace PurePatcher.Process;

// Assumption: there only exists one assembly with a given name (just name, not f.e. name+version pair)
public class AssemblySet {
    internal List<ModifiableAssembly> AllAssemblies { get; } = [];
    private readonly Dictionary<string, ModifiableAssembly> nameToAsm = new();
    private IAssemblyResolver Resolver { get; }

    public AssemblySet() {
        Resolver = new AssemblyResolver(this);
    }

    public ModifiableAssembly AddAssembly(string ownerName, string friendlyName, string? asmFilePath, Assembly? asm) {
        Logger.Verbose($"Adding assembly {friendlyName}");

        var masm = asmFilePath != null
            ? new ModifiableAssembly(ownerName, friendlyName, asmFilePath, Resolver)
            : new ModifiableAssembly(ownerName, friendlyName, asm!, Resolver);

        nameToAsm[masm.AsmDefinition.ShortName()] = masm;
        AllAssemblies.Add(masm);
        return masm;
    }

    public bool HasAssembly(string name) {
        return nameToAsm.ContainsKey(name);
    }

    public ModifiableAssembly? FindAssembly(string name) {
        return nameToAsm.GetValueSafe(name);
    }

    internal ModifiableAssembly? FindAssembly(TypeDefinition typeDef) {
        return FindAssembly(typeDef.Module.Assembly.ShortName());
    }

    internal TypeDefinition ReflectionToCecil(Type type) {
        // Use any assembly as they all go through the same assembly resolver
        return AllAssemblies[0].ModuleDefinition.ImportReference(type).Resolve();
    }

    internal Dictionary<ModifiableAssembly, HashSet<ModifiableAssembly>> AllAssembliesToDependants() {
        var dependants = new Dictionary<ModifiableAssembly, HashSet<ModifiableAssembly>>();

        foreach (var asm in nameToAsm.Values)
        foreach (var reference in asm.ModuleDefinition.AssemblyReferences) {
            var refAsm = FindAssembly(reference.Name);
            if (refAsm == null) continue;

            if (!dependants.TryGetValue(refAsm, out var set))
                dependants[refAsm] = set = [];

            set.Add(asm);
        }

        return dependants;
    }

    private class AssemblyResolver(AssemblySet processor) : IAssemblyResolver {
        public AssemblyDefinition? Resolve(AssemblyNameReference name) {
            return processor.FindAssembly(name.Name)?.AsmDefinition;
        }

        public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters parameters) {
            return Resolve(name);
        }

        public void Dispose() { }
    }
}