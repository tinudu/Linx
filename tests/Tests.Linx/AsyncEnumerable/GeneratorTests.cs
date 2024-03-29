﻿//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx;
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Timing;
//    using System;
//    using System.Threading.Tasks;
//    using Xunit;

//    public class GeneratorTests
//    {
//        [Fact]
//        public async Task TestReturn()
//        {
//            var result = await LinxAsyncEnumerable.Return(42).Single(default);
//            Assert.Equal(42, result);

//            var t0 = DateTimeOffset.Now;
//            var delay = TimeSpan.FromDays(30);
//            var actual = await VirtualTime.Run(async () =>
//            {
//                await Time.Current.Delay(delay, default).ConfigureAwait(false);
//                return 42;
//            }, t0);
//            Assert.Equal(new Timestamped<int>(t0 + delay, 42), actual);
//        }

//        [Fact]
//        public async Task TestThrows()
//        {
//            var ex = await Assert.ThrowsAsync<Exception>(() => LinxAsyncEnumerable.Throw<int>(new Exception("Boom!")).Any(default));
//            Assert.Equal("Boom!", ex.Message);
//        }
//    }
//}
