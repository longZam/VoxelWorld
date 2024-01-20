using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace VoxelWorld.Core.WorldLoaders;


public class WorldLoader
{
    private readonly DirectoryInfo directory, configDirectory;
    private readonly IWorldGeneratorFactory worldGeneratorFactory;
    private readonly ConcurrentDictionary<string, World> activatedWorlds;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> requestSemaphores;
    private readonly ConcurrentDictionary<string, WorldConfig> configurations;
    private readonly ConcurrentDictionary<string, bool> worldsActivationStatus;


    public WorldLoader(DirectoryInfo directory, IWorldGeneratorFactory worldGeneratorFactory, IHostApplicationLifetime lifetime)
    {
        this.directory = directory;
        this.configDirectory = directory.CreateSubdirectory("world-configurations");
        this.worldGeneratorFactory = worldGeneratorFactory;
        this.activatedWorlds = new();
        this.requestSemaphores = new();
        this.configurations = new();
        this.worldsActivationStatus = new();
        
        var configs = configDirectory.GetFiles("*.json");

        lifetime.ApplicationStopping.Register(OnStopping);
        lifetime.ApplicationStopped.Register(OnStopped);

        Parallel.ForEach(configs, config =>
        {
            string worldName = Path.GetFileNameWithoutExtension(config.Name);
            string text = File.ReadAllText(config.FullName);
            configurations[worldName] = JsonConvert.DeserializeObject<WorldConfig>(text);
            worldsActivationStatus[worldName] = true;
            LoadWorldAsync(worldName).Wait();
        });
    }

    private void OnStopping()
    {
        // todo: 저장 작업
    }

    private void OnStopped()
    {

    }

    public async Task<World> LoadWorldAsync(string worldName, CancellationToken cancellationToken = default)
    {
        if (!configurations.ContainsKey(worldName))
            throw new ArgumentException("The corresponding world is not registered and cannot be loaded.", nameof(worldName));
        if (activatedWorlds.ContainsKey(worldName))
            throw new ArgumentException("This world is already activated.", nameof(worldName));

        var semaphore = requestSemaphores.GetOrAdd(worldName, _ => new(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            DirectoryInfo worldDirectory = directory.CreateSubdirectory(worldName);
            WorldConfig worldConfig = configurations[worldName];
            WorldGenerator generator = worldGeneratorFactory.Create(worldConfig);
            Region.Factory factory = new(generator);
            FileSystemRegionLoader regionLoader = new(worldDirectory, factory);
            FileSystemChunkLoader chunkLoader = new(regionLoader);
            activatedWorlds[worldName] = await Task.Run(() => new World(chunkLoader));
            worldsActivationStatus[worldName] = true;
            return GetWorld(worldName);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task UnloadWorldAsync(string worldName, CancellationToken cancellationToken = default)
    {
        if (!activatedWorlds.ContainsKey(worldName))
            throw new ArgumentException("The name corresponds to a world that is either not activated or does not exist.", nameof(worldName));
        
        var semaphore = requestSemaphores.GetOrAdd(worldName, _ => new(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // todo: 적당한 exception 생각해내기
            if (!activatedWorlds.Remove(worldName, out var world))
                throw new Exception();
            
            worldsActivationStatus[worldName] = false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public World GetWorld(string worldName)
    {
        return activatedWorlds[worldName];
    }

    public IReadOnlyDictionary<string, bool> GetWorldsActivationStatus()
    {
        return worldsActivationStatus;
    }
}