using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    public static readonly RectInt CHUNK_BOUNDARY = new(Vector2Int.Min / Chunk.WIDTH, Vector2Int.Max / Chunk.WIDTH);

    private readonly IChunkLoader chunkLoader;
    private readonly QuadTreeInteger<Chunk> chunkTree;
    private readonly ConcurrentDictionary<Vector2Int, SemaphoreSlim> requestSemaphores;


    public World(IChunkLoader chunkLoader)
    {
        this.chunkLoader = chunkLoader;
        this.chunkTree = new QuadTreeInteger<Chunk>(in CHUNK_BOUNDARY);
        this.requestSemaphores = new ConcurrentDictionary<Vector2Int, SemaphoreSlim>();
    }
    
    public async Task<Chunk> LoadChunkAsync(Vector2Int position, CancellationToken cancellationToken = default)
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
            await Task.Run(() => chunkTree.Insert(in position, result), cancellationToken);

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

    public static Vector2Int WorldToRegion(in Vector3Int worldPosition)
    {
        // 음수 부호를 유지하기 위한 비트 시프트 연산
        return new Vector2Int(worldPosition.x >> 9,
                            worldPosition.y >> 9);
    }

    public static Vector2Int ChunkToRegion(in Vector2Int chunkPosition)
    {
        // 음수 부호 유지하기 위한 비트 시프트 연산
        return new Vector2Int(chunkPosition.x >> 5,
                            chunkPosition.y >> 5);
    }

    public static Vector2Int WorldToChunk(in Vector3Int worldPosition)
    {
        // 음수 부호 유지하기 위한 비트 시프트 연산
        return new Vector2Int(worldPosition.x >> 4,
                            worldPosition.y >> 4);
    }
    
    public static Vector2Int RegionToChunk(in Vector2Int regionPosition)
    {
        return Region.WIDTH * Chunk.WIDTH * regionPosition;
    }

    public static Vector3Int ChunkToWorld(in Vector2Int chunkPosition)
    {
        Vector2Int worldPosition = chunkPosition * Chunk.WIDTH;

        return new(
            worldPosition.x,
            worldPosition.y,
            0
        );
    }

    public static Vector3Int RegionToWorld(in Vector2Int regionPosition)
    {
        return ChunkToWorld(RegionToChunk(regionPosition));
    }
}