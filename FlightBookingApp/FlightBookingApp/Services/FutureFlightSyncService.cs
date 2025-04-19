using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FlightBookingApp.Services
{
    public class FutureFlightSyncService
    {
        private readonly FutureFlightDataService _futureFlightDataService;
        private readonly ILogger<FutureFlightSyncService> _logger;

        public FutureFlightSyncService(FutureFlightDataService futureFlightDataService, ILogger<FutureFlightSyncService> logger)
        {
            _futureFlightDataService = futureFlightDataService;
            _logger = logger;
        }

        public async Task SyncFlightsAsync()
        {
            _logger.LogInformation("[FutureFlightSyncService] Starting flight sync using Aviation Edge API...");
            Console.WriteLine("[FutureFlightSyncService] Starting flight sync using Aviation Edge API...");
            try
            {
                await _futureFlightDataService.FetchAndSaveAirportDataAsync();
                await _futureFlightDataService.FetchAndSaveGlobalFlightDataAsync();
                await _futureFlightDataService.CleanOldFlightsAsync();
                _logger.LogInformation("[FutureFlightSyncService] Flight sync completed successfully using Aviation Edge API.");
                Console.WriteLine("[FutureFlightSyncService] Flight sync completed successfully using Aviation Edge API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FutureFlightSyncService] Error during flight sync.");
                Console.WriteLine($"[FutureFlightSyncService] Error during flight sync: {ex.Message}");
                throw;
            }
        }
    }
}