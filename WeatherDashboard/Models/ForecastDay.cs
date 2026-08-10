namespace WeatherDashboard.Models
{
    public class ForecastDay
    {
        public DateTime Date { get; set; }


    public double MinTemperature { get; set; }

        public double MaxTemperature { get; set; }

        public string Description { get; set; } = "";

        public string Icon { get; set; } = "🌤️";
    }


}
