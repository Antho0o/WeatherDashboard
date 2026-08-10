using Microsoft.AspNetCore.Mvc;
using WeatherDashboard.Services;

namespace WeatherDashboard.Controllers
{
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? city)
        {
            city = string.IsNullOrWhiteSpace(city)
                ? "Port Elizabeth"
                : city.Trim();

            ViewBag.SearchCity = city;

            // Prevent extremely long searches
            if (city.Length > 100)
            {
                ViewData["Error"] =
                    "Please enter a shorter city name.";

                return View();
            }

            var weather =
                await _weatherService.GetWeatherAsync(city);

            if (weather == null)
            {
                ViewData["Error"] =
                    $"Unable to find weather for \"{city}\".";

                return View();
            }

            return View(weather);
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}