using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clock.Weather
{
    public class WeatherService
    {
        private string url;
        public WeatherService(string apiKey, string lng, string lat)
        { 
            url  = $@"https://api.openweathermap.org/data/2.5/onecall?lat={lat}&lon={lng}&units=imperial&appid={apiKey}"; 
        }
		
        public async Task<Forecast> GetForecastAsync()
        {
			var httpClient = HttpClientFactory.Create();
			var responseJson = await httpClient.GetStringAsync(url);
			var  data = JsonConvert.DeserializeObject<WeatherApiResponse>(responseJson);
			return new Forecast(data);
		}
    }
}
