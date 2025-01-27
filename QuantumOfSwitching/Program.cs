using System.Diagnostics;

internal class Program
{
    /// <summary>
    ///     Флаг остановки, для выхода из бесконечного цикла
    /// </summary>
    static bool _stopping = false;
    
    private static void Main(string[] args)
    {
        var processorNum = args.Length > 0 ? int.Parse(args.First()) - 1 : 1;
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << processorNum); // Привязываем наш процесс к конкретному ядру
        // Это нужно, чтобы оба наших потока всегда работали на одном ядре и конкурировали за квант времени для выполнения 

        // Создаем два потока
        Thread thread1 = new Thread(Thread1Method); // Создаем первый поток, в котором будет выполнятся бесконечный цикл занимающий время выполнения
        Thread thread2 = new Thread(Thread2Method); // Создаем второй поток, который будет пытаться получить время на выполнение и записать его

        // Запускаем потоки
        thread1.Start();
        thread2.Start();

        // Ждем завершения потоков
        thread1.Join();
        thread2.Join();

        static void Thread1Method()
        {
            while (!_stopping)
            {
                // Бесконечный цикл для занятия процессора работой (бесполезной, но работой)
            }
        }

        static void Thread2Method()
        {
            Stopwatch stopwatch = new Stopwatch(); // Создаем таймер
            long totalTime = 0; // Запоминаем обще время ожидайний
            int switchCount = 0; // Количество переключений контекста

            while (switchCount < 100) // Измеряем 100 переключений контекста
            {
                stopwatch.Restart(); // Сбрасываем таймер
                Thread.Sleep(0); // Вызываем переключение контекста, чтобы управление точно ушло в первый поток в метод Thread1Method
                stopwatch.Stop(); // Останавливаем таймер после того как управление вернулось в наш поток
                totalTime += stopwatch.ElapsedTicks; // Сохраняем время прошедщее с момента отдачи управления и его получения обратно
                switchCount++; // Увеличиваем счетчик переключений контекста
            }

            long averageTicks = totalTime / switchCount; // Считаем среднее время (в тиках) кванта времени выделяемого потокам
            long averageTime = averageTicks * 1000 / Stopwatch.Frequency; // Переводим тики в миллисекунды

            // Выводим среднее время выделяемое потоку на выполнение
            // В разных ОС время будет разное, как и реализации (к примеру можно прочитать про планировщик Linux https://habr.com/ru/articles/807645/ (их еще и несколько)
            // В windows тоже есть различие, между настройкой (лучша производительность приложений или сервисов), которая дает приоритет разным типам приложений и их потокам
            // А так же между десктопной Windows - там время Кванта - 2 интервала таймера. А в Windows Server - 12 интервалов таймера (т.к. серверным приложениям желательно меньше переключений контекста, для производительности, да и ядер там обычно поболее)
            // 1 интервал таймера, это примерно 16мс
            Console.WriteLine($"Average time quantum: {averageTime} ms");
            _stopping = true;
        }
    }
}