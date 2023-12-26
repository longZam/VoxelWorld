using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    private readonly IChunkLoader chunkLoader;
    private readonly Octree<Chunk> chunkTree;
    private readonly ReaderWriterLockSlim chunkReaderWriterLockSlim;
    private readonly ConcurrentDictionary<Vector3Int, SemaphoreSlim> requestSemaphores;


    public World(IChunkLoader chunkLoader)
    {
        this.chunkLoader = chunkLoader;
        this.chunkTree = new Octree<Chunk>(Vector3Int.Min / Chunk.CORNER, Vector3Int.Max / Chunk.CORNER);
        this.chunkReaderWriterLockSlim = new ReaderWriterLockSlim();
        this.requestSemaphores = new ConcurrentDictionary<Vector3Int, SemaphoreSlim>();
    }
    
    public async Task<Chunk> LoadChunkAsync(Vector3Int position, CancellationToken cancellationToken = default)
    {
        Chunk? result;

        var semaphore = requestSemaphores.GetOrAdd(position, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            chunkReaderWriterLockSlim.EnterReadLock();
            try
            {
                if (chunkTree.TrySearch(position, out result))
                    return result;
            }
            finally
            {
                chunkReaderWriterLockSlim.ExitReadLock();
            }

            result = await chunkLoader.LoadChunkAsync(position, cancellationToken);

            chunkReaderWriterLockSlim.EnterWriteLock();
            try
            {
                chunkTree.Insert(position, result);
            }
            finally
            {
                chunkReaderWriterLockSlim.ExitWriteLock();
            }
        }
        finally
        {
            semaphore.Release();
        }

        return result;
    }

    public static Vector2Int WorldToRegion(Vector3Int worldPosition)
    {
        return ChunkToRegion(WorldToChunk(worldPosition));
    }

    public static Vector2Int ChunkToRegion(Vector3Int chunkPosition)
    {
        return new Vector2Int(chunkPosition.x >> 4,
                            chunkPosition.y >> 4);
    }

    public static Vector3Int WorldToChunk(Vector3Int worldPosition)
    {
        return new Vector3Int(worldPosition.x >> 4,
                            worldPosition.y >> 4,
                            worldPosition.z >> 4);
    }
    
    public static Vector3Int RegionToChunk(Vector2Int regionPosition)
    {
        return new Vector3Int(regionPosition.x, regionPosition.y, 0) * Region.CHUNK_SIDE;
    }

    public static Vector3Int ChunkToWorld(Vector3Int chunkPosition)
    {
        return chunkPosition * Chunk.CORNER;
    }

    public static Vector3Int RegionToWorld(Vector2Int regionPosition)
    {
        return ChunkToWorld(RegionToChunk(regionPosition));
    }
}