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
    /// marble-diagram :== time-interval* [ completion ]
    /// time-interval :== empty-frame* element-frame
    /// emtpy-frame :== '-'
    /// element-frame :== element | simultanous-elements
    /// element :== [0-9A-Za-z]
    /// simultanous-elements :== '[' element+ ']
    /// completion :== successful-completion | error-completion | forever
    /// successful-completion :== empty-frame* '|'
    /// error-completion :== empty-frame* '#'
    /// forever-completion :== '*' time-interval+
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

            using (var input = diagram.Where(ch => ch != ' ').GetLookAhead())
            {
                var context = new ParseContext { Input = input, FrameSize = settings.FrameSize };
                var prefix = ParseTimeIntervals(context, selector)
                    .Select(ti => new TimeInterval<Notification<T>>(ti.Interval, new Notification<T>(ti.Value)))
                    .ToList();
                IEnumerable<TimeInterval<Notification<T>>> notifications;
                if (!input.HasNext)
                    notifications = prefix;
                else
                {
                    switch (input.Next)
                    {
                        case '|':
                            prefix.Add(new TimeInterval<Notification<T>>(context.Interval + settings.FrameSize, new Notification<T>()));
                            notifications = prefix;
                            break;

                        case '#':
                            prefix.Add(new TimeInterval<Notification<T>>(context.Interval + settings.FrameSize, new Notification<T>(settings.Exception ?? MarbleException.Singleton)));
                            notifications = prefix;
                            break;

                        case '*':
                            if (context.Interval != TimeSpan.Zero)
                                throw new Exception("Empty frames before forever-completion.");
                            input.MoveNext();
                            var suffix = ParseTimeIntervals(context, selector)
                                .Select(ti => new TimeInterval<Notification<T>>(ti.Interval, new Notification<T>(ti.Value)))
                                .ToList();
                            if (suffix.Count == 0)
                                throw new Exception("Empty forever-completion.");
                            notifications = prefix.Concat(Forever(suffix));
                            break;

                        default:
                            throw new Exception("Invalid character.");
                    }
                }

                return notifications;
            }
        }

        private static IEnumerable<TimeInterval<T>> ParseTimeIntervals<T>(ParseContext ctx, Func<char, int, T> selector)
        {
            while (ctx.Input.HasNext)
            {
                switch (ctx.Input.Next)
                {
                    case '-':
                        ctx.Interval += ctx.FrameSize;
                        ctx.Input.MoveNext();
                        continue;
                    case '[':
                        ctx.Input.MoveNext();
                        if (!ctx.Input.HasNext || _elementChars.Contains(ctx.Input.Next))
                            throw new Exception("Empty [].");
                        yield return new TimeInterval<T>(ctx.Interval + ctx.FrameSize, selector(ctx.Input.Next, ctx.Index++));
                        ctx.Input.MoveNext();
                        while (true)
                        {
                            if (!ctx.Input.HasNext)
                                throw new Exception("Unclosed [].");

                            if (ctx.Input.Next == ']')
                            {
                                ctx.Input.MoveNext();
                                break;
                            }

                            if (_elementChars.Contains(ctx.Input.Next))
                            {
                                yield return new TimeInterval<T>(TimeSpan.Zero, selector(ctx.Input.Next, ctx.Index++));
                                ctx.Input.MoveNext();
                            }
                            else
                                throw new Exception("Invalid character.");
                        }
                        ctx.Interval = TimeSpan.Zero;
                        continue;

                    default:
                        if (_elementChars.Contains(ctx.Input.Next))
                        {
                            yield return new TimeInterval<T>(ctx.Interval + ctx.FrameSize, selector(ctx.Input.Next, ctx.Index++));
                            ctx.Input.MoveNext();
                            ctx.Interval = TimeSpan.Zero;
                            continue;
                        }
                        break;
                }
                break;
            }
        }

        private sealed class ParseContext
        {
            public TimeSpan FrameSize;
            public LookAhead<char> Input;
            public TimeSpan Interval;
            public int Index;
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
