# Zombie Bag Drop Fix

[![🧪 Tested On](https://img.shields.io/badge/🧪%20Tested%20On-A20.6%20b9-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/zombie-bag-drop-fix/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/zombie-bag-drop-fix/actions/workflows/release.yml)

![bag drop fix social image](https://github.com/jonathan-robertson/zombie-bag-drop-fix/raw/media/zombie-bag-drop-fix-social-image.jpg)

- [Zombie Bag Drop Fix](#zombie-bag-drop-fix)
  - [Summary](#summary)
    - [Bug/Exploit Details](#bugexploit-details)
    - [How Mod Addresses Bug](#how-mod-addresses-bug)
    - [Is it... Tested?](#is-it-tested)
  - [Admin Commands](#admin-commands)
  - [Compatibility](#compatibility)

## Summary

7 Days to Die modlet: Fixes a vanilla bug causing zombie bag drops to be attempted on chunk reload for cached, dead zombies... by no longer re-loading dead entities on chunk load.

### Bug/Exploit Details

7 Days to Die has a bug that's hard to detect unless you realize it exists or have mods installed that extend the time-to-live of zombie corpses beyond the short (but still exploitable) timeframe.

When a zombie dies, the code (among other things) triggers a ragdoll effect on a zombie and initiates the necessary code to attempt a loot bag drop.

This zombie corpse (which is just the same zombie with a death flag) will then be stored in the chunk's offline memory if all players leave the area or log out from the server.

Upon logging back in or returning to that same chunk containing the dead zombie, a code flow will be initiated that triggers the zombie to ragdoll again... but also will trigger another attempt at a loot bag drop.

***This can be repeated as many times as the players wants in order to produce several dozen bags, depending on chance and how much time the player is willing to put in.***

As you might imagine, this is especially impactful if you happen to run a mod that extends the zombie corpse timeout, often for reasons related to adding back the ability to loot zombie bodies. It's even more impactful if adding zombies or modifying the bag drop chance close to 100% (as Snufkin's server-side Juggernaut does).

### How Mod Addresses Bug

A Harmony prefix jumps in front of calls to `Chunk.Load` to run a pre-scan on the Entity Stubs stored within the Chunk's data. Any stubs for entities with a recorded death time are then removed before the Chunk.Load begins.

This non-zero presence of `deathTime` is used to identify whether the zombie should be dead when the entity for it is generated and placed in the game world... and is of course also what leads down the code flow that triggers the additional bag drop chance in the final lines of `EntityAlive.dropItemOnDeath`.

As a result of intercepting here, animal/zombie corpses that have been abandoned are not re-rendered into the world for any players once they have all abandoned that chunk and this helps to speed up client-side performance (spawning entities in the world generates overhead that is not necessary in this case).

The code used to loop through the entity stub list only once and collect the indexes of dead zombies to a static stack that is then iterated over to remove dead zombies by index in last-in-first-out order (reverse order for list safety).

With *NEAR-ZERO* performance impact, I'm very happy with the results! 🎉

### Is it... Tested?

Yes, of course! 😆 This was a major concern for me as well. Last thing I'd want is to make it so dead zombies only show up for the player who killed them, for example.

Test | Before | After
--- | --- | ---
All players leave chunk containing dead zombies, then one player returns | dead zombies respawns and bag drop chance is attempted again | dead zombies are not respawned, bag drop chances are not attempted again, and zombie reference now no longer exists in chunk
Multiple players are online and one player kills a zombie | the other player is able to see and interact with the zombie (loot from the zombie, if enabled, etc.) | same happens with mod installed, as expected
Multiple players are online and one player kills a zombie and does not leave area. Other player from outside of area approaches | dead zombie renders in for second player and ragdoll effect plays for that player, but bag drop attempt is not triggered | same happens with mod installed, as expected
One player is online and kills a zombie, then second player logs in and visits area first player is in | dead zombie renders in for second player and ragdoll effect plays for that player, but bag drop attempt is not triggered | same happens with mod installed, as expected

## Admin Commands

primary | alternate
:---: | :---:
bagdropfix | bdf

params | description
:---: | ---
N/A | enable/disable debug logging for this mod

> ℹ️ You can always search for this command or any command by running:
>
> - `help * <partial or complete command name>`
> - or get details about this (or any) command and its options by running `help <command>`

## Compatibility

Environment | Compatible | Details
--- | --- | ---
Dedicated Server | Yes | only the server needs this mod (EAC can be **Enabled** on client and server)
Peer-to-Peer Hosting | Yes | only the host needs this mod (EAC must be **Disabled** on host)
Local Single Player | Yes | EAC must be **Disabled**
