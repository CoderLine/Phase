using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Phase.Translator.Utils
{
    public sealed class MetadataFileReferenceResolver : MetadataReferenceResolver, IEquatable<MetadataFileReferenceResolver>
    {
        private readonly string _baseDir;
        private readonly MetadataReferenceCache _cache;

        public MetadataFileReferenceResolver(string baseDir)
        {
            _baseDir = baseDir;
            _cache = new MetadataReferenceCache((path, properties) =>
                MetadataReference.CreateFromFile(path, properties));
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            var path = Path.Combine(baseFilePath ?? _baseDir, reference);
            return ImmutableArray.Create((PortableExecutableReference)_cache.GetReference(path, properties));
        }

        public bool Equals(MetadataFileReferenceResolver other)
        {
            return other != null && _baseDir.Equals(other._baseDir);
        }

        public override int GetHashCode()
        {
            return _baseDir.GetHashCode();
        }

        public override bool Equals(object other) => Equals(other as MetadataFileReferenceResolver);


        private class MetadataReferenceCache
        {
            private ImmutableDictionary<string, ReferenceSet> _referenceSets = ImmutableDictionary<string, ReferenceSet>.Empty;

            private readonly Func<string, MetadataReferenceProperties, MetadataReference> _createReference;

            public MetadataReferenceCache(Func<string, MetadataReferenceProperties, MetadataReference> createReference)
            {
                _createReference = createReference;
            }

            public MetadataReference GetReference(string path, MetadataReferenceProperties properties)
            {
                if (!_referenceSets.TryGetValue(path, out var referenceSet))
                {
                    referenceSet = ImmutableInterlocked.GetOrAdd(ref _referenceSets, path, new ReferenceSet(this));
                }

                return referenceSet.GetAddOrUpdate(path, properties);
            }

            private class ReferenceSet
            {
                private readonly MetadataReferenceCache _cache;

                private readonly NonReentrantLock _gate = new NonReentrantLock();

                // metadata references are held weakly, so even though this is a cache that enables reuse, it does not control lifetime.
                private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _references
                    = new Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>>();

                public ReferenceSet(MetadataReferenceCache cache)
                {
                    _cache = cache;
                }

                public MetadataReference GetAddOrUpdate(string path, MetadataReferenceProperties properties)
                {
                    using (_gate.DisposableWait())
                    {
                        WeakReference<MetadataReference> weakref;
                        MetadataReference mref = null;

                        if (!(_references.TryGetValue(properties, out weakref) && weakref.TryGetTarget(out mref)))
                        {
                            // try to base this metadata reference off of an existing one, so we don't load the metadata bytes twice.
                            foreach (var wr in _references.Values)
                            {
                                if (wr.TryGetTarget(out mref))
                                {
                                    mref = mref.WithProperties(properties);
                                    break;
                                }
                            }

                            if (mref == null)
                            {
                                mref = _cache._createReference(path, properties);
                            }

                            _references[properties] = new WeakReference<MetadataReference>(mref);
                        }

                        return mref;
                    }
                }
            }
        }

        private class NonReentrantLock
        {
            private readonly object _syncLock;
            private volatile int _owningThreadId;
            public NonReentrantLock(bool useThisInstanceForSynchronization = false)
            {
                _syncLock = useThisInstanceForSynchronization ? this : new object();
            }

            public void Wait(CancellationToken cancellationToken = default(CancellationToken))
            {
                if (this.IsOwnedByMe)
                {
                    throw new LockRecursionException();
                }

                CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Fast path to try and avoid allocations in callback registration.
                    lock (_syncLock)
                    {
                        if (!this.IsLocked)
                        {
                            this.TakeOwnership();
                            return;
                        }
                    }

                    cancellationTokenRegistration = cancellationToken.Register(CancellationTokenCanceledEventHandler, _syncLock, useSynchronizationContext: false);
                }

                using (cancellationTokenRegistration)
                {
                    // PERF: First spin wait for the lock to become available, but only up to the first planned yield.
                    // This additional amount of spinwaiting was inherited from SemaphoreSlim's implementation where
                    // it showed measurable perf gains in test scenarios.
                    SpinWait spin = new SpinWait();
                    while (this.IsLocked && !spin.NextSpinWillYield)
                    {
                        spin.SpinOnce();
                    }

                    lock (_syncLock)
                    {
                        while (this.IsLocked)
                        {
                            // If cancelled, we throw. Trying to wait could lead to deadlock.
                            cancellationToken.ThrowIfCancellationRequested();

                            {
                                // Another thread holds the lock. Wait until we get awoken either
                                // by some code calling "Release" or by cancellation.
                                Monitor.Wait(_syncLock);
                            }
                        }

                        // We now hold the lock
                        this.TakeOwnership();
                    }
                }
            }

            /// <summary>
            /// Exit the mutual exclusion.
            /// </summary>
            /// <remarks>
            /// The calling thread must currently hold the lock.
            /// </remarks>
            /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
            public void Release()
            {
                lock (_syncLock)
                {
                    this.ReleaseOwnership();

                    // Release one waiter
                    Monitor.Pulse(_syncLock);
                }
            }

            private bool IsLocked => _owningThreadId != 0;
            private bool IsOwnedByMe => _owningThreadId == Environment.CurrentManagedThreadId;
            private void TakeOwnership()
            {
                _owningThreadId = Environment.CurrentManagedThreadId;
            }
            private void ReleaseOwnership()
            {
                _owningThreadId = 0;
            }

            private static void CancellationTokenCanceledEventHandler(object obj)
            {
                lock (obj)
                {
                    // Release all waiters to check their cancellation tokens.
                    Monitor.PulseAll(obj);
                }
            }

            public SemaphoreDisposer DisposableWait(CancellationToken cancellationToken = default(CancellationToken))
            {
                this.Wait(cancellationToken);
                return new SemaphoreDisposer(this);
            }

            public struct SemaphoreDisposer : IDisposable
            {
                private readonly NonReentrantLock _semaphore;

                public SemaphoreDisposer(NonReentrantLock semaphore)
                {
                    _semaphore = semaphore;
                }

                public void Dispose()
                {
                    _semaphore.Release();
                }
            }
        }
    }
}