namespace Linx.AsyncEnumerable.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enumerable;

    /// <summary>
    /// Marble diagram parser.
    /// </summary>
    /// <remarks>
    /// A parsed marble diagram represents a sequence of <see cref="TimeInterval{T}"/> of <see cref="Notification{T}"/> of <see cref="char"/>.
    /// 
    /// Syntax (spaces are ignored):
    /// <code>
    /// marble-diagram :== next* [ completed | error | forever ]
    /// next :== time-frame* [0-9A-Za-z]
    /// completed :== time-frame* '|'
    /// error :== time-frame* '#'
    /// time-frame :== '-'
    /// forever :== '*' interval-next+
    /// </code>
    /// </remarks>
    public static class MarbleParser
    {
        private static readonly ISet<char> _elementChars = new HashSet<char>("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

        /// <summary>
        /// Parse a marble diagram.
        /// </summary>
        /// <param name="diagram">Marble diagram syntax.</param>
        /// <param name="settings">Optional. Marble parser settings.</param>
        public static IEnumerable<TimeInterval<Notification<char>>> Parse(
            IEnumerable<char> diagram,
            MarbleParserSettings settings = null)
            => Parse(diagram, (ch, i) => ch, settings);

        /// <summary>
        /// Parse a marble diagram.
        /// </summary>
        /// <param name="diagram">Marble diagram syntax.</param>
        /// <param name="elements">Positional element replacements.</param>
        /// <param name="settings">Optional. Marble parser settings.</param>
        public static IEnumerable<TimeInterval<Notification<T>>> Parse<T>(
            IEnumerable<char> diagram,
            IEnumerable<T> elements,
            MarbleParserSettings settings = null)
        {
            var eleList = elements as IReadOnlyList<T> ?? (elements != null ? elements.ToList() : throw new ArgumentNullException(nameof(elements)));
            return Parse(diagram, (ch, i) => eleList[i], settings);
        }

        /// <summary>
        /// Parse a marble diagram.
        /// </summary>
        /// <param name="diagram">Marble diagram syntax.</param>
        /// <param name="settings">Optional. Marble parser settings.</param>
        /// <param name="elements">Positional element replacements.</param>
        public static IEnumerable<TimeInterval<Notification<T>>> Parse<T>(
            IEnumerable<char> diagram,
            MarbleParserSettings settings,
            params T[] elements)
        {
            var eleList = elements as IReadOnlyList<T> ?? (elements != null ? elements.ToList() : throw new ArgumentNullException(nameof(elements)));
            return Parse(diagram, (ch, i) => eleList[i], settings);
        }

        /// <summary>
        /// Parse a marble diagram.
        /// </summary>
        /// <param name="diagram">Marble diagram syntax.</param>
        /// <param name="selector">Function to convert elements to <typeparamref name="T"/>.</param>
        /// <param name="settings">Optional. Marble parser settings.</param>
        public static IEnumerable<TimeInterval<Notification<T>>> Parse<T>(
            IEnumerable<char> diagram,
            Func<char, int, T> selector,
            MarbleParserSettings settings = null)
        {
            if (diagram == null) throw new ArgumentNullException(nameof(diagram));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (settings == null) settings = new MarbleParserSettings();

            using (var context = new ParseContext<T>(diagram, selector, settings))
                try
                {
                    var prefix = ParseTimeIntervals(context).ToList();
                    IEnumerable<TimeInterval<Notification<T>>> notifications;
                    if (!context.IsCompleted && context.HasNext && context.Next == '*') // forever
                    {
                        context.MoveNext();
                        var suffix = ParseTimeIntervals(context).ToList();
                        if (suffix.Count == 0)
                            throw new MarbleParseException("Empty forever.", context.Position);
                        if (context.IsCompleted)
                            throw new MarbleParseException("Forever may not be completed.", context.Position);
                        notifications = prefix.Concat(Forever(suffix));
                    }
                    else
                        notifications = prefix;

                    if (context.HasNext)
                        throw new MarbleParseException("Unexpected character.", context.Position);
                    return notifications;
                }
                catch (MarbleParseException) { throw; }
                catch (Exception ex) { throw new MarbleParseException(ex.Message, context.Position); }
        }

        private static IEnumerable<TimeInterval<Notification<T>>> ParseTimeIntervals<T>(ParseContext<T> ctx)
        {
            var interval = TimeSpan.Zero;
            while (ctx.HasNext)
                switch (ctx.Next)
                {
                    case '-':
                        interval += ctx.FrameSize;
                        ctx.MoveNext();
                        continue;
                    case '|':
                        yield return new TimeInterval<Notification<T>>(interval, Notification.Completed<T>());
                        ctx.MoveNext();
                        ctx.IsCompleted = true;
                        yield break;
                    case '#':
                        yield return new TimeInterval<Notification<T>>(interval, Notification.Error<T>(ctx.Error));
                        ctx.MoveNext();
                        ctx.IsCompleted = true;
                        yield break;
                    default:
                        if (!_elementChars.Contains(ctx.Next))
                            yield break;
                        var value = ctx.Selector(ctx.Next, ctx.Index++);
                        yield return new TimeInterval<Notification<T>>(interval, Notification.Next(value));
                        ctx.MoveNext();
                        interval = TimeSpan.Zero;
                        break;
                }
            if (interval != TimeSpan.Zero)
                throw new MarbleParseException("No notification for interval.", ctx.Position);
        }

        private sealed class ParseContext<T> : IDisposable
        {
            private readonly LookAhead<char> _input;

            public readonly Func<char, int, T> Selector;
            public readonly TimeSpan FrameSize;
            public readonly Exception Error;
            public int Position { get; private set; }
            public int Index;
            public bool IsCompleted;

            public ParseContext(IEnumerable<char> diagram, Func<char, int, T> selector, MarbleParserSettings settingsOpt)
            {
                _input = diagram.Where(ch => ch != ' ').GetLookAhead();
                Selector = selector;
                FrameSize = settingsOpt?.FrameSize ?? MarbleParserSettings.DefaultFrameSize;
                Error = settingsOpt?.Error ?? MarbleException.Singleton;
            }

            public bool HasNext => _input.HasNext;
            public char Next => _input.Next;
            public void MoveNext()
            {
                _input.MoveNext();
                Position++;
            }

            public void Dispose() => _input.Dispose();
        }

        private static IEnumerable<T> Forever<T>(IReadOnlyCollection<T> items)
        {
            while (true)
                foreach (var item in items)
                    yield return item;
            // ReSharper disable once IteratorNeverReturns
        }
    }
}
