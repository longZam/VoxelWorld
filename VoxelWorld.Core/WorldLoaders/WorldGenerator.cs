namespace VoxelWorld.Core.WorldLoaders;


public class WorldGenerator
{
    private readonly IEnumerable<IWorldModifier> worldModifiers;
    private readonly PerlinNoise3D noise;


    public WorldGenerator(WorldConfig config, IEnumerable<IWorldModifier> worldModifiers)
    {
        this.worldModifiers = worldModifiers;
        this.noise = new PerlinNoise3D(config.seed);
    }

    public void Generate(in Vector2Int chunkPosition, Chunk targetChunk)
    {
        Vector3Int worldPositionOffset = World.ChunkToWorld(in chunkPosition);

        foreach (var worldModifier in worldModifiers)
        {
            targetChunk.ParallelWrite((in Vector3Int localPosition, ushort previousBlock) => 
                worldModifier.Modify(worldPositionOffset + localPosition, previousBlock, noise));
        }
    }
}