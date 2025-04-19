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
using System.IO;

namespace FlightBookingApp.Services
{
    public class FutureFlightDataService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();
        private readonly Dictionary<string, (string Name, string City, string Country)> _airportData;

        private readonly Dictionary<string, decimal> _airlinePriceFactors = new Dictionary<string, decimal>
        {
            { "Vietnam Airlines", 1.2m }, { "Vietjet Air", 0.9m }, { "Bamboo Airways", 1.0m },
            { "Delta Air Lines", 1.3m }, { "American Airlines", 1.4m }, { "Emirates", 1.5m },
            { "Unknown Airline", 1.0m }
        };

        private readonly string _apiKey = "1364b0-0897cd"; // API key của bạn
        private readonly string _baseUrl = "http://aviation-edge.com/v2/public/flightsFuture"; // Base URL của Aviation Edge Future Schedules API
        private readonly List<string> _airports = new List<string> { "HAN", "SGN", "DAD", "PQC", "CXR", "HUI", "VII", "HPH", "UIH", "BMV" }; // Danh sách sân bay Việt Nam

        public FutureFlightDataService(IConfiguration configuration, HttpClient httpClient, ApplicationDbContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            _airportData = LoadAirportData();
        }

        private Dictionary<string, (string Name, string City, string Country)> LoadAirportData()
        {
            var airportMap = new Dictionary<string, (string Name, string City, string Country)>();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "airports.dat");
            if (!File.Exists(path))
            {
                Console.WriteLine($"[FutureFlightDataService] File airports.dat not found at {path}");
                return airportMap;
            }

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 5)
                {
                    string iata = parts[4].Trim('"');
                    string name = parts[1].Trim('"');
                    string city = parts[2].Trim('"');
                    string country = parts[3].Trim('"');
                    if (iata != "\\N" && !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country) && !airportMap.ContainsKey(iata))
                    {
                        string normalizedCity = iata switch
                        {
                            "HAN" => "Hà Nội",
                            "DAD" => "Đà Nẵng",
                            "SGN" => "TP. Hồ Chí Minh",
                            "DAL" => "Đà Lạt",
                            "NHA" => "Nha Trang",
                            "HUI" => "Huế",
                            "VII" => "Vinh",
                            "PQC" => "Phú Quốc",
                            "CXR" => "Cam Ranh",
                            "VCA" => "Cần Thơ",
                            "DIN" => "Điện Biên Phủ",
                            "VDO" => "Vân Đồn",
                            "HPH" => "Hải Phòng",
                            "UIH" => "Quy Nhơn",
                            "TBB" => "Tuy Hòa",
                            "BMV" => "Buôn Ma Thuột",
                            "CAH" => "Cà Mau",
                            "VCS" => "Côn Đảo",
                            "VCL" => "Chu Lai",
                            "THD" => "Thanh Hóa",
                            _ => city
                        };
                        string normalizedCountry = country;
                        airportMap[iata] = (name, normalizedCity, normalizedCountry);
                    }
                }
            }
            Console.WriteLine($"[FutureFlightDataService] Loaded {airportMap.Count} airports from airports.dat");
            return airportMap;
        }

        public async Task FetchAndSaveAirportDataAsync()
        {
            Console.WriteLine("[FutureFlightDataService] FetchAndSaveAirportDataAsync started.");
            try
            {
                string apiKey = _configuration["AviationEdge:ApiKey"] ?? _apiKey;
                string baseUrl = _configuration["AviationEdge:BaseUrl"] ?? "http://aviation-edge.com";

                string airportUrl = $"{baseUrl}/v2/public/airportDatabase?key={apiKey}";
                Console.WriteLine($"[FutureFlightDataService] Calling Airport API: {airportUrl}");
                var airportResponse = await _httpClient.GetAsync(airportUrl);
                if (!airportResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[FutureFlightDataService] Airport API call failed: {airportResponse.StatusCode} - {airportResponse.ReasonPhrase}");
                    await AddFakeAirportIfEmptyAsync();
                    return;
                }

                string airportJson = await airportResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[FutureFlightDataService] Raw JSON response (first 500 chars): {airportJson.Substring(0, Math.Min(airportJson.Length, 500))}");
                var airportData = JsonDocument.Parse(airportJson).RootElement;

                if (airportData.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine($"[FutureFlightDataService] Invalid airport response: Not an array.");
                    await AddFakeAirportIfEmptyAsync();
                    return;
                }

                var airportsToAdd = new List<Airport>();
                foreach (var airport in airportData.EnumerateArray())
                {
                    string? iataCode = null;
                    try
                    {
                        iataCode = GetJsonProperty(airport, "codeIataAirport")?.GetString();
                        if (string.IsNullOrEmpty(iataCode)) continue;

                        string? nameFromApi = GetJsonProperty(airport, "nameAirport")?.GetString();
                        string? cityFromApi = GetJsonProperty(airport, "nameCity")?.GetString();
                        string? countryFromApi = GetJsonProperty(airport, "nameCountry")?.GetString();
                        double latitude = GetDoubleFromJsonElement(airport.GetProperty("latitudeAirport"));
                        double longitude = GetDoubleFromJsonElement(airport.GetProperty("longitudeAirport"));

                        string derivedName;
                        string derivedCity;
                        string derivedCountry;
                        if (_airportData.TryGetValue(iataCode, out var airportInfo))
                        {
                            derivedName = airportInfo.Name;
                            derivedCity = airportInfo.City;
                            derivedCountry = airportInfo.Country;
                        }
                        else
                        {
                            derivedName = nameFromApi ?? "Unknown Airport";
                            derivedCity = cityFromApi ?? (nameFromApi != null ?
                                nameFromApi.Replace(" International", "").Replace(" Airport", "").Trim() : "Unknown City");
                            derivedCountry = countryFromApi ?? "Unknown Country";
                        }

                        Console.WriteLine($"[FutureFlightDataService] Airport {iataCode}: NameFromAPI={nameFromApi}, DerivedName={derivedName}, CityFromAPI={cityFromApi}, DerivedCity={derivedCity}, CountryFromAPI={countryFromApi}, DerivedCountry={derivedCountry}");

                        var existingAirport = await _context.Airports
                            .FirstOrDefaultAsync(a => a.IataCode == iataCode);

                        if (existingAirport == null)
                        {
                            var newAirport = new Airport
                            {
                                IataCode = iataCode,
                                Name = derivedName,
                                City = derivedCity,
                                Country = derivedCountry,
                                Latitude = latitude,
                                Longitude = longitude
                            };
                            airportsToAdd.Add(newAirport);
                        }
                        else
                        {
                            existingAirport.Name = derivedName;
                            existingAirport.City = derivedCity;
                            existingAirport.Country = derivedCountry;
                            existingAirport.Latitude = latitude;
                            existingAirport.Longitude = longitude;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FutureFlightDataService] Error processing airport {iataCode ?? "unknown"}: {ex.Message}");
                    }
                }

                if (airportsToAdd.Any())
                {
                    await _context.Airports.AddRangeAsync(airportsToAdd);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[FutureFlightDataService] Total airports saved: {airportsToAdd.Count}");
                }
                else
                {
                    Console.WriteLine("[FutureFlightDataService] No new airports to save.");
                    await AddFakeAirportIfEmptyAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FutureFlightDataService] Error in FetchAndSaveAirportDataAsync: {ex.Message}");
                await AddFakeAirportIfEmptyAsync();
            }
        }

        private async Task AddFakeAirportIfEmptyAsync()
        {
            if (!await _context.Airports.AnyAsync())
            {
                Console.WriteLine("[FutureFlightDataService] Airports table is empty. Adding a fake airport.");
                var fakeAirport = new Airport
                {
                    IataCode = "FAKE",
                    Name = "Fake Airport",
                    City = "Fake City",
                    Country = "Fake Country",
                    Latitude = 0,
                    Longitude = 0
                };
                _context.Airports.Add(fakeAirport);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[FutureFlightDataService] Added fake airport with ID {fakeAirport.AirportId}.");
            }
        }

        public async Task FetchAndSaveGlobalFlightDataAsync()
        {
            Console.WriteLine("[FutureFlightDataService] FetchAndSaveGlobalFlightDataAsync started.");
            try
            {
                var airportIds = await _context.Airports
                    .ToDictionaryAsync(a => a.IataCode, a => a.AirportId);

                if (!airportIds.Any())
                {
                    Console.WriteLine("[FutureFlightDataService] No airports found in database. Syncing airports first...");
                    await FetchAndSaveAirportDataAsync();
                    airportIds = await _context.Airports
                        .ToDictionaryAsync(a => a.IataCode, a => a.AirportId);
                }

                if (!airportIds.Any())
                {
                    Console.WriteLine("[FutureFlightDataService] No airports retrieved. Aborting flight data fetch.");
                    return;
                }

                Console.WriteLine($"[FutureFlightDataService] Found {airportIds.Count} airports in database.");

                int totalProcessed = 0;
                DateTime now = DateTime.UtcNow.Date;
                DateTime endDate = now.AddMonths(12); // Lấy dữ liệu cho 12 tháng tới

                foreach (var airportCode in _airports)
                {
                    if (!airportIds.ContainsKey(airportCode))
                    {
                        Console.WriteLine($"[FutureFlightDataService] Airport {airportCode} not found in database. Skipping.");
                        continue;
                    }

                    totalProcessed += await FetchAndSaveSchedulesForAirportAsync(airportCode, "departure", airportIds, now, endDate);
                    totalProcessed += await FetchAndSaveSchedulesForAirportAsync(airportCode, "arrival", airportIds, now, endDate);
                }

                Console.WriteLine($"[FutureFlightDataService] Total flights processed and saved: {totalProcessed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FutureFlightDataService] Error fetching global flight data: {ex.Message}");
            }
        }

        private async Task<int> FetchAndSaveSchedulesForAirportAsync(string airportCode, string type, Dictionary<string, int> airportIds, DateTime startDate, DateTime endDate)
        {
            int flightsSaved = 0;

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string formattedDate = date.ToString("yyyy-MM-dd");
                string url = $"{_baseUrl}?key={_apiKey}&type={type}&iataCode={airportCode}&date={formattedDate}";

                try
                {
                    Console.WriteLine($"[FutureFlightDataService] Fetching {type} schedules for {airportCode} on {formattedDate}...");
                    var response = await _httpClient.GetStringAsync(url);
                    using var document = JsonDocument.Parse(response);
                    var root = document.RootElement;

                    if (!root.EnumerateArray().Any())
                    {
                        Console.WriteLine($"[FutureFlightDataService] No {type} schedules found for {airportCode} on {formattedDate}.");
                        continue;
                    }

                    flightsSaved += await SaveTimetableToDatabaseAsync(root, airportCode, type, airportIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FutureFlightDataService] Error fetching {type} schedules for {airportCode} on {formattedDate}: {ex.Message}");
                }
            }

            return flightsSaved;
        }

        private async Task<int> SaveTimetableToDatabaseAsync(JsonElement data, string airportCode, string type, Dictionary<string, int> airportIds)
        {
            var flightsToAdd = new List<Flight>();
            int airportId = airportIds[airportCode];
            int departureAirportId = type == "departure" ? airportId : 0;
            int destinationAirportId = type == "arrival" ? airportId : 0;

            foreach (var flight in data.EnumerateArray())
            {
                try
                {
                    string? flightNumber = GetJsonProperty(flight, "flight", "iataNumber")?.GetString();
                    if (string.IsNullOrEmpty(flightNumber))
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight from/to {airportCode}: Missing flight number, skipping.");
                        continue;
                    }

                    string? otherAirportCode = type == "departure"
                        ? GetJsonProperty(flight, "arrival", "iataCode")?.GetString()
                        : GetJsonProperty(flight, "departure", "iataCode")?.GetString();

                    if (string.IsNullOrEmpty(otherAirportCode) || !airportIds.ContainsKey(otherAirportCode))
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight {flightNumber} from/to {airportCode}: Invalid other airport {otherAirportCode}, skipping.");
                        continue;
                    }

                    if (type == "departure")
                    {
                        destinationAirportId = airportIds[otherAirportCode];
                    }
                    else
                    {
                        departureAirportId = airportIds[otherAirportCode];
                    }

                    string? airline = GetJsonProperty(flight, "airline", "name")?.GetString() ?? "Unknown Airline";
                    string? status = GetJsonProperty(flight, "status")?.GetString() ?? "Scheduled";

                    string? departureTimeStr = GetJsonProperty(flight, "departure", "scheduledTime")?.GetString();
                    if (string.IsNullOrEmpty(departureTimeStr) || !DateTime.TryParse(departureTimeStr, out var departureTime))
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight {flightNumber} from/to {airportCode}: Missing or invalid DepartureTime, skipping.");
                        continue;
                    }
                    departureTime = departureTime.ToUniversalTime();

                    string? arrivalTimeStr = GetJsonProperty(flight, "arrival", "scheduledTime")?.GetString();
                    if (string.IsNullOrEmpty(arrivalTimeStr) || !DateTime.TryParse(arrivalTimeStr, out var arrivalTime))
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight {flightNumber} from/to {airportCode}: Missing or invalid ArrivalTime, skipping.");
                        continue;
                    }
                    arrivalTime = arrivalTime.ToUniversalTime();

                    if (arrivalTime <= departureTime)
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight {flightNumber} from/to {airportCode}: ArrivalTime ({arrivalTime}) is not greater than DepartureTime ({departureTime}), skipping.");
                        continue;
                    }

                    // Kiểm tra trùng lặp dựa trên nơi đi, nơi đến, thời gian khởi hành và hãng hàng không
                    bool flightExists = await _context.Flights.AnyAsync(f =>
                        f.DepartureAirportId == departureAirportId &&
                        f.DestinationAirportId == destinationAirportId &&
                        f.DepartureTime == departureTime &&
                        f.Airline == airline);

                    if (flightExists)
                    {
                        Console.WriteLine($"[FutureFlightDataService] Flight from {departureAirportId} to {destinationAirportId} at {departureTime} with airline {airline} already exists, skipping.");
                        continue;
                    }

                    decimal price = CalculatePriceBasedOnDistanceAndAirline(await CalculateDistanceAsync(airportCode, otherAirportCode), airline);
                    int stops = (await CalculateDistanceAsync(airportCode, otherAirportCode)) > 2000 ? _random.Next(0, 2) : 0;

                    var newFlight = new Flight
                    {
                        FlightNumber = flightNumber,
                        DepartureAirportId = departureAirportId,
                        DestinationAirportId = destinationAirportId,
                        DepartureTime = departureTime,
                        ArrivalTime = arrivalTime,
                        Airline = airline,
                        Price = price,
                        AvailableSeats = 100,
                        Stops = stops,
                        Status = status
                    };

                    flightsToAdd.Add(newFlight);
                    Console.WriteLine($"[FutureFlightDataService] Added flight {flightNumber} from/to {airportCode}, Departure: {departureTime}, Arrival: {arrivalTime}, Price: {price} VND");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FutureFlightDataService] Error processing flight from/to {airportCode}: {ex.Message}");
                }
            }

            if (flightsToAdd.Any())
            {
                try
                {
                    Console.WriteLine($"[FutureFlightDataService] Saving {flightsToAdd.Count} flights to database...");
                    await _context.Flights.AddRangeAsync(flightsToAdd);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[FutureFlightDataService] Successfully saved {flightsToAdd.Count} flights from/to {airportCode}");
                    return flightsToAdd.Count;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FutureFlightDataService] Error saving flights to database: {ex.Message}");
                    return 0;
                }
            }

            Console.WriteLine($"[FutureFlightDataService] No new flights to save for {airportCode}");
            return 0;
        }

        public async Task CleanOldFlightsAsync()
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                var oldFlights = await _context.Flights
                    .Where(f => f.DepartureTime < now)
                    .ToListAsync();

                if (oldFlights.Any())
                {
                    _context.Flights.RemoveRange(oldFlights);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[FutureFlightDataService] Removed {oldFlights.Count} old flights.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FutureFlightDataService] Error cleaning old flights: {ex.Message}");
            }
        }

        private async Task<double> CalculateDistanceAsync(string departureAirportCode, string destinationAirportCode)
        {
            var departureAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == departureAirportCode);
            var destinationAirport = await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == destinationAirportCode);

            if (departureAirport == null || destinationAirport == null)
            {
                Console.WriteLine($"[FutureFlightDataService] Could not calculate distance: Departure ({departureAirportCode}) or Destination ({destinationAirportCode}) airport not found.");
                return 1000;
            }

            double lat1 = departureAirport.Latitude;
            double lon1 = departureAirport.Longitude;
            double lat2 = destinationAirport.Latitude;
            double lon2 = destinationAirport.Longitude;

            const double R = 6371;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
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
            Console.WriteLine($"[FutureFlightDataService] Calculated price for {airline} (distance: {distance} km): {finalPrice}");
            return finalPrice;
        }

        private JsonElement? GetJsonProperty(JsonElement element, params string[] propertyPath)
        {
            JsonElement current = element;
            foreach (var prop in propertyPath)
            {
                if (!current.TryGetProperty(prop, out current)) return null;
            }
            return current;
        }

        private double GetDoubleFromJsonElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
                return element.TryGetDouble(out double value) ? value : 0;
            if (element.ValueKind == JsonValueKind.String)
                return double.TryParse(element.GetString(), out double value) ? value : 0;
            return 0;
        }
    }
}