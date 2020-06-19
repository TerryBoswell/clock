using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clock.Weather
{

	public class Weather
	{
		public int id { get; set; }
		public string main { get; set; }
		public string description { get; set; }
		public string icon { get; set; }
	}

	public class Current : ForecastBaseWithTemp
	{
		public int sunrise { get; set; }
		public int sunset { get; set; }
		public double uvi { get; set; }
		public int visibility { get; set; }
		public double wind_gust { get; set; }
		public double temp { get; set; }
		public double feels_like { get; set; }
	}

	public class Minutely
	{
		public int dt { get; set; }
		public int precipitation { get; set; }
	}


	public class Hourly : ForecastBaseWithTemp
	{

	}

	public abstract class ForecastBaseWithTemp : ForecastBase
	{
		public double temp { get; set; }
		public double feels_like { get; set; }
	}

	public abstract class ForecastBase
	{
		public int dt { get; set; }
		public int pressure { get; set; }
		public int humidity { get; set; }
		public double dew_point { get; set; }
		public int clouds { get; set; }
		public double wind_speed { get; set; }
		public int wind_deg { get; set; }
		public IList<Weather> weather { get; set; }
		public Direction WindDirection
		{
			get
			{
				return new Direction(this.wind_deg) { Speed = this.wind_speed };
			}

		}
		public DateTimeOffset DateOf
		{
			get
			{
				return DateTimeOffset.FromUnixTimeSeconds(dt).ToLocalTime();
			}
		}
	}

	public class Temp
	{
		public double day { get; set; }
		public double min { get; set; }
		public double max { get; set; }
		public double night { get; set; }
		public double eve { get; set; }
		public double morn { get; set; }
	}

	public class FeelsLike
	{
		public double day { get; set; }
		public double night { get; set; }
		public double eve { get; set; }
		public double morn { get; set; }
	}



	public class Daily : ForecastBase
	{
		public Temp temp { get; set; }
		public FeelsLike feels_like { get; set; }
		public int sunrise { get; set; }
		public int sunset { get; set; }
		public double uvi { get; set; }
		public double? rain { get; set; }
	}

	public class WeatherApiResponse
	{
		public double lat { get; set; }
		public double lon { get; set; }
		public string timezone { get; set; }
		public int timezone_offset { get; set; }
		public Current Current { get; set; }
		public IList<Minutely> Minutely { get; set; }
		public IList<Hourly> Hourly { get; set; }
		public IList<Daily> Daily { get; set; }
	}


	public class Direction
	{
		List<System.Tuple<string, double, double>> sets = new List<System.Tuple<string, double, double>>()
	{
		new Tuple<string, double, double>("N", 348.75, 11.25),
		new Tuple<string, double, double>("NNE", 11.25, 33.75),
		new Tuple<string, double, double>("NE", 33.75, 56.25),
		new Tuple<string, double, double>("ENE", 56.25, 78.75),
		new Tuple<string, double, double>("E", 78.75, 101.25),
		new Tuple<string, double, double>("ESE", 101.25, 123.75),
		new Tuple<string, double, double>("SE", 123.75, 146.25),
		new Tuple<string, double, double>("SSE", 146.25, 168.75),
		new Tuple<string, double, double>("S", 168.75, 191.25),
		new Tuple<string, double, double>("SSW", 191.25, 213.75),
		new Tuple<string, double, double>("SW", 213.75, 236.25),
		new Tuple<string, double, double>("WSW", 236.25, 258.75),
		new Tuple<string, double, double>("W", 258.75, 281.25),
		new Tuple<string, double, double>("WNW", 281.25, 303.75),
		new Tuple<string, double, double>("NW", 303.75, 326.25),
		new Tuple<string, double, double>("NNW", 326.25, 348.75)
	};
		private void calc()
		{
			if (this.Value <= sets.First().Item3 || this.Value >= sets.First().Item2)
			{
				this.Name = sets.First().Item1;
				return;
			}
			foreach (var tuple in sets.Skip(1))
			{
				if (this.Value >= tuple.Item2 && this.Value <= tuple.Item3)
				{
					this.Name = tuple.Item1;
					break;
				}
			}
		}
		public Direction(double windDegree)
		{
			this.Value = windDegree;
			calc();
		}
		public string Name { get; set; }
		public double Value { get; set; }
		public double Speed { get; set; }

	}
	public class Forecast
	{
		public Forecast(WeatherApiResponse data)
		{
			HourlySummary = new List<WeatherSummary>();
			Daily = new List<DailySummary>();
			Current = new WeatherSummary(data.Current);
			Current.FeelsLike = (int)data.Current.feels_like;
			Current.Temperature = (int)data.Current.temp;
			foreach (var wi in data.Hourly)
				HourlySummary.Add(new WeatherSummary(wi));

			CalcBarometricTrend(HourlySummary);
			foreach (var d in data.Daily)
				Daily.Add(new DailySummary(d));
		}
		private void CalcBarometricTrend(List<WeatherSummary> items)
		{
			for (int i = 1; i < items.Count - 1; i++)
			{
				items[i].BarometricTrend = Math.Round(items[i].BarometricPressure - items[i - 1].BarometricPressure, 2);
			}
		}
		public List<WeatherSummary> HourlySummary { get; set; }
		public WeatherSummary Current { get; set; }
		public List<DailySummary> Daily { get; set; }
	}

	public class WeatherSummary : WeatherSummaryBase
	{
		public WeatherSummary(ForecastBaseWithTemp item) : base(item)
		{
			this.FeelsLike = (int)item.feels_like;
			this.Temperature = (int)item.temp;

		}
		public int Temperature { get; set; }
		public int FeelsLike { get; set; }
	}

	public class DailySummary : WeatherSummaryBase
	{
		public DailySummary(Daily item) : base(item)
		{
			this.HighTemperature = (int)item.temp.max;
			this.LowTemperature = (int)item.temp.min;
			this.DayFeelsLike = (int)item.feels_like.day;
			this.NightFeelsLike = (int)item.feels_like.night;
		}
		public int HighTemperature { get; set; }
		public int LowTemperature { get; set; }
		public int DayFeelsLike { get; set; }
		public int NightFeelsLike { get; set; }
	}

	public abstract class WeatherSummaryBase
	{
		public WeatherSummaryBase(ForecastBase item)
		{
			this.Period = item.DateOf.DateTime;
			SetBarometricPressure(item.pressure);
			this.Humidity = item.humidity;
			this.WindDirection = item.WindDirection.Name;
			this.WindSpeed = item.WindDirection.Speed;
			this.WeatherDescription = $"{item.weather.First().main} - {item.weather.First().description}";
		}

		private const double AvgPressure = 29.92;
		private void SetBarometricPressure(int hPa)
		{
			this.BarometricPressure = Math.Round(((hPa * 1) * 100) / 3386.389, 2);
			if (this.BarometricPressure < AvgPressure)
			{
				this.BarometricPressureClassification = "Low";
			}
			else if (this.BarometricPressure > AvgPressure)
			{
				this.BarometricPressureClassification = "High";
			}
			else
			{
				this.BarometricPressureClassification = "Average";
			}
		}

		public DateTime Period { get; set; }
		public int Humidity { get; set; }
		public double BarometricPressure { get; set; }
		public string BarometricPressureClassification { get; set; }
		public double WindSpeed { get; set; }
		public string WindDirection { get; set; }
		public double? BarometricTrend { get; set; }
		public string WeatherDescription { get; set; }
	}

}
