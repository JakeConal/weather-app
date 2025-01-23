using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using weather_app.Commands;
using weather_app.Models;

namespace weather_app.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private string selectedCity;
        private WeatherData currentWeatherData;
        private ForecastResponse forecast;
        private List<Hour> selectedDayHours;
        private SeriesCollection hourlySeriesCollection;
        private List<string> hourlyLabels;
        private SeriesCollection rainProbabilitySeriesCollection;
        private List<string> rainProbabilityLabels;
        private double latitude;
        private double longitude;
        private string mapUrl;

        public List<string> Cities { get; set; }
        public ICommand PowerCommand { get; }
        public ICommand MinimizedCommand { get; }
        public ICommand MaximizedCommand { get; }
        public ICommand SelectDayCommand { get; }

        public string SelectedCity
        {
            get => selectedCity;
            set
            {
                selectedCity = value;
                GetCurrentWeather();
                GetDailyForecast();
                OnPropertyChanged();
            }
        }

        public WeatherData CurrentWeatherData
        {
            get => currentWeatherData;
            set
            {
                currentWeatherData = value;
                OnPropertyChanged();
                if (currentWeatherData?.Location != null)
                {
                    Latitude = currentWeatherData.Location.Lat;
                    Longitude = currentWeatherData.Location.Lon;
                }
            }
        }

        public ForecastResponse Forecast
        {
            get => forecast;
            set
            {
                forecast = value;
                OnPropertyChanged();
                if (forecast?.Forecast?.ForecastDays != null && forecast.Forecast.ForecastDays.Count > 0)
                {
                    SelectedDayHours = forecast.Forecast.ForecastDays[0].Hour;
                    UpdateRainProbabilityChart();
                }
            }
        }

        public List<Hour> SelectedDayHours
        {
            get => selectedDayHours;
            set
            {
                selectedDayHours = value;
                UpdateHourlyChart();
                OnPropertyChanged();
            }
        }

        public SeriesCollection HourlySeriesCollection
        {
            get => hourlySeriesCollection;
            set
            {
                hourlySeriesCollection = value;
                OnPropertyChanged();
            }
        }

        public List<string> HourlyLabels
        {
            get => hourlyLabels;
            set
            {
                hourlyLabels = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection RainProbabilitySeriesCollection
        {
            get => rainProbabilitySeriesCollection;
            set
            {
                rainProbabilitySeriesCollection = value;
                OnPropertyChanged();
            }
        }

        public List<string> RainProbabilityLabels
        {
            get => rainProbabilityLabels;
            set
            {
                rainProbabilityLabels = value;
                OnPropertyChanged();
            }
        }

        public double Latitude
        {
            get => latitude;
            set
            {
                latitude = value;
                UpdateMapUrl();
                OnPropertyChanged();
            }
        }

        public double Longitude
        {
            get => longitude;
            set
            {
                longitude = value;
                UpdateMapUrl();
                OnPropertyChanged();
            }
        }

        public string MapUrl
        {
            get => mapUrl;
            set
            {
                mapUrl = value;
                OnPropertyChanged();
            }
        }

        private void UpdateMapUrl()
        {
            string gridHeight = "241";
            string gridWidth = "342";
            MapUrl = $"https://www.bing.com/maps/embed/viewer.aspx?v=3&h={gridHeight}&w={gridWidth}&cp={Latitude}~{Longitude}&lvl=7";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel()
        {
            SelectedCity = "Hanoi";
            Cities = new List<string>
                    {
                        "Hanoi", "Ho Chi Minh City", "Da Nang", "Hai Phong", "Can Tho", "Nha Trang", "Hue", "Vung Tau", "Quy Nhon", "Da Lat",
                        "London", "New York", "Tokyo", "Sydney", "Paris", "Berlin", "Moscow", "Beijing", "Mumbai",
                        "Los Angeles", "Cairo", "Rio de Janeiro", "Toronto", "Dubai", "Singapore", "Bangkok", "Istanbul", "Seoul", "Mexico City"
                    };
            PowerCommand = new RelayCommand(_ => Application.Current.Shutdown());
            MinimizedCommand = new RelayCommand(_ => Application.Current.MainWindow.WindowState = WindowState.Minimized);
            MaximizedCommand = new RelayCommand(_ => ToggleWindowState());
            SelectDayCommand = new RelayCommand(param => SelectedDayHours = ((ForecastDay)param).Hour);

            GetCurrentWeather();
            GetDailyForecast();
        }

        private void ToggleWindowState()
        {
            Application.Current.MainWindow.WindowState =
                Application.Current.MainWindow.WindowState == WindowState.Maximized ?
                WindowState.Normal :
                WindowState.Maximized;
        }

        private async void GetCurrentWeather()
        {
            string baseUrl = "http://api.weatherapi.com/v1";
            string apiMethod = "/current.json";
            string apiKey = "a946d343e1df44018ff72855251601";

            string url = $"{baseUrl}{apiMethod}?key={apiKey}&q={SelectedCity}";

            using (HttpClient client = new HttpClient())
            {
                string response = await client.GetStringAsync(url);
                CurrentWeatherData = JsonConvert.DeserializeObject<WeatherData>(response);
                OnPropertyChanged(nameof(CurrentWeatherData));
            }
        }

        private async void GetDailyForecast()
        {
            string baseUrl = "http://api.weatherapi.com/v1";
            string apiMethod = "/forecast.json";
            string apiKey = "a946d343e1df44018ff72855251601";

            string url = $"{baseUrl}{apiMethod}?key={apiKey}&q={SelectedCity}&days=7";

            using (HttpClient client = new HttpClient())
            {
                string response = await client.GetStringAsync(url);
                Forecast = JsonConvert.DeserializeObject<ForecastResponse>(response);
                OnPropertyChanged(nameof(Forecast));
            }
        }

        private void UpdateHourlyChart()
        {
            if (SelectedDayHours == null) return;

            var values = new ChartValues<double>();
            var labels = new List<string>();

            foreach (var hour in SelectedDayHours)
            {
                values.Add(hour.TempC);
                labels.Add(DateTime.Parse(hour.Time).ToString("HH:mm"));
            }

            HourlySeriesCollection = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Temperature",
                            Values = values
                        }
                    };

            HourlyLabels = labels;
        }

        private void UpdateRainProbabilityChart()
        {
            if (Forecast?.Forecast?.ForecastDays == null) return;

            var values = new ChartValues<int>();
            var labels = new List<string>();

            foreach (var day in Forecast.Forecast.ForecastDays)
            {
                values.Add(day.Day.DailyChanceOfRain);
                labels.Add(DateTime.Parse(day.Date).ToString("ddd"));
            }

            RainProbabilitySeriesCollection = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Chance of Rain",
                            Values = values
                        }
                    };

            RainProbabilityLabels = labels;
        }
    }
}
