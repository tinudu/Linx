namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Enumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class SampleTests
    {
        [Fact]
        public async Task Success()
        {
            using (var vt = new VirtualTime())
            {
                //                         1   2 345678901   2 345   67 89
                var testee = Marble.Parse("-abc- ---------def- ---efg- ----|").DematerializeAsyncEnumerable().Sample(TimeSpan.FromSeconds(2));
                var expect = Marble.Parse("-a  -c---------d  -f---e  -g----|");
                var eq = testee.Sample(TimeSpan.FromSeconds(2)).AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

    }
}
