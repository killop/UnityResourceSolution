using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    [Serializable]
    internal class PreviewCacheNode
    {
        [SerializeField] private string _guid;
        [SerializeField] private Texture2D _preview;
        [SerializeField] private double _priority;
        [SerializeField] private int _queueIndex;

        public string Guid
        {
            get { return _guid; }
            set { _guid = value; }
        }

        public Texture2D Preview
        {
            get { return _preview; }
            set { _preview = value; }
        }

        /// <summary>
        /// The Priority to insert this node at.
        /// Cannot be manually edited - see queue.Enqueue() and queue.UpdatePriority() instead
        /// </summary>
        public double Priority 
        {
            get { return _priority; }
            protected internal set { _priority = value; }
        }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex
        {
            get { return _queueIndex; }
            internal set { _queueIndex = value; }
        }

        public PreviewCacheNode(string guid, Texture2D preview)
        {
            _guid = guid;
            _preview = preview;
        }

#if DEBUG
        /// <summary>
        /// The queue this node is tied to. Used only for debug builds.
        /// </summary>
        public object Queue { get; internal set; }
#endif
    }

    /// <summary>
    /// An implementation of a min-Priority Queue using a heap.  Has O(1) .Contains()!
    /// See https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started for more information
    /// </summary>
    internal sealed class PreviewCachePriorityQueue : IEnumerable<PreviewCacheNode>
    {
        private int _numNodes;
        private PreviewCacheNode[] _nodes;

        /// <summary>
        /// Instantiate a new Priority Queue
        /// </summary>
        /// <param name="maxNodes">The max nodes ever allowed to be enqueued (going over this will cause undefined behavior)</param>
        public PreviewCachePriorityQueue(int maxNodes)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("New queue size cannot be smaller than 1");
            }
#endif

            _numNodes = 0;
            _nodes = new PreviewCacheNode[maxNodes + 1];
        }

        /// <summary>
        /// Returns the number of nodes in the queue.
        /// O(1)
        /// </summary>
        public int Count
        {
            get
            {
                return _numNodes;
            }
        }

        /// <summary>
        /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
        /// attempting to enqueue another item will cause undefined behavior.  O(1)
        /// </summary>
        public int MaxSize
        {
            get
            {
                return _nodes.Length - 1;
            }
        }

        /// <summary>
        /// Removes every node from the queue.
        /// O(n) (So, don't do this often!)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Clear()
        {
            Array.Clear(_nodes, 1, _numNodes);
            _numNodes = 0;
        }

        /// <summary>
        /// Returns (in O(1)!) whether the given node is in the queue.
        /// If node is or has been previously added to another queue, the result is undefined unless oldQueue.ResetNode(node) has been called
        /// O(1)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool Contains(PreviewCacheNode node)
        {
#if DEBUG
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.Queue != null && !Equals(node.Queue))
            {
                throw new InvalidOperationException("node.Contains was called on a node from another queue.  Please call originalQueue.ResetNode() first");
            }
            if (node.QueueIndex < 0 || node.QueueIndex >= _nodes.Length)
            {
                throw new InvalidOperationException("node.QueueIndex has been corrupted. Did you change it manually? Or add this node to another queue?");
            }
