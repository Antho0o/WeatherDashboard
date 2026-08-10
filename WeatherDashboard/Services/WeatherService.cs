using System.Globalization;
using System.Text.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(
            HttpClient httpClient,
            ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.Timeout =
                TimeSpan.FromSeconds(15);

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "WeatherDashboard/1.0"
                );
            }
        }

        public async Task<WeatherViewModel?> GetWeatherAsync(
            string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            city = city.Trim();

            try
            {
                // ==========================================
                // STEP 1 - FIND CITY
                // ==========================================

                var geocodeUrl =
                    "https://geocoding-api.open-meteo.com/v1/search" +
                    $"?name={Uri.EscapeDataString(city)}" +
                    "&count=1" +
                    "&language=en" +
                    "&format=json";

                using var geocodeResponse =
                    await _httpClient.GetAsync(geocodeUrl);

                if (!geocodeResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Geocoding request failed with status {StatusCode}",
                        geocodeResponse.StatusCode
                    );

                    return null;
                }

                var geocodeJson =
                    await geocodeResponse.Content.ReadAsStringAsync();

                using var geocodeDocument =
                    JsonDocument.Parse(geocodeJson);

                var geocodeRoot =
                    geocodeDocument.RootElement;

                if (!geocodeRoot.TryGetProperty(
                        "results",
                        out var results) ||
                    results.ValueKind != JsonValueKind.Array ||
                    results.GetArrayLength() == 0)
                {
                    return null;
                }

                var location = results[0];

                // ==========================================
                // LOCATION
                // ==========================================

                if (!location.TryGetProperty(
                        "latitude",
                        out var latitudeProperty) ||
                    !location.TryGetProperty(
                        "longitude",
                        out var longitudeProperty))
                {
                    return null;
                }

                var latitude =
                    latitudeProperty.GetDouble();

                var longitude =
                    longitudeProperty.GetDouble();

                var areaName =
                    location.TryGetProperty(
                        "name",
                        out var nameProperty)
                        ? nameProperty.GetString() ?? city
                        : city;

                var country =
                    location.TryGetProperty(
                        "country",
                        out var countryProperty)
                        ? countryProperty.GetString() ?? ""
                        : "";

                // ==========================================
                // STEP 2 - GET WEATHER
                // ==========================================

                var weatherUrl =
                    "https://api.open-meteo.com/v1/forecast" +
                    $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                    $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                    "&current=temperature_2m," +
                    "relative_humidity_2m," +
                    "apparent_temperature," +
                    "weather_code," +
                    "wind_speed_10m," +
                    "surface_pressure," +
                    "is_day" +
                    "&daily=weather_code," +
                    "temperature_2m_max," +
                    "temperature_2m_min" +
                    "&timezone=auto" +
                    "&forecast_days=7";

                using var weatherResponse =
                    await _httpClient.GetAsync(weatherUrl);

                if (!weatherResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Weather request failed with status {StatusCode}",
                        weatherResponse.StatusCode
                    );

                    return null;
                }

                var weatherJson =
                    await weatherResponse.Content.ReadAsStringAsync();

                using var weatherDocument =
                    JsonDocument.Parse(weatherJson);

                var root =
                    weatherDocument.RootElement;

                // ==========================================
                // CURRENT WEATHER
                // ==========================================

                if (!root.TryGetProperty(
                        "current",
                        out var current))
                {
                    return null;
                }

                var temperature =
                    current
                        .GetProperty("temperature_2m")
                        .GetDouble();

                var feelsLike =
                    current
                        .GetProperty("apparent_temperature")
                        .GetDouble();

                var humidity =
                    current
                        .GetProperty("relative_humidity_2m")
                        .GetInt32();

                var windSpeed =
                    current
                        .GetProperty("wind_speed_10m")
                        .GetDouble();

                var pressure =
                    current
                        .GetProperty("surface_pressure")
                        .GetDouble();

                var weatherCode =
                    current
                        .GetProperty("weather_code")
                        .GetInt32();

                var isDay =
                    current
                        .GetProperty("is_day")
                        .GetInt32() == 1;

                // ==========================================
                // CREATE WEATHER MODEL
                // ==========================================

                var weather =
                    new WeatherViewModel
                    {
                        City = areaName,

                        Country = country,

                        Temperature =
                            Math.Round(temperature, 1),

                        FeelsLike =
                            Math.Round(feelsLike, 1),

                        Description =
                            GetWeatherDescription(
                                weatherCode),

                        Humidity =
                            Math.Clamp(humidity, 0, 100),

                        WindSpeed =
                            Math.Round(
                                Math.Max(0, windSpeed),
                                1
                            ),

                        Pressure =
                            Math.Round(
                                Math.Max(0, pressure),
                                0
                            ),

                        IsDay = isDay,

                        Icon =
                            GetWeatherIcon(
                                weatherCode,
                                isDay
                            )
                    };

                // ==========================================
                // FORECAST
                // ==========================================

                if (!root.TryGetProperty(
                        "daily",
                        out var daily))
                {
                    return weather;
                }

                var dates =
                    daily.GetProperty("time");

                var maxTemperatures =
                    daily.GetProperty(
                        "temperature_2m_max"
                    );

                var minTemperatures =
                    daily.GetProperty(
                        "temperature_2m_min"
                    );

                var weatherCodes =
                    daily.GetProperty(
                        "weather_code"
                    );

                var forecastCount =
                    Math.Min(
                        dates.GetArrayLength(),
                        Math.Min(
                            maxTemperatures.GetArrayLength(),
                            Math.Min(
                                minTemperatures.GetArrayLength(),
                                weatherCodes.GetArrayLength()
                            )
                        )
                    );

                // ==========================================
                // CREATE FORECAST
                // ==========================================

                for (
                    var i = 0;
                    i < forecastCount;
                    i++
                )
                {
                    if (!DateTime.TryParse(
                            dates[i].GetString(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var date))
                    {
                        continue;
                    }

                    var minTemperature =
                        minTemperatures[i].GetDouble();

                    var maxTemperature =
                        maxTemperatures[i].GetDouble();

                    var forecastCode =
                        weatherCodes[i].GetInt32();

                    weather.Forecast.Add(
                        new ForecastDay
                        {
                            Date = date,

                            MinTemperature =
                                Math.Round(
                                    minTemperature,
                                    1
                                ),

                            MaxTemperature =
                                Math.Round(
                                    maxTemperature,
                                    1
                                ),

                            Description =
                                GetWeatherDescription(
                                    forecastCode
                                ),

                            Icon =
                                GetWeatherIcon(
                                    forecastCode,
                                    true
                                )
                        }
                    );
                }

                return weather;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning(
                    "Weather request timed out for {City}",
                    city
                );

                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while getting weather for {City}",
                    city
                );

                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received for {City}",
                    city
                );

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected weather error for {City}",
                    city
                );

                return null;
            }
        }


        // ==========================================
        // WEATHER DESCRIPTION
        // ==========================================

        private string GetWeatherDescription(
            int code)
        {
            return code switch
            {
                0 =>
                    "Clear sky",

                1 =>
                    "Mainly clear",

                2 =>
                    "Partly cloudy",

                3 =>
                    "Overcast",

                45 or 48 =>
                    "Fog",

                51 =>
                    "Light drizzle",

                53 =>
                    "Moderate drizzle",

                55 =>
                    "Dense drizzle",

                56 =>
                    "Light freezing drizzle",

                57 =>
                    "Dense freezing drizzle",

                61 =>
                    "Slight rain",

                63 =>
                    "Moderate rain",

                65 =>
                    "Heavy rain",

                66 =>
                    "Light freezing rain",

                67 =>
                    "Heavy freezing rain",

                71 =>
                    "Slight snow",

                73 =>
                    "Moderate snow",

                75 =>
                    "Heavy snow",

                77 =>
                    "Snow grains",

                80 =>
                    "Slight rain showers",

                81 =>
                    "Moderate rain showers",

                82 =>
                    "Violent rain showers",

                85 =>
                    "Slight snow showers",

                86 =>
                    "Heavy snow showers",

                95 =>
                    "Thunderstorm",

                96 =>
                    "Thunderstorm with slight hail",

                99 =>
                    "Thunderstorm with heavy hail",

                _ =>
                    "Unknown weather"
            };
        }


        // ==========================================
        // WEATHER ICON
        // ==========================================

        private string GetWeatherIcon(
            int code,
            bool isDay)
        {
            return code switch
            {
                0 =>
                    isDay
                        ? "☀️"
                        : "🌙",

                1 or 2 =>
                    isDay
                        ? "🌤️"
                        : "☁️",

                3 =>
                    "☁️",

                45 or 48 =>
                    "🌫️",

                51 or
                53 or
                55 or
                56 or
                57 =>
                    "🌦️",

                61 or
                63 or
                65 or
                66 or
                67 or
                80 or
                81 or
                82 =>
                    "🌧️",

                71 or
                73 or
                75 or
                77 or
                85 or
                86 =>
                    "❄️",

                95 or
                96 or
                99 =>
                    "⛈️",

                _ =>
                    "🌤️"
            };
        }
    }
}