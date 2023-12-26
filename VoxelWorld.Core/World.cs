using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    private readonly IChunkLoader chunkLoader;
    private readonly Octree<Chunk> chunkTree;
    private readonly ReaderWriterLockSlim chunkReaderWriterLockSlim;


    public World(IChunkLoader chunkLoader)
    {
        this.chunkLoader = chunkLoader;
        this.chunkTree = new Octree<Chunk>(268_435_456);
        this.chunkReaderWriterLockSlim = new ReaderWriterLockSlim();
    }
    
    public async Task<Chunk> LoadChunkAsync(Vector3Int position, CancellationToken cancellationToken = default)
    {
        Chunk? result = null;

        chunkReaderWriterLockSlim.EnterReadLock();
        if (chunkTree.TrySearch(position, out result))
            return result;
        chunkReaderWriterLockSlim.ExitReadLock();

        result = await chunkLoader.LoadChunkAsync(position, cancellationToken);

        chunkReaderWriterLockSlim.EnterWriteLock();
        chunkTree.Insert(position, result);
        chunkReaderWriterLockSlim.ExitWriteLock();

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