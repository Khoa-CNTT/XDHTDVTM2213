using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FlightBookingApp.Controllers
{
    public class NewsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static DateTime _lastFetchTime = DateTime.MinValue;
        private static List<NewsArticle> _cachedNews = new List<NewsArticle>();
        private const int RefreshIntervalHours = 6;

        public NewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            // Check if we need to refresh the news (every 6 hours)
            if (DateTime.UtcNow.Subtract(_lastFetchTime).TotalHours >= RefreshIntervalHours || !_cachedNews.Any())
            {
                try
                {
                    var newsArticles = await FetchNewsAsync();
                    _cachedNews = newsArticles;
                    _lastFetchTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    // Log the error (you can use your existing logger here)
                    Console.WriteLine($"Error fetching news: {ex.Message}");
                    // If fetching fails, return the cached news (if available)
                    if (!_cachedNews.Any())
                    {
                        TempData["Error"] = "Không thể tải tin tức. Vui lòng thử lại sau.";
                    }
                }
            }

            return View(_cachedNews);
        }

        private async Task<List<NewsArticle>> FetchNewsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiKey = "60bc1a20b8af4cdcadad95c3f8f0e5ac";

            // 1. Lấy 25 tin tức từ Việt Nam
            var vietnamNews = await GetNewsFromApi(client, apiKey, "Vietnam OR aviation OR airlines OR travel OR flight");
            vietnamNews = vietnamNews.Take(25).ToList(); // Giới hạn 25 tin tức từ Việt Nam

            // 2. Lấy 25 tin tức từ thế giới
            var globalNews = await GetNewsFromApi(client, apiKey, "aviation OR airlines OR travel OR flight");
            globalNews = globalNews.Take(25).ToList(); // Giới hạn 25 tin tức từ thế giới

            // Kết hợp kết quả
            var combinedNews = vietnamNews.Concat(globalNews).ToList();

            return combinedNews;
        }

        private async Task<List<NewsArticle>> GetNewsFromApi(HttpClient client, string apiKey, string query)
        {
            var queryParams = new Dictionary<string, string>
    {
        { "q", query },
        { "apiKey", apiKey },
        { "language", "vi" }, 
        { "sortBy", "publishedAt" }
    };

            var uriBuilder = new UriBuilder("https://newsapi.org/v2/everything")
            {
                Query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync()
            };

            client.DefaultRequestHeaders.Add("User-Agent", "FlightBookingApp/1.0 (lluat91@gmail.com)");

            try
            {
                var response = await client.GetAsync(uriBuilder.ToString());

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error fetching news: {errorMessage}");
                    throw new Exception($"Failed to fetch news from News API. Status Code: {response.StatusCode}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                var articles = new List<NewsArticle>();
                if (root.GetProperty("status").GetString() == "ok")
                {
                    foreach (var article in root.GetProperty("articles").EnumerateArray())
                    {
                        articles.Add(new NewsArticle
                        {
                            Title = article.TryGetProperty("title", out var title) ? title.GetString() : "No Title",
                            Description = article.TryGetProperty("description", out var desc) ? desc.GetString() : "No Description",
                            Url = article.TryGetProperty("url", out var urlValue) ? urlValue.GetString() : "#",
                            ImageUrl = article.TryGetProperty("urlToImage", out var img) ? img.GetString() : null,
                            PublishedAt = article.TryGetProperty("publishedAt", out var date) ? DateTime.Parse(date.GetString()) : DateTime.UtcNow,
                            Source = article.GetProperty("source").TryGetProperty("name", out var source) ? source.GetString() : "Unknown Source"
                        });
                    }
                }
                else
                {
                    Console.WriteLine("Error: API response status not OK.");
                    throw new Exception("Failed to fetch valid news.");
                }

                return articles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching news from API: {ex.Message}");
                return new List<NewsArticle>(); // Return empty list to avoid breaking the app
            }
        }

    }


    // Model for news articles
    public class NewsArticle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Source { get; set; }
    }
}