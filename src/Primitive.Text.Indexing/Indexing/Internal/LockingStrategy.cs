using System;
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
            private readonly ReaderWriterLockSlim lockObject = new ReaderWriterLockSlim();

            protected override void EnterReadLock()
            {
                lockObject.EnterReadLock();
            }

            protected override void ExitReadLock()
            {
                lockObject.ExitReadLock();
            }

            protected override void EnterWriteLock()
            {
                lockObject.EnterWriteLock();
            }

            protected override void ExitWriteLock()
            {
                lockObject.ExitWriteLock();
            }
        }

        public sealed class Exclusive : LockingStrategy
        {
            private readonly object lockObject = new object();

            protected override void EnterReadLock()
            {
                Monitor.Enter(lockObject);
            }

            protected override void ExitReadLock()
            {
                Monitor.Exit(lockObject);
            }

            protected override void EnterWriteLock()
            {
                Monitor.Enter(lockObject);
            }

            protected override void ExitWriteLock()
            {
                Monitor.Exit(lockObject);
            }
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
    }
}