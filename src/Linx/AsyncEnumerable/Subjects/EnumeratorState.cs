namespace Linx.AsyncEnumerable.Subjects;

internal enum EnumeratorState : byte
{
    Initial,
    Accepting,
    Emitting,
    Final
}