
using System.Collections.Concurrent;
using System.IO.Compression;

namespace VoxelWorld.Core.WorldLoaders;


public class FileSystemRegionLoader : IRegionLoader
{
    private readonly string directoryPath;
    private readonly ConcurrentDictionary<Vector2Int, TaskCompletionSource<Region>> requests;
    private readonly ConcurrentDictionary<Vector2Int, Region> cache;


    public FileSystemRegionLoader(string directoryPath)
    {
        this.directoryPath = directoryPath;
        this.requests = new ConcurrentDictionary<Vector2Int, TaskCompletionSource<Region>>();
        this.cache = new ConcurrentDictionary<Vector2Int, Region>();
    }

    public async Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (cache.TryGetValue(regionPosition, out var result))
            return result;

        // 이미 로드 중인 작업 있으면 그 작업에 대기하기
        if (requests.TryGetValue(regionPosition, out var tcs))
            return await tcs.Task.WaitAsync(cancellationToken);

        // 서로 추가하다가 실패한 쪽은 task를 얻어서 대기하기
        if (!requests.TryAdd(regionPosition, new TaskCompletionSource<Region>()))
            return await requests[regionPosition].Task.WaitAsync(cancellationToken);

        string regionPath = Path.Combine(directoryPath, $"{regionPosition.x}_{regionPosition.y}.region");
        Region region = new Region();

        // 파일 시스템에서 이미 Region이 존재하는지 확인하고 읽어들임
        if (File.Exists(regionPath))
        {
            using FileStream fileStream = File.OpenRead(regionPath);
            using GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using BinaryReader binaryReader = new BinaryReader(gZipStream);

            region.Deserialize(binaryReader);
        }

        // 작업이 완료되었으니 dictionary에서 tcs를 제거하고 대기자들에게 결과를 통지
        if (requests.Remove(regionPosition, out tcs))
            tcs.SetResult(region);

        cache.TryAdd(regionPosition, region);
        return region;
    }

    public async Task Save()
    {
        List<Task> tasks = new List<Task>();

        foreach (var regionKvp in cache)
            tasks.Add(Task.Run(() => Save(directoryPath, regionKvp.Key, regionKvp.Value)));
        
        await Task.WhenAll(tasks);
    }

    private static void Save(string path, Vector2Int position, Region region)
    {
        string tempPath = Path.GetTempFileName();

        // 임시 파일에 쓰기
        using (FileStream fileStream = File.OpenWrite(tempPath))
        {
            using GZipStream gZipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            using BinaryWriter binaryWriter = new BinaryWriter(gZipStream);
            region.Serialize(binaryWriter);
        }
        
        Directory.CreateDirectory(path);
        path = Path.Combine(path, $"{position.x}_{position.y}.region");

        if (File.Exists(path))
            File.Delete(path);
        
        File.Move(tempPath, path);
    }
}