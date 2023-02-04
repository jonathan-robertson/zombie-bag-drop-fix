using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ZombieBagDropFix
{
    [HarmonyPatch(typeof(Chunk), "OnLoad")]
    internal class Chunk_OnLoad_Patches
    {
        private static readonly ModLog<Chunk_OnLoad_Patches> _log = new ModLog<Chunk_OnLoad_Patches>();
        private static readonly Stack<int> _indexesToRemove = new Stack<int>();

        /// <summary>
        /// Remove dead entity stubs upon chunk write to prevent bag drop bug.
        /// </summary>
        /// <param name="_world">Used to confirm that the entity stub's id isn't already loaded.</param>
        /// <param name="___entityStubs">The list to potentially remove entries from.</param>
        public static void Prefix(World _world, ref List<EntityCreationData> ___entityStubs)
        {
            try
            {
                // check each entity stub... this is an offline form of entity data used to recreate the heavier Entity object on chunk load.
                for (var i = 0; i < ___entityStubs.Count; i++)
                {
                    // if the entity stub's id is not already loaded into the game world (i.e. would be loaded in the method we're patching)...
                    // and if the entity stub has deathTime set (i.e. is dead and would have death code leading to bag drop chance triggered)...
                    if (!(_world.GetEntity(___entityStubs[i].id) != null) && ___entityStubs[i].deathTime > 0)
                    {
                        if (ModApi.DebugMode)
                        {
                            var entityClass = EntityClass.GetEntityClass(___entityStubs[i].entityClass);
                            var identifier = entityClass.classname == null
                                ? ___entityStubs[i].id.ToString()
                                : entityClass.entityClassName + "_" + ___entityStubs[i].id.ToString();
                            _log.Debug($"Removing dead entity stub before Chunk.OnLoad for entity {identifier} to improve performance and prevent additional bag drop attempts bug.");
                        }
                        _indexesToRemove.Push(i); // collect these indexes to a stack for reverse-order processing in a moment
                    }
                }
                // remove each detected entity stub from the chunk by index permanently, in reverse order via LIFO (safe removal for lists)
                while (_indexesToRemove.Count > 0)
                {
                    ___entityStubs.RemoveAt(_indexesToRemove.Pop());
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle Chunk.OnLoad Prefix.", e);
            }
        }
    }
}
