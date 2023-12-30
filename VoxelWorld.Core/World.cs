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
        this.chunkTree = new Octree<Chunk>(new(Vector3Int.Min / Chunk.CORNER, Vector3Int.Max / Chunk.CORNER));
        this.requestSemaphores = new ConcurrentDictionary<Vector3Int, SemaphoreSlim>();
    }
    
    public async Task<Chunk> LoadChunkAsync(Vector3Int position, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // 같은 요청에 대해 동시에 수행할 수 없도록 락 걸기
        // 해시 충돌이 우려되긴 하나, 아주 약간의 성능 저하만이 예상되므로 무시 가능하다고 판단
        var semaphore = requestSemaphores.GetOrAdd(position, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // 캐싱되어 있는 청크는 바로 반환
            if (chunkTree.TrySearch(position, out var result))
                return result;

            // 네트워크, 혹은 파일 시스템 등의 서비스로부터 청크 로딩을 요청
            result = await chunkLoader.LoadChunkAsync(position, cancellationToken);

            // 나중에 빠르게 접근하기 위한 캐싱
            await Task.Run(() => chunkTree.Insert(position, result), cancellationToken);

            // 요청 세마포어 쌓이지 않게 제거
            requestSemaphores.Remove(position, out var value);

            // 결과 반환
            return result;
        }
        finally
        {
            // 락 해제
            semaphore.Release();
        }
    }

    public static Vector2Int WorldToRegion(Vector3Int worldPosition)
    {
        return ChunkToRegion(WorldToChunk(worldPosition));
    }

    public static Vector2Int ChunkToRegion(Vector3Int chunkPosition)
    {
        return new Vector2Int(chunkPosition.x >> Region.CHUNK_CORNER_BIT,
                            chunkPosition.y >> Region.CHUNK_CORNER_BIT);
    }

    public static Vector3Int WorldToChunk(Vector3Int worldPosition)
    {
        return new Vector3Int(worldPosition.x >> Chunk.CORNER_BIT,
                            worldPosition.y >> Chunk.CORNER_BIT,
                            worldPosition.z >> Chunk.CORNER_BIT);
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