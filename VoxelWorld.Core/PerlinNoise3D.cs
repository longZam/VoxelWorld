namespace VoxelWorld.Core;


public class PerlinNoise3D
{
	private readonly int seed;
    private readonly FastNoiseLite fastNoiseLite;


    public PerlinNoise3D(int seed)
    {
		this.seed = seed;
        this.fastNoiseLite = new FastNoiseLite(seed);
		this.fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
    }

    public float Noise3D(float x, float y, float z, float frequency, float amplitude, float persistence, int octave)
	{
		float noise = 0.0f;

		for (int i = 0; i < octave; ++i)
		{
			// Get all permutations of noise for each individual axis
			float noiseXY = fastNoiseLite.GetNoise(x * frequency + seed, y * frequency + seed) * amplitude;
			float noiseXZ = fastNoiseLite.GetNoise(x * frequency + seed, z * frequency + seed) * amplitude;
			float noiseYZ = fastNoiseLite.GetNoise(y * frequency + seed, z * frequency + seed) * amplitude;

			// Reverse of the permutations of noise for each individual axis
			float noiseYX = fastNoiseLite.GetNoise(y * frequency + seed, x * frequency + seed) * amplitude;
			float noiseZX = fastNoiseLite.GetNoise(z * frequency + seed, x * frequency + seed) * amplitude;
			float noiseZY = fastNoiseLite.GetNoise(z * frequency + seed, y * frequency + seed) * amplitude;

			// Use the average of the noise functions
			noise += (noiseXY + noiseXZ + noiseYZ + noiseYX + noiseZX + noiseZY) / 6.0f;

			amplitude *= persistence;
			frequency *= 2.0f;
		}

		// Use the average of all octaves
		return noise / octave;
	}
}