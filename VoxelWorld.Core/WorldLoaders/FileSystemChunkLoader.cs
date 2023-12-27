


using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class FileSystemChunkLoader : IChunkLoader
{
    private readonly FileSystemRegionLoader regionLoader;
    private readonly WorldGenerator worldGenerator;
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


        Vector2Int regionPosition = World.ChunkToRegion(chunkPosition);

        int x = chunkPosition.x % Region.CHUNK_CORNER;
        int y = chunkPosition.y % Region.CHUNK_CORNER;
        int z = chunkPosition.z % Region.CHUNK_CORNER;

        if (x < 0)
            x += Region.CHUNK_CORNER;
        if (y < 0)
            y += Region.CHUNK_CORNER;
        if (z < 0)
            z += Region.CHUNK_CORNER;

        Region result = await regionLoader.LoadRegionAsync(regionPosition, cancellationToken);

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