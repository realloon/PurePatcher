using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using PurePatcher.Annotations;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;

namespace PurePatcher.Process;

internal partial class FieldAdder(AssemblySet set) {
    internal void ProcessAllAssemblies() => ProcessTypes(set.AllAssemblies
        .Where(asm => asm.ProcessAttributes)
        .SelectMany(asm => asm.ModuleDefinition.Types));

    internal void ProcessTypes(IEnumerable<TypeDefinition> inTypes) =>
        ProcessAccessors(GetAllAddFieldAccessors(inTypes));

    internal void ProcessAccessor(MethodDefinition accessor) => ProcessAccessors([accessor]);

    private void ProcessAccessors(IEnumerable<MethodDefinition> accessors) {
        List<(MethodDefinition Accessor, FieldDefinition Field)> injections = [];

        foreach (var accessor in accessors) {
            ProcessAccessor(accessor, injections);
        }

        PatchInjectionSites(injections);
    }

    private void ProcessAccessor(MethodDefinition accessor,
        ICollection<(MethodDefinition Accessor, FieldDefinition Field)> injections) {
        if (CheckFieldAccessor(accessor) is { } error) {
            var accessorAsm = set.FindAssembly(accessor.DeclaringType);
            throw new InvalidOperationException(
                $"{accessorAsm}: {error} for new field with accessor {accessor.MemberFullName()}");
        }

        var newField = AddFieldToTarget(accessor);
        PatchAccessor(accessor, newField);

        if (HasInjection(accessor)) {
            injections.Add((accessor, newField));
        }

        if (GetExplicitDefaultValue(accessor) is { } attr) {
            PatchCtorsWithDefault(newField, attr);
        }

        if (GetInitValue(accessor) is { } initializerAttr) {
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
        return targetType.Module.ImportReference(FieldType(accessor));
    }

    private static TypeDefinition? FirstParameterTypeResolved(MethodDefinition methodDef) {
        return methodDef.Parameters.First().ParameterType.Resolve();
    }

    private static string FieldName(MethodDefinition accessor) {
        return accessor.DeclaringType.Module.Assembly.ShortName() + accessor.Name + accessor.MetadataToken.RID;
    }

    internal static IEnumerable<MethodDefinition>
        GetAllAddFieldAccessors(IEnumerable<TypeDefinition> inTypes) => inTypes
        .Where(t => t.IsSealed && t.IsAbstract)
        .SelectMany(t => t.Methods, (t, m) => new { t, m })
        .Where(t1 => t1.m.HasCustomAttribute(typeof(AddFieldAttribute).FullName!))
        .Select(t1 => t1.m);
}