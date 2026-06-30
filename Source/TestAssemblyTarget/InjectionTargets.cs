using System.Collections.Generic;

namespace TestAssemblyTarget;

public abstract class BaseComp {
    public BaseWithComps Parent = null!;
}

public class OtherComp : BaseComp;

public class InjectionBase;

public class BaseWithComps : InjectionBase {
    public readonly List<BaseComp> Comps = [];
    public Type[] CompTypes = null!;

    public void InitComps() {
        Comps.Clear();

        foreach (var type in CompTypes) {
            var comp = (BaseComp)Activator.CreateInstance(type);
            comp.Parent = this;
            Comps.Add(comp);
        }
    }
}

public class DerivedWithComps : BaseWithComps;