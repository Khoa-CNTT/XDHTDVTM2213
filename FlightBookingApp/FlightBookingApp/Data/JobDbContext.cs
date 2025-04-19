using Microsoft.EntityFrameworkCore;

namespace FlightBookingApp.Data
{
    public class JobDbContext : ApplicationDbContext
    {
        public JobDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(300); // Tăng timeout lên 300 giây (5 phút)
        }
    }
}