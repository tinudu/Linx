namespace Linx.Expressions.Serialization.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Enumerable;

    /// <summary>
    /// MinDFA for UTF-16 characters.
    /// </summary>
    public sealed class RegX
    {
        /// <summary>
        /// The error state (the empty set of strings).
        /// </summary>
        public static RegX Error { get; }

        /// <summary>
        /// Recognizes just an empty string.
        /// </summary>
        public static RegX EmptyString { get; }

        private static readonly IEqualityComparer<HashSet<RegX>> _setComparer = HashSet<RegX>.CreateSetComparer();
        private static readonly IEqualityComparer<KeyValuePair<RegX, HashSet<RegX>>> _concatKeyComparer = KeyValuePair.GetComparer<RegX, HashSet<RegX>>(null, _setComparer);

        static RegX()
        {
            var transitions = new Transition<RegX>[1];
            Error = new RegX(false, transitions);
            EmptyString = new RegX(true, transitions);
            transitions[0] = new Transition<RegX>(char.MinValue, Error);
        }

        /// <summary>
        /// Create a regex recognizing the specified string.
        /// </summary>
        public static RegX Parse(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            // TODO: parse regex syntax
            var result = EmptyString;
            for (var i = s.Length - 1; i >= 0; i--)
            {
                var ch = s[i];
                Transition<RegX>[] transitions;
                switch (ch)
                {
                    case char.MinValue:
                        transitions = new[] { new Transition<RegX>(char.MinValue, result), new Transition<RegX>((char)(char.MinValue + 1), Error) };
                        break;
                    case char.MaxValue:
                        transitions = new[] { new Transition<RegX>(char.MinValue, Error), new Transition<RegX>(char.MaxValue, result) };
                        break;
                    default:
                        transitions = new[] { new Transition<RegX>(char.MinValue, Error), new Transition<RegX>(ch, result), new Transition<RegX>((char)(ch + 1), Error) };
                        break;
                }
                result = new RegX(false, transitions);
            }
            return result;
        }

        /// <summary>
        /// Gets whether a string is recognized at this point.
        /// </summary>
        public bool IsFinal { get; }

        /// <summary>
        /// Gets the transitions.
        /// </summary>
        /// <remarks>The first entry is always on <see cref="char.MinValue"/>. Characters are in ascending order.</remarks>
        public IReadOnlyList<Transition<RegX>> Transitions { get; }

        private RegX(bool isFinal, IReadOnlyList<Transition<RegX>> transitions)
        {
            IsFinal = isFinal;
            Transitions = transitions;
        }

        /// <summary>
        /// Transition to the next state on the specified character.
        /// </summary>
        public RegX this[char ch]
        {
            get
            {
                var low = 0;
                var high = Transitions.Count;
                var searchBinary = high >> 3;
                while (searchBinary != 0)
                {
                    var mid = (low + high) >> 1;
                    if (Transitions[mid].Range <= ch) low = mid + 1;
                    else high = mid;
                    searchBinary >>= 1;
                }
                while (low < high && Transitions[low].Range <= ch) low++;
                return Transitions[low - 1].Successor;
            }
        }

        public RegX Optional() => IsFinal ? this : Minimize(new RegX(true, Transitions));

        public RegX KleeneStar()
        {
            var opt = IsFinal ? this : new RegX(true, Transitions);
            var d = new Dictionary<HashSet<RegX>, RegX>(_setComparer);

            RegX Build(IEnumerable<RegX> starters)
            {
                var isFinal = false;
                var key = new HashSet<RegX>();
                foreach (var r in starters)
                {
                    isFinal |= r.IsFinal;
                    key.Add(r);
                }
                if (IsFinal) key.Add(opt);
                if (d.TryGetValue(key, out var result)) return result;
                var transitions = new List<Transition<RegX>>();
                result = new RegX(isFinal, transitions);
                d.Add(key, result);
                transitions.AddRange(key.Select(r => r.Transitions).Merge(Build));
                return result;
            }

            return Minimize(Build(new[] { opt }));
        }

        public static RegX Union(RegX x, RegX y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            return Minimize(Union(new[] { x, y }));
        }

        private static RegX Union(IEnumerable<RegX> args)
        {
            var d = new Dictionary<HashSet<RegX>, RegX>(_setComparer);

            RegX Build(IEnumerable<RegX> starters)
            {
                var isFinal = false;
                var set = new HashSet<RegX>();
                foreach (var r in starters)
                {
                    isFinal |= r.IsFinal;
                    set.Add(r);
                }

                if (d.TryGetValue(set, out var result)) return result;
                var transitions = new List<Transition<RegX>>();
                result = new RegX(isFinal, transitions);
                d.Add(set, result);
                transitions.AddRange(set.Select(r => r.Transitions).Merge(Build));
                return result;
            }

            return Minimize(Build(args));
        }

        public RegX Concat(RegX x, RegX y)
        {
            var d = new Dictionary<KeyValuePair<RegX, HashSet<RegX>>, RegX>(_concatKeyComparer);

            RegX Build(RegX x1, HashSet<RegX> starters)
            {
                if (x1.IsFinal && !starters.Contains(y)) starters = new HashSet<RegX>(starters) { y };
                var key = new KeyValuePair<RegX, HashSet<RegX>>(x1, starters);
                if (d.TryGetValue(key, out var result)) return result;
                var transitions = new List<Transition<RegX>>();
                result = new RegX(x1.IsFinal && starters.Any(r => r.IsFinal), transitions);
                d.Add(key, result);
                transitions.AddRange(x1.Transitions.Merge(starters.Select(r => r.Transitions).Merge(rs => new HashSet<RegX>(rs), _setComparer), Build));
                return result;
            }

            return Minimize(Build(x, new HashSet<RegX>()));
        }

        public override string ToString() => GetHashCode().ToString();

        private sealed class EquivalenceComparer
        {
            private readonly Dictionary<TransitionPair, Equivalence> _equivalences = new Dictionary<TransitionPair, Equivalence>();

            public bool AreEquivalent(IReadOnlyList<Transition<RegX>> x, IReadOnlyList<Transition<RegX>> y)
            {
                if (ReferenceEquals(x, y)) return true;
                var eq = GetEquivalence(x, y);
                eq.Conclude();
                return !eq.IsKnownToBeDifferent;
            }

            private Equivalence GetEquivalence(IReadOnlyList<Transition<RegX>> x, IReadOnlyList<Transition<RegX>> y)
            {
                Debug.Assert(!ReferenceEquals(x, y));

                var pair = new TransitionPair(x, y);
                if (_equivalences.TryGetValue(pair, out var eq)) return eq;

                eq = new Equivalence();
                _equivalences.Add(pair, eq);
                using (var ex = new LookAhead<Transition<RegX>>(x))
                using (var ey = new LookAhead<Transition<RegX>>(y))
                {
                    Debug.Assert(ex.HasNext && ex.Next.Range == char.MinValue);
                    var tx = ex.Next; ex.MoveNext();

                    Debug.Assert(ey.HasNext && ey.Next.Range == char.MinValue);
                    var ty = ey.Next; ey.MoveNext();

                    while (true)
                    {
                        if (tx.Successor.IsFinal != ty.Successor.IsFinal)
                            eq.SetDifferent();
                        else if (!ReferenceEquals(tx.Successor, ty.Successor))
                        {
                            var dependsOn = GetEquivalence(tx.Successor.Transitions, ty.Successor.Transitions);
                            if (dependsOn.IsKnownToBeDifferent) eq.SetDifferent();
                            else if (dependsOn != eq && !dependsOn.IsKnown) eq.DependsOn(dependsOn);
                        }
                        if (eq.IsKnownToBeDifferent) return eq;

                        int compare;
                        if (ex.HasNext) compare = ey.HasNext ? ex.Next.Range.CompareTo(ey.Next.Range) : -1;
                        else if (ey.HasNext) compare = +1;
                        else break;
                        if (compare <= 0) { tx = ex.Next; ex.MoveNext(); }
                        if (compare >= 0) { ty = ey.Next; ey.MoveNext(); }
                    }
                }
                return eq;
            }

            private struct TransitionPair : IEquatable<TransitionPair>
            {
                private readonly IReadOnlyList<Transition<RegX>> _x, _y;
                public TransitionPair(IReadOnlyList<Transition<RegX>> x, IReadOnlyList<Transition<RegX>> y) { _x = x; _y = y; }

                public bool Equals(TransitionPair other) => Equals(_x, other._x) && Equals(_y, other._y) || Equals(_x, other._y) && Equals(_y, other._x);
                public override bool Equals(object obj) => obj is TransitionPair pair && Equals(pair);
                public override int GetHashCode() => _x.GetHashCode() ^ _y.GetHashCode();
            }

            private sealed class Equivalence
            {
                private HashSet<Equivalence> _dependants = new HashSet<Equivalence>();
                public bool IsKnown => _dependants == null;
                public bool IsKnownToBeDifferent { get; private set; }

                public void DependsOn(Equivalence other)
                {
                    Debug.Assert(!IsKnownToBeDifferent && _dependants != null && other != this && !other.IsKnownToBeDifferent && other._dependants != null);
                    other._dependants.Add(this);
                }

                public void Conclude()
                {
                    if (_dependants == null) return;
                    var deps = _dependants;
                    _dependants = null;
                    foreach (var dep in deps) dep.Conclude();
                }

                public void SetDifferent()
                {
                    if (IsKnownToBeDifferent) return;
                    IsKnownToBeDifferent = true;
                    var deps = _dependants;
                    _dependants = null;
                    foreach (var dep in deps) dep.SetDifferent();
                }
            }
        }

        private static RegX Minimize(RegX regX)
        {
            var comparer = new EquivalenceComparer();
            var byTransition = new[] { new { Final = EmptyString, NonFinal = Error } }.ToDictionary(x => x.Final.Transitions);
            var distinctTransitions = byTransition.ToList();

            RegX Build(RegX r)
            {
                if (byTransition.TryGetValue(r.Transitions, out var minimized)) return r.IsFinal ? minimized.Final : minimized.NonFinal;

                minimized = distinctTransitions.SingleOrDefault(kv => comparer.AreEquivalent(kv.Key, r.Transitions)).Value;
                if (minimized != null)
                {
                    byTransition.Add(r.Transitions, minimized);
                    return r.IsFinal ? minimized.Final : minimized.NonFinal;
                }

                var transitions = new List<Transition<RegX>>(r.Transitions.Count);
                minimized = new { Final = new RegX(true, transitions), NonFinal = new RegX(false, transitions) };
                byTransition.Add(r.Transitions, minimized);
                distinctTransitions.Add(KeyValuePair.Create(r.Transitions, minimized));
                using (var e = r.Transitions.GetEnumerator())
                {
                    if (!e.MoveNext() || e.Current.Range != char.MinValue) throw new Exception("Does not start with char.MinValue.");
                    var curr = Build(e.Current.Successor);
                    transitions.Add(new Transition<RegX>(char.MinValue, curr));
                    while (e.MoveNext())
                    {
                        var next = Build(e.Current.Successor);
                        if (next == curr) continue;
                        curr = next;
                        transitions.Add(new Transition<RegX>(e.Current.Range, curr));
                    }
                }
                transitions.TrimExcess();
                return r.IsFinal ? minimized.Final : minimized.NonFinal;
            }

            return Build(regX);
        }

    }
}
