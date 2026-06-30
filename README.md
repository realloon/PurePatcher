# PurePatcher

[![Latest annotations version](https://img.shields.io/nuget/v/PurePatcher.Annotations?label=PurePatcher.Annotations)](https://www.nuget.org/packages/PurePatcher.Annotations)

Structured assembly rewriting library/mod for RimWorld

Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2934420800

The project has two main logical components:

- **Assembly rewriter** - in principle platform-agnostic
- **Assembly reloader** - specific to the Mono runtime used by RimWorld's Unity version

## Installation

### Users

- Download the mod by clicking _Code_ > _Download ZIP_ on this repo's main page
- Unzip in RimWorld's Mods folder
- Install HarmonyRimWorld and put Harmony before PurePatcher.
- Put PurePatcher before Core in the mod list.

The mod is currently no longer distributed through GitHub's Releases tab.

PurePatcher requires [HarmonyRimWorld](https://github.com/pardeike/HarmonyRimWorld) and is not a replacement for it.

### Modders

Add the `PurePatcher.Annotations` package to your mod's project:

`<PackageReference Include="PurePatcher.Annotations" Version="<version>" PrivateAssets="all" />`

Similar to Harmony, the package distributes annotations to be used for compiling only and the actual runtime library is installed by the user once using the mod downloaded from here.

To make a RimWorld mod correctly depend on PurePatcher, put this in `About.xml`:

```xml
<modDependencies>
  <li>
    <packageId>Vortex.PurePatcher</packageId>
    <displayName>PurePatcher</displayName>
    <downloadUrl>https://github.com/VortexGaming/PurePatcher</downloadUrl>
  </li>
</modDependencies>
```

Library example (declaring field addition):

```cs
using PurePatcher.Annotations;

[PurePatcherField]
public static extern ref int MyInt(this TargetClass target);
```

## Compiling

Clone anywhere and go to the Source folder. Run `dotnet build` and/or `dotnet test`.
If you want to run it ingame clone to the Mods folder.

## Acknowledgments

Thanks to Pardeike for making [Harmony](https://github.com/pardeike/Harmony), 0x0ade for [MonoMod](https://github.com/MonoMod/MonoMod) and jbevain for [Mono.Cecil](https://github.com/jbevain/cecil).

Thanks to [jikulopo](https://github.com/jikulopo) for an early update to RimWorld 1.6.
