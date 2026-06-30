using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace TestAssemblyTarget;

public abstract class BaseComp {
    // ReSharper disable once NotAccessedField.Global
    public BaseWithComps parent = null!;
}

public class OtherComp : BaseComp;

public class InjectionBase;

public class BaseWithComps : InjectionBase {
    public readonly List<BaseComp> comps = [];
    public Type[] CompTypes = null!;

    public void InitComps() {
        comps.Clear();

        foreach (var type in CompTypes) {
            var comp = (BaseComp)Activator.CreateInstance(type);
            comp.parent = this;
            comps.Add(comp);
        }
    }
}

public class DerivedWithComps : BaseWithComps;