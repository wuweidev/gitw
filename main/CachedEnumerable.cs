using System;
using System.Collections;
using System.Collections.Generic;

namespace gitw
{
    public class CachedEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private IEnumerator<T> enumerator;
        private readonly List<T> cache = new List<T>();

        public CachedEnumerable(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator())
        {
        }

        public CachedEnumerable(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int index = 0;

            for (; index < this.cache.Count; index++)
            {
                yield return this.cache[index];
            }

            while (true)
            {
                T current;

                lock (this.cache)
                {
                    if (index < this.cache.Count)
                    {
                        current = this.cache[index];
                    }
                    else if (this.enumerator != null && this.enumerator.MoveNext())
                    {
                        current = this.enumerator.Current;
                        this.cache.Add(current);
                    }
                    else
                    {
                        break;
                    }
                }

                yield return current;
                index++;
            }

            lock (this.cache)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (this.enumerator != null)
            {
                this.enumerator.Dispose();
                this.enumerator = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
