namespace WeatherDashboard.Models
{
    public class WeatherViewModel
    {
        public string City { get; set; } = "";

    public string Country { get; set; } = "";

        public double Temperature { get; set; }

        public double FeelsLike { get; set; }

        public string Description { get; set; } = "";

        public string Icon { get; set; } = "🌤️";

        public int Humidity { get; set; }

        public double WindSpeed { get; set; }

        public double Pressure { get; set; }

        public bool IsDay { get; set; }

        public List<ForecastDay> Forecast { get; set; } = new();
    }

}