#endif

            return _nodes[node.QueueIndex] == node;
        }

        /// <summary>
        /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken arbitrarily.
        /// If the queue is full, the result is undefined.
        /// If the node is already enqueued, the result is undefined.
        /// If node is or has been previously added to another queue, the result is undefined unless oldQueue.ResetNode(node) has been called
        /// O(log n)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Enqueue(PreviewCacheNode node, double priority)
        {
#if DEBUG
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (_numNodes >= _nodes.Length - 1)
            {
                throw new InvalidOperationException("Queue is full - node cannot be added: " + node);
            }
            if (node.Queue != null && !Equals(node.Queue))
            {
                throw new InvalidOperationException("node.Enqueue was called on a node from another queue.  Please call originalQueue.ResetNode() first");
            }
            if (Contains(node))
            {
                throw new InvalidOperationException("Node is already enqueued: " + node);
            }
            node.Queue = this;
#endif

            node.Priority = priority;
            _numNodes++;
            _nodes[_numNodes] = node;
            node.QueueIndex = _numNodes;
            CascadeUp(node);
        }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void CascadeUp(PreviewCacheNode node)
        {
            //aka Heapify-up
            int parent;
            if (node.QueueIndex > 1)
            {
                parent = node.QueueIndex >> 1;
                PreviewCacheNode parentNode = _nodes[parent];
                if (HasHigherOrEqualPriority(parentNode, node))
                    return;

                //Node has lower priority value, so move parent down the heap to make room
                _nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }
            else
            {
                return;
            }

            while (parent > 1)
            {
                parent >>= 1;
                PreviewCacheNode parentNode = _nodes[parent];
                if (HasHigherOrEqualPriority(parentNode, node))
                    break;

                //Node has lower priority value, so move parent down the heap to make room
                _nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }

            _nodes[node.QueueIndex] = node;
        }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void CascadeDown(PreviewCacheNode node)
        {
            //aka Heapify-down
            int finalQueueIndex = node.QueueIndex;
            int childLeftIndex = 2 * finalQueueIndex;

            // If leaf node, we're done
            if (childLeftIndex > _numNodes)
            {
                return;
            }

            // Check if the left-child is higher-priority than the current node
            int childRightIndex = childLeftIndex + 1;
            PreviewCacheNode childLeft = _nodes[childLeftIndex];
            if (HasHigherPriority(childLeft, node))
            {
                // Check if there is a right child. If not, swap and finish.
                if (childRightIndex > _numNodes)
                {
                    node.QueueIndex = childLeftIndex;
                    childLeft.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childLeft;
                    _nodes[childLeftIndex] = node;
                    return;
                }
                // Check if the left-child is higher-priority than the right-child
                PreviewCacheNode childRight = _nodes[childRightIndex];
                if (HasHigherPriority(childLeft, childRight))
                {
                    // left is highest, move it up and continue
                    childLeft.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childLeft;
                    finalQueueIndex = childLeftIndex;
                }
                else
                {
                    // right is even higher, move it up and continue
                    childRight.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
            }
            // Not swapping with left-child, does right-child exist?
            else if (childRightIndex > _numNodes)
            {
                return;
            }
            else
            {
                // Check if the right-child is higher-priority than the current node
                PreviewCacheNode childRight = _nodes[childRightIndex];
                if (HasHigherPriority(childRight, node))
                {
                    childRight.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
                // Neither child is higher-priority than current, so finish and stop.
                else
                {
                    return;
                }
            }

            while (true)
            {
                childLeftIndex = 2 * finalQueueIndex;

                // If leaf node, we're done
                if (childLeftIndex > _numNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }

                // Check if the left-child is higher-priority than the current node
                childRightIndex = childLeftIndex + 1;
                childLeft = _nodes[childLeftIndex];
                if (HasHigherPriority(childLeft, node))
                {
                    // Check if there is a right child. If not, swap and finish.
                    if (childRightIndex > _numNodes)
                    {
                        node.QueueIndex = childLeftIndex;
                        childLeft.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childLeft;
                        _nodes[childLeftIndex] = node;
                        break;
                    }
                    // Check if the left-child is higher-priority than the right-child
                    PreviewCacheNode childRight = _nodes[childRightIndex];
                    if (HasHigherPriority(childLeft, childRight))
                    {
                        // left is highest, move it up and continue
                        childLeft.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childLeft;
                        finalQueueIndex = childLeftIndex;
                    }
                    else
                    {
                        // right is even higher, move it up and continue
                        childRight.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                }
                // Not swapping with left-child, does right-child exist?
                else if (childRightIndex > _numNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }
                else
                {
                    // Check if the right-child is higher-priority than the current node
                    PreviewCacheNode childRight = _nodes[childRightIndex];
                    if (HasHigherPriority(childRight, node))
                    {
                        childRight.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                    // Neither child is higher-priority than current, so finish and stop.
                    else
                    {
                        node.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = node;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool HasHigherPriority(PreviewCacheNode higher, PreviewCacheNode lower)
        {
            return (higher.Priority < lower.Priority);
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherOrEqualPriority(node, node) (ie. both arguments the same node) will return true
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool HasHigherOrEqualPriority(PreviewCacheNode higher, PreviewCacheNode lower)
        {
            return (higher.Priority <= lower.Priority);
        }

        /// <summary>
        /// Removes the head of the queue and returns it.
        /// If queue is empty, result is undefined
        /// O(log n)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public PreviewCacheNode Dequeue()
        {
#if DEBUG
            if (_numNodes <= 0)
            {
                throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
            }

            if (!IsValidQueue())
            {
                //throw new InvalidOperationException("Queue has been corrupted (Did you update a node priority manually instead of calling UpdatePriority()?" +
                //                                    "Or add the same node to two different queues?)");
            }
#endif

            PreviewCacheNode returnMe = _nodes[1];
            //If the node is already the last node, we can remove it immediately
            if (_numNodes == 1)
            {
                _nodes[1] = null;
                _numNodes = 0;
                return returnMe;
            }

            //Swap the node with the last node
            PreviewCacheNode formerLastNode = _nodes[_numNodes];
            _nodes[1] = formerLastNode;
            formerLastNode.QueueIndex = 1;
            _nodes[_numNodes] = null;
            _numNodes--;

            //Now bubble formerLastNode (which is no longer the last node) down
            CascadeDown(formerLastNode);
            return returnMe;
        }

        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// O(n)
        /// </summary>
        public void Resize(int maxNodes)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("Queue size cannot be smaller than 1");
            }

            if (maxNodes < _numNodes)
            {
                throw new InvalidOperationException("Called Resize(" + maxNodes + "), but current queue contains " + _numNodes + " nodes");
            }
#endif

            PreviewCacheNode[] newArray = new PreviewCacheNode[maxNodes + 1];
            int highestIndexToCopy = Math.Min(maxNodes, _numNodes);
            Array.Copy(_nodes, newArray, highestIndexToCopy + 1);
            _nodes = newArray;
        }

        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// If the queue is empty, behavior is undefined.
        /// O(1)
        /// </summary>
        public PreviewCacheNode First
        {
            get
            {
#if DEBUG
                if (_numNodes <= 0)
                {
                    throw new InvalidOperationException("Cannot call .First on an empty queue");
                }
#endif

                return _nodes[1];
            }
        }

        /// <summary>
        /// This method must be called on a node every time its priority changes while it is in the queue.  
        /// <b>Forgetting to call this method will result in a corrupted queue!</b>
        /// Calling this method on a node not in the queue results in undefined behavior
        /// O(log n)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void UpdatePriority(PreviewCacheNode node, double priority)
        {
#if DEBUG
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.Queue != null && !Equals(node.Queue))
            {
                throw new InvalidOperationException("node.UpdatePriority was called on a node from another queue");
            }
            if (!Contains(node))
            {
                throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + node);
            }
#endif

            node.Priority = priority;
            OnNodeUpdated(node);
        }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void OnNodeUpdated(PreviewCacheNode node)
        {
            //Bubble the updated node up or down as appropriate
            int parentIndex = node.QueueIndex >> 1;

            if (parentIndex > 0 && HasHigherPriority(node, _nodes[parentIndex]))
            {
                CascadeUp(node);
            }
            else
            {
                //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
                CascadeDown(node);
            }
        }

        /// <summary>
        /// Removes a node from the queue.  The node does not need to be the head of the queue.  
        /// If the node is not in the queue, the result is undefined.  If unsure, check Contains() first
        /// O(log n)
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Remove(PreviewCacheNode node)
        {
#if DEBUG
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.Queue != null && !Equals(node.Queue))
            {
                throw new InvalidOperationException("node.Remove was called on a node from another queue");
            }
            if (!Contains(node))
            {
                throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + node);
            }
#endif

            //If the node is already the last node, we can remove it immediately
            if (node.QueueIndex == _numNodes)
            {
                _nodes[_numNodes] = null;
                _numNodes--;
                return;
            }

            //Swap the node with the last node
            PreviewCacheNode formerLastNode = _nodes[_numNodes];
            _nodes[node.QueueIndex] = formerLastNode;
            formerLastNode.QueueIndex = node.QueueIndex;
            _nodes[_numNodes] = null;
            _numNodes--;

            //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
            OnNodeUpdated(formerLastNode);
        }

        /// <summary>
        /// By default, nodes that have been previously added to one queue cannot be added to another queue.
        /// If you need to do this, please call originalQueue.ResetNode(node) before attempting to add it in the new queue
        /// If the node is currently in the queue or belongs to another queue, the result is undefined
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void ResetNode(PreviewCacheNode node)
        {
#if DEBUG
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (node.Queue != null && !Equals(node.Queue))
            {
                throw new InvalidOperationException("node.ResetNode was called on a node from another queue");
            }
            if (Contains(node))
            {
                throw new InvalidOperationException("node.ResetNode was called on a node that is still in the queue");
            }

            node.Queue = null;
#endif

            node.QueueIndex = 0;
        }

        public IEnumerator<PreviewCacheNode> GetEnumerator()
        {
#if NET_VERSION_4_5 // ArraySegment does not implement IEnumerable before 4.5
            IEnumerable<T> e = new ArraySegment<T>(_nodes, 1, _numNodes);
            return e.GetEnumerator();
#else
            for (int i = 1; i <= _numNodes; i++)
                yield return _nodes[i];
#endif
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// <b>Should not be called in production code.</b>
        /// Checks to make sure the queue is still in a valid state.  Used for testing/debugging the queue.
        /// </summary>
        public bool IsValidQueue()
        {
            for (int i = 1; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    int childLeftIndex = 2 * i;
                    if (childLeftIndex < _nodes.Length && _nodes[childLeftIndex] != null && HasHigherPriority(_nodes[childLeftIndex], _nodes[i]))
                        return false;

                    int childRightIndex = childLeftIndex + 1;
                    if (childRightIndex < _nodes.Length && _nodes[childRightIndex] != null && HasHigherPriority(_nodes[childRightIndex], _nodes[i]))
                        return false;
                }
            }
            return true;
        }
    }
}