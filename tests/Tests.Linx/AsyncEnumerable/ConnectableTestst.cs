namespace Tests.Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using Xunit;

    public sealed class ConnectableTestst
    {
        [Fact]
        public async Task Success()
        {
            var src = new[] {1, 2, 3};
            var connectable = src.ToAsyncEnumerable().Connectable(out var connect);
            // ReSharper disable PossibleMultipleEnumeration
            var t1 = connectable.ToList(default);
            var t2 = connectable.Skip(1).First(default);
            // ReSharper restore PossibleMultipleEnumeration
            connect();
            Assert.True(src.SequenceEqual(await t1));
            Assert.Equal(2, await t2);
        }

        [Fact]
        public async Task TestTooLate()
        {
            var connectable = new[] { 1, 2, 3 }.ToAsyncEnumerable().Connectable(out var connect);

            // ReSharper disable PossibleMultipleEnumeration
            var e = connectable.ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                connect();
                await Assert.ThrowsAsync<AlreadyConnectedException>(async () => await e.MoveNextAsync());
            }
            finally { await e.DisposeAsync(); }
            
            e = connectable.ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                connect();
                await Assert.ThrowsAsync<AlreadyConnectedException>(async () => await e.MoveNextAsync());
            }
            finally { await e.DisposeAsync(); }

            // ReSharper restore PossibleMultipleEnumeration
        }
    }
}
