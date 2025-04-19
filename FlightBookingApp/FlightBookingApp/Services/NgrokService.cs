
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
namespace FlightBookingApp.Services
{


    public class NgrokService
    {
        private readonly HttpClient _httpClient;

        public NgrokService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetNgrokPublicUrlAsync()
        {
            try
            {
                // Gọi API của ngrok để lấy danh sách tunnels
                var response = await _httpClient.GetAsync("http://127.0.0.1:4040/api/tunnels");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var tunnels = doc.RootElement.GetProperty("tunnels");

                // Lấy URL công khai từ tunnel đầu tiên (thường là HTTPS)
                foreach (var tunnel in tunnels.EnumerateArray())
                {
                    var publicUrl = tunnel.GetProperty("public_url").GetString();
                    if (!string.IsNullOrEmpty(publicUrl) && publicUrl.StartsWith("https"))
                    {
                        return publicUrl;
                    }
                }

                throw new Exception("Không tìm thấy URL ngrok công khai.");
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy URL ngrok: " + ex.Message, ex);
            }
        }
    }
}
