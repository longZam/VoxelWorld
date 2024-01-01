using System.Buffers;
using System.Diagnostics;
using VoxelWorld.Core.Proto;

namespace VoxelWorld.Core;


public class Chunk
{
    public const int CORNER_BIT = 4;
    public const int CORNER = CORNER_BIT * CORNER_BIT;
    public const int SIDE = CORNER * CORNER;
    public const int VOLUME = CORNER * CORNER * CORNER;

    private readonly ushort[] rawData;
    private readonly ReaderWriterLockSlim rwlock;


    /// <summary>
    /// <para>
    /// 해당 LocalPosition의 블럭을 읽고 쓸 수 있습니다.
    /// </para>
    /// <para>
    /// 스레드로부터 안전하기 위해 매 접근마다 lock을 사용합니다.
    /// 병렬로 동시에 읽거나 쓰려면 ParallelRead, ParallelWrite 함수의 사용을 고려하십시오.
    /// </para>
    /// </summary>
    public ushort this[in Vector3Int localPosition]
    {
        get
        {
            rwlock.EnterReadLock();

            try
            {
                return rawData[LocalPositionToIndex(localPosition)];
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }
        set
        {
            rwlock.ExitWriteLock();

            try
            {
                rawData[LocalPositionToIndex(localPosition)] = value;
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
    }


    public Chunk(bool clear = false)
    {
        this.rawData = ArrayPool<ushort>.Shared.Rent(CORNER * CORNER * CORNER);
        this.rwlock = new();

        if (clear)
            Array.Clear(this.rawData, 0, VOLUME);
    }

    ~Chunk()
    {
        ArrayPool<ushort>.Shared.Return(this.rawData);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        rwlock.EnterReadLock();

        try
        {
            for (int i = 0; i < VOLUME; i++)
                    binaryWriter.Write(rawData[i]);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        rwlock.EnterWriteLock();

        try
        {
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = binaryReader.ReadUInt16();
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    public void Serialize(LoadChunkResponse response)
    {
        rwlock.EnterReadLock();

        try
        {
            for (int i = 0; i < VOLUME; i++)
                response.RawData.Add(rawData[i]);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    public void Deserialize(LoadChunkResponse response)
    {
        rwlock.EnterWriteLock();

        try
        {
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = (ushort)response.RawData[i];
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    public void CopyRawData(Span<ushort> destination)
    {
        if (destination.Length != VOLUME)
            throw new ArgumentException($"Buffer size must be exactly {VOLUME}.", nameof(destination));
        
        rwlock.EnterReadLock();

        try
        {
            rawData.CopyTo(destination);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    public delegate void ParallelReadAction(in Vector3Int position, in ushort block);
    public delegate void ParallelWriteAction(in Vector3Int position, ref ushort block);

    public void ParallelRead(ParallelReadAction action)
    {
        rwlock.EnterReadLock();

        try
        {
            Parallel.For(0, VOLUME, i =>
            {
                action(IndexToLocalPosition(i), in rawData[i]);
            });
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    public void ParallelWrite(ParallelWriteAction action)
    {
        rwlock.EnterWriteLock();

        try
        {
            Parallel.For(0, VOLUME, i =>
            {
                action(IndexToLocalPosition(i), ref rawData[i]);
            });
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    private static int LocalPositionToIndex(in Vector3Int localPosition)
    {
        Debug.Assert(0 <= localPosition.x && localPosition.x < CORNER, $"x must be 0 <= x < {CORNER}, x = {localPosition.x}");
        Debug.Assert(0 <= localPosition.y && localPosition.y < CORNER, $"y must be 0 <= y < {CORNER}, y = {localPosition.y}");
        Debug.Assert(0 <= localPosition.z && localPosition.z < CORNER, $"z must be 0 <= z < {CORNER}, z = {localPosition.z}");

        return localPosition.x + CORNER * localPosition.y + SIDE * localPosition.z;
    }

    private static Vector3Int IndexToLocalPosition(in int index)
    {
        Debug.Assert(0 <= index && index < VOLUME, $"index must be 0 <= index < {VOLUME}, index = {index}");

        return new(
            index % CORNER,
            index / CORNER % CORNER,
            index / SIDE % CORNER
        );
    }
}