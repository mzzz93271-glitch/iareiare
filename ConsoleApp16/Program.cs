using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

internal class Program
{
    private const int ItemCount = 10;
    private const int RunCount = 5;

    
    private const bool TestException = false;

    private static void Main()
    {
        Console.WriteLine("|№ запуска|Последовательно|Многопоточно|Что быстрее|");
        Console.WriteLine("|---:|---:|---:|---|");

        for (int run = 1; run <= RunCount; run++)
        {
            RunResult sequential = RunSequential();
            RunResult multithreaded = RunMultithreaded();

            string faster =
                sequential.Elapsed < multithreaded.Elapsed ? "Последовательно" :
                sequential.Elapsed > multithreaded.Elapsed ? "Многопоточно" :
                "Одинаково";

            Console.WriteLine(
                $"|{run}|{sequential.Elapsed.TotalMilliseconds:F1} мс|" +
                $"{multithreaded.Elapsed.TotalMilliseconds:F1} мс|{faster}|");

            Console.WriteLine(
                $"  Успешных операций: последовательно — {sequential.SuccessCount}, " +
                $"многопоточно — {multithreaded.SuccessCount}");

            PrintErrors("Последовательная версия", sequential.Errors);
            PrintErrors("Многопоточная версия", multithreaded.Errors);
        }
    }

    private static RunResult RunSequential()
    {
        var errors = new List<string>();
        int successCount = 0;

        var stopwatch = Stopwatch.StartNew();

        for (int item = 0; item < ItemCount; item++)
        {
            try
            {
                DoWork(item);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Операция {item}: {ex}");
            }
        }

        stopwatch.Stop();
        return new RunResult(stopwatch.Elapsed, successCount, errors);
    }

    private static RunResult RunMultithreaded()
    {
        var errors = new ConcurrentQueue<string>();
        int successCount = 0;
        var threads = new Thread[ItemCount];

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < ItemCount; i++)
        {
            int item = i;

            threads[i] = new Thread(() =>
            {
                try
                {
                    DoWork(item);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    errors.Enqueue($"Операция {item}: {ex}");
                }
            });

            threads[i].Start();
        }

     
        foreach (Thread thread in threads)
            thread.Join();

        stopwatch.Stop();
        return new RunResult(stopwatch.Elapsed, successCount, errors.ToList());
    }

    private static void DoWork(int item)
    {
       
        Thread.Sleep(100);

        
        if (TestException && item == 3)
            throw new InvalidOperationException("Тестовая ошибка.");
    }

    private static void PrintErrors(string title, List<string> errors)
    {
        foreach (string error in errors)
            Console.Error.WriteLine($"{title}: {error}");
    }

    private sealed class RunResult
    {
        public TimeSpan Elapsed { get; }
        public int SuccessCount { get; }
        public List<string> Errors { get; }

        public RunResult(TimeSpan elapsed, int successCount, List<string> errors)
        {
            Elapsed = elapsed;
            SuccessCount = successCount;
            Errors = errors;
        }
    }
}
