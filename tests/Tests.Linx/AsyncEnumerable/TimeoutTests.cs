using System;
using Linx.AsyncEnumerable;
using Linx.Testing;
using Xunit;

namespace Tests.Linx.AsyncEnumerable;

public sealed class TimeoutTests
{
    [Fact]
    public void TestNoTimeout()
    {
        VirtualTime.Run(vt =>
        {
            const string seq = "a-b--c--d-|";
            var testee = vt.Parse(seq).Timeout(3 * LinxTesting.DefaultTimeFrame, vt);
            return testee.Expect(seq, vt);
        });
    }

    [Fact]
    public void TestTimeout()
    {
        VirtualTime.Run(vt =>
        {
            const string seq = "a-b--c-----d|";
            const string exp = "a-b--c---#";
            var testee = vt.Parse(seq).Timeout(3 * LinxTesting.DefaultTimeFrame, vt);
            return testee.Expect(exp, vt, ex => ex is TimeoutException);
        });
    }
}
