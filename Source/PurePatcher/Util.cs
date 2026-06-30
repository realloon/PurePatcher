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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
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
        var result = new HashSet<T>();
        var todo = new Queue<T>();

        foreach (var o in start)
            todo.Enqueue(o);

        while (todo.Count > 0) {
            var t = todo.Dequeue();
            result.Add(t);

            foreach (var d in next(t)) {
                if (result.Contains(d)) continue;

                todo.Enqueue(d);
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