namespace FlightBookingApp.Models
{
    public class StatisticsSummary
    {
        public int Id { get; set; }
        public int CompletedFlights { get; set; }
        public int ActiveFlights { get; set; }
        public int CanceledFlights { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalVisits { get; set; }
        public int VisitsToday { get; set; }
        public int TotalTickets { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}