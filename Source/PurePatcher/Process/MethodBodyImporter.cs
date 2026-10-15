using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace PurePatcher.Process;

// Copies a method body from one module into a method of another module.
internal static class MethodBodyImporter {
    internal static void Import(MethodDefinition target, MethodDefinition source) {
        var sourceBody = source.Body;
        var newBody = new MethodBody(target) {
            InitLocals = sourceBody.InitLocals
        };
        target.Body = newBody;

        var variableMap = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in sourceBody.Variables) {
            var importedVariable = new VariableDefinition(target.Module.ImportReference(variable.VariableType));
            newBody.Variables.Add(importedVariable);
            variableMap[variable] = importedVariable;
        }

        var instructionMap = sourceBody.Instructions.ToDictionary(
            instruction => instruction,
            _ => Instruction.Create(OpCodes.Nop));

        foreach (var sourceInstruction in sourceBody.Instructions) {
            var targetInstruction = instructionMap[sourceInstruction];
            targetInstruction.OpCode = sourceInstruction.OpCode;
            newBody.Instructions.Add(targetInstruction);
        }

        foreach (var sourceInstruction in sourceBody.Instructions) {
            instructionMap[sourceInstruction].Operand = ImportOperand(
                sourceInstruction.Operand,
                target,
                source,
                instructionMap,
                variableMap);
        }

        foreach (var handler in sourceBody.ExceptionHandlers) {
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

    internal static void ReplaceString(MethodDefinition method, string placeholder, string replacement) {
        foreach (var instruction in method.Body.Instructions) {
            if (instruction.OpCode != OpCodes.Ldstr) continue;

            // The compiler folds concatenated placeholders into a single literal.
            var value = (string)instruction.Operand;
            if (value.Contains(placeholder, StringComparison.Ordinal)) {
                instruction.Operand = value.Replace(placeholder, replacement, StringComparison.Ordinal);
            }
        }
    }

    private static object? ImportOperand(object? operand, MethodDefinition target, MethodDefinition source,
        IReadOnlyDictionary<Instruction, Instruction> instructionMap,
        IReadOnlyDictionary<VariableDefinition, VariableDefinition> variableMap) => operand switch {
        null => null,
        Instruction instruction => instructionMap[instruction],
        Instruction[] instructions => instructions.Select(instruction => instructionMap[instruction]).ToArray(),
        VariableDefinition variable => variableMap[variable],
        ParameterDefinition parameter => MapParameter(parameter, target, source),
        MethodReference method => target.Module.ImportReference(method),
        FieldReference field => target.Module.ImportReference(field),
        TypeReference type => target.Module.ImportReference(type),
        CallSite => throw new InvalidOperationException(
            $"Replacement method {source.MemberFullName()} cannot use calli instructions."),
        _ => operand
    };

    private static Instruction? MapInstruction(Instruction? instruction,
        IReadOnlyDictionary<Instruction, Instruction> instructionMap) {
        return instruction == null ? null : instructionMap[instruction];
    }

    private static ParameterDefinition MapParameter(ParameterDefinition parameter, MethodDefinition target,
        MethodDefinition source) {
        var index = source.Parameters.IndexOf(parameter);
        if (index < 0) {
            throw new InvalidOperationException(
                $"Unknown replacement parameter {parameter.Name} in {source.MemberFullName()}.");
        }

        if (!target.HasThis) {
            return target.Parameters[index];
        }

        return index == 0 ? target.Body.ThisParameter : target.Parameters[index - 1];
    }
}