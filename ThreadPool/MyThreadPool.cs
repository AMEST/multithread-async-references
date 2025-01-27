using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadPool;

internal class MyThreadPool : IThreadPool
{
	/// <summary>
	///		Очередь задач для выполнения
	/// </summary>
	private readonly Queue<Action> _taskQueue = new Queue<Action>();
	/// <summary>
	///		Массив наших потоков
	/// </summary>
	private readonly List<Thread> _threads = new List<Thread>();
	/// <summary>
	///		Флаг, указывающий, запущено ли уничтожение ThreadPool и высвобождение памяти
	/// </summary>
	private bool _isDisposed;
	/// <summary>
	///		Объект блокировки
	/// </summary>
	private readonly object _lock = new object();

	public MyThreadPool(int concurrency)
	{
		//	Создаем нужное количество потоков и запускаем их
		for (int i = 0; i < concurrency; i++)
		{
			var thread = new Thread(WorkerThread);
			// Обязательно говорим, что они фоновые, это позволяет при завершении основного потока приложения, завершить выполнение, а не ждать, пока потоки остановятся
			thread.IsBackground = true; 
			_threads.Add(thread);
			thread.Start();
		}
	}
	
	/// <summary>
	///		Реализация паттерна Disposable
	///		Выполняется при явнов вызове pool.Dispose() или при использовании "using var pool = new MyThreadPool()" в конце блока {}
	/// </summary>
	public void Dispose()
	{
		// Берем блокировку, чтобы знать, что никто более не работает с ThreadPool
		lock (_lock)
		{
			_isDisposed = true; // Выставляем флаг "утилизации" нашего пула (приведет к выходу из цикла у потоков и запрет на выставление новых задач)
			Monitor.PulseAll(_lock); // Переводим всех ожидающих Monitor.Wait в очередь готовых взять блокировку (Monitor.Enter)
		}
	
		// Ждем завершения потоков. Метод завершится при завершении всех потокв
		foreach (var thread in _threads)
		{
			thread.Join();
		}
	}

	/// <summary>
	///		Добавляем новое задание в очередь на выполнение в ThreadPool
	/// </summary>
	public void EnqueueAction(Action action)
	{
		// Если нет Action, то мы должны кидать исключение, что неверные входящие данные
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		//	Берем блокировку
		lock (_lock)
		{
			// Если наш ThreadPool "утилизируется", то мы не должны больше принимать новые задачи в очередь и должны выкидывать исключение ObectDisposedException
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(MyThreadPool));

			// Если все хорошо, то добавляем задание в очередь
			_taskQueue.Enqueue(action);
			// Оповещаем, что произошло событие над объектом блокировки. Один поток из очереди ожидающих (Monitor.Wait) перейдет в очередь готовых взять блокировку (Monitor.Enter)
			Monitor.Pulse(_lock);
		}
	}

	/// <summary>
	///		Основная логика выпонения заданий в нашем тредпуле (выполняет каждый поток)
	/// </summary>
	private void WorkerThread()
	{
		// Бесконечный цикл, для обработки задач
		while (true)
		{
			Action action = null;
			lock (_lock) // Берем блокировку для работы с очередью
			{
				// Пока заданий в очереди нет и объект не "утилизируется"
				while (_taskQueue.Count == 0 && !_isDisposed) 
					Monitor.Wait(_lock); // Приостанавливаем блокировку и засыпаем, пока кто-то не вызовет Monitor.Pulse / Monitor.PulseAll

				// Если наш пул "утилизируется" и задач на выполнение не осталось. То выходим из цикла и завершаем работу потока
				if (_isDisposed && _taskQueue.Count == 0)
					return;
				
				// Пытаемся взять задание из очереди
				action = _taskQueue.Dequeue();
			}
			// Выполняем задание взятое из очереди. ? - управляющий символ, который делает проверку на null и вызывает Invoke() только если action != null
			action?.Invoke();
		}
	}
}