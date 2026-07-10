using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using PurePatcher.Annotations;

namespace PurePatcher.Process;

internal partial class FieldAdder {
    private readonly Dictionary<(TypeDefinition targetType, TypeDefinition compType), (MethodDefinition initMethod,
        FieldDefinition listField)> _injectionSites = new();

    internal void RegisterInjection(Type targetType, Type compType, string initMethod, string listField) {
        RegisterInjection(
            set.ReflectionToCecil(targetType),
            set.ReflectionToCecil(compType),
            initMethod,
            listField
        );
    }

    internal void RegisterInjection(TypeDefinition targetType, TypeDefinition compType, string initMethod,
        string listFieldName) {
        var method = targetType.Methods.FirstOrDefault(m => m.Name == initMethod);
        if (method == null) {
            throw new Exception($"Injection site {targetType}:{initMethod} not found");
        }

        var listField = targetType.Fields.FirstOrDefault(m => m.Name == listFieldName);
        if (listField == null) {
            throw new Exception($"Component list field {targetType}:{listFieldName} not found");
        }

        if (method.Body.Instructions.Last().OpCode != OpCodes.Ret) {
            throw new Exception($"Expected last instruction of injection site {targetType}:{initMethod} to be Ret");
        }

        _injectionSites[(targetType, compType)] = (method, listField);
    }

    private void PatchInjectionSite(MethodDefinition accessor, FieldDefinition newField) {
        Logger.Verbose("Patching the component initialization site for injection");

        var (initMethod, listField) = GetInjectionSite(accessor)!.Value;
        var body = initMethod.Body;
        var targetType = initMethod.Module.ImportReference(newField.DeclaringType);

        // Set to null in the prefix
        var clearField = Instruction.Create(OpCodes.Nop);
        var clearDone = Instruction.Create(OpCodes.Nop);
        Instruction[] clearInstructions = [
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Isinst, targetType),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Brtrue_S, clearField),
            Instruction.Create(OpCodes.Pop),
            Instruction.Create(OpCodes.Br_S, clearDone),
            clearField,
            Instruction.Create(OpCodes.Ldnull),
            Instruction.Create(OpCodes.Stfld, newField),
            clearDone
        ];

        for (var i = 0; i < clearInstructions.Length; i++) {
            body.Instructions.Insert(i, clearInstructions[i]);
        }

        var retInst = body.Instructions.Last();
        body.Instructions.Remove(retInst);

        // Inject in the postfix
        body.GetILProcessor().Emit(OpCodes.Ldarg_0);
        body.GetILProcessor().Emit(OpCodes.Isinst, targetType);
        body.GetILProcessor().Emit(OpCodes.Dup);

        var injectField = Instruction.Create(OpCodes.Nop);
        var injectDone = Instruction.Create(OpCodes.Nop);
        body.GetILProcessor().Emit(OpCodes.Brtrue_S, injectField);
        body.GetILProcessor().Emit(OpCodes.Pop);
        body.GetILProcessor().Emit(OpCodes.Br_S, injectDone);
        body.GetILProcessor().Append(injectField);
        body.GetILProcessor().Emit(OpCodes.Ldflda, newField);
        body.GetILProcessor().Emit(OpCodes.Ldarg_0);
        body.GetILProcessor().Emit(OpCodes.Ldfld, listField);
        body.GetILProcessor().Emit(
            OpCodes.Call,
            new GenericInstanceMethod(initMethod.Module.ImportReference(
                AccessTools.Method(typeof(InjectionHelper), nameof(InjectionHelper.TryInject)))) {
                GenericArguments = { newField.FieldType }
            }
        );
        body.GetILProcessor().Append(injectDone);

        body.Instructions.Add(retInst);
    }

    // Find the unique (component owner type, component type) pair for this accessor and then
    // return the corresponding injection site
    private (MethodDefinition, FieldDefinition)? GetInjectionSite(MethodDefinition accessor) {
        var fieldTarget = FirstParameterTypeResolved(accessor)!;

        // (field target or its base, field type or its base)
        var possibleTypes =
            from targetType in fieldTarget.BaseTypesAndSelfResolved()
            from fieldType in FieldType(accessor).Resolve().BaseTypesAndSelfResolved()
            select (targetType, fieldType);

        // (supertype of field target, field type or its base)
        possibleTypes = possibleTypes.Concat(_injectionSites.Keys.Select(p => p.targetType)
            .Where(t => t != fieldTarget && t.BaseTypesAndSelfResolved().Contains(fieldTarget))
            .SelectMany(_ => FieldType(accessor).Resolve().BaseTypesAndSelfResolved(),
                (targetType, fieldType) => (targetType, fieldType)));

        // SingleOrDefault is used to throw on ambiguity
        var siteId = possibleTypes.SingleOrDefault(p => _injectionSites.ContainsKey(p));
        if (siteId == default) return null;

        return _injectionSites[siteId];
    }

    private static bool HasInjection(MethodDefinition accessor) => accessor
        .HasCustomAttribute(typeof(BindComponentAttribute).FullName!);
}