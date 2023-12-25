// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.IO.Compression;
using VoxelWorld.Core;
using VoxelWorld.Core.WorldLoaders;

string worldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "world");
WorldGenerator worldGenerator = new WorldGenerator(0);
worldGenerator.modifiers.Add(0, (previousBlock, noiseGenerator, x, y, z) =>
{
    if (z > 80)
        return BlockType.Air;
    return noiseGenerator.Noise3D(x, y, z, 16, 16, 16, 1) > 0.5f ? BlockType.Grass : BlockType.Air;
});

FileSystemRegionLoader regionLoader = new FileSystemRegionLoader(worldPath);
FileSystemChunkLoader chunkLoader = new FileSystemChunkLoader(regionLoader, worldGenerator);
World world = new World(chunkLoader);

List<Task<Chunk>> tasks = new List<Task<Chunk>>();

Console.Write("스폰 청크 로딩 중... ");
int size = 64;

Stopwatch stopwatch = Stopwatch.StartNew();
for (int z = 0; z < 32; z++)
    for (int y = -size; y < size; y++)
        for (int x = -size; x < size; x++)
            tasks.Add(world.LoadChunkAsync(new Vector3Int(x, y, z)));

await Task.WhenAll(tasks);
stopwatch.Stop();

Console.WriteLine("완료!");
Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");

Console.Write("청크 저장 중... ");
stopwatch.Restart();
await regionLoader.Save();
stopwatch.Stop();
Console.WriteLine("완료!");
Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");