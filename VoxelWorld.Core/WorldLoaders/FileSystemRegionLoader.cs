
using System.Collections.Concurrent;
using System.IO.Compression;

namespace VoxelWorld.Core.WorldLoaders;


public class FileSystemRegionLoader : IRegionLoader
{
    private readonly string directoryPath;
    private readonly Octree<Region> regionTree;
    private readonly ConcurrentDictionary<Vector2Int, SemaphoreSlim> requestSemaphores;


    public FileSystemRegionLoader(string directoryPath)
    {
        this.directoryPath = directoryPath;
        this.regionTree = new Octree<Region>(Vector3Int.Min / Region.BLOCK_CORNER, Vector3Int.Max / Region.BLOCK_CORNER);
        this.requestSemaphores = new ConcurrentDictionary<Vector2Int, SemaphoreSlim>();
    }

    public async Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var semaphore = requestSemaphores.GetOrAdd(regionPosition, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            Vector3Int rPos3D = new Vector3Int(regionPosition.x, regionPosition.y, 0);

            // 캐싱되어 있는 region은 바로 반환
            if (regionTree.TrySearch(rPos3D, out var result))
                return result;
            
            // 캐싱되어 있지 않으니 직접 생성하여 메모리에 올리기
            result = new Region();

            await Task.Run(() =>
            {
                string regionPath = Path.Combine(directoryPath, $"{regionPosition.x}_{regionPosition.y}.region");
                
                // 파일 시스템에서 이미 Region이 존재하는지 확인하고 읽어들임
                if (!File.Exists(regionPath))
                    return;
                using FileStream fileStream = File.OpenRead(regionPath);
                using GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using BinaryReader binaryReader = new BinaryReader(gZipStream);
                result.Deserialize(binaryReader);

            }, cancellationToken);


            // 나중에 빠르게 접근하기 위한 캐싱
            await Task.Run(() => regionTree.Insert(rPos3D, result), cancellationToken);
            // 요청 세마포어 쌓이지 않게 제거
            requestSemaphores.Remove(regionPosition, out var value);

            // 결과 반환
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Save()
    {
        List<Task> tasks = new List<Task>();

        regionTree.Preorder((position, region) =>
            tasks.Add(Task.Run(() => Save(directoryPath, new Vector2Int(position.x, position.y), region))));
        
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