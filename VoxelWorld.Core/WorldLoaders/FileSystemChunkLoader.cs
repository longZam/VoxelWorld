using System.Collections.Concurrent;

namespace VoxelWorld.Core.WorldLoaders;


/// <summary>
/// 로컬 컴퓨터의 파일 시스템에서 청크를 로딩하는 클래스
/// </summary>
public class FileSystemChunkLoader : IChunkLoader
{
    private readonly FileSystemRegionLoader regionLoader;

    /// <summary>
    /// 같은 내용의 중복 요청으로 인한 충돌을 방지하는 세마포어
    /// </summary>
    private readonly ConcurrentDictionary<Vector2Int, SemaphoreSlim> requestSemaphores;


    public FileSystemChunkLoader(FileSystemRegionLoader regionLoader)
    {
        this.regionLoader = regionLoader;
        this.requestSemaphores = new ConcurrentDictionary<Vector2Int, SemaphoreSlim>();
    }

    public async Task<Chunk> LoadChunkAsync(Vector2Int chunkPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 청크 위치로부터 region의 좌표를 계산
        Vector2Int regionPosition = World.ChunkToRegion(chunkPosition);

        Region region = await regionLoader.LoadRegionAsync(regionPosition, cancellationToken);

        // 같은 요청의 중복 작업이 동시에 수행됨을 방지
        var semaphore = requestSemaphores.GetOrAdd(chunkPosition, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // region 내에서의 로컬 청크 좌표 계산
            Vector2Int localChunkPosition = new(
                (chunkPosition.x % Region.WIDTH + Region.WIDTH) % Region.WIDTH,
                (chunkPosition.y % Region.WIDTH + Region.WIDTH) % Region.WIDTH
            );

            // 결과 반환
            return await region.GetChunkAsync(localChunkPosition, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }
}