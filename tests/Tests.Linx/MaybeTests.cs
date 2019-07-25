namespace Tests.Linx
{
    using System;
    using global::Linx;
    using Xunit;

    public sealed class MaybeTests
    {
        [Fact]
        public void TestConstructors()
        {
            TestMembers(new Maybe<int>(), false, 42);
            TestMembers(new Maybe<int>(42), true, 42);
            TestMembers(new Maybe<string>(null), true, null);
            TestMembers(new Maybe<int?>(null), true, null);
        }

        [Fact]
        public void TestImplicitConversion()
        {
            TestMembers(42, true, 42);
            TestMembers<string>(null, true, null);
            TestMembers<int?>(null, true, null);
        }

        [Fact]
        public void TestFactory()
        {
            TestMembers(42.Maybe(), true, 42);
            TestMembers(default(string).Maybe(), true, null);
            TestMembers(default(int?).Maybe(), true, null);
        }

        [Fact]
        public void TestTryParse()
        {
            TestMembers(Maybe.TryParse<int>("42", int.TryParse), true, 42);
            TestMembers(Maybe.TryParse<int>("bla", int.TryParse), false, 42);
        }

        private static void TestMembers<T>(Maybe<T> maybe, bool shouldHaveValue, T value)
        {
            if (shouldHaveValue)
            {
                Assert.True(maybe.HasValue);
                Assert.Equal(value, maybe.Value);
                Assert.Equal(value, maybe.GetValueOrDefault());
                Assert.Equal(value, maybe.GetValueOrDefault(default));
                Assert.True(maybe.TryGetValue(out var v));
                Assert.Equal(value, v);
                Assert.Equal(value?.ToString() ?? string.Empty, maybe.ToString());
                Assert.Equal(value, (T)maybe);
            }
            else
            {
                Assert.False(maybe.HasValue);
                Assert.Throws<InvalidOperationException>(() => maybe.Value);
                Assert.Equal(default, maybe.GetValueOrDefault());
                Assert.Equal(value, maybe.GetValueOrDefault(value));
                Assert.False(maybe.TryGetValue(out var v));
                Assert.Equal(default, v);
                Assert.Equal(string.Empty, maybe.ToString());
                Assert.Throws<InvalidOperationException>(() => (T)maybe);
            }
        }
    }
}
