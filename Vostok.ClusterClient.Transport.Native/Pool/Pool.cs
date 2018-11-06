using System;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Transport.Native.Pool
{
    internal class Pool<T> : UnboundedObjectPool<T>, IPool<T>
        where T : class
    {
        public Pool(Func<T> resourceFactory)
            : base(resourceFactory)
        {
        }
    }
}