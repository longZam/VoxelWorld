namespace VoxelWorld.Core;


public delegate BlockType WorldModifier(BlockType previousBlock, PerlinNoise3D noiseGenerator, int x, int y, int z);

public class WorldGenerator
{
    private readonly int seed;
    private readonly PerlinNoise3D noiseGenerator;
    
    public readonly SortedList<int, WorldModifier> modifiers;


    public WorldGenerator(int seed)
    {
        this.seed = seed;
        this.noiseGenerator = new PerlinNoise3D(seed);
        this.modifiers = new SortedList<int, WorldModifier>();
    }


    public void Modify(Vector3Int chunkPosition, Chunk chunk)
    {
        Vector3Int worldOffset = World.ChunkToWorld(chunkPosition);

        for (int i = 0; i < modifiers.Count; i++)
        {
            // 클로저 대응용
            WorldModifier modifier = modifiers[i];

            Parallel.For(0, Chunk.VOLUME, (index) =>
            {
                Vector3Int localPosition = new()
                {
                    x = index % Chunk.CORNER,
                    y = index / Chunk.CORNER % Chunk.CORNER,
                    z = index / Chunk.SIDE % Chunk.CORNER
                };
                
                chunk[localPosition.x, localPosition.y, localPosition.z] = 
                                modifier.Invoke(chunk[localPosition.x, localPosition.y, localPosition.z],
                                noiseGenerator,
                                localPosition.x + worldOffset.x,
                                localPosition.y + worldOffset.y,
                                localPosition.z + worldOffset.z);
                
            });
        }

        chunk.initialized = true;
    }
}