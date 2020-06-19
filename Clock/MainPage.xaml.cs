using Clock.Weather;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Clock
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DispatcherTimer dispatcherTimer;
        readonly string largeMapUrl = @"https://maps.darksky.net/@radar,38.890,-76.347,8";
        readonly string smallMapUrl = @"https://maps.darksky.net/@radar,39.278,-76.475,11";
        WeatherService weatherService;
        public MainPage()
        {
            this.InitializeComponent();
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.TryEnterFullScreenMode();
            weatherService = new WeatherService("0ecd9b91c35ca6a4e69d6e2bfbd04b96", "-76.4533", "39.3086");
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            UpdateClock();
            UpdateWeather();
        }

        private async void UpdateWeather()
        {
            main.Visibility = Visibility.Collapsed;
            var forecast = await weatherService.GetForecastAsync();
            Temp.Text = forecast.Current.Temperature.ToString();
            FeelsLike.Text = forecast.Current.FeelsLike.ToString();
            Humidity.Text = forecast.Current.Humidity.ToString();
            Wind.Text = $"{(int)forecast.Current.WindSpeed}{forecast.Current.WindDirection}";
            CurrentConditions.Text = forecast.Current.WeatherDescription;
            var when = DateTime.Now;
            var tides = await TideData.GetNextTides(when, TideData.Location.BowleysBar);            
            HighTide.Text = tides.HighTide;
            LowTide.Text = tides.LowTide;
            LargeMap.Navigate(new Uri(largeMapUrl));
            SmallMap.Navigate(new Uri(smallMapUrl));
            main.Visibility = Visibility.Visible;
        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            UpdateClock();
            var n = DateTime.Now;
            if (n.Second == 0m)
            {
                UpdateWeather();
            }
        }

        private void UpdateClock()
        {
            Date.Text = DateTime.Now.ToString("MM/dd");
            Clock.Text = DateTime.Now.ToString("h:mm");
        }
    }
}
