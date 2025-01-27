using System.Diagnostics;

/// <summary>
///     Альтернативная реализация расчета кванта времени выделяемого потоку на выполнение
///     Принцип тот же самый, только нет явной отдачи управления (приостановки потока), а подсчет едет за счет постоянной проверки текущего времени
///     И моментом переключения считаем когда время между интерациями было больше epsilon
/// </summary>
internal static class AlternativeProgram
{
    public static void AlternativeMain()
    {
        const int iterationsCount = 60;
        var epsilon = TimeSpan.FromMilliseconds(10);

        var sum = 0.0;

        var th = new Thread(() =>
        {
            DateTime start, end;
            var i = 0;
            end = start = DateTime.UtcNow;
            while (true)
            {
                var now = DateTime.UtcNow;
                if (now - end > epsilon)
                {
                    i++;
                    sum += (end - start).TotalMilliseconds;
                    if (i == iterationsCount) break;
                    end = start = now;
                }
                else
                    end = now;
            }
        }) { Name = "Валерий" };

        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 7);

        th.Start();
        while (th.IsAlive);
        th.Join();
        Console.WriteLine(sum / iterationsCount + "ms");
    } 
}
