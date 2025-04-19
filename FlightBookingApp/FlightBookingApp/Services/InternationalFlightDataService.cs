using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FlightBookingApp.Data;
using FlightBookingApp.Models;

namespace FlightBookingApp.Services
{
    public class InternationalFlightDataService
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
            { "Singapore Airlines", 1.3m },
            { "Emirates", 1.5m },
            { "Qatar Airways", 1.4m },
            { "Korean Air", 1.2m },
            { "Japan Airlines", 1.3m },
            { "Cathay Pacific", 1.3m },
            { "United Airlines", 1.4m },
            { "Delta Air Lines", 1.4m },
            { "Air France", 1.3m },
            { "Lufthansa", 1.3m },
            { "Thai Airways", 1.1m },
            { "China Southern Airlines", 1.1m },
            { "British Airways", 1.3m },
            { "American Airlines", 1.4m },
            { "Turkish Airlines", 1.3m },
            { "Etihad Airways", 1.5m },
            { "ANA (All Nippon Airways)", 1.3m },
            { "EVA Air", 1.2m },
            { "Asiana Airlines", 1.2m },
            { "Air Canada", 1.3m },
            { "Qantas", 1.3m },
            { "LATAM Airlines", 1.2m },
            { "South African Airways", 1.2m },
            { "Aeroflot", 1.1m },
            { "Air India", 1.1m },
            { "Philippine Airlines", 1.1m },
            { "Garuda Indonesia", 1.1m },
            { "Malaysia Airlines", 1.2m },
            { "KLM Royal Dutch Airlines", 1.3m },
            { "Swiss International Air Lines", 1.3m },
            { "Iberia", 1.2m },
            { "Finnair", 1.2m },
            { "Austrian Airlines", 1.3m },
            { "Saudia", 1.2m },
            { "EgyptAir", 1.1m },
            { "Ethiopian Airlines", 1.1m },
            { "Unknown Airline", 1.0m }
        };

        // Danh sách sân bay Việt Nam có chuyến bay quốc tế
        private readonly List<string> _vietnamInternationalAirports = new List<string>
        {
            "HAN", "SGN", "DAD", "CXR", "PQC", "HPH", "VDO", "HUI", "VCA", "VII", "UIH", "DLI"
        };

        // Danh sách sân bay quốc tế lớn trên thế giới, phân chia theo khu vực
        private readonly Dictionary<string, List<string>> _internationalAirportsByRegion = new Dictionary<string, List<string>>
        {
            { "USA", new List<string> { "JFK", "LAX", "SFO", "ORD", "DFW", "ATL", "MIA", "SEA", "BOS", "IAH", "DEN", "PHX", "LAS", "MSP", "DTW" } }, // Mỹ
            { "Europe", new List<string> { "LHR", "CDG", "FRA", "AMS", "MUC", "ZRH", "MAD", "BCN", "FCO", "MXP", "VIE", "CPH", "OSL", "HEL", "DUB", "BRU", "LIS", "ATH", "PRG", "BUD" } }, // Châu Âu
            { "Asia", new List<string> { "ICN", "NRT", "HND", "KIX", "SIN", "HKG", "TPE", "BKK", "KUL", "PEK", "PVG", "CAN", "CTU", "SZX", "KMG", "DEL", "BOM", "MAA", "BLR", "HYD", "SGN", "HAN", "DAD" } }, // Châu Á
            { "MiddleEast", new List<string> { "DXB", "DOH", "IST", "AUH", "RUH", "JED", "AMM", "KWI", "BAH", "MCT" } }, // Trung Đông
            { "Australia", new List<string> { "SYD", "MEL", "BNE", "PER", "ADL", "CBR", "OOL" } }, // Úc
            { "SouthAmerica", new List<string> { "GRU", "EZE", "SCL", "BOG", "LIM", "GIG", "MVD", "UIO", "CCS" } }, // Nam Mỹ
            { "Africa", new List<string> { "JNB", "CPT", "LOS", "NBO", "CAI", "ADD", "ACC", "DAR", "TUN", "ALG" } }, // Châu Phi
            { "Canada", new List<string> { "YYZ", "YVR", "YUL", "YYC", "YEG", "YOW" } }, // Canada
            { "Russia", new List<string> { "SVO", "DME", "LED", "VVO", "KHV" } }, // Nga
            { "India", new List<string> { "DEL", "BOM", "MAA", "BLR", "HYD", "CCU", "AMD", "COK" } }, // Ấn Độ
            { "SoutheastAsia", new List<string> { "MNL", "CGK", "DPS", "SUB", "PEN", "RGN", "PNH", "VTE" } } // Đông Nam Á khác
        };

        // Danh sách tất cả sân bay quốc tế (dùng để kiểm tra và nối chuyến)
        private readonly List<string> _allInternationalAirports;

        // Danh sách hãng hàng không quốc tế
        private readonly List<string> _internationalAirlines = new List<string>
        {
            "Vietnam Airlines",
            "Vietjet Air",
            "Bamboo Airways",
            "Singapore Airlines",
            "Emirates",
            "Qatar Airways",
            "Korean Air",
            "Japan Airlines",
            "Cathay Pacific",
            "United Airlines",
            "Delta Air Lines",
            "Air France",
            "Lufthansa",
            "Thai Airways",
            "China Southern Airlines",
            "British Airways",
            "American Airlines",
            "Turkish Airlines",
            "Etihad Airways",
            "ANA (All Nippon Airways)",
            "EVA Air",
            "Asiana Airlines",
            "Air Canada",
            "Qantas",
            "LATAM Airlines",
            "South African Airways",
            "Aeroflot",
            "Air India",
            "Philippine Airlines",
            "Garuda Indonesia",
            "Malaysia Airlines",
            "KLM Royal Dutch Airlines",
            "Swiss International Air Lines",
            "Iberia",
            "Finnair",
            "Austrian Airlines",
            "Saudia",
            "EgyptAir",
            "Ethiopian Airlines"
        };

        // Tốc độ máy bay (km/h)
        private const double AVERAGE_FLIGHT_SPEED = 800.0; // 800 km/h
        // Thời gian cất cánh và hạ cánh (giờ)
        private const double TAKEOFF_LANDING_TIME = 0.5; // 30 phút
        // Thời gian dừng tại sân bay trung gian (giờ)
        private const double TRANSIT_TIME_MIN = 1.0; // 1 giờ
        private const double TRANSIT_TIME_MAX = 2.0; // 2 giờ
        // Các mốc phút cố định
        private readonly int[] _fixedMinutes = new int[] { 0, 15, 30, 45 };

        public InternationalFlightDataService(IConfiguration configuration, HttpClient httpClient, ApplicationDbContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            // Tạo danh sách tất cả sân bay quốc tế từ các khu vực
            _allInternationalAirports = _internationalAirportsByRegion.Values.SelectMany(x => x).Distinct().ToList();
        }

        public async Task<(bool Success, string Message)> FetchAndSaveInternationalFlightDataAsync()
        {
            Console.WriteLine("[InternationalFlightDataService] FetchAndSaveInternationalFlightDataAsync started.");
            try
            {
                // Kiểm tra và xóa các bản ghi có DepartureTime hoặc ArrivalTime không hợp lệ
                var invalidFlights = await _context.Flights
                    .FromSqlRaw("SELECT * FROM Flights WHERE ISDATE(DepartureTime) = 0 OR ISDATE(ArrivalTime) = 0")
                    .ToListAsync();

                if (invalidFlights.Any())
                {
                    Console.WriteLine($"[InternationalFlightDataService] Found {invalidFlights.Count} flights with invalid DepartureTime or ArrivalTime. Removing them...");
                    _context.Flights.RemoveRange(invalidFlights);
                    await _context.SaveChangesAsync();
                }

                // Lấy danh sách sân bay và hãng hàng không từ database
                var airportIds = await _context.Airports
                    .ToDictionaryAsync(a => a.IataCode, a => a.AirportId);

                var airlines = await _context.Airlines
                    .ToDictionaryAsync(a => a.Name, a => a.AirlineId, StringComparer.OrdinalIgnoreCase);

                // Kiểm tra xem tất cả sân bay có trong database không
                foreach (var airport in _vietnamInternationalAirports.Concat(_allInternationalAirports))
                {
                    if (!airportIds.ContainsKey(airport))
                    {
                        Console.WriteLine($"[InternationalFlightDataService] Airport {airport} not found in database.");
                        return (false, $"Airport {airport} not found in database.");
                    }
                }

                // Tạo lịch chuyến bay quốc tế
                int totalProcessed = await GenerateInternationalFlightScheduleAsync(airportIds, airlines);
                Console.WriteLine($"[InternationalFlightDataService] Total international flights processed and saved: {totalProcessed}");
                return (true, $"Successfully synchronized {totalProcessed} new international flights.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InternationalFlightDataService] Error generating international flight data: {ex.Message}");
                Console.WriteLine($"[InternationalFlightDataService] Stack trace: {ex.StackTrace}");
                return (false, $"Error generating international flight data: {ex.Message}");
            }
        }

        private async Task<int> GenerateInternationalFlightScheduleAsync(Dictionary<string, int> airportIds, Dictionary<string, int> airlines)
        {
            var flightsToAdd = new List<Flight>();
            DateTime startDate = new DateTime(2025, 4, 14, 0, 0, 0, DateTimeKind.Utc); // Bắt đầu từ 14/04/2025
            DateTime endDate = startDate.AddMonths(7); // 14/08/2025

            // Danh sách các cặp sân bay đã sử dụng trong ngày để tránh trùng lặp
            var dailyFlightPairs = new Dictionary<DateTime, HashSet<(string, string)>>();
            var allDestinations = new HashSet<(string, string)>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (!dailyFlightPairs.ContainsKey(date.Date))
                {
                    dailyFlightPairs[date.Date] = new HashSet<(string, string)>();
                }
                var currentDayPairs = dailyFlightPairs[date.Date];

                foreach (var departureAirportCode in _vietnamInternationalAirports)
                {
                    int departureAirportId = airportIds[departureAirportCode];

                    // Mỗi sân bay VN sẽ có chuyến bay đến TẤT CẢ sân bay lớn ở Mỹ, và một số sân bay khác
                    var destinations = new List<string>();
                    // Đảm bảo bay đến tất cả sân bay ở Mỹ
                    destinations.AddRange(_internationalAirportsByRegion["USA"]);
                    // Thêm một số sân bay ngẫu nhiên từ các khu vực khác
                    foreach (var region in _internationalAirportsByRegion.Keys.Where(k => k != "USA"))
                    {
                        var regionAirports = _internationalAirportsByRegion[region];
                        destinations.AddRange(regionAirports.OrderBy(x => _random.Next()).Take(3)); // Lấy 3 sân bay mỗi khu vực
                    }

                    foreach (var destinationAirportCode in destinations)
                    {
                        int destinationAirportId = airportIds[destinationAirportCode];
                        int numberOfFlights = _random.Next(5, 11); // 5-10 chuyến mỗi ngày từ mỗi sân bay VN đến mỗi sân bay quốc tế

                        for (int i = 0; i < numberOfFlights; i++)
                        {
                            string airline = _internationalAirlines[_random.Next(_internationalAirlines.Count)];
                            int? airlineId = airlines.ContainsKey(airline) ? airlines[airline] : (int?)null;

                            // Tạo thời gian khởi hành với phút cố định (00, 15, 30, 35)
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
                                Console.WriteLine($"[InternationalFlightDataService] Skipping flight from {departureAirportCode} to {destinationAirportCode} at {departureTime:dd/MM/yyyy HH:mm} as it already exists.");
                                continue;
                            }

                            // Quyết định xem chuyến bay có nối chuyến hay không (50% khả năng nối chuyến)
                            bool hasTransit = _random.NextDouble() > 0.5;
                            int stops = hasTransit ? 1 : 0;
                            double totalFlightHours;
                            DateTime arrivalTime;
                            string transitAirportCode = null;

                            if (!hasTransit)
                            {
                                // Chuyến bay thẳng
                                double distanceKm = await CalculateDistanceAsync(departureAirportCode, destinationAirportCode);
                                totalFlightHours = distanceKm / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;
                                arrivalTime = departureTime.AddHours(totalFlightHours);
                            }
                            else
                            {
                                // Chuyến bay nối chuyến
                                transitAirportCode = _allInternationalAirports
                                    .Where(a => a != departureAirportCode && a != destinationAirportCode)
                                    .OrderBy(x => _random.Next())
                                    .First();
                                int transitAirportId = airportIds[transitAirportCode];

                                // Tính thời gian từ sân bay khởi hành đến sân bay trung gian
                                double distanceToTransit = await CalculateDistanceAsync(departureAirportCode, transitAirportCode);
                                double hoursToTransit = distanceToTransit / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;

                                // Thời gian dừng tại sân bay trung gian (1-2 giờ)
                                double transitHours = TRANSIT_TIME_MIN + _random.NextDouble() * (TRANSIT_TIME_MAX - TRANSIT_TIME_MIN);

                                // Tính thời gian từ sân bay trung gian đến sân bay đích
                                double distanceFromTransit = await CalculateDistanceAsync(transitAirportCode, destinationAirportCode);
                                double hoursFromTransit = distanceFromTransit / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;

                                // Tổng thời gian
                                totalFlightHours = hoursToTransit + transitHours + hoursFromTransit;
                                arrivalTime = departureTime.AddHours(totalFlightHours);
                            }

                            decimal price = CalculatePriceBasedOnDistanceAndAirline(totalFlightHours * AVERAGE_FLIGHT_SPEED, airline);

                            // Thêm chuyến bay outbound (VN → Quốc tế)
                            var outboundFlight = new Flight
                            {
                                FlightNumber = $"{airline.Substring(0, 2).ToUpper()}{_random.Next(1000, 9999)}",
                                DepartureAirportId = departureAirportId,
                                DestinationAirportId = destinationAirportId,
                                DepartureTime = departureTime,
                                ArrivalTime = arrivalTime,
                                Airline = airline,
                                AirlineId = airlineId,
                                Price = price,
                                AvailableSeats = 100,
                                Stops = stops,
                                Status = "Scheduled"
                            };
                            flightsToAdd.Add(outboundFlight);

                            // Thêm chuyến bay khứ hồi (Quốc tế → VN)
                            int returnRandomHour = _random.Next(0, 24);
                            int returnRandomMinute = _fixedMinutes[_random.Next(_fixedMinutes.Length)];
                            DateTime returnDepartureTime = arrivalTime.Date.AddDays(_random.Next(1, 3)).AddHours(returnRandomHour).AddMinutes(returnRandomMinute);
                            DateTime returnArrivalTime;

                            if (!hasTransit)
                            {
                                // Chuyến bay thẳng
                                returnArrivalTime = returnDepartureTime.AddHours(totalFlightHours);
                            }
                            else
                            {
                                // Chuyến bay nối chuyến (theo cùng sân bay trung gian)
                                double distanceToTransit = await CalculateDistanceAsync(destinationAirportCode, transitAirportCode);
                                double hoursToTransit = distanceToTransit / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;

                                double transitHours = TRANSIT_TIME_MIN + _random.NextDouble() * (TRANSIT_TIME_MAX - TRANSIT_TIME_MIN);

                                double distanceFromTransit = await CalculateDistanceAsync(transitAirportCode, departureAirportCode);
                                double hoursFromTransit = distanceFromTransit / AVERAGE_FLIGHT_SPEED + TAKEOFF_LANDING_TIME;

                                double returnTotalFlightHours = hoursToTransit + transitHours + hoursFromTransit;
                                returnArrivalTime = returnDepartureTime.AddHours(returnTotalFlightHours);
                            }

                            bool returnFlightExists = await _context.Flights
                                .AnyAsync(f => f.DepartureAirportId == destinationAirportId &&
                                               f.DestinationAirportId == departureAirportId &&
                                               f.DepartureTime == returnDepartureTime);

                            if (returnFlightExists)
                            {
                                Console.WriteLine($"[InternationalFlightDataService] Skipping return flight from {destinationAirportCode} to {departureAirportCode} at {returnDepartureTime:dd/MM/yyyy HH:mm} as it already exists.");
                                continue;
                            }

                            var returnFlight = new Flight
                            {
                                FlightNumber = $"{airline.Substring(0, 2).ToUpper()}{_random.Next(1000, 9999)}",
                                DepartureAirportId = destinationAirportId,
                                DestinationAirportId = departureAirportId,
                                DepartureTime = returnDepartureTime,
                                ArrivalTime = returnArrivalTime,
                                Airline = airline,
                                AirlineId = airlineId,
                                Price = price,
                                AvailableSeats = 100,
                                Stops = stops,
                                Status = "Scheduled"
                            };
                            flightsToAdd.Add(returnFlight);

                            // Thêm cặp sân bay vào danh sách đã sử dụng trong ngày
                            currentDayPairs.Add((departureAirportCode, destinationAirportCode));
                            allDestinations.Add((departureAirportCode, destinationAirportCode));
                        }
                    }
                }

                if (flightsToAdd.Count > 1000)
                {
                    await _context.Flights.AddRangeAsync(flightsToAdd);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[InternationalFlightDataService] Saved {flightsToAdd.Count} flights for date {date:dd/MM/yyyy}");
                    flightsToAdd.Clear();
                }
            }

            if (flightsToAdd.Any())
            {
                await _context.Flights.AddRangeAsync(flightsToAdd);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[InternationalFlightDataService] Saved {flightsToAdd.Count} remaining flights");
            }

            return flightsToAdd.Count;
        }

        private async Task<double> CalculateDistanceAsync(string departureAirportCode, string destinationAirportCode)
        {
            var departureAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == departureAirportCode);
            var destinationAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == destinationAirportCode);

            if (departureAirport == null || destinationAirport == null)
            {
                Console.WriteLine($"[InternationalFlightDataService] Could not calculate distance: Departure ({departureAirportCode}) or Destination ({destinationAirportCode}) airport not found.");
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

            Console.WriteLine($"[InternationalFlightDataService] Distance from {departureAirportCode} to {destinationAirportCode}: {distance:F2} km");
            return distance;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private decimal CalculatePriceBasedOnDistanceAndAirline(double distance, string airline)
        {
            decimal basePrice = 2000000m; // Giá cơ bản cho chuyến bay quốc tế cao hơn
            decimal pricePerKm = 3000m; // Giá mỗi km cao hơn cho chuyến quốc tế
            decimal baseDistancePrice = basePrice + (decimal)distance * pricePerKm;
            decimal airlineFactor = _airlinePriceFactors.ContainsKey(airline) ? _airlinePriceFactors[airline] : 1.0m;
            decimal finalPrice = Math.Max(3000000m, baseDistancePrice * airlineFactor);

            // Làm tròn giá tiền về mức nghìn đồng gần nhất
            finalPrice = Math.Round(finalPrice / 1000) * 1000;

            Console.WriteLine($"[InternationalFlightDataService] Calculated price for {airline} (distance: {distance} km): {finalPrice} VND");
            return finalPrice;
        }
    }
}