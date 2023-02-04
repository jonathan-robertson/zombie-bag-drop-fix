# Zombie Bag Drop Fix

[![🧪 Tested On](https://img.shields.io/badge/🧪%20Tested%20On-A20.6%20b9-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/zombie-bag-drop-fix/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/zombie-bag-drop-fix/actions/workflows/release.yml)

![bag drop fix social image](https://github.com/jonathan-robertson/zombie-bag-drop-fix/raw/media/zombie-bag-drop-fix-social-image.jpg)

- [Zombie Bag Drop Fix](#zombie-bag-drop-fix)
  - [Summary](#summary)
    - [What This Mod Does](#what-this-mod-does)
    - [Compatibility](#compatibility)
    - [Admin Commands](#admin-commands)
    - [Is it... Tested?](#is-it-tested)
    - [Technical Details (How the Mod Works)](#technical-details-how-the-mod-works)

## Summary

7 Days to Die modlet: Fixes a vanilla bug causing zombie bag drops to be attempted on chunk reload for cached, dead zombies... by no longer re-loading dead entities on chunk load.

### What This Mod Does

This mod permanently removes all dead zombie "entity stubs" from a chunk during its reload process.

This reload process only triggers when a dead zombie entity has already been fully unloaded from active memory in the game - either due to saving/closing a local game, restarting a server, or due to dynamic unloading of the chunk due to no players/observers being within range of the chunk.

> ℹ️ A key way to identify if an entity is fully unloaded would be to run the admin command `listentities`. If the entity is missing from this list, that would indicate the entity has been unloaded and its stub is now recorded in the chunk/region in a form of "cold storage".

This adjustment accomplishes 2 goals:

1. [Bug Avoidance] *Avoids* the workflow which leads to a dead entity 'stub' (like a zombie) being reconstructed as an active entity object, which (if a zombie) leads to the zombie bag drop chance being unintentionally attempted once again.
2. [Slight Performance Improvement (more of a side-effect)] Lower active entity count for an entity that is not functionally meaningful (dead zombie/animal that has been abandoned) only serves to drain client and server resources without benefit.

### Compatibility

Environment | Compatible | Details
--- | --- | ---
Dedicated Server | Yes | only the server needs this mod (EAC can be **Enabled** on client and server)
Peer-to-Peer Hosting | Yes | only the host needs this mod (EAC must be **Disabled** on host)
Local Single Player | Yes | EAC must be **Disabled**

### Admin Commands

> ℹ️ You can always search for this command or any command by running:
>
> - `help * <partial or complete command name>`
> - or get details about this (or any) command and its options by running `help <command>`

primary | alternate | params | description
:---: | :---: | :---: | ---
bagdropfix | bdf | N/A | enable/disable debug logging for this mod (disabled by default)

This command will result in producing the following something like the following log entry (visible in server logs, active telnet connections, or from the local admin console if running locally):

> 2023-02-02T19:44:23 380.457 INF [ZombieBagDropFix.Patches.Chunk_OnLoad_Patches] DEBUG: Removing dead entity stub before Chunk.OnLoad for entity zombieMarlene_4416 to improve performance and prevent additional bag drop attempts bug.

*Note that leaving debug mode off does **very slightly** improve performance.*

### Is it... Tested?

Yes, of course! 😆 This was a major concern for me as well. Last thing I'd want is to make it so dead zombies only show up for the player who killed them, for example.

Test | Before | After
--- | --- | ---
All players leave chunk containing dead zombies, then one player returns | dead zombies respawns and bag drop chance is attempted again | dead zombies are not respawned, bag drop chances are not attempted again, and zombie reference now no longer exists in chunk
Multiple players are online and one player kills a zombie | the other player is able to see and interact with the zombie (loot from the zombie, if enabled, etc.) | same happens with mod installed, as expected
Multiple players are online and one player kills a zombie and does not leave area. Other player from outside of area approaches | dead zombie renders in for second player and ragdoll effect plays for that player, but bag drop attempt is not triggered | same happens with mod installed, as expected
One player is online and kills a zombie, then second player logs in and visits area first player is in | dead zombie renders in for second player and ragdoll effect plays for that player, but bag drop attempt is not triggered | same happens with mod installed, as expected

### Technical Details (How the Mod Works)

A Harmony prefix jumps in front of calls to `Chunk.Load` to run a pre-scan on the Entity Stubs stored within the Chunk's data. Any stubs for entities with a recorded death time are then removed before the Chunk.Load process begins.

If the `deathTime` value on the stub is greater than zero, we know the recorded zombie data *would* be turned into a dead EntityZombie immediately after spawning in. How the game handles dead zombie spawning (currently in A20.6 b9) results in an additional and seemingly unintentional zombie bag drop chance triggering in the final lines of `EntityAlive.dropItemOnDeath`.

So as a result of intercepting with a Prefix on `Chunk.Load`, animal/zombie corpses that have been abandoned are *not* respawned into the world for any players once all online players have abandoned that chunk or once the server or local game has been restarted. The code used to loop through the entity stub list only once and collect the indexes of dead zombies to a static stack that is then iterated over to remove dead zombies by index in last-in-first-out order (reverse order for list safety).

While this also helps to speed up client-side performance (spawning entities in the world generates overhead that is not necessary in this case), improving performance in vanilla is not the core goal of this mod. That said, I have carefully structured the code I'm *adding* with performance in mind and am very happy with the results! After all, a bug fix that slows performance can end up adding more problems than it seeks to fix.

> 🎉 This mod/bug-fix is both open source and highly performant; please feel free to review it for yourself and offer suggestions to make it even better!
