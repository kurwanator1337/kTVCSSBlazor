using kTVCSSBlazor.Db.Interfaces;
using kTVCSSBlazor.Db.Models.Highlights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Telegram.Bot.Types;
using Telegram.Bot;
using static Dapper.SqlMapper;

namespace kTVCSSBlazor.Db.Repository
{
    public class Highlights(IConfiguration configuration, ILogger logger, IVips vip) : Context(configuration, logger), IHighlights
    {
        public static MemoryCache MemoryCache = new(new MemoryCacheOptions() { });
        private string ConnectionString { get; set; } = configuration.GetConnectionString("db");
        private IVips Vip { get; set; } = vip;

        public async Task<List<Result>> GetByPlayer(int id, string requester)
        {
            string steam = string.Empty;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand($"SELECT STEAMID FROM Players WHERE ID = {id}", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            steam = reader.GetString(0);
                        }
                    }
                }
            }

            if (!await Vip.IsVip(requester)) return null;

            List<int> matchesList = new List<int>();
            List<Match> logs = new List<Match>();
            List<Result> results = new List<Result>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                MemoryCache.TryGetValue("matchesList", out matchesList);
                if (matchesList == null)
                {
                    matchesList = new List<int>();
                    using (SqlCommand command = new SqlCommand($"SELECT DISTINCT MATCHID FROM [kTVCSS].[dbo].[MatchesLogs] WHERE (MESSAGE LIKE '%RAMPAGE%' OR MESSAGE LIKE '%QUAD KILL%') AND DATEPART(DAYOFYEAR, GETDATE()) - DATEPART(DAYOFYEAR, DATETIME) <= 7 AND DATEPART(YEAR, DATETIME) = {DateTime.Now.Year}", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                matchesList.Add(int.Parse(reader[0].ToString()));
                            }
                        }
                    }
                }

                MemoryCache.Set("matchesList", matchesList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));

                MemoryCache.TryGetValue("matchesLogs", out logs);
                if (logs == null)
                {
                    logs = new List<Match>();

                    foreach (var i in matchesList)
                    {
                        logs.Add(new Match()
                        {
                            Id = i
                        });
                    }

                    foreach (int i in matchesList)
                    {
                        using (SqlCommand command = new SqlCommand($"SELECT MESSAGE, DEMONAME FROM [kTVCSS].[dbo].[MatchesLogs] AS ML INNER JOIN MatchesDemos AS MD ON ML.MATCHID = MD.ID WHERE ML.MATCHID = {i} ORDER BY DATETIME ASC", connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    logs.FirstOrDefault(x => x.Id == i).Messages.Add(reader[0].ToString());
                                    logs.FirstOrDefault(x => x.Id == i).Demo = reader[1].ToString();
                                }
                            }
                        }
                    }

                    MemoryCache.Set("matchesLogs", logs, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            foreach (Match match in logs)
            {
                for (int i = 0; i < match.Messages.Count; i++)
                {
                    if (match.Messages[i].Contains("RAMPAGE"))
                    {
                        try
                        {
                            string nickName = match.Messages[i].Substring(0, match.Messages[i].IndexOf("MADE")).Trim();

                            List<string> temp = new List<string>();

                            for (int k = i; k > 0; k--)
                            {
                                if (match.Messages[k].Contains(nickName))
                                {
                                    if (!match.Messages[k].Contains("> said:"))
                                    {
                                        temp.Add(match.Messages[k]);
                                    }
                                }

                                if (match.Messages[k].Contains("<Round Start>"))
                                {
                                    if (temp.Count(x => x.Contains(steam)) < 4)
                                    {
                                        break;
                                    }

                                    try
                                    {
                                        string log = match.Messages[k].Substring(match.Messages[k].IndexOf("["));
                                        log = log.Replace("[", "").Replace("]", "");

                                        string firstTempString = temp.FirstOrDefault().Substring(temp.FirstOrDefault().IndexOf("["));
                                        firstTempString = firstTempString.Replace("[", "").Replace("]", "");
                                        TimeSpan start = TimeSpan.Parse(firstTempString);

                                        string lastTempString = temp.LastOrDefault().Substring(temp.LastOrDefault().IndexOf("["));
                                        lastTempString = lastTempString.Replace("[", "").Replace("]", "");
                                        TimeSpan end = TimeSpan.Parse(lastTempString);

                                        string originalDemoName = Path.Combine(@"wwwroot/demos", match.Demo + ".dem.zip");

                                        if (!System.IO.File.Exists(originalDemoName))
                                        {
                                            //break;
                                        }

                                        double ticks = (Math.Round(end.TotalSeconds) * 100);
                                        ticks = ticks - (5 * 100);

                                        results.Add(new Result()
                                        {
                                            Name = nickName,
                                            DemoName = match.Demo,
                                            TimeSpan = TimeSpan.Parse(log),
                                            Length = Math.Round(start.TotalSeconds - end.TotalSeconds),
                                            Ticks = ticks,
                                            Type = "Rampage"
                                        });
                                    }
                                    catch (Exception)
                                    {
                                        // error string
                                    }

                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }

                    if (match.Messages[i].Contains("quad kill"))
                    {
                        try
                        {
                            string nickName = match.Messages[i].Substring(0, match.Messages[i].IndexOf("made")).Trim();

                            List<string> temp = new List<string>();

                            for (int k = i; k > 0; k--)
                            {
                                if (match.Messages[k].Contains(nickName))
                                {
                                    if (!match.Messages[k].Contains("> said:"))
                                    {
                                        temp.Add(match.Messages[k]);
                                    }
                                }

                                if (match.Messages[k].Contains("<Round Start>"))
                                {
                                    if (temp.Count(x => x.Contains(steam)) < 3)
                                    {
                                        break;
                                    }

                                    try
                                    {
                                        string log = match.Messages[k].Substring(match.Messages[k].IndexOf("["));
                                        log = log.Replace("[", "").Replace("]", "");

                                        string firstTempString = temp.FirstOrDefault().Substring(temp.FirstOrDefault().IndexOf("["));
                                        firstTempString = firstTempString.Replace("[", "").Replace("]", "");
                                        TimeSpan start = TimeSpan.Parse(firstTempString);

                                        string lastTempString = temp.LastOrDefault().Substring(temp.LastOrDefault().IndexOf("["));
                                        lastTempString = lastTempString.Replace("[", "").Replace("]", "");
                                        TimeSpan end = TimeSpan.Parse(lastTempString);

                                        string originalDemoName = Path.Combine(@"wwwroot/demos", match.Demo + ".dem.zip");

                                        if (!System.IO.File.Exists(originalDemoName))
                                        {
                                            //break;
                                        }

                                        double ticks = (Math.Round(end.TotalSeconds) * 100);
                                        ticks = ticks - (5 * 100);

                                        results.Add(new Result()
                                        {
                                            Name = nickName,
                                            DemoName = match.Demo,
                                            TimeSpan = TimeSpan.Parse(log),
                                            Length = Math.Round(start.TotalSeconds - end.TotalSeconds),
                                            Ticks = ticks,
                                            Type = "Quadro"
                                        });
                                    }
                                    catch (Exception)
                                    {
                                        // error string
                                    }

                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString);
                        }
                    }
                }
            }

            return results;
        }

        public async Task<List<Result>> GetByMatch(int id, string steam)
        {
            List<int> matchesList = new List<int>() { id };
            List<Match> logs = new List<Match>();
            List<Result> results = new List<Result>();

            if (!await Vip.IsVip(steam)) return null;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                foreach (int i in matchesList)
                {
                    logs.Add(new Match()
                    {
                        Id = i
                    });

                    using (SqlCommand command = new SqlCommand($"SELECT MESSAGE, DEMONAME FROM [kTVCSS].[dbo].[MatchesLogs] AS ML INNER JOIN MatchesDemos AS MD ON ML.MATCHID = MD.ID WHERE ML.MATCHID = {id} ORDER BY DATETIME ASC", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.FirstOrDefault(x => x.Id == id).Messages.Add(reader[0].ToString());
                                logs.FirstOrDefault(x => x.Id == id).Demo = reader[1].ToString();
                            }
                        }
                    }
                }
            }

            foreach (Match match in logs)
            {
                for (int i = 0; i < match.Messages.Count; i++)
                {
                    if (match.Messages[i].Contains("RAMPAGE"))
                    {
                        string nickName = match.Messages[i].Substring(0, match.Messages[i].IndexOf("MADE")).Trim();

                        List<string> temp = new List<string>();

                        for (int k = i; k > 0; k--)
                        {
                            if (match.Messages[k].Contains(nickName))
                            {
                                if (!match.Messages[k].Contains("> said:"))
                                {
                                    temp.Add(match.Messages[k]);
                                }
                            }

                            if (match.Messages[k].Contains("<Round Start>"))
                            {
                                try
                                {
                                    string log = match.Messages[k].Substring(match.Messages[k].IndexOf("["));
                                    log = log.Replace("[", "").Replace("]", "");

                                    string firstTempString = temp.FirstOrDefault().Substring(temp.FirstOrDefault().IndexOf("["));
                                    firstTempString = firstTempString.Replace("[", "").Replace("]", "");
                                    TimeSpan start = TimeSpan.Parse(firstTempString);

                                    string lastTempString = temp.LastOrDefault().Substring(temp.LastOrDefault().IndexOf("["));
                                    lastTempString = lastTempString.Replace("[", "").Replace("]", "");
                                    TimeSpan end = TimeSpan.Parse(lastTempString);

                                    string originalDemoName = Path.Combine(@"wwwroot/demos", match.Demo + ".dem.zip");

                                    if (!System.IO.File.Exists(originalDemoName))
                                    {
                                        //break;
                                    }

                                    double ticks = (Math.Round(end.TotalSeconds) * 100);
                                    ticks = ticks - (5 * 100);

                                    results.Add(new Result()
                                    {
                                        Name = nickName,
                                        DemoName = match.Demo,
                                        TimeSpan = TimeSpan.Parse(log),
                                        Length = Math.Round(start.TotalSeconds - end.TotalSeconds),
                                        Ticks = ticks,
                                        Type = "Rampage"
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }

                                break;
                            }
                        }
                    }

                    if (match.Messages[i].Contains("quad kill"))
                    {
                        string nickName = match.Messages[i].Substring(0, match.Messages[i].IndexOf("made")).Trim();

                        List<string> temp = new List<string>();

                        for (int k = i; k > 0; k--)
                        {
                            if (match.Messages[k].Contains(nickName))
                            {
                                if (!match.Messages[k].Contains("> said:"))
                                {
                                    temp.Add(match.Messages[k]);
                                }
                            }

                            if (match.Messages[k].Contains("<Round Start>"))
                            {
                                try
                                {
                                    string log = match.Messages[k].Substring(match.Messages[k].IndexOf("["));
                                    log = log.Replace("[", "").Replace("]", "");

                                    string firstTempString = temp.FirstOrDefault().Substring(temp.FirstOrDefault().IndexOf("["));
                                    firstTempString = firstTempString.Replace("[", "").Replace("]", "");
                                    TimeSpan start = TimeSpan.Parse(firstTempString);

                                    string lastTempString = temp.LastOrDefault().Substring(temp.LastOrDefault().IndexOf("["));
                                    lastTempString = lastTempString.Replace("[", "").Replace("]", "");
                                    TimeSpan end = TimeSpan.Parse(lastTempString);

                                    string originalDemoName = Path.Combine(@"wwwroot/demos", match.Demo + ".dem.zip");

                                    if (!System.IO.File.Exists(originalDemoName))
                                    {
                                        //break;
                                    }

                                    double ticks = (Math.Round(end.TotalSeconds) * 100);
                                    ticks = ticks - (5 * 100);

                                    results.Add(new Result()
                                    {
                                        Name = nickName,
                                        DemoName = match.Demo,
                                        TimeSpan = TimeSpan.Parse(log),
                                        Length = Math.Round(start.TotalSeconds - end.TotalSeconds),
                                        Ticks = ticks,
                                        Type = "Quadro"
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
