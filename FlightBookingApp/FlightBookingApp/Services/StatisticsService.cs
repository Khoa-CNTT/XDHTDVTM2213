using FlightBookingApp.Data;
using FlightBookingApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FlightBookingApp.Services
{
    public class StatisticsService
    {
        private readonly JobDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(JobDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Phương thức dành riêng cho Hangfire, không có tham số tùy chọn
        public async Task UpdateStatisticsEverySecondForHangfireAsync()
        {
            await UpdateStatisticsEverySecondAsync(CancellationToken.None);
        }

        // Phương thức thực hiện cập nhật mỗi giây trong 60 giây
        public async Task UpdateStatisticsEverySecondAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting statistics update every second at {Time}", DateTime.UtcNow);

                // Chạy trong 60 giây (mỗi phút)
                for (int i = 0; i < 60; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Statistics update every second was canceled.");
                        break;
                    }

                    await UpdateStatisticsAsync();
                    await Task.Delay(1000, cancellationToken); // Chờ 1 giây trước khi chạy lần tiếp theo
                }

                _logger.LogInformation("Completed statistics update every second at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatisticsEverySecondAsync");
                throw;
            }
        }

        public async Task UpdateStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Starting statistics update at {Time}", DateTime.UtcNow);

                var today = DateTime.Today;

                var flightStats = await _context.Flights
                    .GroupBy(f => 1)
                    .Select(g => new
                    {
                        CompletedFlights = g.Count(f => f.ArrivalTime < DateTime.Now),
                        ActiveFlights = g.Count(f => f.DepartureTime <= DateTime.Now && f.ArrivalTime >= DateTime.Now),
                        CanceledFlights = g.Count(f => f.Status == "Canceled")
                    })
                    .FirstOrDefaultAsync();

                var totalRevenue = await _context.Bookings
                    .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

                var visitStats = await _context.WebsiteVisits
                    .GroupBy(v => 1)
                    .Select(g => new
                    {
                        TotalVisits = g.Count(),
                        VisitsToday = g.Count(v => v.VisitDate.Date == today)
                    })
                    .FirstOrDefaultAsync();

                var totalTickets = await _context.Bookings.CountAsync();

                var statsSummary = await _context.StatisticsSummary.FirstOrDefaultAsync();
                if (statsSummary == null)
                {
                    statsSummary = new StatisticsSummary();
                    _context.StatisticsSummary.Add(statsSummary);
                }

                statsSummary.CompletedFlights = flightStats?.CompletedFlights ?? 0;
                statsSummary.ActiveFlights = flightStats?.ActiveFlights ?? 0;
                statsSummary.CanceledFlights = flightStats?.CanceledFlights ?? 0;
                statsSummary.TotalRevenue = totalRevenue;
                statsSummary.TotalVisits = visitStats?.TotalVisits ?? 0;
                statsSummary.VisitsToday = visitStats?.VisitsToday ?? 0;
                statsSummary.TotalTickets = totalTickets;
                statsSummary.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Statistics update completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statistics");
                throw;
            }
        }
    }
}