
using System.Collections.Concurrent;
using System.IO.Compression;

namespace VoxelWorld.Core.WorldLoaders;


public class FileSystemRegionLoader
{
    private readonly DirectoryInfo directory;
    private readonly Region.Factory regionFactory;
    private readonly QuadTreeInteger<Region> regionTree;
    private readonly ConcurrentDictionary<Vector2Int, SemaphoreSlim> requestSemaphores;


    public FileSystemRegionLoader(DirectoryInfo directory, Region.Factory regionFactory)
    {
        this.directory = directory;
        this.regionFactory = regionFactory;
        this.regionTree = new QuadTreeInteger<Region>(new(Vector2Int.Min / (Region.WIDTH * Chunk.WIDTH), Vector2Int.Max / (Region.WIDTH * Chunk.WIDTH)));
        this.requestSemaphores = new ConcurrentDictionary<Vector2Int, SemaphoreSlim>();
    }

    public async Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var semaphore = requestSemaphores.GetOrAdd(regionPosition, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // 캐싱되어 있는 region은 바로 반환
            if (regionTree.TrySearch(regionPosition, out var result))
                return result;
            
            // 캐싱되어 있지 않으니 직접 생성하여 메모리에 올리기
            result = await Task.Run(() =>
            {
                var result = regionFactory.Create(in regionPosition);
                string regionPath = Path.Combine(directory.FullName, $"{regionPosition.x}_{regionPosition.y}.region");
                
                // 파일 시스템에서 이미 Region이 존재하는지 확인하고 읽어들임
                if (!File.Exists(regionPath))
                    return result;
                
                using FileStream fileStream = File.OpenRead(regionPath);
                using GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using BinaryReader binaryReader = new BinaryReader(gZipStream);
                result.Deserialize(binaryReader);
                return result;
            }, cancellationToken);

            // 나중에 빠르게 접근하기 위한 캐싱
            await Task.Run(() => regionTree.Insert(regionPosition, result), cancellationToken);
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

}