using System;
using System.Linq;
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
        Console.WriteLine("Background service started.");
        // Run the SLA checks every 5 minutes
        _timer = new Timer(PerformSLAChecks, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private void PerformSLAChecks(object state)
    {
        var currentTime = DateTime.UtcNow.Add(TimeSpan.FromHours(8));
        var dayOfWeek = currentTime.DayOfWeek;

        // Check if it is a weekday (Monday to Friday), within working hours (8 AM - 5 PM) and not a holiday
        if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday &&
            currentTime.Hour >= 8 && currentTime.Hour < 17 && !IsHoliday(currentTime))
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var slaMonitoringService = scope.ServiceProvider.GetRequiredService<SLAMonitoringService>();
                slaMonitoringService.MonitorTickets();
            }
        }
    }

    // Static holiday list for the Philippines (Fixed Date)
    private bool IsHoliday(DateTime date)
    {
        // List of fixed holidays
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),  // New Year's Day
            new DateTime(date.Year, 4, 9),  // Araw ng Kagitingan
            new DateTime(date.Year, 5, 1),  // Labor Day
            new DateTime(date.Year, 6, 12), // Independence Day
            new DateTime(date.Year, 11, 30), // Bonifacio Day
            new DateTime(date.Year, 12, 25), // Christmas Day
            new DateTime(date.Year, 12, 30), // Rizal Day
        };

        // Check for movable holidays (e.g., National Heroes Day - last Monday of August)
        var nationalHeroesDay = GetLastMondayOfAugust(date.Year);
        holidays = holidays.Concat(new[] { nationalHeroesDay }).ToArray();

        // Add special non-working days like Christmas Eve, New Year's Eve, etc.
        holidays = holidays.Concat(new[]
        {
            new DateTime(date.Year, 12, 24), // Christmas Eve
            new DateTime(date.Year, 12, 31), // New Year's Eve
        }).ToArray();

        return holidays.Contains(date.Date);
    }

    // Get the last Monday of August (National Heroes Day)
    private DateTime GetLastMondayOfAugust(int year)
    {
        var lastDayOfAugust = new DateTime(year, 8, 31);
        var daysToSubtract = (lastDayOfAugust.DayOfWeek == DayOfWeek.Monday) ? 0 : (int)lastDayOfAugust.DayOfWeek + 1;
        return lastDayOfAugust.AddDays(-daysToSubtract);
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
