namespace Linx.AsyncEnumerable.Subjects
{
    using System.Threading;
    using System.Threading.Tasks;

    internal interface ISubject<T>
    {
        ManualResetEventSlim Gate { get; }
        void AddLocked(Enumerator<T> enumerator);
        Task RemoveLocked(Enumerator<T> enumerator);
    }
}
