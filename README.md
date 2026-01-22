<div align="center">

# [SwiftlyS2] FreeArmor

<a href="https://github.com/a2Labs-cc/SW2-FreeArmor/releases/latest">
  <img src="https://img.shields.io/github/v/release/a2Labs-cc/SW2-FreeArmor?label=release&color=07f223&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-FreeArmor/issues">
  <img src="https://img.shields.io/github/issues/a2Labs-cc/SW2-FreeArmor?label=issues&color=E63946&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-FreeArmor/releases">
  <img src="https://img.shields.io/github/downloads/a2Labs-cc/SW2-FreeArmor/total?label=downloads&color=3A86FF&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-FreeArmor/stargazers">
  <img src="https://img.shields.io/github/stars/a2Labs-cc/SW2-FreeArmor?label=stars&color=e3d322&style=for-the-badge">
</a>

<br/>
<sub>Made by <a href="https://github.com/agasking1337" target="_blank" rel="noopener noreferrer">aga</a></sub>

</div>


## Overview

**FreeArmor** gives players free armor on spawn during non-pistol rounds.

- Pistol rounds: `0` armor, no helmet
- Other rounds: `100` armor + helmet

## Support

Need help or have questions? Join our Discord server:

<p align="center">
  <a href="https://discord.gg/d853jMW2gh" target="_blank">
    <img src="https://img.shields.io/badge/Join%20Discord-5865F2?logo=discord&logoColor=white&style=for-the-badge" alt="Discord">
  </a>
</p>


## Download Shortcuts
<ul>
  <li>
    <code>📦</code>
    <strong>&nbsp;Download Latest Plugin Version</strong> &rarr;
    <a href="https://github.com/a2Labs-cc/SW2-FreeArmor/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>⚙️</code>
    <strong>&nbsp;Download Latest SwiftlyS2 Version</strong> &rarr;
    <a href="https://github.com/swiftly-solution/swiftlys2/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

## Installation

1. Download/build the plugin (publish output lands in `build/publish/FreeArmor/`).
2. Copy the published plugin folder to your server:

```
.../game/csgo/addons/swiftlys2/plugins/FreeArmor/
```
3. Ensure the `resources/` folder (translations, gamedata) is alongside the DLL.
4. Start/restart the server.

## Configuration

The plugin uses SwiftlyS2's JSON config system.

- **File name**: `config.jsonc`
- **Section**: `FreeArmor`

On first run the config is created automatically.

### Key Configuration Options

- `Enabled`: Master on/off switch (default: true)
- `AccessFlag`: Permission required to receive armor.
  - `""` (empty): everyone gets armor
  - `"vip"` (example): only players with that permission get armor

### CVars

- `freearmor_enabled <0|1>`: Enable/disable FreeArmor globally at runtime.

Examples:

```text
freearmor_enabled 0
freearmor_enabled 1
```

## Building

```bash
dotnet build
```

## Credits
- Readme template by [criskkky](https://github.com/criskkky)
- Release workflow based on [K4ryuu/K4-Guilds-SwiftlyS2 release workflow](https://github.com/K4ryuu/K4-Guilds-SwiftlyS2/blob/main/.github/workflows/release.yml)
