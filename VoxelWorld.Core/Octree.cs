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
        public readonly Vector3Int min, max, center;

        public InternalNode(Vector3Int min, Vector3Int max)
        {
            children = ArrayPool<INode?>.Shared.Rent(8);
            this.min = min;
            this.max = max;
            this.center = (max + min + Vector3Int.One) / 2;
        }

        ~InternalNode()
        {
            ArrayPool<INode?>.Shared.Return(children, true);
        }
    }


    private readonly InternalNode root;
    private readonly ReaderWriterLockSlim rwlock;


    public Octree(Vector3Int min, Vector3Int max)
    {
        this.root = new InternalNode(min, max);
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
                int offset = GetOffsetFromPositionalRelationship(current.center, position);
                INode? child = current.children[offset];

                if (child is LeafNode childLeafNode)
                {
                    if (childLeafNode.position == position)
                        throw new ArgumentException($"Item with same key has already been added");

                    Vector3Int newChildSize = (current.max - current.min + Vector3Int.One) / 2; // 여기서 문제?
                    Vector3Int newChildMin = GetChildMinFromOffset(offset, current.min, newChildSize);

                    InternalNode newChildInternalNode = new InternalNode(newChildMin, newChildMin + newChildSize);
                    int childLeafOffset = GetOffsetFromPositionalRelationship(newChildInternalNode.center, childLeafNode.position);
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
        int offset = GetOffsetFromPositionalRelationship(parent.center, position);

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
                int offset = GetOffsetFromPositionalRelationship(current.center, position);
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
    
    public void OverlapCubeAll(Vector3Int min, Vector3Int max, List<LeafNode> results)
    {
        results.Clear();

        rwlock.EnterReadLock();

        try
        {
            OverlapCubeAll(root, min, max, results);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static void OverlapCubeAll(InternalNode current, Vector3Int min, Vector3Int max, List<LeafNode> results)
    {
        // 해당 internalNode가 쿼리 범위와 겹치지 않으면 자식 또한 겹치지 않으므로 제외 가능함
        if (!CollisionCheck(min, max, current.min, current.max))
            return;

        for (int i = 0; i < 8; i++)
        {
            var child = current.children[i];
            
            if (child == null)
                continue;
            // 자식 internalNode에 재귀적으로 수행
            else if (child is InternalNode childInternalNode)
                OverlapCubeAll(childInternalNode, min, max, results);
            else if (child is LeafNode childLeafNode)
                if (CollisionCheck(min, max, childLeafNode.position, childLeafNode.position))
                    results.Add(childLeafNode);
        }
    }

    public static bool CollisionCheck(Vector3Int aMin, Vector3Int aMax, Vector3Int bMin, Vector3Int bMax)
    {
        // X 축에 대한 충돌 여부 확인
        if (aMax.x < bMin.x || aMin.x > bMax.x)
            return false;

        // Y 축에 대한 충돌 여부 확인
        if (aMax.y < bMin.y || aMin.y > bMax.y)
            return false;

        // Z 축에 대한 충돌 여부 확인
        if (aMax.z < bMin.z || aMin.z > bMax.z)
            return false;

        return true;
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