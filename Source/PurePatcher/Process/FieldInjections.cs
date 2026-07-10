using System.Collections;
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

    private void PatchInjectionSites(
        IReadOnlyCollection<(MethodDefinition Accessor, FieldDefinition Field)> injections) {
        foreach (var site in injections.GroupBy(injection => GetInjectionSite(injection.Accessor)!.Value)) {
            var (initMethod, listField) = site.Key;
            PatchInjectionSite(initMethod, listField, site.Select(injection => injection.Field).ToArray());
        }
    }

    private static void PatchInjectionSite(MethodDefinition initMethod, FieldDefinition listField,
        FieldDefinition[] fields) {
        Logger.Verbose($"Patching {initMethod.FullName} for {fields.Length} component bindings");

        var body = initMethod.Body;
        var module = initMethod.Module;
        var il = body.GetILProcessor();

        // Cache each applicable target cast once so all bindings at this site can share one list scan.
        var targetGroups = fields
            .GroupBy(field => field.DeclaringType)
            .Select(group => {
                var type = module.ImportReference(group.Key);
                var alwaysApplies = initMethod.DeclaringType.BaseTypesAndSelfResolved().Contains(group.Key);
                return (Type: type, Fields: group.ToArray(), Variable: new VariableDefinition(type),
                    AlwaysApplies: alwaysApplies);
            })
            .ToArray();

        var listVariable = new VariableDefinition(module.ImportReference(typeof(IList)));
        // A mod component type in the game's local-variable signature can cause circular type loading on Mono.
        var componentVariable = new VariableDefinition(module.TypeSystem.Object);
        var indexVariable = new VariableDefinition(module.TypeSystem.Int32);
        var remainingVariable = new VariableDefinition(module.TypeSystem.Int32);
        var clearMethod = module.ImportReference(
            AccessTools.Method(typeof(InjectionHelper), nameof(InjectionHelper.Clear)));

        body.InitLocals = true;
        body.Variables.Add(listVariable);
        body.Variables.Add(componentVariable);
        body.Variables.Add(indexVariable);
        body.Variables.Add(remainingVariable);

        foreach (var target in targetGroups) {
            body.Variables.Add(target.Variable);
        }

        List<Instruction> prefix = [];
        foreach (var target in targetGroups) {
            prefix.Add(Instruction.Create(OpCodes.Ldarg_0));
            if (!target.AlwaysApplies) {
                prefix.Add(Instruction.Create(OpCodes.Isinst, target.Type));
            }
            prefix.Add(Instruction.Create(OpCodes.Stloc, target.Variable));

            var nextTarget = Instruction.Create(OpCodes.Nop);
            if (!target.AlwaysApplies) {
                prefix.Add(Instruction.Create(OpCodes.Ldloc, target.Variable));
                prefix.Add(Instruction.Create(OpCodes.Brfalse, nextTarget));
            }

            foreach (var field in target.Fields) {
                var clear = new GenericInstanceMethod(clearMethod) {
                    GenericArguments = { target.Type, field.FieldType }
                };
                prefix.Add(target.AlwaysApplies
                    ? Instruction.Create(OpCodes.Ldarg_0)
                    : Instruction.Create(OpCodes.Ldloc, target.Variable));
                prefix.Add(Instruction.Create(OpCodes.Ldflda, field));
                prefix.Add(target.AlwaysApplies
                    ? Instruction.Create(OpCodes.Ldarg_0)
                    : Instruction.Create(OpCodes.Ldloc, target.Variable));
                prefix.Add(Instruction.Create(OpCodes.Call, clear));
            }

            if (!target.AlwaysApplies) {
                prefix.Add(nextTarget);
            }
        }

        for (var i = 0; i < prefix.Count; i++) {
            body.Instructions.Insert(i, prefix[i]);
        }

        var ret = body.Instructions.Last();
        body.Instructions.Remove(ret);

        var done = Instruction.Create(OpCodes.Nop);
        var loopBody = Instruction.Create(OpCodes.Nop);
        var loopCheck = Instruction.Create(OpCodes.Nop);
        var countGetter = module.ImportReference(
            AccessTools.PropertyGetter(typeof(ICollection), nameof(ICollection.Count)));
        var itemGetter = module.ImportReference(AccessTools.PropertyGetter(typeof(IList), "Item"));

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, listField);
        il.Emit(OpCodes.Stloc, listVariable);
        il.Emit(OpCodes.Ldloc, listVariable);
        il.Emit(OpCodes.Brfalse, done);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, remainingVariable);

        foreach (var target in targetGroups) {
            var nextTarget = Instruction.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldloc, target.Variable);
            il.Emit(OpCodes.Brfalse, nextTarget);
            il.Emit(OpCodes.Ldloc, remainingVariable);
            il.Emit(OpCodes.Ldc_I4, target.Fields.Length);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, remainingVariable);
            il.Append(nextTarget);
        }

        il.Emit(OpCodes.Ldloc, remainingVariable);
        il.Emit(OpCodes.Brfalse, done);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, indexVariable);
        il.Emit(OpCodes.Br, loopCheck);
        il.Append(loopBody);
        il.Emit(OpCodes.Ldloc, listVariable);
        il.Emit(OpCodes.Ldloc, indexVariable);
        il.Emit(OpCodes.Callvirt, itemGetter);
        il.Emit(OpCodes.Stloc, componentVariable);

        foreach (var target in targetGroups) {
            var nextTarget = Instruction.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldloc, target.Variable);
            il.Emit(OpCodes.Brfalse, nextTarget);

            foreach (var field in target.Fields) {
                var nextField = Instruction.Create(OpCodes.Nop);
                var noMatch = Instruction.Create(OpCodes.Nop);

                il.Emit(OpCodes.Ldloc, target.Variable);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Brtrue, nextField);
                il.Emit(OpCodes.Ldloc, target.Variable);
                il.Emit(OpCodes.Ldloc, componentVariable);
                il.Emit(OpCodes.Isinst, field.FieldType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brfalse, noMatch);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ldloc, remainingVariable);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Stloc, remainingVariable);
                il.Emit(OpCodes.Ldloc, remainingVariable);
                il.Emit(OpCodes.Brfalse, done);
                il.Emit(OpCodes.Br, nextField);
                il.Append(noMatch);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Pop);
                il.Append(nextField);
            }

            il.Append(nextTarget);
        }

        il.Emit(OpCodes.Ldloc, indexVariable);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, indexVariable);
        il.Append(loopCheck);
        il.Emit(OpCodes.Ldloc, indexVariable);
        il.Emit(OpCodes.Ldloc, listVariable);
        il.Emit(OpCodes.Callvirt, countGetter);
        il.Emit(OpCodes.Blt, loopBody);
        il.Append(done);
        body.Instructions.Add(ret);
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