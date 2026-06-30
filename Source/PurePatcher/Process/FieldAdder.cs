using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using MonoMod.Utils;
using PurePatcher.Annotations;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;

namespace PurePatcher.Process;

internal partial class FieldAdder(AssemblySet set) {
    internal void ProcessAllAssemblies() {
        foreach (var asm in set.AllAssemblies.Where(a => a.ProcessAttributes)) {
            ProcessTypes(asm.ModuleDefinition.Types);
        }
    }

    internal void ProcessTypes(IEnumerable<TypeDefinition> inTypes) {
        foreach (var accessor in GetAllPurePatcherFieldAccessors(inTypes)) {
            ProcessAccessor(accessor);
        }
    }

    internal void ProcessAccessor(MethodDefinition accessor) {
        if (CheckFieldAccessor(accessor) is { } error) {
            var accessorAsm = set.FindAssembly(accessor.DeclaringType);
            Logger.Error($"{accessorAsm}: {error} for new field with accessor {accessor.MemberFullName()}");
            return;
        }

        var newField = AddFieldToTarget(accessor);
        PatchAccessor(accessor, newField);

        if (HasInjection(accessor)) {
            PatchInjectionSite(accessor, newField);
        }

        if (GetExplicitDefaultValue(accessor) is { } attr) {
            PatchCtorsWithDefault(newField, attr);
        }

        if (GetValueInitializer(accessor) is { } initializerAttr) {
            PatchCtorsWithInitializer(accessor, newField, initializerAttr);
        }
    }

    private FieldDefinition AddFieldToTarget(MethodDefinition accessor) {
        var targetType = FirstParameterTypeResolved(accessor)!;
        var fieldType = ImportFieldTypeIntoTargetModule(accessor);

        Logger.Verbose($"Adding new field {FieldName(accessor)} of type {fieldType} to type {targetType}");

        var ceField = new FieldDefinition(
            FieldName(accessor),
            FieldAttributes.Public,
            fieldType
        );

        var ceTargetType = targetType.Module.Resolve(targetType);
        ceTargetType.Fields.Add(ceField);

        var targetAsm = set.FindAssembly(targetType)!;
        targetAsm.Modified = true;

        return ceField;
    }

    private void PatchAccessor(MethodDefinition accessor, FieldDefinition newField) {
        Logger.Verbose("Patching the accessor");

        accessor.ImplAttributes &= ~MethodImplAttributes.InternalCall; // Unextern

        var body = accessor.Body = new MethodBody(accessor);
        var il = body.GetILProcessor();

        var fieldOwner = accessor.Parameters.First().ParameterType;
        if (fieldOwner.IsByReference)
            fieldOwner = ((ByReferenceType)fieldOwner).ElementType;

        var fieldRef = new FieldReference(
            FieldName(accessor),
            accessor.Module.ImportReference(newField.FieldType,
                accessor.Module.ImportReference(newField.DeclaringType)),
            fieldOwner
        );

        // Simple getter or ref-getter body
        // ldarg 0
        // ldfld/ldflda newfield
        // ret

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(accessor.ReturnType.IsByReference ? OpCodes.Ldflda : OpCodes.Ldfld, fieldRef);
        il.Emit(OpCodes.Ret);

        var accessorAsm = set.FindAssembly(accessor.DeclaringType)!;
        accessorAsm.Modified = true;
    }

    private static TypeReference FieldType(MethodDefinition accessor) => accessor.ReturnType.IsByReference
        ? ((ByReferenceType)accessor.ReturnType).ElementType
        : accessor.ReturnType;


    private static TypeReference ImportFieldTypeIntoTargetModule(MethodDefinition accessor) {
        var targetType = FirstParameterTypeResolved(accessor)!;
        var fieldType = FieldType(accessor);
        return targetType.Module.ImportReference(
            fieldType,
            DummyMethodReference.Create(accessor.Name, targetType.Module.ImportReference(accessor.DeclaringType),
                targetType.GenericParameters)
        );
    }

    private static TypeDefinition? FirstParameterTypeResolved(MethodDefinition methodDef) {
        return methodDef.Parameters.First().ParameterType.Resolve();
    }

    private static string FieldName(MethodDefinition accessor) {
        return accessor.DeclaringType.Module.Assembly.ShortName() + accessor.Name + accessor.MetadataToken.RID;
    }

    internal static IEnumerable<MethodDefinition>
        GetAllPurePatcherFieldAccessors(IEnumerable<TypeDefinition> inTypes) => inTypes
        .Where(t => t.IsSealed && t.IsAbstract)
        .SelectMany(t => t.Methods, (t, m) => new { t, m })
        .Where(t1 => t1.m.HasCustomAttribute(typeof(PurePatcherFieldAttribute).FullName!))
        .Select(t1 => t1.m);
}

internal sealed class DummyMethodReference : MethodReference {
    public override Collection<GenericParameter> GenericParameters { get; }

    private DummyMethodReference(Collection<GenericParameter> genericParameters) {
        GenericParameters = genericParameters;
    }

    public static DummyMethodReference Create(string name, TypeReference declaringType,
        Collection<GenericParameter> genericParameters) {
        var reference = new DummyMethodReference(genericParameters) {
            Name = name,
            DeclaringType = declaringType
        };
        return reference;
    }
}