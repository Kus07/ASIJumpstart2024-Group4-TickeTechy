using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class SLAMonitoringBackgroundTask : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IServiceProvider _serviceProvider;

    public SLAMonitoringBackgroundTask(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run SLA checks every 5 minutes
        _timer = new Timer(PerformSLAChecks, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private void PerformSLAChecks(object state)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var slaMonitoringService = scope.ServiceProvider.GetRequiredService<SLAMonitoringService>();
            slaMonitoringService.MonitorTickets();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
