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
}