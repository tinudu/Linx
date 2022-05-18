using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable.Subjects;

internal interface ISubject<T>
{
    ManualResetEventSlim Gate { get; }
    void AddLocked(Enumerator<T> enumerator);
    Task RemoveLocked(Enumerator<T> enumerator);
}
