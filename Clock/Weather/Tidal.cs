using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clock.Weather
{
    public static class TideData
    {
        
        public enum Location
        {
            BowleysBar = 1112367,
            RockyPoint = 1112369,
            OCPier = 8570280,
            OCInlet = 8570283
        }

        public const string Bowleys_Bar = "Bowleys Bar";
        public const string Rockey_Point = "Rocky Point";
        public const string Ocean_City_Fishing_Pier = "Ocean City Fishing Pier";
        public const string Ocent_City_Inlet = "Ocean City Inlet";

        public static List<string> GetLocations()
        {
            return new List<string>() { Bowleys_Bar, Rockey_Point, Ocean_City_Fishing_Pier, Ocent_City_Inlet };
        }
        public static Location GetLocationId(string location)
        {
            switch (location)
            {
                case Bowleys_Bar:
                    return Location.BowleysBar;
                case Rockey_Point:
                    return Location.RockyPoint;
                case Ocean_City_Fishing_Pier:
                    return Location.OCPier;
                case Ocent_City_Inlet:
                    return Location.OCInlet;
                default:
                    return Location.BowleysBar;
            }
        }

        public static List<DailyData> Data;
        public static async Task<DailyData> LoadTideData(DateTime today, Location location)
        {
            var cachedData = Cache.GetDailyData(today, location);
            if (cachedData != null)
                return cachedData;

            List<DailyData> data = new List<DailyData>();
            //var responseJson = await httpClient.GetStringAsync(url);
            using (var client = HttpClientFactory.Create())
            {
                var startDate = today;
                IList<KeyValuePair<string, string>> nameValueCollection = new List<KeyValuePair<string, string>>();
                nameValueCollection.Add(new KeyValuePair<string, string>("site", String.Format("Maryland&station_number={0}", ((Int32)location))));
                nameValueCollection.Add(new KeyValuePair<string, string>("month", String.Format("{0}&year={1}", today.Month, today.Year)));
                nameValueCollection.Add(new KeyValuePair<string, string>("start_date", startDate.Day.ToString()));
                nameValueCollection.Add(new KeyValuePair<string, string>("maximum_days", "2"));
                // get response
                var response = await client.PostAsync("https://www.saltwatertides.com/cgi-local/maryland.cgi", new FormUrlEncodedContent(nameValueCollection));
                var contents = await response.Content.ReadAsStringAsync();
                HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(contents);
                var root = html.DocumentNode;
                var content = root.Descendants("PRE");
                System.IO.StringReader reader = new StringReader(content.First().InnerText);


                var line = string.Empty;

                while (line != null)
                {
                    line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.StartsWith("Day"))
                        continue;
                    if (line.Contains("Sunset"))
                        continue;

                    int day = 0;
                    if (!int.TryParse(line.Substring(4, 2), out day))
                        throw new Exception("Cannot parse day");

                    DailyData.TideType type = DailyData.TideType.Low;
                    if (!Enum.TryParse<DailyData.TideType>(line.Substring(11, 4), out type))
                        throw new Exception("Cannot parse tide type");
                    DateTime timeOfTide = new DateTime();
                    if (!DateTime.TryParse(line.Substring(17, 8), out timeOfTide))
                        throw new Exception("Cannot parse time of tide");

                    DateTime? timeOfSun = null;
                    if (line.Length > 42)
                    {
                        DateTime sun;
                        if (!DateTime.TryParse(line.Substring(35, 8), out sun))
                            throw new Exception("Cannot parse time of sun");
                        timeOfSun = sun;
                    }
                    DailyData dailyData;
                    //First Record
                    if (!data.Any(x => x.DayOfMonth == day))
                        data.Add(new DailyData(day, startDate));

                    dailyData = data.First(x => x.DayOfMonth == day);

                    if (type == DailyData.TideType.Low)
                    {
                        if (dailyData.FirstLowTide.HasValue)
                            dailyData.SecondLowTide = timeOfTide;
                        else
                            dailyData.FirstLowTide = timeOfTide;
                    }
                    else
                    {
                        if (dailyData.FirstHighTide.HasValue)
                            dailyData.SecondHighTide = timeOfTide;
                        else
                            dailyData.FirstHighTide = timeOfTide;
                    }

                    if (timeOfSun.HasValue)
                    {
                        if (!dailyData.SunRise.HasValue)
                            dailyData.SunRise = timeOfSun;
                        else
                            dailyData.SunSet = timeOfSun;
                    }


                }

                Data = data;
            }
            if (data.Any())
                foreach (var d in data)
                    Cache.StoreDailyData(d, d.Date, location);
            return data.First();
        }

        public static async Task<NextTides> GetNextTides(DateTime when, Location location)
        {
            DateTime? nextHighTide = null;
            DateTime highWhen = when;
            DateTime? nextLowTide = null;
            DateTime lowWhen = when;
            while (!nextHighTide.HasValue && highWhen < when.AddDays(3))
            {                
                var d = await LoadTideData(highWhen, location);                
                nextHighTide = d.GetNextHighTide(when);
                highWhen = highWhen.AddDays(1);
            }
            while (!nextLowTide.HasValue && lowWhen < when.AddDays(3))
            {
                var d = await LoadTideData(lowWhen, location);
                nextLowTide = d.GetNextLowTide(when);
                lowWhen = lowWhen.AddDays(1);
            }
            return new NextTides() {
                HighTide = nextHighTide.HasValue ? nextHighTide.Value.ToString("h:mm tt") : "",
                LowTide = nextLowTide.HasValue ? nextLowTide.Value.ToString("h:mm tt") : ""
            };
        }

    }

    public static class Cache
    {
        private static Dictionary<string, DailyData> dailyData = new Dictionary<string, DailyData>();
        private static string getDocId(DateTime today, TideData.Location location)
        {
            return $"{today.Year}_{today.Month}_{today.Day}_{location}.txt".Replace(' ', '_');
        }

        public static void StoreDailyData(DailyData data, DateTime today, TideData.Location location)
        {
            if (GetDailyData(today, location) != null)
                return;
            try
            {
                dailyData.Add(getDocId(today, location), data);                
            }
            catch (Exception ex)
            {
                
            }

        }

        public static void ClearDailyData(DateTime today, TideData.Location location)
        {
            dailyData = new Dictionary<string, DailyData>();
            
        }

        public static DailyData GetDailyData(DateTime today, TideData.Location location)
        {
            try
            {
                if (dailyData != null && dailyData.ContainsKey(getDocId(today, location)))
                    return dailyData[getDocId(today, location)];                
            }
            catch { }
            return null;
        }

    }

    public class NextTides
    { 
        public string HighTide { get; set; }
        public string LowTide { get; set; }
    }

    public class DailyData
    {
        public enum TideType
        {
            Low,
            High
        }

        public List<DateTime?> Times = new List<DateTime?>();
        public readonly int DayOfMonth;
        public DateTime Date;

        public DateTime? GetNextHighTide(DateTime when)
        {
            if (firstHighTide != null && firstHighTide > when)
                return firstHighTide;
            else if (secondHighTide != null && secondHighTide > when)
                return secondHighTide;
            else
                return null;            
        }

        public DateTime? GetNextLowTide(DateTime when)
        {
            if (FirstLowTide != null && FirstLowTide > when)
                return firstLowTide;
            else if (secondLowTide != null && secondLowTide > when)
                return secondLowTide;
            else
                return null;
        }

        private DateTime? firstHighTide;
        public DateTime? FirstHighTide
        {
            get
            {
                return firstHighTide;
            }
            set
            {
                firstHighTide = setDate(value);
                Times.Add(firstHighTide);
            }
        }

        private DateTime? setDate(DateTime? date)
        {
            if (!date.HasValue)
                return date;
            var v = date.Value;
            if (v.Date != Date.Date)
            {
                var timeDiff = v.Subtract(v.Date);
                date = Date.Add(timeDiff);
            }
            return date;
        }

        private DateTime? firstLowTide;
        public DateTime? FirstLowTide
        {
            get { return firstLowTide; }
            set
            {
                firstLowTide = setDate(value);
                Times.Add(firstLowTide);
            }
        }
        private DateTime? secondHighTide;
        public DateTime? SecondHighTide
        {
            get { return secondHighTide; }
            set
            {
                secondHighTide = setDate(value);
                Times.Add(secondHighTide);
            }
        }
        private DateTime? secondLowTide;
        public DateTime? SecondLowTide
        {
            get { return secondLowTide; }
            set
            {
                secondLowTide = setDate(value);
                Times.Add(secondLowTide);
            }
        }

        private DateTime? sunRise;
        public DateTime? SunRise
        {
            get { return sunRise; }
            set { sunRise = setDate(value); }
        }

        private DateTime? sunSet;
        public DateTime? SunSet
        {
            get { return sunSet; }
            set { sunSet = setDate(value); }
        }

        public DailyData(int dayOfMonth, DateTime startDate)
        {
            DayOfMonth = dayOfMonth;
            var dayDiff = dayOfMonth - startDate.Day;
            Date = startDate.Date.AddDays(dayDiff);
            if (dayDiff < 0)
                Date = Date.AddMonths(1);
        }
    }
}
