# PurePatcher

[![PurePatcher.Annotations](https://img.shields.io/nuget/v/PurePatcher.Annotations?label=PurePatcher.Annotations)](https://www.nuget.org/packages/PurePatcher.Annotations)

PurePatcher is a library for structured assembly rewriting.

It provides an annotation package for mod projects, and a runtime mod that applies the requested rewrites in RimWorld.

## Features

- Add fields to existing game types.
- Bind and initialize added fields.
- Replace target method bodies with structured C# methods.

Rewrites are applied only at load time, and patched code has no persistent runtime overhead.

## Use

Add the annotation package to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="PurePatcher.Annotations" Version="1.4.0" PrivateAssets="all" ExcludeAssets="runtime" />
</ItemGroup>
```

Like Harmony, PurePatcher requires runtime support from the PurePatcher mod.

Declare the dependency in your `About.xml`:

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

## How It Works

PurePatcher rewrites assemblies at game load time. It uses Mono.Cecil to modify IL, then loads the rewritten assembly bytes before the game continues.

## Acknowledgments

PurePatcher is forked from [Prepatcher](https://github.com/Zetrith/Prepatcher), and builds on [Harmony](https://github.com/pardeike/Harmony), [MonoMod](https://github.com/monomod/monomod), and [Mono.Cecil](https://github.com/jbevain/cecil).
