using System;
using System.Collections.Generic;
using System.Linq;
using Linx.AsyncEnumerable;
using Linx.Enumerable;
using Linx.Notifications;

namespace Linx.Testing
{
    partial class VirtualTime
    {
        private static readonly ISet<char> _elementChars = new HashSet<char>("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

        /// <summary>
        /// Parse with a time frame length of <see cref="LinxTesting.DefaultTimeFrame"/>.
        /// </summary>
        public IAsyncEnumerable<char> Parse(string pattern) => Parse(pattern, LinxTesting.DefaultTimeFrame);

        /// <summary>
        /// Creates a sequence from a pattern.
        /// </summary>
        /// <param name="pattern">See remarks.</param>
        /// <param name="timeFrame">The length of a time frame.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="ParseException"><paramref name="pattern"/> has invalid syntax.</exception>
        /// <remarks>
        /// Pattern syntax:
        /// <code>
        /// pattern :== next* [ completed | error | forever ]?
        /// next :== '-'* [0-9A-Za-z]
        /// completed :== '-'* '|'
        /// error :== '-'* '#'
        /// forever :== '*' next+
        /// </code>
        /// '-' represents a <see cref="TimeInterval"/> of length <paramref name="timeFrame"/>.
        /// '|' represents the end of the sequence.
        /// '#' represents a <see cref="TestException"/> thrown at the end of the sequence.
        /// '*' repeats the notifications following it indefinitely.
        /// </remarks>
        public IAsyncEnumerable<char> Parse(string pattern, TimeSpan timeFrame)
        {
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));
            if (timeFrame <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeFrame));

            return Notifications(pattern, timeFrame).Replay(this);
        }

        private static IEnumerable<TimeInterval<Notification<char>>> Notifications(string pattern, TimeSpan timeFrame)
        {
            using var la = new LookAhead<(int Pos, TimeSpan Interval, char Char)>(Tokens(pattern, timeFrame));

            IEnumerable<TimeInterval<Notification<char>>> result;

            var prolog = new List<TimeInterval<Notification<char>>>();
            while (la.HasNext && _elementChars.Contains(la.Next.Char))
            {
                prolog.Add(new(la.Next.Interval, Notification.Next(la.Next.Char)));
                la.MoveNext();
            }

            if (la.HasNext)
            {
                var completion = la.Next;
                if (completion.Char == '|')
                {
                    prolog.Add(new(completion.Interval, Notification.Completed<char>()));
                    la.MoveNext();
                }
                else if (completion.Char == '#')
                {
                    prolog.Add(new(completion.Interval, Notification.Error<char>(TestException.Singleton)));
                    la.MoveNext();
                }
                else if (completion.Char == '*')
                {
                    if (completion.Interval == TimeSpan.Zero)
                        throw new ParseException("Forever may not have time frame", completion.Pos);

                    var forever = new List<TimeInterval<Notification<char>>>();
                    la.MoveNext();

                    while (la.HasNext && _elementChars.Contains(la.Next.Char))
                    {
                        forever.Add(new(la.Next.Interval, Notification.Next(la.Next.Char)));
                        la.MoveNext();
                    }

                    if (forever.Count == 0)
                        throw new ParseException("Empty forever.", completion.Pos);

                    IEnumerable<TimeInterval<Notification<char>>> Forever()
                    {
                        while (true)
                            foreach (var next in forever)
                                yield return next;
                    }

                    result = prolog.Count == 0 ? Forever() : prolog.Concat(Forever());
                }
                else
                    throw new ParseException("Unexpected character: " + completion.Char, completion.Pos);
            }

            if (la.HasNext)
                throw new ParseException("Unexpected character: " + la.Next.Char, la.Next.Pos);

            return prolog;
        }

        // enumerate '-'* c tokens, ignoring ' '
        private static IEnumerable<(int Pos, TimeSpan Interval, char Char)> Tokens(string pattern, TimeSpan timeFrame)
        {
            var pos = 0;
            using var la = new LookAhead<char>(pattern);

            // skip initial ' '
            while (la.HasNext && la.Next == ' ')
            {
                pos++;
                la.MoveNext();
            }

            void MoveNext()
            {
                while (la.MoveNext())
                {
                    pos++;
                    if (la.Next != ' ')
                        break;
                }
            }

            while (la.HasNext)
            {
                var frames = 0;
                while (la.HasNext && la.Next == '-')
                {
                    frames++;
                    MoveNext();
                }

                if (la.HasNext)
                {
                    yield return (pos, frames * timeFrame, la.Next);
                    MoveNext();
                }
                else if (frames > 0)
                    throw new ParseException("Time frames at end of pattern.", pos);
            }
        }
    }
}
