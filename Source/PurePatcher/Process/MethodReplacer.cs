using Mono.Cecil;
using Mono.Cecil.Cil;
using PurePatcher.Annotations;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace PurePatcher.Process;

internal static class MethodReplacer {
    internal static void RunReplacements(AssemblySet set) {
        var replacedTargets = new HashSet<MethodDefinition>();

        foreach (var assembly in set.AllAssemblies.Where(assembly => assembly.ProcessAttributes))
        foreach (var replacement in FindReplacementMethods(assembly.ModuleDefinition)) {
            var target = FindTargetMethod(set, replacement);

            if (!replacedTargets.Add(target)) {
                throw new InvalidOperationException(
                    $"Duplicate replacement for {target.MemberFullName()}.");
            }

            ReplaceMethodBody(target, replacement);
            set.FindAssembly(target.DeclaringType)!.Modified = true;
        }
    }

    private static IEnumerable<MethodDefinition> FindReplacementMethods(ModuleDefinition module) => AllTypes(module)
        .SelectMany(type => type.Methods)
        .Where(method => GetReplaceMethodAttribute(method) != null);

    private static MethodDefinition FindTargetMethod(AssemblySet set, MethodDefinition replacement) {
        ValidateReplacementMethod(replacement);

        var attribute = GetReplaceMethodAttribute(replacement)!;
        var targetType = GetTargetType(attribute);
        ValidateTargetType(set, targetType, replacement);

        var targetMethodName = GetTargetMethodName(attribute);
        var matches = targetType.Methods
            .Where(target => MethodMatchesReplacement(target, targetMethodName, replacement))
            .ToArray();

        if (matches.Length == 0) {
            throw new InvalidOperationException(
                $"Could not find target method {targetType.FullName}.{targetMethodName} matching {replacement.MemberFullName()}.");
        }

        if (matches.Length > 1) {
            throw new InvalidOperationException(
                $"Ambiguous target method {targetType.FullName}.{targetMethodName} matching {replacement.MemberFullName()}.");
        }

        ValidateTargetMethod(matches[0], replacement);
        return matches[0];
    }

    private static bool MethodMatchesReplacement(MethodDefinition target, string targetMethodName,
        MethodDefinition replacement) {
        if (target.Name != targetMethodName) {
            return false;
        }

        if (replacement.ReturnType.FullName != target.ReturnType.FullName) {
            return false;
        }

        var parameterOffset = target.HasThis ? 1 : 0;
        if (replacement.Parameters.Count != target.Parameters.Count + parameterOffset) {
            return false;
        }

        if (target.HasThis &&
            replacement.Parameters[0].ParameterType.FullName != target.DeclaringType.FullName) {
            return false;
        }

        for (var i = 0; i < target.Parameters.Count; i++) {
            if (replacement.Parameters[i + parameterOffset].ParameterType.FullName !=
                target.Parameters[i].ParameterType.FullName) {
                return false;
            }
        }

        return true;
    }

    private static void ReplaceMethodBody(MethodDefinition target, MethodDefinition replacement) {
        Logger.Verbose($"Replacing method body: {target.MemberFullName()}");

        var replacementBody = replacement.Body;
        var newBody = new MethodBody(target) {
            InitLocals = replacementBody.InitLocals
        };
        target.Body = newBody;

        var variableMap = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in replacementBody.Variables) {
            var importedVariable = new VariableDefinition(target.Module.ImportReference(variable.VariableType));
            newBody.Variables.Add(importedVariable);
            variableMap[variable] = importedVariable;
        }

        var instructionMap = replacementBody.Instructions.ToDictionary(
            instruction => instruction,
            _ => Instruction.Create(OpCodes.Nop));

        foreach (var replacementInstruction in replacementBody.Instructions) {
            var targetInstruction = instructionMap[replacementInstruction];
            targetInstruction.OpCode = replacementInstruction.OpCode;
            newBody.Instructions.Add(targetInstruction);
        }

        foreach (var replacementInstruction in replacementBody.Instructions) {
            instructionMap[replacementInstruction].Operand = ImportOperand(
                replacementInstruction.Operand,
                target,
                replacement,
                instructionMap,
                variableMap);
        }

