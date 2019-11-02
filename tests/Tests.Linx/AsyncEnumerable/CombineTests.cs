namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class CombineTests
    {
        [Fact]
        public async Task Success()
        {
            var seq1 = Marble.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-|");
            var seq2 = Marble.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|");
            var expc = Marble.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I---f------g-|", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie", "If", "Ig");
            var testee = seq1.Combine(seq2, (x, y) => $"{x}{y}");
            await expc.AssertEqual(testee);
        }

        [Fact]
        public async Task Error()
        {
            var seq1 = Marble.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-#");
            var seq2 = Marble.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|");
            var expc = Marble.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I-#", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie");
            var testee = seq1.Combine(seq2, (x, y) => $"{x}{y}");
            await expc.AssertEqual(testee);
        }
    }
}
