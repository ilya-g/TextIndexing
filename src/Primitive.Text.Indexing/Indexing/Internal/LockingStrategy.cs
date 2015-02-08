using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Primitive.Text.Indexing.Internal
{
    internal abstract class LockingStrategy
    {
        public ReadLock InReadLock() { return new ReadLock(this); }
        public WriteLock InWriteLock() { return new WriteLock(this); }

        protected abstract void EnterReadLock();
        protected abstract void ExitReadLock();
        protected abstract void EnterWriteLock();
        protected abstract void ExitWriteLock();

        public sealed class ReadWrite : LockingStrategy
        {
            private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

            protected override void EnterReadLock()
            {
                @lock.EnterReadLock();
            }

            protected override void ExitReadLock()
            {
                @lock.ExitReadLock();
            }

            protected override void EnterWriteLock()
            {
                @lock.EnterWriteLock();
            }

            protected override void ExitWriteLock()
            {
                @lock.ExitWriteLock();
            }
        }

        public sealed class Exclusive : LockingStrategy
        {
            private readonly object @lock = new object();

            protected override void EnterReadLock()
            {
                Monitor.Enter(@lock);
            }

            protected override void ExitReadLock()
            {
                Monitor.Exit(@lock);
            }

            protected override void EnterWriteLock()
            {
                Monitor.Enter(@lock);
            }

            protected override void ExitWriteLock()
            {
                Monitor.Exit(@lock);
            }
        }

        public sealed class PrioritizedReadWrite : LockingStrategy
        {
            private readonly ReaderWriterPriorityLock @lock = new ReaderWriterPriorityLock();
            protected override void EnterReadLock()
            {
                @lock.Enter(isRead: true);
            }

            protected override void ExitReadLock()
            {
                @lock.Exit(isRead: true);
            }

            protected override void EnterWriteLock()
            {
                @lock.Enter(isRead: false);
            }

            protected override void ExitWriteLock()
            {
                @lock.Exit(isRead: false);
            }
        }

        public sealed class SnapshotLocking : LockingStrategy
        {
            protected override void EnterReadLock() {}
            protected override void ExitReadLock() {}
            protected override void EnterWriteLock() { throw new NotSupportedException("This locking strategy don't support write operations"); }
            protected override void ExitWriteLock() { throw new NotSupportedException("This locking strategy don't support write operations"); }
        }

        public struct ReadLock : IDisposable
        {
            private readonly LockingStrategy locking;
            public ReadLock(LockingStrategy locking) 
            {
                this.locking = locking;
                locking.EnterReadLock();
            }

            public void Dispose()
            {
                locking.ExitReadLock();
            }
        }

        public struct WriteLock : IDisposable
        {
            private readonly LockingStrategy locking;
            public WriteLock(LockingStrategy locking) 
            {
                this.locking = locking;
                locking.EnterWriteLock();
            }

            public void Dispose()
            {
                locking.ExitWriteLock();
            }
        }

        /// <summary>
        ///  Sharing-read exlusive-write lock, that gives priority to reading locks
        /// </summary>
        private class ReaderWriterPriorityLock
        {
            private readonly ManualResetEventSlim readEvent = new ManualResetEventSlim(true);
            private int readQueueCount = 0;
            private readonly Queue<ManualResetEventSlim> writeQueue = new Queue<ManualResetEventSlim>();

            private static bool IsSetOrNull(ManualResetEventSlim @event) { return @event == null || @event.IsSet; }

            public void Enter(bool isRead)
            {
                ManualResetEventSlim thisRequest;

                lock (this)
                {
                    bool writeIsActive = writeQueue.Count > 0 && IsSetOrNull(writeQueue.Peek());
                    bool wait = writeIsActive || (!isRead && (readQueueCount > 0 || writeQueue.Count > 0));
                    if (isRead)
                    {
                        readQueueCount += 1;
                        if (!wait) return;
                        (thisRequest = readEvent).Reset();
                    }
                    else
                    {
                        if (!wait)
                        {
                            writeQueue.Enqueue(null);
                            return;
                        }
                        writeQueue.Enqueue(thisRequest = new ManualResetEventSlim(false));
                    }
                }
                thisRequest.Wait();
            }

            public void Exit(bool isRead)
            {
                lock (this)
                {
                    if (isRead)
                    {
                        Debug.Assert(readEvent.IsSet, "readEvent.IsSet");
                        readQueueCount -= 1;
                    }
                    else
                    {
                        var writeEvent = writeQueue.Dequeue();
                        Debug.Assert(IsSetOrNull(writeEvent), "writeEvent.IsSet");
                        if (writeEvent != null) 
                            writeEvent.Dispose();
                    }

                    if (readQueueCount > 0)
                    {
                        readEvent.Set();
                    }
                    else if (writeQueue.Count > 0)
                    {

                        var writeEvent = writeQueue.Peek();
                        Debug.Assert(writeEvent != null);
                        writeEvent.Set();
                    }
                }
            }
        }

    }
}