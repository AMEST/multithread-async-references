using System.Threading;

namespace LockFree
{
    public class LockFreeStack<T> : IStack<T>
    {
        private Node<T> _head;

        /// <summary>
        ///     Метод для получения (и удаления) элемента из стека
        /// </summary>
        public T Pop()
        {
            // Цикл выполняется до тех пор, пока не будет успешно удален элемент из стека
            while (true)
            {
                // Получаем текущую голову стека
                Node<T> currentHead = _head;
                // Получаем следующий элемент после текущей головы
                Node<T> nextHead = currentHead.Next;
                
                // Удаляем элемент из стека и возвращаем его значение
                // CompareExchange - атомарно сравнивает значение currentHead с expectedHead (текущей головой стека _head)
                // Если значения совпадают, заменяет значение currentHead на nextHead (следующий элемент в стэке)
                // Если значения не совпадают, значение currentHead остается неизмененным
                // CompareExchange возвращает оригинальный элемент, который был в ref _head (ну и соответственно в comparand)
                if (Interlocked.CompareExchange(ref _head, nextHead, currentHead) == currentHead)
                    return currentHead.Value;
            }
        }

        public void Push(T obj)
        {
            // Создаем новый узел с добавляемым элементом
            Node<T> newNode = new Node<T> { Value = obj };
            
            // Цикл выполняется до тех пор, пока не будет успешно добавлен элемент в стек
            while (true)
            {
                // Получаем текущую голову стека
                Node<T> currentHead = _head;
                // Добавляем новый узел в стек, ставя его после текущей головы
                newNode.Next = currentHead;
                
                // Добавляем новый элемент в стек
                // CompareExchange - атомарно сравнивает значение currentHead с expectedHead (новым узлом newNode)
                // Если значения совпадают, заменяет значение currentHead на newNode (новый узел)
                // Если значения не совпадают, значение currentHead остается неизмененным
                // CompareExchange возвращает оригинальный элемент, который был в ref _head (ну и соответственно в comparand)
                if (Interlocked.CompareExchange(ref _head, newNode, currentHead) == currentHead)
                    break;
            }
        }
    }
}