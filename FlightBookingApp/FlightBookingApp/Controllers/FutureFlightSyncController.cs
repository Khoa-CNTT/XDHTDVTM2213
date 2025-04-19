using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlightBookingApp.Services;
using Hangfire;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FlightBookingApp.Data;
using Microsoft.EntityFrameworkCore;
using FlightBookingApp.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightBookingApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class FutureFlightSyncController : Controller
    {
        private readonly FutureFlightSyncService _futureFlightSyncService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FutureFlightSyncController> _logger;
        private readonly IMemoryCache _cache;

        public FutureFlightSyncController(
            FutureFlightSyncService futureFlightSyncService,
            ApplicationDbContext context,
            ILogger<FutureFlightSyncController> logger,
            IMemoryCache cache)
        {
            _futureFlightSyncService = futureFlightSyncService;
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 50,
            DateTime? lastDepartureTime = null,
            DateTime? filterDate = null,
            string departureAirport = null,
            string destinationAirport = null,
            string airline = null)
        {
            _logger.LogInformation("[FutureFlightSyncController] Accessing Index page.");
            Console.WriteLine("[FutureFlightSyncController] Accessing Index page.");

            // Kiểm tra session UserId
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                _logger.LogWarning("[FutureFlightSyncController] UserId not found in session, redirecting to Login.");
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                // Lấy tổng số chuyến bay từ cache hoặc database
                if (!_cache.TryGetValue("TotalFlights", out int totalFlights))
                {
                    totalFlights = await _context.Flights.CountAsync();
                    _cache.Set("TotalFlights", totalFlights, TimeSpan.FromMinutes(10));
                }

                var totalPages = (int)Math.Ceiling((double)totalFlights / pageSize);
                page = Math.Max(1, page);

              

                if (page == 1)
                {
                    
                }

               

                // Lấy danh sách chuyến bay
                var flights = await GetFlightsFromDatabaseAsync(page, pageSize, lastDepartureTime, filterDate, departureAirport, destinationAirport, airline);

                if (flights.Any())
                {
                   
                }

              

                if (!flights.Any())
                {
                    if (filterDate.HasValue)
                    {
                        TempData["InfoMessage"] = $"Không có chuyến bay vào ngày {filterDate.Value.ToString("dd/MM/yyyy")}. Vui lòng chọn ngày khác.";
                    }
                    else
                    {
                        TempData["InfoMessage"] = "Không có chuyến bay nào để hiển thị. Vui lòng đồng bộ dữ liệu.";
                    }
                }

                return View(flights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FutureFlightSyncController] Error in Index action while fetching flights.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách chuyến bay.";
                return View(new List<Flight>());
            }
        }

        private async Task<List<Flight>> GetFlightsFromDatabaseAsync(
            int pageNumber = 1,
            int pageSize = 50,
            DateTime? lastDepartureTime = null,
            DateTime? filterDate = null,
            string departureAirport = null,
            string destinationAirport = null,
            string airline = null)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("[FutureFlightSyncController] Starting GetFlightsFromDatabaseAsync at {Time}", startTime);

            try
            {
                _context.Database.SetCommandTimeout(60);

                IQueryable<Flight> query = _context.Flights
                    .AsNoTracking()
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.DestinationAirport);

                // Chỉ lấy các chuyến bay từ ngày hiện tại trở đi
                DateTime startDate = DateTime.UtcNow;

                if (filterDate.HasValue)
                {
                    startDate = filterDate.Value.Date;
                    DateTime endDate = startDate.AddDays(1);

                    // Kiểm tra xem có chuyến bay trong ngày được chọn không
                    var flightsInDay = await query
                        .Where(f => f.DepartureTime >= startDate && f.DepartureTime < endDate)
                        .ToListAsync();

                    if (!flightsInDay.Any())
                    {
                        // Nếu không có chuyến bay trong ngày, tìm chuyến bay gần nhất
                        var nearestFlightBefore = await query
                            .Where(f => f.DepartureTime < startDate)
                            .OrderByDescending(f => f.DepartureTime)
                            .FirstOrDefaultAsync();

                        var nearestFlightAfter = await query
                            .Where(f => f.DepartureTime >= endDate)
                            .OrderBy(f => f.DepartureTime)
                            .FirstOrDefaultAsync();

                        if (nearestFlightBefore != null || nearestFlightAfter != null)
                        {
                            string suggestion = "Gợi ý: ";
                            if (nearestFlightBefore != null)
                            {
                                suggestion += $"Chuyến bay gần nhất trước ngày {startDate.ToString("dd/MM/yyyy")} là vào {nearestFlightBefore.DepartureTime.ToString("dd/MM/yyyy HH:mm")} ";
                            }
                            if (nearestFlightAfter != null)
                            {
                                suggestion += nearestFlightBefore != null ? "và " : "";
                                suggestion += $"Chuyến bay gần nhất sau ngày {startDate.ToString("dd/MM/yyyy")} là vào {nearestFlightAfter.DepartureTime.ToString("dd/MM/yyyy HH:mm")}";
                            }
                            TempData["SuggestionMessage"] = suggestion;
                        }
                    }

                    query = query.Where(f => f.DepartureTime >= startDate && f.DepartureTime < endDate);
                }
                else
                {
                    query = query.Where(f => f.DepartureTime >= startDate);
                }

                // Bộ lọc nâng cao
                if (!string.IsNullOrEmpty(departureAirport))
                {
                    query = query.Where(f => f.DepartureAirport != null && f.DepartureAirport.Name.Contains(departureAirport, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(destinationAirport))
                {
                    query = query.Where(f => f.DestinationAirport != null && f.DestinationAirport.Name.Contains(destinationAirport, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(airline))
                {
                    query = query.Where(f => f.Airline.Contains(airline, StringComparison.OrdinalIgnoreCase));
                }

                query = query.OrderBy(f => f.DepartureTime);

                if (lastDepartureTime.HasValue)
                {
                    query = query.Where(f => f.DepartureTime > lastDepartureTime.Value);
                }

                var flights = await query
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("[FutureFlightSyncController] GetFlightsFromDatabaseAsync completed in {Duration} ms", (DateTime.UtcNow - startTime).TotalMilliseconds);
                return flights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FutureFlightSyncController] Error fetching flights from database.");
                return new List<Flight>();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncNow()
        {
            Console.WriteLine("[FutureFlightSyncController] SyncNow action triggered at {0}", DateTime.UtcNow);
            _logger.LogInformation("[FutureFlightSyncController] SyncNow action triggered at {Time}", DateTime.UtcNow);
            try
            {
                int initialFlightCount = await _context.Flights.CountAsync();
                await _futureFlightSyncService.SyncFlightsAsync();
                int finalFlightCount = await _context.Flights.CountAsync();

                if (finalFlightCount == initialFlightCount)
                {
                    TempData["SyncMessage"] = "Đồng bộ hoàn tất, nhưng không có chuyến bay mới được thêm. Kiểm tra Aviation Edge API hoặc dữ liệu nguồn.";
                }
                else
                {
                    TempData["SyncMessage"] = $"Đồng bộ dữ liệu chuyến bay thành công! Đã thêm {finalFlightCount - initialFlightCount} chuyến bay mới từ Aviation Edge API.";
                }
                _logger.LogInformation("[FutureFlightSyncController] SyncNow completed successfully.");
                Console.WriteLine("[FutureFlightSyncController] SyncNow completed successfully.");
            }
            catch (Exception ex)
            {
                TempData["SyncMessage"] = $"Lỗi khi đồng bộ dữ liệu (FutureFlightSyncService): {ex.Message}";
                _logger.LogError(ex, "[FutureFlightSyncController] Error in SyncNow action.");
                Console.WriteLine($"[FutureFlightSyncController] Error in SyncNow action: {ex.Message}");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult StopSync()
        {
            _logger.LogInformation("[FutureFlightSyncController] StopSync action triggered.");
            Console.WriteLine("[FutureFlightSyncController] StopSync action triggered.");
            TempData["SyncMessage"] = "Đã dừng đồng bộ dữ liệu.";
            return RedirectToAction("Index");
        }
    }

    // Extension methods để hỗ trợ session
  
}