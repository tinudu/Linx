//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using System.Threading.Tasks;
//    using Xunit;

//    public sealed class CombineTests
//    {
//        [Fact]
//        public async Task Success()
//        {
//            var seq1 = TestSequence.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-|");
//            var seq2 = TestSequence.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|");
//            var expc = TestSequence.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I---f------g-|", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie", "If", "Ig");
//            var testee = seq1.Combine(seq2).Select(t => $"{t.Item1}{t.Item2}");
//            await expc.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task Error()
//        {
//            var seq1 = TestSequence.Parse("-A---B-- ---C--- -D----E- ---F--- -G----H- ---I-#");
//            var seq2 = TestSequence.Parse("- --- --a--- ---b- ---- -c--- ---d- ---- -e--- ---f------g-|");
//            var expc = TestSequence.Parse("- --- --a---C---b-D----E-c---F---d-G----H-e---I-#", null, "Ba", "Ca", "Cb", "Db", "Eb", "Ec", "Fc", "Fd", "Gd", "Hd", "He", "Ie");
//            var testee = seq1.Combine(seq2).Select(t => $"{t.Item1}{t.Item2}");
//            await expc.AssertEqual(testee);
//        }
//    }
//}
