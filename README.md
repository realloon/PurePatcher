# PurePatcher

[![PurePatcher.Annotations](https://img.shields.io/nuget/v/PurePatcher.Annotations?label=PurePatcher.Annotations)](https://www.nuget.org/packages/PurePatcher.Annotations)

PurePatcher is a RimWorld mod and library for structured assembly rewriting.

It provides a small annotation package for mod projects, and a runtime mod that applies the requested rewrites in RimWorld.

## Use

Install PurePatcher as a RimWorld mod. Harmony must be loaded before PurePatcher, and PurePatcher must be loaded before Core.

For mod projects, reference the annotations package:

```xml
<PackageReference Include="PurePatcher.Annotations" Version="1.0.0" PrivateAssets="all" />
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
using PurePatcher.Annotations;

[PurePatcherField]
public static extern ref int MyInt(this TargetClass target);
```

## Build

```sh
dotnet build Source/PurePatcher.slnx
```

## Acknowledgments

PurePatcher is forked from [Prepatcher](https://github.com/Zetrith/Prepatcher), and builds on Harmony, MonoMod, and Mono.Cecil.
