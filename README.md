# PurePatcher

[![PurePatcher.Annotations](https://img.shields.io/nuget/v/PurePatcher.Annotations?label=PurePatcher.Annotations)](https://www.nuget.org/packages/PurePatcher.Annotations)

PurePatcher is a RimWorld mod and library for structured assembly rewriting.

It provides a small annotation package for mod projects, and a runtime mod that applies the requested rewrites in RimWorld.

## Use

Install PurePatcher as a RimWorld mod. Harmony must be loaded before PurePatcher, and PurePatcher must be loaded before Core.

For mod projects, reference the annotations package:

```xml
<ItemGroup>
    <PackageReference Include="PurePatcher.Annotations" Version="1.4.0" PrivateAssets="all" ExcludeAssets="runtime" />
</ItemGroup>
```

Declare the mod dependency in `About.xml`:

```xml
<modDependencies>
  <li>
    <packageId>Vortex.PurePatcher</packageId>
    <displayName>PurePatcher</displayName>
    <downloadUrl>https://github.com/realloon/PurePatcher</downloadUrl>
  </li>
</modDependencies>
```

## Example

```cs
using RimWorld;
using PurePatcher.Annotations;

public static class RefuelablePatch {
    [AddField]
    [DefaultValue(0)]
    private static extern ref int InspectCount(this CompRefuelable comp);

    [ReplaceMethod(typeof(CompRefuelable), nameof(CompRefuelable.CompInspectStringExtra))]
    public static string CompInspectStringExtra(CompRefuelable comp) {
        comp.InspectCount()++;
        return $"{comp.Props.FuelLabel}: {comp.Fuel} ({comp.InspectCount()})";
    }
}
```

## Acknowledgments

PurePatcher is forked from [Prepatcher](https://github.com/Zetrith/Prepatcher), and builds on Harmony, MonoMod, and Mono.Cecil.
