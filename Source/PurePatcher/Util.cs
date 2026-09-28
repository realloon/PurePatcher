using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PurePatcher;

internal static class Util {
    public static string ShortName(this AssemblyDefinition asm) => asm.Name.Name;

    public static string ManagedFolderOS() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return "Resources/Data/Managed";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            return "Managed";
        }

        throw new Exception("Unknown platform");
    }

    public static IEnumerable<TypeDefinition> BaseTypesAndSelfResolved(this TypeDefinition? type) {
        while (type != null) {
            yield return type;
            type = type.BaseType?.Resolve();
        }
    }

    public static IEnumerable<T> Bfs<T>(IEnumerable<T> start, Func<T, IEnumerable<T>> next) {
        var result = new HashSet<T>(start);
        var todo = new Queue<T>(result);

        while (todo.Count > 0) {
            foreach (var d in next(todo.Dequeue())) {
                if (result.Add(d)) {
                    todo.Enqueue(d);
                }
            }
        }

        return result;
    }

    public static void SetEmptyBody(MethodDefinition def) {
        def.Body = new MethodBody(def) {
            Instructions = { Instruction.Create(OpCodes.Ret) }
        };
    }
}