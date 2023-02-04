# Bug Report for Unexpected Triggering of Zombie Bag Drop Flow

7 Days to Die appears to *unintentionally* trigger a loot bag drop attempt for dead entities that are reloaded from entity stubs as a chunk containing them is reloaded. In other words, the zombie a player killed that was already dead and already 'rolled' for a bag drop chance (perhaps even dropped a loot bag!) now 'rolls' for that chance again when reloaded from a chunk's stored entity stub.

In this report, I attempt to explain what seems to be an unintentional bug and offer a possible solution for how it might be resolved in the simplest way I can personally come up with.

Since The Fun Pimps have far more experience with their own codebase and in the Game Industry than I do, I have no illusion about this being the perfect or right solution; I'm simply attempting to be helpful if possible and reflect a little love back to The Fun Pimps team that they've showed modders over the years. 💖

- [Bug Report for Unexpected Triggering of Zombie Bag Drop Flow](#bug-report-for-unexpected-triggering-of-zombie-bag-drop-flow)
  - [How to Trigger the Bug](#how-to-trigger-the-bug)
  - [Thoughts for The Fun Pimps on How This Might Be Resolved in Source](#thoughts-for-the-fun-pimps-on-how-this-might-be-resolved-in-source)
    - [`EntityAlive` (with new field, method, and conditional check added)](#entityalive-with-new-field-method-and-conditional-check-added)
    - [`EntityCreationData.ApplyToEntity` (with new setter call and embedded conditional added)](#entitycreationdataapplytoentity-with-new-setter-call-and-embedded-conditional-added)

## How to Trigger the Bug

This can happen in any of the following scenarios so long as a dead zombie is present in the given chunk:

1. continuing a local game
2. restarting then reconnecting to a dedicated server (or peer-to-peer multiplayer session)
3. simply leaving the chunk and returning to it

- [Zombie Bag Drop Bug Demonstration (YouTube, unlisted)](https://youtu.be/dP-1otDCcPE)

## Thoughts for The Fun Pimps on How This Might Be Resolved in Source

Unlike the `zombie-bag-drop-fix` modlet I created, I'd assume you want to continue supporting the reload of dead zombie as 7DTD currently does. With this in mind, here is a suggested solution for doing so with (hopefull) the smallest, simplest solution possible... But I wouldn't be surprised if The Fun Pimps can come up with an ever better solution.

1. I suspect that adding a new boolean field to `EntityAlive` (perhaps named `wasAlreadyKilled`) may help... and this be transient; temporary and without the need to save to any file.
2. This field could be left alone throughout the typical code flow, but when the entity is created from an entity recreated from EntityCreationData, a check against `EntityCreationData.deathTime` could be performed.
    - Context: *I believe `deathTime > 0` is taken to mean that the entity this entity-stub is referencing has already been killed. Therefore, any loot bag drop attempt anticipated by the player would've already been performed in the past.*
3. And finally, the `EntityAlive.dropItemOnDeath` method could be updated to check against the new `EntityAlive.wasAlreadyKilled` field. If `EntityAlive.wasAlreadyKilled` is true, the code could opt to skip over the loot bag drop calls.

### `EntityAlive` (with new field, method, and conditional check added)

```csharp
public abstract class EntityAlive : Entity
{
    // ...

    // === START NEW CODE ===========================================================================================
    public bool wasAlreadyKilled = false;
    // === END NEW CODE =============================================================================================

    // ...

    // === START NEW CODE ===========================================================================================
    public void SetWasAlreadyKilled(bool _wasAlreadyKilled)
    {
        wasAlreadyKilled = _wasAlreadyKilled;
    }
    // === END NEW CODE =============================================================================================

    // ...

    protected virtual void dropItemOnDeath()
    {
        for (var i = 0; i < this.inventory.GetItemCount(); i++)
        {
            ItemStack item = this.inventory.GetItem(i);
            var forId = ItemClass.GetForId(item.itemValue.type);
            if (forId != null && forId.CanDrop())
            {
                this.world.GetGameManager().ItemDropServer(item, this.position, new Vector3(0.5f, 0f, 0.5f), -1, Constants.cItemDroppedOnDeathLifetime, false);
                this.inventory.SetItem(i, ItemValue.None.Clone(), 0, true);
            }
        }
        this.inventory.SetFlashlight(false);
        this.equipment.DropItems();
        // === START NEW CODE =======================================================================================
        if (wasAlreadyKilled)
        {
            // no need to drop a loot bag since a bag drop attempt has already been made for this entity once before.
            return;
        }
        // === END NEW CODE =========================================================================================
        if (this.world.IsDark())
        {
            this.lootDropProb *= 1f;
        }
        if (this.entityThatKilledMe)
        {
            this.lootDropProb = EffectManager.GetValue(PassiveEffects.LootDropProb, this.entityThatKilledMe.inventory.holdingItemItemValue, this.lootDropProb, this.entityThatKilledMe, null, default(FastTags), true, true, true, true, 1, true);
        }
        if (this.lootDropProb > this.rand.RandomFloat)
        {
            GameManager.Instance.DropContentOfLootContainerServer(BlockValue.Air, new Vector3i(this.position), this.entityId);
        }
    }
}
```

### `EntityCreationData.ApplyToEntity` (with new setter call and embedded conditional added)

```csharp
public class EntityCreationData
{
    // ...
    public void ApplyToEntity(Entity _e)
    {
        var entityAlive = _e as EntityAlive;
        if (entityAlive)
        {
            if (this.stats != null)
            {
                this.stats.CopyBuffChangedDelegates(entityAlive.Stats);
                entityAlive.Stats = this.stats;
                entityAlive.Stats.Entity = entityAlive;
            }
            else
            {
                entityAlive.Stats.InitWithOldFormatData(this.health, this.stamina, this.sickness, this.gassiness);
            }
            if (entityAlive.Health <= 0)
            {
                entityAlive.HasDeathAnim = false;
            }
            entityAlive.SetDeathTime(this.deathTime);
            // === START NEW CODE =======================================================================================
            entityAlive.SetWasAlreadyKilled(this.deathTime > 0);
            // === END NEW CODE =========================================================================================
            entityAlive.setHomeArea(this.homePosition, this.homeRange);
            var entityPlayer = _e as EntityPlayer;
            if (entityPlayer)
            {
                entityPlayer.playerProfile = this.playerProfile;
            }
            entityAlive.bodyDamage = this.bodyDamage;
            entityAlive.IsSleeper = this.isSleeper;
            if (entityAlive.IsSleeper)
            {
                entityAlive.IsSleeperPassive = this.isSleeperPassive;
            }
            entityAlive.CurrentHeadState = this.headState;
            entityAlive.IsDancing = this.isDancing;
        }
        _e.lootContainer = this.lootContainer;
        _e.spawnByAllowShare = this.spawnByAllowShare;
        _e.spawnById = this.spawnById;
        _e.spawnByName = this.spawnByName;
        var entityTrader = _e as EntityTrader;
        if (entityTrader)
        {
            entityTrader.TileEntityTrader = this.traderData;
        }
        if (this.sleeperPose != 255 && entityAlive)
        {
            entityAlive.TriggerSleeperPose((int)this.sleeperPose);
        }
        _e.SetSpawnerSource(this.spawnerSource);
        if (this.entityData.Length > 0L)
        {
            this.entityData.Position = 0L;
            try
            {
                using (var pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    pooledBinaryReader.SetBaseStream(this.entityData);
                    _e.Read(this.readFileVersion, pooledBinaryReader);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
                Log.Error("Error loading entity " + (_e?.ToString()));
            }
        }
    }

    // ...
}
```
