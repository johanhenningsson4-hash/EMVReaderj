/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Module :         BufferPool.cs
'   
'  Description:     Memory pool for efficient buffer management
'==========================================================================================*/

using System;
using System.Collections.Concurrent;

namespace EMVCard
{
    /// <summary>
    /// Memory pool for efficient buffer reuse and reduced allocations
    /// </summary>
    public static class BufferPool
    {
        private static readonly ConcurrentQueue<byte[]> SmallBuffers = new ConcurrentQueue<byte[]>();
        private static readonly ConcurrentQueue<byte[]> LargeBuffers = new ConcurrentQueue<byte[]>();
        
        private const int SmallBufferSize = 512;
        private const int LargeBufferSize = 4096;
        private const int MaxPooledBuffers = 10;

        /// <summary>
        /// Rent a buffer of the specified minimum size
        /// </summary>
        public static byte[] Rent(int minimumSize)
        {
            if (minimumSize <= SmallBufferSize)
            {
                if (SmallBuffers.TryDequeue(out byte[] buffer))
                {
                    return buffer;
                }
                return new byte[SmallBufferSize];
            }
            else if (minimumSize <= LargeBufferSize)
            {
                if (LargeBuffers.TryDequeue(out byte[] buffer))
                {
                    return buffer;
                }
                return new byte[LargeBufferSize];
            }
            else
            {
                // For very large buffers, don't pool
                return new byte[minimumSize];
            }
        }

        /// <summary>
        /// Return a buffer to the pool
        /// </summary>
        public static void Return(byte[] buffer)
        {
            if (buffer == null) return;

            // Clear the buffer for security
            Array.Clear(buffer, 0, buffer.Length);

            if (buffer.Length == SmallBufferSize && SmallBuffers.Count < MaxPooledBuffers)
            {
                SmallBuffers.Enqueue(buffer);
            }
            else if (buffer.Length == LargeBufferSize && LargeBuffers.Count < MaxPooledBuffers)
            {
                LargeBuffers.Enqueue(buffer);
            }
            // Large buffers are not pooled, let GC handle them
        }

        /// <summary>
        /// Get current pool statistics
        /// </summary>
        public static (int SmallCount, int LargeCount) GetPoolStats()
        {
            return (SmallBuffers.Count, LargeBuffers.Count);
        }
    }
}