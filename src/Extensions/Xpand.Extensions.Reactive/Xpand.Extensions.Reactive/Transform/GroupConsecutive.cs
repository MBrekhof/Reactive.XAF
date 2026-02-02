using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IList<T>> GroupConsecutive<T>(this IObservable<T> source, Func<T, T, bool> isNeighbour) 
            => Observable.Create<IList<T>>(observer => {
                var buffer = new List<T>();
                return source.Subscribe(
                    onNext: item => {
                        if (buffer.Count > 0 && !isNeighbour(buffer.Last(), item)) {
                            observer.OnNext(new List<T>(buffer));
                            buffer.Clear();
                        }

                        buffer.Add(item);
                    },
                    onError: observer.OnError,
                    onCompleted: () => {
                        if (buffer.Count > 0) observer.OnNext(buffer);
                        observer.OnCompleted();
                    });
            });
    }
}