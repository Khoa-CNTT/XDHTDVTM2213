using Microsoft.AspNetCore.Mvc;
using FlightBookingApp.Data;
using FlightBookingApp.Models;
using FlightBookingApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightBookingApp.Controllers
{
    public class VietnamFlightSyncController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VietnamFlightDataService _vietnamFlightDataService;

        // Danh sách sân bay Việt Nam (đồng bộ với VietnamFlightDataService)
        private readonly string[] _vietnamAirports = new[]
        {
            "HAN", "SGN", "DAD", "PQC", "CXR", "HUI", "VII", "HPH", "UIH", "BMV",
            "VCA", "DIN", "VDO", "TBB", "CAH", "VCS", "VCL", "DLI", "PXU", "NHA",
            "PHA", "SQH", "VDH", "VKG"
        };

        public VietnamFlightSyncController(ApplicationDbContext context, VietnamFlightDataService vietnamFlightDataService)
        {
            _context = context;
            _vietnamFlightDataService = vietnamFlightDataService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 50, string filterDate = null, string departureAirport = null, string destinationAirport = null, string airline = null)
        {
            // Lưu các giá trị bộ lọc vào ViewBag để hiển thị lại trên giao diện
            ViewBag.FilterDate = filterDate;
            ViewBag.DepartureAirport = departureAirport;
            ViewBag.DestinationAirport = destinationAirport;
            ViewBag.Airline = airline;

            // Truy vấn danh sách chuyến bay, chỉ lấy các chuyến bay nội địa Việt Nam
            var query = _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.DestinationAirport)
                .Where(f => _vietnamAirports.Contains(f.DepartureAirport.IataCode) && _vietnamAirports.Contains(f.DestinationAirport.IataCode))
                .AsQueryable();

            // Áp dụng bộ lọc mặc định: chỉ hiển thị các chuyến bay trong 1 tháng tới nếu không có bộ lọc ngày
            if (string.IsNullOrEmpty(filterDate))
            {
                var defaultEndDate = DateTime.UtcNow.AddMonths(1);
                query = query.Where(f => f.DepartureTime >= DateTime.UtcNow && f.DepartureTime <= defaultEndDate);
            }

            // Áp dụng bộ lọc ngày
            if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out var selectedDate))
            {
                query = query.Where(f => f.DepartureTime.Date == selectedDate.Date);
            }

            // Áp dụng bộ lọc sân bay khởi hành
            if (!string.IsNullOrEmpty(departureAirport))
            {
                query = query.Where(f => f.DepartureAirport.Name.Contains(departureAirport, StringComparison.OrdinalIgnoreCase) ||
                                       f.DepartureAirport.IataCode.Contains(departureAirport, StringComparison.OrdinalIgnoreCase));
            }

            // Áp dụng bộ lọc sân bay đích
            if (!string.IsNullOrEmpty(destinationAirport))
            {
                query = query.Where(f => f.DestinationAirport.Name.Contains(destinationAirport, StringComparison.OrdinalIgnoreCase) ||
                                       f.DestinationAirport.IataCode.Contains(destinationAirport, StringComparison.OrdinalIgnoreCase));
            }

            // Áp dụng bộ lọc hãng hàng không
            if (!string.IsNullOrEmpty(airline))
            {
                query = query.Where(f => f.Airline.Contains(airline, StringComparison.OrdinalIgnoreCase));
            }

            // Tính tổng số bản ghi và số trang
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // Lấy dữ liệu cho trang hiện tại
            var flights = await query
                .OrderBy(f => f.DepartureTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lưu thông tin phân trang vào ViewBag
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = totalPages > 0 ? totalPages : 1;

            return View(flights);
        }

        [HttpPost]
        public async Task<IActionResult> SyncVietnamFlights()
        {
            var (success, message) = await _vietnamFlightDataService.FetchAndSaveVietnamFlightDataAsync();
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Index");
        }
    }
}