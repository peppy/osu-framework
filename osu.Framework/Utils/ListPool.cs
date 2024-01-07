// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace osu.Framework.Utils
{
    public class ListPool<T> : DefaultObjectPool<List<T>>
    {
        public ListPool()
            : base(new ListPoolPolicy(), 50)
        {
        }

        public class ListPoolPolicy : IPooledObjectPolicy<List<T>>
        {
            public List<T> Create() => new List<T>();

            public bool Return(List<T> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
