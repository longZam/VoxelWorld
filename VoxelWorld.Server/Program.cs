// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using VoxelWorld.Core;

string worldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "world");

WorldGenerator worldGenerator = new WorldGenerator(0);
worldGenerator.modifiers.Add(0, (previousBlock, noiseGenerator, x, y, z) =>
{
    if (z > 80)
        return BlockType.Air;
    return noiseGenerator.Noise3D(x, y, z, 16, 16, 16, 1) > 0.5f ? BlockType.Grass : BlockType.Air;
});
FileSystemRegionLoader regionLoader = new FileSystemRegionLoader(worldPath, worldGenerator);
World world = new World(regionLoader);

List<Task<Region>> tasks = new List<Task<Region>>();

Console.Write("스폰 청크 로딩 중... ");

// 스폰 청크 로딩
for (int x = -10; x < 10; x++)
    for (int z = -10; z < 10; z++)
        tasks.Add(regionLoader.LoadRegionAsync(x, z));

Region[] regions = await Task.WhenAll(tasks);
Console.WriteLine("완료!");

Dictionary<(int x, int z), Region> regionDic = new Dictionary<(int x, int z), Region>();

for (int x = -10; x < 10; x++)
    for (int z = -10; z < 10; z++)
        regionDic[(x, z)] = regions[(x + 10) * 10 + (z + 10)];

List<Task> saveTasks = new List<Task>();

foreach (var regionKvp in regionDic)
{
    (int x, int z) = (regionKvp.Key.x, regionKvp.Key.z);

    saveTasks.Add(SaveRegion(Directory.CreateDirectory(worldPath), x, z, regionKvp.Value));
}

Console.Write("저장 중... ");
await Task.WhenAll(saveTasks);
Console.WriteLine("완료!");

static Task SaveRegion(DirectoryInfo directoryInfo, int regionX, int regionZ, Region region)
{
    return Task.Run(() => 
    {
        using FileStream fileStream = File.OpenWrite(Path.Combine(directoryInfo.FullName, $"{regionX}_{regionZ}.region"));
        using GZipStream gZipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using BinaryWriter binaryWriter = new BinaryWriter(gZipStream);

        region.Serialize(binaryWriter);
    });
}