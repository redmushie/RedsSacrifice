# Red's Sacrifice
Changes the Artifact of Sacrifice to be much more balanced, especially in multiplayer.

## Motivation

In the base game, the Artifact of Sacrifice has a fixed chance of dropping an item **based on monster type alone**. This does **not** scale properly with the amount of players, since the game will then spawn bigger and stronger monsters, **not** proportionally more.

## How it works

Internally in Risk of Rain 2, the spawning logic uses **monster credits**. This mod functions by actually **recording** how many of these credits are spent when a monster is spawned, and how many credits were being distributed at the time.

Based on these metrics, we can determine the relative _value_ of a monster. From there, all we do is convert the value to a percentage to determine the item drop chance.

## Configuring
The mod can be configured though it's **config file**, `me.RedMushie.RedsSacrifice.cfg`. You can also configure it during gameplay
using console commands. The defaults are very sensible though :-).

## Console commands

### `rs_enabled [true|false]`
Enables or disables the mod functionality.

### `rs_debug [true|false]`
Enables or disables debugging log output in the console.

### `rs_global_mult [value]`
Sets the global drop chance multiplier (as percentage). Default value is `100`.

### `rs_shrine_multiplier [value]`
Sets the Combat Shrine drop chance multiplier (as percentage). Default value is `200`.

### `rs_classic_regular_mult [value]`
Sets the Classic non-boss wave drop chance multiplier (as percentage). Default value is `100`.

### `rs_classic_boss_mult [value]`
Sets the Classic boss wave drop chance multiplier (as percentage). Default value is `0`.

### `rs_sim_regular_mult [value]`
Sets the Simulacrum non-boss wave drop chance multiplier (as percentage). Default value is `100`.

### `rs_sim_boss_mult [value]`
Sets the Simulacrum boss wave drop chance multiplier (as percentage). Default value is `100`.

## Changelog

### v1.1.1 - 2022/03/21
- Attempt to fix NullReferenceException in the SpawnCard_onSpawnedServerGlobal.

### v1.1.0 - 2022/03/13
- Switch from Unity-based logging to BepInEx-based logging;
- Add config file support for all settings;
- Cleanly separated the Simulacrum calculation logic from the regular game calculation logic;
- Redid Simulacrum calculations;

### v1.0.4 - 2022/03/11
- Removed time component in SimulacrumBaseline calculation. Gameplay improved drastically.

### v1.0.3 - 2022/03/11
- Disabled debug logging that was left on by default on accident.

### v1.0.2 - 2022/03/11
- Fixed TeleporterBossBaseline overriding SimulacrumBaseline
- Tweak SimulacrumBaseline calculation to provide 66% more items, since it was providing astonishingly few.

### v1.0.1 - 2022/03/10
- Fixed accidental inclusion of AssetBundle loading logic

### v1.0.0 - 2022/03/10
Initial release!
- Fixed inability to distringuish Simulacrum waves from other CombatDirectors.