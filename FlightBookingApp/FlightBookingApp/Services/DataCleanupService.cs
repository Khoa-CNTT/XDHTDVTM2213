using FlightBookingApp.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FlightBookingApp.Services
{
    public class DataCleanupService
    {
        private readonly ApplicationDbContext _context;

        public DataCleanupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CleanupOldDataAsync()
        {
            try
            {
                Console.WriteLine("[DataCleanupService] Starting cleanup of old data...");

              
                DateTime currentDate = DateTime.UtcNow.Date;
                DateTime cutoffDate = currentDate.AddDays(-10); 

                Console.WriteLine($"[DataCleanupService] Cleaning up data before {cutoffDate:dd/MM/yyyy}...");

                // 1. Xóa các hóa đơn (Invoices) cũ
                var oldInvoices = await _context.Invoices
                    .Include(i => i.Booking)
                    .ThenInclude(b => b.Flight)
                    .Where(i => i.Booking != null && i.Booking.Flight != null && i.Booking.Flight.DepartureTime < cutoffDate)
                    .ToListAsync();

                if (oldInvoices.Any())
                {
                    _context.Invoices.RemoveRange(oldInvoices);
                    Console.WriteLine($"[DataCleanupService] Removed {oldInvoices.Count} old invoices.");
                }
                else
                {
                    Console.WriteLine("[DataCleanupService] No old invoices found to remove.");
                }

                // 2. Xóa các hành khách (Passengers) cũ
                var oldPassengers = await _context.Passengers
                    .Include(p => p.Booking)
                    .ThenInclude(b => b.Flight)
                    .Where(p => p.Booking != null && p.Booking.Flight != null && p.Booking.Flight.DepartureTime < cutoffDate)
                    .ToListAsync();

                if (oldPassengers.Any())
                {
                    _context.Passengers.RemoveRange(oldPassengers);
                    Console.WriteLine($"[DataCleanupService] Removed {oldPassengers.Count} old passengers.");
                }
                else
                {
                    Console.WriteLine("[DataCleanupService] No old passengers found to remove.");
                }

                // 3. Xóa các thanh toán (Payments) cũ
                var oldPayments = await _context.Payments
                    .Include(p => p.Booking)
                    .ThenInclude(b => b.Flight)
                    .Where(p => p.Booking != null && p.Booking.Flight != null && p.Booking.Flight.DepartureTime < cutoffDate)
                    .ToListAsync();

                if (oldPayments.Any())
                {
                    _context.Payments.RemoveRange(oldPayments);
                    Console.WriteLine($"[DataCleanupService] Removed {oldPayments.Count} old payments.");
                }
                else
                {
                    Console.WriteLine("[DataCleanupService] No old payments found to remove.");
                }

                // 4. Xóa các đặt vé (Bookings) cũ
                var oldBookings = await _context.Bookings
                    .Include(b => b.Flight)
                    .Where(b => b.Flight != null && b.Flight.DepartureTime < cutoffDate)
                    .ToListAsync();

                if (oldBookings.Any())
                {
                    _context.Bookings.RemoveRange(oldBookings);
                    Console.WriteLine($"[DataCleanupService] Removed {oldBookings.Count} old bookings.");
                }
                else
                {
                    Console.WriteLine("[DataCleanupService] No old bookings found to remove.");
                }

                // 5. Xóa các chuyến bay (Flights) cũ
                var oldFlights = await _context.Flights
                    .Where(f => f.DepartureTime < cutoffDate)
                    .ToListAsync();

                if (oldFlights.Any())
                {
                    _context.Flights.RemoveRange(oldFlights);
                    Console.WriteLine($"[DataCleanupService] Removed {oldFlights.Count} old flights.");
                }
                else
                {
                    Console.WriteLine("[DataCleanupService] No old flights found to remove.");
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();
                Console.WriteLine("[DataCleanupService] Cleanup of old data completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataCleanupService] Error during cleanup of old data: {ex.Message}");
                Console.WriteLine($"[DataCleanupService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}