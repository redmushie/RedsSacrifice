# Red's Sacrifice
Changes the Artifact of Sacrifice to be much more balanced, especially in multiplayer.

## Motivation

In the base game, the Artifact of Sacrifice has a fixed chance of dropping an item **based on monster type alone**. This does **not** scale properly with the amount of players, since the game will then spawn bigger and stronger monsters, **not** proportionally more.

## How it works

Internally in Risk of Rain 2, the spawning logic uses **monster credits**. This mod functions by actually **recording** how many of these credits are spent when a monster is spawned, and how many credits were being distributed at the time.

Based on these metrics, we can determine the relative _value_ of a monster. From there, all we do is convert the value to a percentage to determine the item drop chance.

## Configuring
The mod can currently only be configured at runtime using the console commands. The defaults are very sensible though :-).

## Console commands

### `rs_debugging [true|false]`
Enables or disables debugging log output in the console.

### `rs_globalmult [value]`
Sets the global multiplier (as percentage).
Default value is `100`.

### `rs_wavemult [value]`
Sets the wave multiplier (as percentage). Default value is `100`.

### `rs_shrinemult [value]`
Sets the combat shrine multiplier (**not** as percentage). Default value is `2`.

### `rs_simulacrummult [value]`
Sets the Simulacrum multiplier (as percentage). Default value is `100`.


## Changelog

### v1.0.0 - 2022/03/10
Initial release!
- Fixed inability to distringuish Simulacrum waves from other CombatDirectors.