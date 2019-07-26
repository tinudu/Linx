namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using global::Linx.AsyncEnumerable.Notifications;
    using Xunit;

    public sealed class NotificationTests
    {
        [Fact]
        public void TestNotificationFactory()
        {
            var ex = new InvalidOperationException("TestTestTest");

            var n = Notification.Next<Exception>(ex);
            Assert.True(n.Kind == NotificationKind.Next && n.Value == ex && n.Error == null);

            n = Notification.Completed<Exception>();
            Assert.True(n.Kind == NotificationKind.Completed && n.Value == null && n.Error == null);

            n = Notification.Error<Exception>(ex);
            Assert.True(n.Kind == NotificationKind.Error && n.Value == null && n.Error == ex);

            Assert.Throws<ArgumentNullException>(() => Notification.Error<string>(null));
        }

        [Fact]
        public void TestEquality()
        {
            var ex = new InvalidOperationException("TestTestTest");

            var next1 = Notification.Next<Exception>(ex);
            var next2 = Notification.Next<Exception>(new InvalidOperationException("TestTestTest"));
            var completed = Notification.Completed<Exception>();
            var error1 = Notification.Error<Exception>(ex);
            var error2 = Notification.Error<Exception>(new InvalidOperationException("TestTestTest"));
            var error3 = Notification.Error<Exception>(new InvalidOperationException("Different message"));
            var error4 = Notification.Error<Exception>(new Exception("TestTestTest"));

            Assert.True(TestEq(next1, next1));
            Assert.False(TestEq(next1, next2));
            Assert.False(TestEq(next1, completed));
            Assert.False(TestEq(next1, error1));

            Assert.True(TestEq(completed, completed));
            Assert.False(TestEq(completed, error1));

            Assert.True(TestEq(error1, error1));
            Assert.True(TestEq(error1, error2));
            Assert.False(TestEq(error1, error3));
            Assert.False(TestEq(error1, error4));
        }

        private static bool TestEq<T>(Notification<T> n1, Notification<T> n2)
        {
            if (!n1.Equals(n2))
                return false;
            if (n1.GetHashCode() != n2.GetHashCode())
                throw new Exception("Equal, but different hash code.");
            return true;
        }
    }
}
