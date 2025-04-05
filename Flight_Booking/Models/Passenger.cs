using System.ComponentModel.DataAnnotations;

namespace FlightBookingApp.Models
{
    public class Passenger
    {
        public int PassengerId { get; set; }
        public int BookingId { get; set; }
        public string? FullName { get; set; } // Cho phép NULL
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // Cho phép NULL
        public string? IdType { get; set; } // Cho phép NULL
        public DateTime? IdExpiry { get; set; }
        public string? IdCountry { get; set; } // Cho phép NULL
        public string? Nationality { get; set; } // Cho phép NULL
        public decimal? LuggageFee { get; set; }

        public Booking Booking { get; set; }
    }
}