        foreach (var handler in replacementBody.ExceptionHandlers) {
            newBody.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
                CatchType = handler.CatchType == null ? null : target.Module.ImportReference(handler.CatchType),
                TryStart = MapInstruction(handler.TryStart, instructionMap),
                TryEnd = MapInstruction(handler.TryEnd, instructionMap),
                HandlerStart = MapInstruction(handler.HandlerStart, instructionMap),
                HandlerEnd = MapInstruction(handler.HandlerEnd, instructionMap),
                FilterStart = MapInstruction(handler.FilterStart, instructionMap)
            });
        }
    }

    private static object? ImportOperand(object? operand, MethodDefinition target, MethodDefinition replacement,
        IReadOnlyDictionary<Instruction, Instruction> instructionMap,
        IReadOnlyDictionary<VariableDefinition, VariableDefinition> variableMap) {
        return operand switch {
            null => null,
            Instruction instruction => instructionMap[instruction],
            Instruction[] instructions => instructions.Select(instruction => instructionMap[instruction]).ToArray(),
            VariableDefinition variable => variableMap[variable],
            ParameterDefinition parameter => MapParameter(parameter, target, replacement),
            MethodReference method => target.Module.ImportReference(method),
            FieldReference field => target.Module.ImportReference(field),
            TypeReference type => target.Module.ImportReference(type),
            CallSite => throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} cannot use calli instructions."),
            _ => operand
        };
    }

    private static Instruction? MapInstruction(Instruction? instruction,
        IReadOnlyDictionary<Instruction, Instruction> instructionMap) {
        return instruction == null ? null : instructionMap[instruction];
    }

    private static ParameterDefinition MapParameter(ParameterDefinition parameter, MethodDefinition target,
        MethodDefinition replacement) {
        var index = replacement.Parameters.IndexOf(parameter);
        if (index < 0) {
            throw new InvalidOperationException(
                $"Unknown replacement parameter {parameter.Name} in {replacement.MemberFullName()}.");
        }

        if (!target.HasThis) {
            return target.Parameters[index];
        }

        return index == 0 ? target.Body.ThisParameter : target.Parameters[index - 1];
    }

    private static void ValidateReplacementMethod(MethodDefinition replacement) {
        if (!replacement.IsStatic) {
            throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} must be static.");
        }

        if (replacement.Body == null) {
            throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} must have a managed body.");
        }

        if (HasGenericParameters(replacement.DeclaringType)) {
            throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} cannot be declared on a generic type.");
        }

        if (replacement.HasGenericParameters || replacement.ReturnType.IsGenericParameter ||
            replacement.Parameters.Any(parameter => parameter.ParameterType.IsGenericParameter)) {
            throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} cannot be generic.");
        }

        if (replacement.ReturnType.IsByReference ||
            replacement.Parameters.Any(parameter => parameter.ParameterType.IsByReference)) {
            throw new InvalidOperationException(
                $"Replacement method {replacement.MemberFullName()} cannot use ref or out parameters.");
        }
    }

    private static void ValidateTargetType(AssemblySet set, TypeDefinition targetType,
        MethodDefinition replacement) {
        var targetAssembly = set.FindAssembly(targetType);
        if (targetAssembly == null) {
            throw new InvalidOperationException(
                $"Target type {targetType.FullName} for {replacement.MemberFullName()} is not in the assembly set.");
        }

        if (!targetAssembly.AllowPatches) {
            throw new InvalidOperationException(
                $"Target type {targetType.FullName} for {replacement.MemberFullName()} is not modifiable.");
        }

        if (HasGenericParameters(targetType)) {
            throw new InvalidOperationException(
                $"Target type {targetType.FullName} for {replacement.MemberFullName()} cannot be generic.");
        }
    }

    private static void ValidateTargetMethod(MethodDefinition target, MethodDefinition replacement) {
        if (target.IsConstructor) {
            throw new InvalidOperationException(
                $"Target method {target.MemberFullName()} for {replacement.MemberFullName()} cannot be a constructor.");
        }

        if (target.HasThis && target.DeclaringType.IsValueType) {
            throw new InvalidOperationException(
                $"Target method {target.MemberFullName()} for {replacement.MemberFullName()} cannot be a struct instance method.");
        }

        if (target.IsAbstract || target.IsPInvokeImpl || target.Body == null) {
            throw new InvalidOperationException(
                $"Target method {target.MemberFullName()} for {replacement.MemberFullName()} must have a managed body.");
        }

        if (target.HasGenericParameters || target.ReturnType.IsGenericParameter ||
            target.Parameters.Any(parameter => parameter.ParameterType.IsGenericParameter)) {
            throw new InvalidOperationException(
                $"Target method {target.MemberFullName()} for {replacement.MemberFullName()} cannot be generic.");
        }

        if (target.ReturnType.IsByReference ||
            target.Parameters.Any(parameter => parameter.ParameterType.IsByReference)) {
            throw new InvalidOperationException(
                $"Target method {target.MemberFullName()} for {replacement.MemberFullName()} cannot use ref or out parameters.");
        }
    }

    private static bool HasGenericParameters(TypeReference type) {
        while (type != null) {
            if (type.HasGenericParameters) return true;

            type = type.DeclaringType;
        }

        return false;
    }

    private static TypeDefinition GetTargetType(CustomAttribute attribute) {
        var targetType = (TypeReference)attribute.ConstructorArguments[0].Value;
        return targetType.Resolve()
               ?? throw new InvalidOperationException($"Could not resolve target type {targetType.FullName}.");
    }

    private static string GetTargetMethodName(CustomAttribute attribute) => (string)attribute
        .ConstructorArguments[1].Value;

    private static CustomAttribute? GetReplaceMethodAttribute(MethodDefinition method) => method
        .CustomAttributes
        .SingleOrDefault(attribute => attribute.AttributeType.FullName == typeof(ReplaceMethodAttribute).FullName);

    private static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module) => module.Types
        .SelectMany(SelfAndNestedTypes);

    private static IEnumerable<TypeDefinition> SelfAndNestedTypes(TypeDefinition type) {
        yield return type;

        foreach (var result in type.NestedTypes.SelectMany(SelfAndNestedTypes)) {
            yield return result;
        }
    }
}
