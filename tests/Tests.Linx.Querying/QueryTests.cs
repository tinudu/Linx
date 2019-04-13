namespace Tests.Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Linx.Expressions;
    using global::Linx.Querying;
    using Xunit;

    public sealed class QueryTests
    {
        public interface ICustomer
        {
            int Id { get; }
            string Name { get;
            }
        }

        private interface IContext
        {
            IEnumerable<ICustomer> Customers { get; }
        }

        [Fact]
        public void TestAll()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).All(c => false);
            Assert.Equal("All", Express.Method(testee).Name);
            Assert.Equal(typeof(Func<IContext, bool>), testee.Type);
        }

        [Fact]
        public void TestAny()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Any();
            Assert.Equal("Any", Express.Method(testee).Name);
            Assert.Equal(typeof(Func<IContext, bool>), testee.Type);
        }

        [Fact]
        public void TestAny1()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Any(c => false);
            Assert.Equal("Any", Express.Method(testee).Name);
            Assert.Equal(typeof(Func<IContext, bool>), testee.Type);
        }

        [Fact]
        public void TestConcat()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Concat(Query<IContext>.Create(ctx1 => ctx1.Customers));
            Assert.Equal("Concat", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestGroupByK()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).GroupBy(x => x.Name);
            Assert.Equal("GroupBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<IGrouping<string, ICustomer>>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestGroupByKe()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).GroupBy(x => x.Name, x => x.Id);
            Assert.Equal("GroupBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<IGrouping<string, int>>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestGroupByKr()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).GroupBy(x => x.Name, (n, cs) => n + cs.Count());
            Assert.Equal("GroupBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<string>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestGroupByKer()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).GroupBy(x => x.Name, x => x.Id, (n, ids) => n + ids.Count());
            Assert.Equal("GroupBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<string>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestOrderBy()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).OrderBy(x => x.Name);
            Assert.Equal("OrderBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IOrderedEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestOrderByDescending()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).OrderByDescending(x => x.Name);
            Assert.Equal("OrderByDescending", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IOrderedEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestSelect()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Select(c => c.Name);
            Assert.Equal("Select", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<string>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestSelect1()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Select((c, i) => c.Id + i);
            Assert.Equal("Select", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<int>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestThenBy()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers.OrderBy(x => x.Name)).ThenBy(x => x.Id);
            Assert.Equal("ThenBy", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IOrderedEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestThenByDescending()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers.OrderBy(x => x.Name)).ThenByDescending(x => x.Id);
            Assert.Equal("ThenByDescending", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IOrderedEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestWhere()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Where(c => c.Name == "Gygax");
            Assert.Equal("Where", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<ICustomer>>), testee.Lambda.Type);
        }

        [Fact]
        public void TestWhere1()
        {
            var testee = Query<IContext>.Create(ctx => ctx.Customers).Where((c, i) => ((c.Id + i) & 1) == 0);
            Assert.Equal("Where", Express.Method(testee.Lambda).Name);
            Assert.Equal(typeof(Func<IContext, IEnumerable<ICustomer>>), testee.Lambda.Type);
        }
    }
}
