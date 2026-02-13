/*=========================================================================================
'  EMVCard.Tests - BufferPool Unit Tests
'  
'  Description: Tests for memory buffer pool functionality
'==========================================================================================*/

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EMVCard;

namespace EMVCard.Tests.UnitTests
{
    [TestClass]
    public class BufferPoolTests
    {
        [TestMethod]
        public void BufferPool_RentSmallBuffer_ReturnsCorrectSize()
        {
            // Act
            var buffer = BufferPool.Rent(256);

            // Assert
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length >= 256);
            Assert.IsTrue(buffer.Length <= 512); // Should be small buffer size

            // Cleanup
            BufferPool.Return(buffer);
        }

        [TestMethod]
        public void BufferPool_RentLargeBuffer_ReturnsCorrectSize()
        {
            // Act
            var buffer = BufferPool.Rent(2048);

            // Assert
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length >= 2048);
            Assert.IsTrue(buffer.Length <= 4096); // Should be large buffer size

            // Cleanup
            BufferPool.Return(buffer);
        }

        [TestMethod]
        public void BufferPool_RentExtraLargeBuffer_ReturnsExactSize()
        {
            // Arrange
            int requestedSize = 8192; // Larger than pooled sizes

            // Act
            var buffer = BufferPool.Rent(requestedSize);

            // Assert
            Assert.IsNotNull(buffer);
            Assert.AreEqual(requestedSize, buffer.Length);

            // Note: Extra large buffers are not pooled, so we don't need to return
        }

        [TestMethod]
        public void BufferPool_ReturnAndRent_ReusesBuffer()
        {
            // Arrange
            var buffer1 = BufferPool.Rent(256);
            buffer1[0] = 42; // Mark the buffer

            // Act
            BufferPool.Return(buffer1);
            var buffer2 = BufferPool.Rent(256);

            // Assert
            Assert.AreSame(buffer1, buffer2); // Should be the same buffer object
            Assert.AreEqual(0, buffer2[0]); // Should be cleared
        }

        [TestMethod]
        public void BufferPool_Return_ClearsBuffer()
        {
            // Arrange
            var buffer = BufferPool.Rent(512);
            
            // Fill buffer with test data
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(i % 256);
            }

            // Act
            BufferPool.Return(buffer);
            var reusedBuffer = BufferPool.Rent(512);

            // Assert
            Assert.AreSame(buffer, reusedBuffer);
            Assert.IsTrue(reusedBuffer.All(b => b == 0)); // All bytes should be zero
        }

        [TestMethod]
        public void BufferPool_ReturnNull_DoesNotThrow()
        {
            // Act & Assert
            AssertDoesNotThrow(() => BufferPool.Return(null));
        }

        [TestMethod]
        public void BufferPool_GetPoolStats_ReturnsCorrectCounts()
        {
            // Arrange
            var initialStats = BufferPool.GetPoolStats();
            
            var smallBuffer = BufferPool.Rent(256);
            var largeBuffer = BufferPool.Rent(2048);

            BufferPool.Return(smallBuffer);
            BufferPool.Return(largeBuffer);

            // Act
            var stats = BufferPool.GetPoolStats();

            // Assert
            Assert.AreEqual(initialStats.SmallCount + 1, stats.SmallCount);
            Assert.AreEqual(initialStats.LargeCount + 1, stats.LargeCount);
        }

        [TestMethod]
        public void BufferPool_ExceedPoolLimit_DoesNotPool()
        {
            // Arrange - Rent and return more than the pool limit (10)
            var buffers = new byte[15][];
            
            for (int i = 0; i < 15; i++)
            {
                buffers[i] = BufferPool.Rent(256);
            }

            var initialStats = BufferPool.GetPoolStats();

            // Act - Return all buffers
            for (int i = 0; i < 15; i++)
            {
                BufferPool.Return(buffers[i]);
            }

            var finalStats = BufferPool.GetPoolStats();

            // Assert - Should not exceed the pool limit
            Assert.IsTrue(finalStats.SmallCount <= initialStats.SmallCount + 10);
        }

        [TestMethod]
        public void BufferPool_MultipleRentReturn_HandlesCorrectly()
        {
            // Arrange
            var buffers = new byte[5][];

            // Act - Rent multiple buffers
            for (int i = 0; i < 5; i++)
            {
                buffers[i] = BufferPool.Rent(512);
                Assert.IsNotNull(buffers[i]);
                buffers[i][0] = (byte)(i + 1); // Mark each buffer
            }

            // Return all buffers
            for (int i = 0; i < 5; i++)
            {
                BufferPool.Return(buffers[i]);
            }

            // Rent again and verify reuse
            var reusedBuffers = new byte[5][];
            for (int i = 0; i < 5; i++)
            {
                reusedBuffers[i] = BufferPool.Rent(512);
                Assert.AreEqual(0, reusedBuffers[i][0]); // Should be cleared
            }

            // Assert - At least some buffers should be reused
            int reuseCount = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (ReferenceEquals(buffers[i], reusedBuffers[j]))
                    {
                        reuseCount++;
                        break;
                    }
                }
            }

            Assert.IsTrue(reuseCount > 0, "At least some buffers should be reused");

            // Cleanup
            for (int i = 0; i < 5; i++)
            {
                BufferPool.Return(reusedBuffers[i]);
            }
        }

        [TestMethod]
        public void BufferPool_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int operationsPerThread = 100;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            var tasks = new System.Threading.Tasks.Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            var buffer = BufferPool.Rent(256);
                            buffer[0] = (byte)i; // Write to buffer
                            System.Threading.Thread.Sleep(1); // Small delay
                            BufferPool.Return(buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            Assert.AreEqual(0, exceptions.Count, 
                $"Thread safety test failed with {exceptions.Count} exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
        }

        [TestMethod]
        public void BufferPool_WrongSizeReturn_HandlesGracefully()
        {
            // Arrange - Create a buffer that doesn't match pool sizes
            var customBuffer = new byte[1000]; // Not 512 or 4096

            // Act & Assert - Should not throw
            AssertDoesNotThrow(() => BufferPool.Return(customBuffer));
            
            // Buffer should not be pooled (stats should not change)
            var statsBefore = BufferPool.GetPoolStats();
            BufferPool.Return(customBuffer);
            var statsAfter = BufferPool.GetPoolStats();
            
            Assert.AreEqual(statsBefore.SmallCount, statsAfter.SmallCount);
            Assert.AreEqual(statsBefore.LargeCount, statsAfter.LargeCount);
        }

        /// <summary>
        /// Helper method that doesn't throw exceptions (for .NET Framework 4.7.2 compatibility)
        /// </summary>
        private void AssertDoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}