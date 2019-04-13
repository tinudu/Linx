namespace Linx.Expressions.Serialization.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class TransitionDictionary
    {
        public static IEnumerable<Transition<TResult>> Merge<T1, T2, TResult>(this IEnumerable<Transition<T1>> transitionDirectory1, IEnumerable<Transition<T2>> transitionDirectory2, Func<T1, T2, TResult> resultSelector, IEqualityComparer<TResult> comparer = null)
        {
            if (comparer == null) comparer = EqualityComparer<TResult>.Default;

            using (var la1 = new LookAhead<T1>(transitionDirectory1))
            using (var la2 = new LookAhead<T2>(transitionDirectory2))
            {
                var curr = resultSelector(la1.Current, la2.Current);
                yield return new Transition<TResult>(char.MinValue, curr);
                while (true)
                {
                    char min;
                    if (la1.Next != null)
                    {
                        if (la2.Next != null)
                        {
                            var next1 = la1.Next.Value;
                            var next2 = la2.Next.Value;
                            var cmp = next1.Range.CompareTo(next2.Range);
                            if (cmp < 0) { min = next1.Range; la1.MoveNext(); }
                            else if (cmp > 0) { min = next2.Range; la2.MoveNext(); }
                            else { min = next1.Range; la1.MoveNext(); la2.MoveNext(); }
                        }
                        else { min = la1.Next.Value.Range; la1.MoveNext(); }
                    }
                    else if (la2.Next != null) { min = la2.Next.Value.Range; la2.MoveNext(); }
                    else break;

                    var next = resultSelector(la1.Current, la2.Current);
                    if (comparer.Equals(next, curr)) continue;
                    curr = next;
                    yield return new Transition<TResult>(min, curr);
                }
            }
        }

        public static IEnumerable<Transition<TResult>> Merge<TSource, TResult>(this IEnumerable<IEnumerable<Transition<TSource>>> transitionDictionaries, Func<IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TResult> comparer = null)
        {
            if (comparer == null) comparer = EqualityComparer<TResult>.Default;

            var lookAheads = new List<LookAhead<TSource>>();
            try
            {
                foreach (var td in transitionDictionaries) lookAheads.Add(new LookAhead<TSource>(td));
                var currents = lookAheads.Select(la => la.Current);
                // ReSharper disable once PossibleMultipleEnumeration
                var curr = resultSelector(currents);
                yield return new Transition<TResult>(char.MinValue, curr);

                while (true)
                {
                    var minN = (char?)null;
                    foreach (var la in lookAheads)
                    {
                        if (la.Next == null || minN != null && la.Next.Value.Range >= minN.Value) continue;
                        minN = la.Next.Value.Range;
                    }
                    if (minN == null) break;
                    var min = minN.Value;
                    foreach (var la in lookAheads)
                    {
                        if (la.Next == null || la.Next.Value.Range >= min) continue;
                        la.MoveNext();
                    }
                    // ReSharper disable once PossibleMultipleEnumeration
                    var next = resultSelector(currents);
                    if (comparer.Equals(next, curr)) continue;
                    curr = next;
                    yield return new Transition<TResult>(min, curr);
                }
            }
            finally
            {
                foreach (var la in lookAheads) la.Dispose();
            }
        }

        private sealed class LookAhead<TState> : IDisposable
        {
            private readonly IEnumerator<Transition<TState>> _enumerator;

            public TState Current { get; private set; }
            public Transition<TState>? Next { get; private set; }

            public LookAhead(IEnumerable<Transition<TState>> transitionDictionary)
            {
                _enumerator = transitionDictionary.GetEnumerator();
                try
                {
                    if (!_enumerator.MoveNext() || _enumerator.Current.Range != char.MinValue) throw new ArgumentException("Does not start with char.MinValue.");
                    Current = _enumerator.Current.Successor;
                    if (!_enumerator.MoveNext()) return;
                    Next = _enumerator.Current;
                    if (Next.Value.Range == char.MinValue) throw new ArgumentException("Not ascending.");
                }
                catch
                {
                    _enumerator.Dispose();
                    throw;
                }
            }

            public void MoveNext()
            {
                if (Next == null) throw new InvalidOperationException();
                var curr = Next.Value;
                Current = curr.Successor;
                if (_enumerator.MoveNext())
                {
                    Next = _enumerator.Current;
                    if (Next.Value.Range <= curr.Range) throw new ArgumentException("Not ascending");
                }
                else
                    Next = null;
            }

            public void Dispose() => _enumerator.Dispose();
        }
    }
}
