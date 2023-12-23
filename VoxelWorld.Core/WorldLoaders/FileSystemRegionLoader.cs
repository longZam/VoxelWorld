
using System.Collections.Concurrent;
using System.IO.Compression;

namespace VoxelWorld.Core.WorldLoaders;


public class FileSystemRegionLoader : IRegionLoader
{
    private readonly string directoryPath;
    private readonly ConcurrentDictionary<Vector2Int, TaskCompletionSource<Region>> load;


    public FileSystemRegionLoader(string directoryPath)
    {
        this.directoryPath = directoryPath;
        this.load = new ConcurrentDictionary<Vector2Int, TaskCompletionSource<Region>>();
    }

    public async Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // 이미 로드 중인 작업 있으면 그 작업에 대기하기
        if (load.TryGetValue(regionPosition, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);

        // 서로 추가하다가 실패한 쪽은 task를 얻어서 대기하기
        if (!load.TryAdd(regionPosition, new TaskCompletionSource<Region>()))
            return await load[regionPosition].Task.WaitAsync(cancellationToken);
        
        string regionPath = Path.Combine(directoryPath, $"{regionPosition.x}_{regionPosition.y}.region");
        Region region = new Region(regionPosition);
        
        // 파일 시스템에서 이미 Region이 존재하는지 확인하고 읽어들임
        if (File.Exists(regionPath))
        {
            using FileStream fileStream = File.OpenRead(regionPath);
            using GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using BinaryReader binaryReader = new BinaryReader(gZipStream);

            region.Deserialize(binaryReader);
        }

        // 작업이 완료되었으니 dictionary에서 tcs를 제거하고 대기자들에게 결과를 통지
        if (load.Remove(regionPosition, out tcs))
            tcs.SetResult(region);

        return region;
    }
}