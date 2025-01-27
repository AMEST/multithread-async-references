using System.Threading;

namespace LockFree;

public class LockFreeQueue<T> : IQueue<T>
{
    private Node<T> _head;
    private Node<T> _tail;

    // Конструктор инициализирует очередь с фиктивным узлом (dummy node)
    public LockFreeQueue()
    {
        Node<T> dummy = new Node<T>();
        _head = dummy;
        _tail = dummy;
    }

    /// <summary>
    ///     Метод добавления элемента в очередь
    /// </summary>
    public void Enqueue(T obj)
    {
        // Создаем новый узел с добавляемым элементом
        var newNode = new Node<T>() { Value = obj };
        while (true)
        {
            // Получаем текущий хвост очереди
            var currentTail = _tail;
            // Получаем следующий узел после текущего хвоста
            var next = currentTail.Next;
            // Если текущий хвост не равен реальному хвосту очереди, переходим к следующему циклу
            if (currentTail != _tail) continue;
            
            // Если следующий узел не существует, мы можем добавить новый узел в очередь
            if (next == null)
            {
                // Если успешно добавлен, обновляем реальный хвост очереди
                if (Interlocked.CompareExchange(ref currentTail.Next, newNode, null) == null)
                {
                    Interlocked.CompareExchange(ref _tail, newNode, currentTail);
                    return;
                }
            }
            else // Если следующий узел существует, обновляем реальный хвост очереди
            {
                Interlocked.CompareExchange(ref _tail, next, currentTail);
            }
        }
    }

    /// <summary>
    ///     Метод получения (и удаления) элемента из очереди
    /// </summary>
    public bool TryDequeue(out T result)
    {
        // Цикл выполняется до тех пор, пока не будет успешно удален элемент из очереди
        while (true)
        {
            // Получаем текущую голову и хвост очереди
            var currentHead = _head;
            var currentTail = _tail;
            // Получаем следующий узел после текущей головы
            var next = currentHead?.Next;
            // Если текущая голова не равна реальной голове очереди, переходим к следующему циклу
            if (currentHead != _head) continue;
            
            // Если текущая голова и хвост равны, это означает, что очередь пуста
            if (currentHead == currentTail)
            {
                // Если следующий узел не существует, означает, что очередь пуста
                if (next == null)
                {
                    result = default;
                    return false;
                }
                // Обновляем реальный хвост очереди
                Interlocked.CompareExchange(ref _tail, next, currentTail);
            }
            else // Если текущая голова не равна хвосту, это означает, что очередь не пуста
            {
                // Удаляем элемент из очереди и получаем его значение
                result = next.Value;
                // Обновляем реальную голову очереди
                if (Interlocked.CompareExchange(ref _head, next, currentHead) == currentHead)
                    return true;
            }
        }
    }
}