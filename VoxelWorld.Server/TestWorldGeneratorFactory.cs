using VoxelWorld.Core;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Server.Test;


public class TestWorldGeneratorFactory : IWorldGeneratorFactory
{
    public WorldGenerator Create(WorldConfig worldConfig)
    {
        IEnumerable<IWorldModifier> modifiers = worldConfig.worldType switch
        {
            "empty" => Array.Empty<IWorldModifier>(),
            "earth" => new IWorldModifier[]
            {
                new TestFuncModifier((in Vector3Int worldPosition, ushort previousBlock, PerlinNoise3D noise) =>
                {
                    // todo: 주어진 매개변수를 기반으로 적절히 배치 할 블록을 반환
                    return previousBlock;
                }),
            },
            _ => throw new NotImplementedException()
        };

        return new(worldConfig, modifiers);
    }
}

public class TestFuncModifier : IWorldModifier
{
    public delegate ushort TestWorldModifierFunc(in Vector3Int worldPosition, ushort previousBlock, PerlinNoise3D noise);
    private readonly TestWorldModifierFunc func;


    public TestFuncModifier(TestWorldModifierFunc func)
    {
        this.func = func;
    }

    public ushort Modify(in Vector3Int worldPosition, ushort previousBlock, PerlinNoise3D noise)
    {
        return func(in worldPosition, previousBlock, noise);
    }
}