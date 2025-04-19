using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlightBookingApp.Data; // Namespace của DbContext
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; // Để serialize/deserialize JSON
using System;

namespace FlightBookingApp.Controllers
{
    public class FlightDetailsController : Controller
    {
        private readonly ApplicationDbContext _context; // DbContext của bạn
        private readonly ILogger<FlightDetailsController> _logger;
        

        public FlightDetailsController(ApplicationDbContext context, ILogger<FlightDetailsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int flightId)
        {
            _logger.LogInformation("Lấy chi tiết chuyến bay ID: {FlightId}", flightId);

            try
            {
                // Lấy thông tin chuyến bay từ cơ sở dữ liệu
                var flight = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.DestinationAirport)
                    .Include(f => f.AirlineNavigation)
                    .FirstOrDefaultAsync(f => f.FlightId == flightId);

                if (flight == null)
                {
                    _logger.LogWarning("Không tìm thấy chuyến bay: {FlightId}", flightId);
                    TempData["Error"] = "Không tìm thấy chuyến bay.";
                    return RedirectToAction("ReturnToSearchResults", "Home");
                }

                // Tạo mô tả chi tiết bằng OpenAI
                string description = await GenerateFlightDescription(flight);

                // Tạo view model
                var viewModel = new
                {
                    Flight = flight,
                    Description = description
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết chuyến bay ID: {FlightId}", flightId);
                TempData["Error"] = "Có lỗi xảy ra khi tải chi tiết chuyến bay.";
                return RedirectToAction("ReturnToSearchResults", "Home");
            }
        }

        private async Task<string> GenerateFlightDescription(FlightBookingApp.Models.Flight flight)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OPENAI_API_KEY);

                    var requestBody = new
                    {
                        model = "gpt-4o-mini",
                        messages = new[]
                        {
                            new { role = "system", content = "Bạn là một trợ lý thông minh của ViVu Airline, chuyên cung cấp mô tả chi tiết và hấp dẫn về các chuyến bay." },
                            new { role = "user", content = $@"Tạo một mô tả chi tiết và sáng tạo (khoảng 150-200 từ) về chuyến bay dựa trên thông tin sau:
- Hãng bay: {flight.Airline}
- Mã chuyến bay: {flight.FlightNumber}
- Khởi hành: {flight.DepartureAirport.City} ({flight.DepartureAirport.IataCode}) lúc {flight.DepartureTime:HH:mm, dd/MM/yyyy}
- Đến: {flight.DestinationAirport.City} ({flight.DestinationAirport.IataCode}) lúc {flight.ArrivalTime:HH:mm, dd/MM/yyyy}
- Thời gian bay: {(flight.ArrivalTime - flight.DepartureTime).TotalHours:F1} giờ
- Điểm dừng: {(flight.Stops == 0 ? "Bay thẳng" : $"{flight.Stops} điểm dừng")}

Mô tả cần:
- Nêu rõ là bay thẳng hoặc liệt kê các thành phố dừng chân giả tưởng (nếu có điểm dừng).
- Bao gồm chi tiết thực tế về trải nghiệm chuyến bay (ví dụ: loại máy bay, dịch vụ trên chuyến).
- Nhấn mạnh danh tiếng hoặc đặc điểm của hãng bay.
- Đề cập đến sức hấp dẫn của thành phố khởi hành và đích đến.
- Thu hút và hơi quảng cáo nhưng phải thực tế.
Không được mâu thuẫn với dữ liệu đã cung cấp. Viết bằng tiếng Việt." }
                        },
                        max_tokens = 300,
                        temperature = 0.7
                    };

                    var jsonContent = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("Phản hồi từ OpenAI API: {Response}", responseString);

                    dynamic result = JsonConvert.DeserializeObject(responseString);

                    if (result == null || result.error != null)
                    {
                      
                        return $"Lỗi từ API: {result?.error?.message ?? "Không thể kết nối tới máy chủ."}";
                    }

                    if (result?.choices == null || result.choices.Count == 0)
                    {
                        _logger.LogWarning("Không nhận được phản hồi từ OpenAI API.");
                        return "Không nhận được phản hồi từ API. Vui lòng thử lại.";
                    }

                    return result.choices[0].message.content.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mô tả chuyến bay ID: {FlightId}, Message: {Message}, InnerException: {InnerException}, StackTrace: {StackTrace}",
                    flight.FlightId, ex.Message, ex.InnerException?.Message, ex.StackTrace);
                return "Không thể tạo mô tả chi tiết do lỗi hệ thống. Vui lòng thử lại sau.";
            }
        }
    }
}