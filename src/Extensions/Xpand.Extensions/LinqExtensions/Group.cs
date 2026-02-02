using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IGrouping<TK, TV> AsGroup<TK, TV>(this IEnumerable<TV> source, TK key) 
            => Create(key, source);

        private static IGrouping<TK, TV> Create<TK, TV>(TK key, IEnumerable<TV> source) 
            => new SimpleGroupWrapper<TK, TV>(key, source);

        public static IEnumerable<List<T>> GroupConsecutive<T>(this IEnumerable<T> source, Func<T, T, bool> isNeighbour) {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext()) yield break;
            var currentGroup = new List<T> { enumerator.Current };
            var prev = enumerator.Current;

            while (enumerator.MoveNext()) {
                var curr = enumerator.Current;
                if (isNeighbour(prev, curr)) {
                    currentGroup.Add(curr);
                } else {
                    yield return currentGroup;
                    currentGroup = [curr];
                }
                prev = curr;
            }
            yield return currentGroup;
        }
        
        internal class SimpleGroupWrapper<TK, TV>(TK key, IEnumerable<TV> source) : IGrouping<TK, TV> {
            private readonly IEnumerable<TV> _source = source ?? throw new NullReferenceException("source");

            public TK Key{ get; } = key;

            public IEnumerator<TV> GetEnumerator() => _source.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _source.GetEnumerator();
        }
        
    }
}