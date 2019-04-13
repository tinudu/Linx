namespace Linx.Expressions.Serialization.Parsing
{
    using System;
    using System.Collections.Generic;
    using Enumerable;

    public sealed class Tokenizer
    {
        public struct Tokenized
        {
            public IToken Token { get; }
            public bool IsSeparated { get; }

            public Tokenized(IToken token, bool isSeparated)
            {
                Token = token;
                IsSeparated = isSeparated;
            }
        }

        private static IEnumerable<char> LineTerminated(IEnumerable<char> input)
        {
            var la = new LookAhead<char>(input);
            var eol = true;
            while (la.HasNext)
                switch (la.Next)
                {
                    case '\u001A': // ctrl-Z
                        la.MoveNext();
                        if (!la.HasNext) continue;
                        eol = false;
                        yield return '\u001A';
                        break;
                    case '\u000D':
                    case '\u000A':
                    case '\u2028':
                    case '\u2029':
                        eol = true;
                        yield return la.Next;
                        la.MoveNext();
                        break;
                    default:
                        eol = false;
                        yield return la.Next;
                        la.MoveNext();
                        break;
                }
            if (eol) yield break;
            yield return '\u000D';
        }

        private readonly IEnumerable<char> _input;

        public Tokenizer(IEnumerable<char> input) => _input = input != null ? LineTerminated(input) : throw new ArgumentNullException(nameof(input));
    }
}
