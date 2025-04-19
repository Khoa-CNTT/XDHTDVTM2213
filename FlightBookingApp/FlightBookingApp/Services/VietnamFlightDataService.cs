using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FlightBookingApp.Data;
using FlightBookingApp.Models;

namespace FlightBookingApp.Services
{
    public class VietnamFlightDataService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();
        private readonly Dictionary<string, decimal> _airlinePriceFactors = new Dictionary<string, decimal>
        {
            { "Vietnam Airlines", 1.2m },
            { "Vietjet Air", 0.9m },
            { "Bamboo Airways", 1.0m },
            { "Pacific Airlines", 0.95m },
            { "Vietravel Airlines", 0.85m },
            { "Unknown Airline", 1.0m }
        };

        // Danh sách sân bay Việt Nam (khớp với bảng Airports)
        private readonly List<string> _vietnamAirports = new List<string>
        {
            "HAN", "SGN", "DAD", "PQC", "CXR", "HUI", "VII", "HPH", "UIH", "BMV",
            "VCA", "DIN", "VDO", "TBB", "CAH", "VCS", "VCL", "DLI", "PXU", "NHA",
            "PHA", "SQH", "VDH", "VKG"
        };

        // Danh sách hãng hàng không Việt Nam
        private readonly List<string> _vietnamAirlines = new List<string>
        {
            "Vietnam Airlines",
            "Vietjet Air",
            "Bamboo Airways",
            "Pacific Airlines",
            "Vietravel Airlines"
        };

        // Tốc độ máy bay (km/h)
        private const double AVERAGE_FLIGHT_SPEED = 800.0; // 800 km/h
        // Thời gian cất cánh và hạ cánh (giờ)
        private const double TAKEOFF_LANDING_TIME = 0.5; // 30 phút
        // Thời gian bay tối thiểu (giờ)
        private const double MINIMUM_FLIGHT_TIME = 0.5; // Giảm xuống 30 phút
        // Các mốc phút cố định
        private readonly int[] _fixedMinutes = new int[] { 0, 15, 30, 45 };

