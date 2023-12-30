using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace VoxelWorld.Core;


public class Octree<T>
    where T : notnull
{
    public interface INode { }

    public sealed class LeafNode : INode
    {
        public readonly Vector3Int position;
        public readonly T item;


        public LeafNode(Vector3Int position, T item)
        {
            this.position = position;
            this.item = item;
        }
    }

    public sealed class InternalNode : INode
    {
        public readonly INode?[] children;
        public readonly Bounds bounds;

        public InternalNode(Bounds bounds)
        {
            this.children = ArrayPool<INode?>.Shared.Rent(8);
            this.bounds = bounds;
        }

        ~InternalNode()
        {
            ArrayPool<INode?>.Shared.Return(children, true);
        }
    }


    private readonly InternalNode root;
    private readonly ReaderWriterLockSlim rwlock;


    public Octree(Bounds bounds)
    {
        this.root = new InternalNode(bounds);
        this.rwlock = new ReaderWriterLockSlim();
    }


    public void Insert(Vector3Int position, T item)
    {
        rwlock.EnterWriteLock();

        try
        {
            InternalNode current = root;

            while (true)
            {
                int offset = GetOffsetFromPositionalRelationship(current.bounds.Center, position);
                INode? child = current.children[offset];

                if (child is LeafNode childLeafNode)
                {
                    if (childLeafNode.position == position)
                        throw new ArgumentException($"Item with same key has already been added");

                    Vector3Int newChildSize = (current.bounds.max - current.bounds.min) / 2;
                    Vector3Int newChildMin = GetChildMinFromOffset(offset, current.bounds.min, newChildSize);
                    Bounds bounds = new Bounds(newChildMin, newChildMin + newChildSize);
                    InternalNode newChildInternalNode = new InternalNode(bounds);
                    int childLeafOffset = GetOffsetFromPositionalRelationship(newChildInternalNode.bounds.Center, childLeafNode.position);
                    newChildInternalNode.children[childLeafOffset] = childLeafNode;
                    current.children[offset] = newChildInternalNode;
                    current = newChildInternalNode;
                }
                else if (child is InternalNode childInternalNode)
                {
                    current = childInternalNode;
                }
                else
                {
                    current.children[offset] = new LeafNode(position, item);
                    break;
                }
            }

        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    public bool Remove(Vector3Int position)
    {
        rwlock.EnterWriteLock();

        try
        {
            return Remove(root, position);
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    private bool Remove(InternalNode parent, Vector3Int position)
    {
        int offset = GetOffsetFromPositionalRelationship(parent.bounds.Center, position);

        INode? child = parent.children[offset];

        if (child is InternalNode internalNode)
        {
            if (Remove(internalNode, position))
            {
                for (int i = 0; i < 8; i++)
                    if (internalNode.children[i] != null)
                        return true;

                // 자식이 하나도 없는 내부 노드를 제거
                parent.children[offset] = null;
                return true;
            }

            return false;
        }

        if (child is LeafNode leafNode)
        {
            parent.children[offset] = null;
            return true;
        }

        return false;
    }

    public void Preorder(Action<Vector3Int, T> action)
    {
        rwlock.EnterReadLock();
        
        try
        {
            Preorder(root, action);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private void Preorder(INode? node, Action<Vector3Int, T> action)
    {
        if (node == null)
            return;

        if (node is InternalNode internalNode)
        {
            for (int i = 0; i < 8; i++)
                Preorder(internalNode.children[i], action);

            return;
        }

        if (node is LeafNode leafNode)
        {
            action.Invoke(leafNode.position, leafNode.item);
            return;
        }
    }

    public bool TrySearch(Vector3Int position, [NotNullWhen(true)] out T? result)
    {
        rwlock.EnterReadLock();

        try
        {
            InternalNode current = root;

            while (true)
            {
                int offset = GetOffsetFromPositionalRelationship(current.bounds.Center, position);
                var child = current.children[offset];

                if (child is InternalNode childInternalNode)
                {
                    current = childInternalNode;
                    continue;
                }
                else if (child is LeafNode childLeafNode)
                {
                    if (childLeafNode.position == position)
                    {
                        result = childLeafNode.item;
                        return true;
                    }
                }

                result = default;
                return false;
            }

        }
        finally
        {
            rwlock.ExitReadLock();
        }

    }
    
    public void OverlapCubeAll(Bounds bounds, List<LeafNode> results)
    {
        rwlock.EnterReadLock();

        try
        {
            results.Clear();
            OverlapCubeAll(root, bounds, results);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static void OverlapCubeAll(InternalNode current, Bounds bounds, List<LeafNode> results)
    {
        // 해당 internalNode가 쿼리 범위와 겹치지 않으면 자식 또한 겹치지 않으므로 제외 가능함
        if (!Bounds.Overlaps(bounds, current.bounds))
            return;

        for (int i = 0; i < 8; i++)
        {
            var child = current.children[i];
            
            if (child == null)
                continue;
            // 자식 internalNode에 재귀적으로 수행
            else if (child is InternalNode childInternalNode)
                OverlapCubeAll(childInternalNode, bounds, results);
            else if (child is LeafNode childLeafNode)
                if (Bounds.Overlaps(bounds, new(childLeafNode.position, childLeafNode.position)))
                    results.Add(childLeafNode);
        }
    }

    public void OverlapCubesAllComplement(IReadOnlyList<Bounds> bounds, List<LeafNode> results)
    {
        rwlock.EnterReadLock();

        try
        {
            results.Clear();
            OverlapCubesAllComplement(root, bounds, results);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static void OverlapCubesAllComplement(InternalNode current, IReadOnlyList<Bounds> bounds, List<LeafNode> results)
    {
        static bool HasCollidedOnce(Bounds target, IReadOnlyList<Bounds> bounds)
        {
            for (int i = 0; i < bounds.Count; i++)
                if (Bounds.Overlaps(target, bounds[i]))
                    return true;
            
            return false;
        }

        // 한 번이라도 닿은 대상은 무시
        if (HasCollidedOnce(current.bounds, bounds))
            return;

        for (int i = 0; i < 8; i++)
        {
            var child = current.children[i];
            
            if (child == null)
                continue;
            // 자식 internalNode에 재귀적으로 수행
            else if (child is InternalNode childInternalNode)
                OverlapCubesAllComplement(childInternalNode, bounds, results);
            // 한 번도 안 닿았으면 추가
            else if (child is LeafNode childLeafNode)
                if (!HasCollidedOnce(new(childLeafNode.position, childLeafNode.position), bounds))
                    results.Add(childLeafNode);
        }
    }

    private static Vector3Int GetChildMinFromOffset(int offset, Vector3Int parentMin, Vector3Int childSize)
    {
        Vector3Int childMin = parentMin;

        if ((offset & 1) != 0)
            childMin.x += childSize.x;
        if ((offset & 2) != 0)
            childMin.y += childSize.y;
        if ((offset & 4) != 0)
            childMin.z += childSize.z;

        return childMin;
    }

    private static int GetOffsetFromPositionalRelationship(Vector3Int start, Vector3Int end)
    {
        int offset = 0;

        if (start.x <= end.x)
            offset += 1;
        if (start.y <= end.y)
            offset += 2;
        if (start.z <= end.z)
            offset += 4;

        return offset;
    }
}