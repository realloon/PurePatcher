using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PurePatcher.Process;

// A pasted body runs from inside its target type, so members the original call site could reach may
// no longer be reachable: widen them to assembly visibility.
internal static class AccessWidening {
    internal static void WidenSameModuleReferences(MethodDefinition method) {
        var module = method.Module;

        WidenTypeSignature(method.ReturnType, module);
        foreach (var parameter in method.Parameters) {
            WidenTypeSignature(parameter.ParameterType, module);
        }

        if (method.Body == null) return;

        foreach (var variable in method.Body.Variables) {
            WidenTypeSignature(variable.VariableType, module);
        }

        foreach (var instruction in method.Body.Instructions) {
            switch (instruction.Operand) {
                case MethodReference reference:
                    WidenResolved(() => reference.Resolve(), module);
                    break;
                case FieldReference reference:
                    WidenResolved(() => reference.Resolve(), module);
                    break;
                case TypeReference reference:
                    WidenResolved(() => reference.Resolve(), module);
                    break;
            }
        }
    }

    private static void WidenTypeSignature(TypeReference type, ModuleDefinition module) =>
        WidenResolved(() => type.Resolve(), module);

    // A reference the resolver cannot map is not a reason to fail the whole patch.
    private static void WidenResolved(Func<IMetadataTokenProvider?> resolve, ModuleDefinition module) {
        IMetadataTokenProvider? member;
        try {
            member = resolve();
        } catch (Exception exception) {
            Logger.Verbose($"Skipping access widening for an unresolvable reference: {exception.Message}");
            return;
        }

        Widen(member, module);
    }

    private static void Widen(IMetadataTokenProvider? member, ModuleDefinition module) {
        switch (member) {
            case MethodDefinition { IsPublic: false } method when method.DeclaringType?.Module == module:
                WidenType(method.DeclaringType);
                method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) |
                                    MethodAttributes.Assembly;
                break;
            case FieldDefinition { IsPublic: false } field when field.DeclaringType?.Module == module:
                WidenType(field.DeclaringType);
                field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Assembly;
                break;
            case TypeDefinition type:
                WidenType(type.Module == module ? type : null);
                break;
        }
    }

    private static void WidenType(TypeDefinition? type) {
        while (type is { IsNested: true } && !type.IsNestedPublic && !type.IsNestedAssembly) {
            type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedAssembly;
            type = type.DeclaringType;
        }
    }
}
