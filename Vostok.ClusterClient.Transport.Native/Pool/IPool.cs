namespace Vostok.Clusterclient.Transport.Native.Pool
{
    internal interface IPool<T>
        where T : class
    {
        /// <summary>
        ///     Acquires a resource from pool, allocating a new one if necessary.
        /// </summary>
        T Acquire();

        /// <summary>
        ///     Releases a previously acquired resource back to pool.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Attempt to release a resource that wasn't acquired earlier.</exception>
        void Return(T resource);
    }
}