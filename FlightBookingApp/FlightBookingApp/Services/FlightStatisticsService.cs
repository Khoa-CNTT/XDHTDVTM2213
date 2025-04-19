/*using FlightBookingApp.Data;
using FlightBookingApp.Models;
using Microsoft.EntityFrameworkCore;


namespace FlightBookingApp.Services

{
  
    public class FlightStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public FlightStatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateStatisticsAsync()
        {
            try
            {
                // Tính toán thống kê trên toàn bộ bảng Flights
                var stats = await _context.Flights
                    .GroupBy(f => 1)
                    .Select(g => new
                    {
                        CompletedFlights = g.Count(f => f.ArrivalTime < DateTime.Now),
                        ActiveFlights = g.Count(f => f.DepartureTime <= DateTime.Now && f.ArrivalTime >= DateTime.Now),
                        CanceledFlights = g.Count(f => f.Status == "Canceled")
                    })
                    .FirstOrDefaultAsync();

                // Lấy hoặc tạo bản ghi trong FlightStatistics
             *//*   var flightStats = await _context.FlightStatistics.FirstOrDefaultAsync();
                if (flightStats == null)
                {
                    flightStats = new FlightStatistics();
                    _context.FlightStatistics.Add(flightStats);
                }*//*

                // Cập nhật số liệu
                flightStats.CompletedFlights = stats?.CompletedFlights ?? 0;
                flightStats.ActiveFlights = stats?.ActiveFlights ?? 0;
                flightStats.CanceledFlights = stats?.CanceledFlights ?? 0;
                flightStats.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                // Ví dụ: _logger.LogError(ex, "Error updating flight statistics");
                throw;
            }
        }
    }
}
*/