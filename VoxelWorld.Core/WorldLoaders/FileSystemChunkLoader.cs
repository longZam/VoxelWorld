using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


/// <summary>
/// 로컬 컴퓨터의 파일 시스템에서 청크를 로딩하는 클래스
/// </summary>
public class FileSystemChunkLoader : IChunkLoader
{
    private readonly FileSystemRegionLoader regionLoader;
    private readonly WorldGenerator worldGenerator;
    
    /// <summary>
    /// 같은 내용의 중복 요청으로 인한 충돌을 방지하는 세마포어
    /// </summary>
    private readonly ConcurrentDictionary<Vector3Int, SemaphoreSlim> requestSemaphores;


    public FileSystemChunkLoader(FileSystemRegionLoader regionLoader, WorldGenerator worldGenerator)
    {
        this.regionLoader = regionLoader;
        this.worldGenerator = worldGenerator;
        this.requestSemaphores = new ConcurrentDictionary<Vector3Int, SemaphoreSlim>();
    }

    public async Task<Chunk> LoadChunkAsync(Vector3Int chunkPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 청크 위치로부터 region의 좌표를 계산
        Vector2Int regionPosition = World.ChunkToRegion(chunkPosition);

        // region 내에서의 로컬 청크 좌표 계산
        int x = chunkPosition.x % Region.CHUNK_CORNER;
        int y = chunkPosition.y % Region.CHUNK_CORNER;
        int z = chunkPosition.z % Region.CHUNK_CORNER;

        // 음수에 대한 로컬 좌표 처리
        if (x < 0)
            x += Region.CHUNK_CORNER;
        if (y < 0)
            y += Region.CHUNK_CORNER;
        if (z < 0)
            z += Region.CHUNK_CORNER;

        Region result = await regionLoader.LoadRegionAsync(regionPosition, cancellationToken);

        // 같은 요청의 중복 작업이 동시에 수행됨을 방지
        var semaphore = requestSemaphores.GetOrAdd(chunkPosition, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // 초기화 안 된 청크는 초기화 작업
            if (!result[x, y, z].initialized)
                worldGenerator.Modify(chunkPosition, result[x, y, z]);

            // 요청 세마포어 쌓이지 않게 제거
            requestSemaphores.Remove(chunkPosition, out var value);

            // 결과 반환
            return result[x, y, z];
        }
        finally
        {
            semaphore.Release();
        }
    }
}