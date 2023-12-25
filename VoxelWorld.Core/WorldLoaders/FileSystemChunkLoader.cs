


using System.Collections.Concurrent;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class FileSystemChunkLoader : IChunkLoader
{
    private readonly FileSystemRegionLoader regionLoader;
    private readonly ConcurrentDictionary<Vector3Int, TaskCompletionSource<Chunk>> requests;
    private readonly WorldGenerator worldGenerator;


    public FileSystemChunkLoader(FileSystemRegionLoader regionLoader, WorldGenerator worldGenerator)
    {
        this.regionLoader = regionLoader;
        this.requests = new ConcurrentDictionary<Vector3Int, TaskCompletionSource<Chunk>>();
        this.worldGenerator = worldGenerator;
    }

    public async Task<Chunk> LoadChunkAsync(Vector3Int chunkPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Vector2Int regionPosition = new Vector2Int(chunkPosition.x, chunkPosition.y) / Region.CHUNK_CORNER;
        int x = chunkPosition.x % Region.CHUNK_CORNER;
        int y = chunkPosition.y % Region.CHUNK_CORNER;
        int z = chunkPosition.z % Region.CHUNK_CORNER;
        if (x < 0)
            x += Region.CHUNK_CORNER;
        if (y < 0)
            y += Region.CHUNK_CORNER;
        if (z < 0)
            z += Region.CHUNK_CORNER;
        
        // 이미 로드 중인 작업 있으면 그 작업에 대기하기
        if (requests.TryGetValue(chunkPosition, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);

        // 서로 추가하다가 실패한 쪽은 task를 얻어서 대기하기
        if (!requests.TryAdd(chunkPosition, new TaskCompletionSource<Chunk>()))
            return await requests[chunkPosition].Task.WaitAsync(cancellationToken);
        
        Region result = await regionLoader.LoadRegionAsync(regionPosition, cancellationToken);

        if (!result[x, y, z].initialized)
            worldGenerator.Modify(chunkPosition, result[x, y, z]);

        if (requests.Remove(chunkPosition, out tcs))
            tcs.SetResult(result[x, y, z]);
        
        return result[x, y, z];
    }
}