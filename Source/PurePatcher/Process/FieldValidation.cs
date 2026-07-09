using Mono.Cecil;

namespace PurePatcher.Process;

internal partial class FieldAdder {
    private string? CheckFieldAccessor(MethodDefinition accessor) {
        if (accessor.Parameters.Count() != 1) {
            return "Accessor must have exactly one parameter";
        }

        var target = FirstParameterTypeResolved(accessor);
        if (target == null) {
            return "Couldn't resolve target type";
        }

        if (target.IsInterface) {
            return "Target type can't be an interface";
        }

        if (accessor.DeclaringType.HasGenericParameters || accessor.HasGenericParameters ||
            target.HasGenericParameters || FieldType(accessor).ContainsGenericParameter) {
            return "AddField declarations cannot contain unbound generic parameters";
        }

        if (!set.FindAssembly(target)!.AllowPatches) {
            return "Target type is not modifiable";
        }

        if (!HasInjection(accessor)) return null;

        if (GetInjectionSite(accessor) == null) {
            return "Unknown component binding owner type/component type pair";
        }

        return accessor.ReturnType.IsByReference ? "Component-bound field cannot have a setter" : null;
    }
}