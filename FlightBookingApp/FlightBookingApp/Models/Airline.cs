namespace FlightBookingApp.Models
{
    public class Airline
    {
        public int AirlineId { get; set; }
        public string? Name { get; set; }
        public string? IataCode { get; set; } // Thêm cột IataCode
        public string? LogoUrl { get; set; }
    }
}
