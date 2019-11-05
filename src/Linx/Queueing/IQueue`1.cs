namespace Linx.Queueing
{
    internal interface IQueue<T>
    {
        bool IsEmpty { get; }
        bool IsFull { get; }
        void Enqueue(T item);
        T Dequeue();
        void Clear();
        void TrimExcess();
    }
}
