using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Collections.Pooled;

namespace Arch.Core;

/// <summary>
/// An archetype, stores entities with the same components, tightly packed in <see cref="Chunks"/>
/// </summary>
public sealed unsafe class Archetype {
    
    public const int TOTAL_CAPACITY = 16000; // 16KB, fits perfectly into one L1 Cache
    
    public Archetype(params Type[] types) {

        Types = types;
        EntitiesPerChunk = CalculateEntitiesPerChunk(types);
        
        // The bitmask/set 
        BitSet = BitSetExtensions.From(types);
        EntityIdToChunkIndex = new PooledDictionary<int, int>(EntitiesPerChunk);
        Chunks = Array.Empty<Chunk>();
    }

    /// <summary>
    /// Sets the capacity and either makes the internal <see cref="Chunks"/> and <see cref="EntityIdToChunkIndex"/> arrays bigger or smaller. 
    /// </summary>
    /// <param name="newCapacity"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCapacity(int newCapacity) {

        // More size needed
        if (newCapacity > Capacity) {
            
            // Increase chunk array size
            var newChunks = ArrayPool<Chunk>.Shared.Rent(newCapacity);
            Array.Copy(Chunks, newChunks, Size);
            ArrayPool<Chunk>.Shared.Return(Chunks);
            Chunks = newChunks;   
            
            // Increase mapping
            EntityIdToChunkIndex.EnsureCapacity(newCapacity * EntitiesPerChunk);
        }
        else {

            // Always keep capacity for atleast one chunk
            if (newCapacity <= 0) newCapacity = 1;

            // Decrease chunk size
            var newChunks = ArrayPool<Chunk>.Shared.Rent(newCapacity);
            Array.Copy(Chunks, newChunks, Size-1);
            ArrayPool<Chunk>.Shared.Return(Chunks);
            Chunks = newChunks;  

            // Decrease mapping
            EntityIdToChunkIndex.TrimExcess(newCapacity*EntitiesPerChunk);
        }
    }
    
    /// <summary>
    /// Adds an <see cref="Entity"/> to this chunk.
    /// Increases the <see cref="Size"/>. 
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>True if a new chunk was created</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(in Entity entity) {

        // If there reserved chunks, but no used yet... fill them 
        if (Capacity > 0 && Capacity >= Size) {
            
            ref var lastChunk = ref LastChunk;
            if (lastChunk.Size < lastChunk.Capacity) {

                lastChunk.Add(in entity);
                EntityIdToChunkIndex[entity.EntityId] = Size-1;
                
                // Last chunk was filled, still capacity, increase size to make next entity fill reserved chunk 
                if (lastChunk.Size == EntitiesPerChunk && Capacity >= Size + 1) {
                    Size++;
                    return false;
                }
                
                return false;
            }
        }
        
        // Create new chunk
        var newChunk = new Chunk(EntitiesPerChunk, Types);
        newChunk.Add(in entity);
            
        // Resize chunks
        SetCapacity(Size+1);
        
        // Add chunk & map entity
        Chunks[Size] = newChunk;
        EntityIdToChunkIndex[entity.EntityId] = Size;
        Capacity++;
        Size++;

        return true;
    }
    
    /// <summary>
    /// Reserves memory for a specific amount of <see cref="Entity"/>'s.
    /// Highly efficient bulk adding. Once allocated, you can traverse over the arch to fill it.
    /// Allocates on top of the existing <see cref="Capacity"/>.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>True if a new chunk was created</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AllocateFor(in int amount) {
            
        // Put into the last partial empty chunk 
        if (Size > 0) {
            
            // Calculate amount of required chunks
            ref var lastChunk = ref LastChunk;
            var freeSpots = lastChunk.Capacity - lastChunk.Size;
            var neededSpots = amount - freeSpots;
            var neededChunks = neededSpots / EntitiesPerChunk;
            
            // Set capacity and insert new empty chunks
            SetCapacity(Capacity+neededChunks);
            for (var index = 0; index < neededChunks; index++) {
                
                var newChunk = new Chunk(EntitiesPerChunk, Types);
                Chunks[Capacity+index] = newChunk;
            }
            Capacity += neededChunks;
        }
        else {
            
            // Allocate new chunks in one go
            var neededChunks = (int)Math.Ceiling(((float)amount / EntitiesPerChunk));
            SetCapacity(Capacity+neededChunks);
            for (var index = 0; index < neededChunks; index++) {
                
                var newChunk = new Chunk(EntitiesPerChunk, Types);
                Chunks[Capacity+index] = newChunk;
            } 
            Capacity += neededChunks; // So many chunks are allocated
            Size = 1; // Since no other chunks are allocated... 
        }
    }
    
    /// <summary>
    /// Sets an component into the fitting component array at an index.
    /// </summary>
    /// <param name="index">The index</param>
    /// <param name="cmp">The component</param>
    /// <typeparam name="T">The type</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(in int index, in T cmp) {

        var chunkIndex = EntityIdToChunkIndex[index];
        ref var chunk = ref Chunks[chunkIndex];
        chunk.Set(in index, in cmp);
    }

    /// <summary>
    /// Checks wether this chunk contains an array of the type.
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    /// <returns>True if it does, false if it doesnt</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() {

        var id = Component<T>.Id;
        return BitSet.IsSet(id);
    }
    
    /// <summary>
    /// Returns an component from the fitting component array by its index.
    /// </summary>
    /// <param name="index">The index</param>
    /// <typeparam name="T">The type</typeparam>
    /// <returns>The component</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>(in int index) {
        
        var chunkIndex = EntityIdToChunkIndex[index];
        ref var chunk = ref Chunks[chunkIndex];
        return ref chunk.Get<T>(in index);
    }
    
    /// <summary>
    /// Removes an <see cref="Entity"/> from this chunk and all its components. 
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>True if a chunk was destroyed</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(in Entity entity) {
        
        var chunkIndex = EntityIdToChunkIndex[entity.EntityId];
        ref var chunk = ref Chunks[chunkIndex];
        
        // If its the last chunk, simply remove the entity
        if (chunkIndex == Size-1) {
            chunk.Remove(entity);
            EntityIdToChunkIndex.Remove(entity.EntityId);
            return false;
        }
        
        // Move the last entity from the last chunk into the chunk to replace the removed entity directly
        var index = chunk.EntityIdToIndex[entity.EntityId];
        var movedEntityId = chunk.ReplaceIndexWithLastEntityFrom(index, ref LastChunk);
        EntityIdToChunkIndex.Remove(entity.EntityId);
        EntityIdToChunkIndex[movedEntityId] = chunkIndex;
        
        if (LastChunk.Size != 0) return false;
        
        // Remove last unused chunk & resize to free memory
        SetCapacity(Size-1);
        Capacity--;
        Size--;
        return true;
    }
    
    /// <summary>
    /// The types with which the <see cref="BitSet"/> was created.
    /// </summary>
    public Type[] Types { get; set; }
    
    /// <summary>
    /// The bitmask for querying, contains the component flags set for this archetype.
    /// </summary>
    public BitSet BitSet { get; set; }

    /// <summary>
    /// For mapping the entity id to the chunk it is in. 
    /// </summary>
    public PooledDictionary<int, int> EntityIdToChunkIndex { get; set; }
    
    /// <summary>
    /// A array of active chunks within this archetype. 
    /// </summary>
    public Chunk[] Chunks { get; private set; }
    
    /// <summary>
    /// Returns the last chunk from the <see cref="Chunks"/>
    /// </summary>
    public ref Chunk LastChunk => ref Chunks[Size-1];
 
    /// <summary>
    /// The chunk capacity, how many chunks are there in total. 
    /// </summary>
    public int Capacity { get; private set; }
    
    /// <summary>
    /// Indicates how many full chunks are currently being used.
    /// Partial empty chunks do not count. 
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// The amount of entities fitting in each chunk. 
    /// </summary>
    public int EntitiesPerChunk { get; private set; }

    /// <summary>
    /// Calculates how many entities with the types fit into one chunk. 
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    public static int CalculateEntitiesPerChunk(Type[] types) {
        return TOTAL_CAPACITY/(sizeof(Entity)+types.ToByteSize());
    }
}