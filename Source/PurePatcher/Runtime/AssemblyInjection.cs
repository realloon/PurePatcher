using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PurePatcher.Process;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace PurePatcher.Runtime;

// Adds the runtime support code the patched Assembly-CSharp needs into Assembly-CSharp itself,
// so nothing has to be resolvable from the game's assembly list.
internal static class AssemblyInjection {
    internal const string Namespace = "PurePatcher.Runtime";
    internal const string ReloadRedirectType = "ReloadRedirect";
    internal const string PatchInfoType = "PatchInfo";
    internal const string BootGuardType = "BootGuard";
    internal const string FingerprintField = "Fingerprint";

    private static ModuleDefinition? _templateModule;

    private static ModuleDefinition TemplateModule => _templateModule ??= ModuleDefinition.ReadModule(
        typeof(AssemblyInjection).Assembly.Location,
        new ReaderParameters { InMemory = true });

    internal static string? TryReadFingerprint(Assembly assembly) => assembly
        .GetType($"{Namespace}.{PatchInfoType}")
        ?.GetField(FingerprintField, BindingFlags.Public | BindingFlags.Static)
        ?.GetValue(null) as string;

    internal static string? TryReadFingerprintFromFile(string path) {
        using var module = ModuleDefinition.ReadModule(path);
        return module.GetType($"{Namespace}.{PatchInfoType}")
            ?.Fields.FirstOrDefault(field => field.Name == FingerprintField)
            ?.Constant as string;
    }

    internal static void InjectRedirect(ModuleDefinition module, string cacheFolder) {
        var type = CreateHolderType(module, ReloadRedirectType);

        type.Fields.Add(new FieldDefinition("Loaded", FieldAttributes.Public | FieldAttributes.Static,
            module.ImportReference(typeof(Dictionary<string, Assembly>))));

        var cachedFileName = new MethodDefinition("CachedFileName", MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.String);
        cachedFileName.Parameters.Add(new ParameterDefinition("filePath", Mono.Cecil.ParameterAttributes.None,
            module.TypeSystem.String));
        type.Methods.Add(cachedFileName);
        MethodBodyImporter.Import(cachedFileName, TemplateMethod(nameof(ReloadRedirectTemplate),
            nameof(ReloadRedirectTemplate.CachedFileName)));

        var loadedAssemblies = new MethodDefinition("LoadedAssemblies",
            MethodAttributes.Public | MethodAttributes.Static,
            module.ImportReference(typeof(Dictionary<string, Assembly>)));
        type.Methods.Add(loadedAssemblies);
        MethodBodyImporter.Import(loadedAssemblies, TemplateMethod(nameof(ReloadRedirectTemplate),
            nameof(ReloadRedirectTemplate.LoadedAssemblies)));

        var method = new MethodDefinition("LoadFrom", MethodAttributes.Public | MethodAttributes.Static,
            module.ImportReference(typeof(Assembly)));
        method.Parameters.Add(new ParameterDefinition("filePath", Mono.Cecil.ParameterAttributes.None,
            module.TypeSystem.String));
        type.Methods.Add(method);

        MethodBodyImporter.Import(method, TemplateMethod(nameof(ReloadRedirectTemplate),
            nameof(ReloadRedirectTemplate.LoadFrom)));
        MethodBodyImporter.ReplaceString(method, ReloadRedirectTemplate.CacheFolderPlaceholder, cacheFolder);

        // Transplanted bodies must only call members of the injected type, never the template assembly.
        RetargetTemplateCalls(type);
    }

    private static void RetargetTemplateCalls(TypeDefinition injectedType) {
        const string templateTypeName = $"{Namespace}.{nameof(ReloadRedirectTemplate)}";

        foreach (var instruction in injectedType.Methods.SelectMany(method => method.Body.Instructions)) {
            switch (instruction.Operand) {
                case MethodReference method when method.DeclaringType.FullName == templateTypeName:
                    instruction.Operand = injectedType.Methods.FirstOrDefault(candidate =>
                                               candidate.Name == method.Name) ??
                                           throw new MissingMethodException(injectedType.FullName, method.Name);
                    break;
                case FieldReference field when field.DeclaringType.FullName == templateTypeName:
                    instruction.Operand = injectedType.Fields.FirstOrDefault(candidate =>
                                               candidate.Name == field.Name) ??
                                           throw new MissingFieldException(injectedType.FullName, field.Name);
                    break;
            }
        }
    }

    internal static TypeDefinition InjectMarker(ModuleDefinition module, string fingerprint) {
        var type = CreateHolderType(module, PatchInfoType);

        type.Fields.Add(new FieldDefinition(FingerprintField,
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
            module.TypeSystem.String) {
            Constant = fingerprint
        });

        return type;
    }

    internal static void InjectBootGuard(ModuleDefinition module, string stateFolder, string fingerprint,
        string modsConfigPath, string packageId) {
        var type = CreateHolderType(module, BootGuardType);
        var method = new MethodDefinition("Check", MethodAttributes.Public | MethodAttributes.Static,
            module.TypeSystem.Void);
        type.Methods.Add(method);

        MethodBodyImporter.Import(method, TemplateMethod(nameof(BootGuardTemplate),
            nameof(BootGuardTemplate.Check)));
        MethodBodyImporter.ReplaceString(method, BootGuardTemplate.StateFolderPlaceholder, stateFolder);
        MethodBodyImporter.ReplaceString(method, BootGuardTemplate.FingerprintPlaceholder, fingerprint);
        MethodBodyImporter.ReplaceString(method, BootGuardTemplate.ModsConfigPathPlaceholder, modsConfigPath);
        MethodBodyImporter.ReplaceString(method, BootGuardTemplate.PackageIdPlaceholder, packageId);

        CallFromModuleInitializer(module, method);
    }

    private static void CallFromModuleInitializer(ModuleDefinition module, MethodDefinition method) {
        var moduleType = module.Types.FirstOrDefault(type => type.Name == "<Module>");
        if (moduleType == null) {
            moduleType = new TypeDefinition("", "<Module>", TypeAttributes.NotPublic, module.TypeSystem.Object);
            module.Types.Insert(0, moduleType);
        }

        var cctor = moduleType.Methods.FirstOrDefault(candidate => candidate.Name == ".cctor" && candidate.IsStatic);
        if (cctor == null) {
            cctor = new MethodDefinition(".cctor",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            moduleType.Methods.Add(cctor);
            cctor.Body = new MethodBody(cctor);

            var il = cctor.Body.GetILProcessor();
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Ret);
            return;
        }

        cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, method));
    }

    private static TypeDefinition CreateHolderType(ModuleDefinition module, string name) {
        var type = new TypeDefinition(Namespace, name,
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            module.TypeSystem.Object);
        module.Types.Add(type);
        return type;
    }

    private static MethodDefinition TemplateMethod(string typeName, string methodName) {
        var type = TemplateModule.GetType($"{Namespace}.{typeName}") ??
                   throw new MissingMemberException(TemplateModule.Name, typeName);
        return type.Methods.First(method => method.Name == methodName);
    }
}