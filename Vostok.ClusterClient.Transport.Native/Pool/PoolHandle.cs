using System;

namespace Vostok.Clusterclient.Transport.Native.Pool
{
    internal struct PoolHandle<T> : IDisposable
        where T : class
    {
        private readonly IPool<T> pool;
        private readonly T resource;

        public PoolHandle(IPool<T> pool, T resource)
        {
            this.pool = pool;
            this.resource = resource;
        }

        public void Dispose() => pool.Return(resource);
    }
}