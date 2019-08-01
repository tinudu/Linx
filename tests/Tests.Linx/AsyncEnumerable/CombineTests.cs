﻿namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class CombineTests
    {
        [Fact]
        public async Task Success()
        {
            using (var vt = new VirtualTime())
            {
                var seq1 = Marble.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-|").DematerializeAsyncEnumerable();
                var seq2 = Marble.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|").DematerializeAsyncEnumerable();
                var expc = Marble.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I---f------g-|", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie", "If", "Ig");
                var testee = seq1.Combine(seq2, (x, y) => $"{x}{y}");
                var eq = testee.AssertEqual(expc, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task Error()
        {
            using (var vt = new VirtualTime())
            {
                var seq1 = Marble.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-#").DematerializeAsyncEnumerable();
                var seq2 = Marble.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|").DematerializeAsyncEnumerable();
                var expc = Marble.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I-#", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie");
                var testee = seq1.Combine(seq2, (x, y) => $"{x}{y}");
                var eq = testee.AssertEqual(expc, default);
                vt.Start();
                await eq;
            }
        }
    }
}