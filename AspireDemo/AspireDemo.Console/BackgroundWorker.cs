// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;

public class BackgroundWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Iteration " + ++i);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
