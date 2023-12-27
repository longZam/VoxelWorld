using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    private readonly IChunkLoader chunkLoader;
    private readonly Octree<Chunk> chunkTree;
    private readonly ConcurrentDictionary<Vector3Int, SemaphoreSlim> requestSemaphores;


    public World(IChunkLoader chunkLoader)
    {
        this.chunkLoader = chunkLoader;
        this.chunkTree = new Octree<Chunk>(Vector3Int.Min / Chunk.CORNER, Vector3Int.Max / Chunk.CORNER);
        this.requestSemaphores = new ConcurrentDictionary<Vector3Int, SemaphoreSlim>();
    }
    
    public async Task<Chunk> LoadChunkAsync(Vector3Int position, CancellationToken cancellationToken = default)
    {
        Chunk? result;

        // 같은 요청에 대해 동시에 수행할 수 없도록 락 걸기
        // 해시 충돌이 우려되긴 하나, 아주 약간의 성능 저하만이 예상되므로 무시 가능하다고 판단
        var semaphore = requestSemaphores.GetOrAdd(position, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (chunkTree.TrySearch(position, out result))
                return result;

            result = await chunkLoader.LoadChunkAsync(position, cancellationToken);

            chunkTree.Insert(position, result);
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