        public VietnamFlightDataService(IConfiguration configuration, HttpClient httpClient, ApplicationDbContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<(bool Success, string Message)> FetchAndSaveVietnamFlightDataAsync()
        {
            Console.WriteLine("[VietnamFlightDataService] FetchAndSaveVietnamFlightDataAsync started.");
            try
            {
                // Kiểm tra và xóa các bản ghi có DepartureTime hoặc ArrivalTime không hợp lệ
                var invalidFlights = await _context.Flights
                    .FromSqlRaw("SELECT * FROM Flights WHERE ISDATE(DepartureTime) = 0 OR ISDATE(ArrivalTime) = 0")
                    .ToListAsync();

                if (invalidFlights.Any())
                {
                    Console.WriteLine($"[VietnamFlightDataService] Found {invalidFlights.Count} flights with invalid DepartureTime or ArrivalTime. Removing them...");
                    _context.Flights.RemoveRange(invalidFlights);
                    await _context.SaveChangesAsync();
                }

                // Lấy danh sách sân bay và hãng hàng không từ database
                var airportIds = await _context.Airports
                    .ToDictionaryAsync(a => a.IataCode, a => a.AirportId);

                var airlines = await _context.Airlines
                    .ToDictionaryAsync(a => a.Name, a => a.AirlineId, StringComparer.OrdinalIgnoreCase);

                // Kiểm tra xem tất cả sân bay có trong database không
                foreach (var airport in _vietnamAirports)
                {
                    if (!airportIds.ContainsKey(airport))
                    {
                        Console.WriteLine($"[VietnamFlightDataService] Airport {airport} not found in database.");
                        return (false, $"Airport {airport} not found in database.");
                    }
                }

                // Tạo lịch chuyến bay cho 6 tháng tới
                int totalProcessed = await GenerateVietnamFlightScheduleAsync(airportIds, airlines);
                Console.WriteLine($"[VietnamFlightDataService] Total flights processed and saved for Vietnam airports: {totalProcessed}");
                return (true, $"Successfully synchronized {totalProcessed} new flights for Vietnam airports.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VietnamFlightDataService] Error generating Vietnam flight data: {ex.Message}");
                Console.WriteLine($"[VietnamFlightDataService] Stack trace: {ex.StackTrace}");
                return (false, $"Error generating Vietnam flight data: {ex.Message}");
            }
        }

        private async Task<int> GenerateVietnamFlightScheduleAsync(Dictionary<string, int> airportIds, Dictionary<string, int> airlines)
        {
            var flightsToAdd = new List<Flight>();
            DateTime startDate = new DateTime(2025, 10, 23, 0, 0, 0, DateTimeKind.Utc); // Bắt đầu từ 12/04/2025
            DateTime endDate = startDate.AddMonths(4); // 12/10/2025

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var departureAirportCode in _vietnamAirports)
                {
                    int departureAirportId = airportIds[departureAirportCode];

                    foreach (var destinationAirportCode in _vietnamAirports)
                    {
                        if (departureAirportCode == destinationAirportCode) continue;

                        int destinationAirportId = airportIds[destinationAirportCode];
                        int numberOfFlights = _random.Next(2, 4);

                        for (int i = 0; i < numberOfFlights; i++)
                        {
                            string airline = _vietnamAirlines[_random.Next(_vietnamAirlines.Count)];
                            int? airlineId = airlines.ContainsKey(airline) ? airlines[airline] : (int?)null;

                            // Tạo thời gian khởi hành với phút cố định (00, 15, 30, 45)
                            int randomHour = _random.Next(0, 24);
                            int randomMinute = _fixedMinutes[_random.Next(_fixedMinutes.Length)];
                            DateTime departureTime = new DateTime(date.Year, date.Month, date.Day, randomHour, randomMinute, 0, DateTimeKind.Utc);

                            // Kiểm tra xem đã có chuyến bay nào trong cùng cặp sân bay với cùng thời gian khởi hành chưa
                            bool flightExists = await _context.Flights
                                .AnyAsync(f => f.DepartureAirportId == departureAirportId &&
                                               f.DestinationAirportId == destinationAirportId &&
                                               f.DepartureTime == departureTime);

                            if (flightExists)
                            {
                                Console.WriteLine($"[VietnamFlightDataService] Skipping flight from {departureAirportCode} to {destinationAirportCode} at {departureTime:dd/MM/yyyy HH:mm} as it already exists.");
                                continue;
                            }

                            // Tính khoảng cách và thời gian bay
                            double distanceKm = await CalculateDistanceAsync(departureAirportCode, destinationAirportCode);
                            double flightHours = distanceKm / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;
                            flightHours = Math.Max(MINIMUM_FLIGHT_TIME, flightHours); // Đảm bảo thời gian bay tối thiểu
                            DateTime arrivalTime = departureTime.AddHours(flightHours);

                            decimal price = CalculatePriceBasedOnDistanceAndAirline(distanceKm, airline);

                            var outboundFlight = new Flight
                            {
                                FlightNumber = $"VN{_random.Next(1000, 9999)}",
                                DepartureAirportId = departureAirportId,
                                DestinationAirportId = destinationAirportId,
                                DepartureTime = departureTime,
                                ArrivalTime = arrivalTime,
                                Airline = airline,
                                AirlineId = airlineId,
                                Price = price,
                                AvailableSeats = 100,
                                Stops = distanceKm > 2000 ? _random.Next(0, 2) : 0,
                                Status = "Scheduled"
                            };
                            flightsToAdd.Add(outboundFlight);

                            // Chuyến khứ hồi
                            int returnRandomHour = _random.Next(0, 24);
                            int returnRandomMinute = _fixedMinutes[_random.Next(_fixedMinutes.Length)];
                            DateTime returnDepartureTime = arrivalTime.Date.AddDays(_random.Next(0, 2)).AddHours(returnRandomHour).AddMinutes(returnRandomMinute);
                            if (returnDepartureTime <= arrivalTime)
                            {
                                returnDepartureTime = returnDepartureTime.AddDays(1);
                            }
                            DateTime returnArrivalTime = returnDepartureTime.AddHours(flightHours); // Sử dụng cùng thời gian bay

                            bool returnFlightExists = await _context.Flights
                                .AnyAsync(f => f.DepartureAirportId == destinationAirportId &&
                                               f.DestinationAirportId == departureAirportId &&
                                               f.DepartureTime == returnDepartureTime);

                            if (returnFlightExists)
                            {
                                Console.WriteLine($"[VietnamFlightDataService] Skipping return flight from {destinationAirportCode} to {departureAirportCode} at {returnDepartureTime:dd/MM/yyyy HH:mm} as it already exists.");
                                continue;
                            }

                            var returnFlight = new Flight
                            {
                                FlightNumber = $"VN{_random.Next(1000, 9999)}",
                                DepartureAirportId = destinationAirportId,
                                DestinationAirportId = departureAirportId,
                                DepartureTime = returnDepartureTime,
                                ArrivalTime = returnArrivalTime,
                                Airline = airline,
                                AirlineId = airlineId,
                                Price = price,
                                AvailableSeats = 100,
                                Stops = distanceKm > 2000 ? _random.Next(0, 2) : 0,
                                Status = "Scheduled"
                            };
                            flightsToAdd.Add(returnFlight);
                        }
                    }

                    if (flightsToAdd.Count > 1000)
                    {
                        await _context.Flights.AddRangeAsync(flightsToAdd);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"[VietnamFlightDataService] Saved {flightsToAdd.Count} flights for date {date:dd/MM/yyyy}");
                        flightsToAdd.Clear();
                    }
                }
            }

            if (flightsToAdd.Any())
            {
                await _context.Flights.AddRangeAsync(flightsToAdd);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[VietnamFlightDataService] Saved {flightsToAdd.Count} remaining flights");
            }

            return flightsToAdd.Count;
        }

        private async Task<double> CalculateDistanceAsync(string departureAirportCode, string destinationAirportCode)
        {
            var departureAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == departureAirportCode);
            var destinationAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == destinationAirportCode);

            if (departureAirport == null || destinationAirport == null)
            {
                Console.WriteLine($"[VietnamFlightDataService] Could not calculate distance: Departure ({departureAirportCode}) or Destination ({destinationAirportCode}) airport not found.");
                return 1000; // Giá trị mặc định nếu không tìm thấy sân bay
            }

            double lat1 = departureAirport.Latitude;
            double lon1 = departureAirport.Longitude;
            double lat2 = destinationAirport.Latitude;
            double lon2 = destinationAirport.Longitude;

            const double R = 6371; // Bán kính Trái Đất (km)
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;

            Console.WriteLine($"[VietnamFlightDataService] Distance from {departureAirportCode} to {destinationAirportCode}: {distance:F2} km");
            return distance;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private decimal CalculatePriceBasedOnDistanceAndAirline(double distance, string airline)
        {
            decimal basePrice = 1000000m;
            decimal pricePerKm = 2000m;
            decimal baseDistancePrice = basePrice + (decimal)distance * pricePerKm;
            decimal airlineFactor = _airlinePriceFactors.ContainsKey(airline) ? _airlinePriceFactors[airline] : 1.0m;
            decimal finalPrice = Math.Max(1500000m, baseDistancePrice * airlineFactor);

            // Làm tròn giá tiền về mức nghìn đồng gần nhất
            finalPrice = Math.Round(finalPrice / 1000) * 1000;

            Console.WriteLine($"[VietnamFlightDataService] Calculated price for {airline} (distance: {distance} km): {finalPrice} VND");
            return finalPrice;
        }
    }
}