using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    // 각 axis에 대해 정렬된 리스트를 만들고, 이진 탐색으로 탐색하기?
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
        worldPosition /= Region.BLOCK_CORNER;
        return new(worldPosition.x, worldPosition.y);
    }

    public static Vector3Int WorldToChunk(Vector3Int worldPosition)
    {
        return worldPosition / Chunk.CORNER;
    }

    public static Vector3Int RegionToWorld(Vector2Int regionPosition)
    {
        return new Vector3Int(regionPosition.x * Region.BLOCK_CORNER,
                              regionPosition.y * Region.BLOCK_CORNER,
                              0);
    }

    public static Vector3Int ChunkToWorld(Vector3Int chunkPosition)
    {
        return chunkPosition * Chunk.CORNER;
    }
}