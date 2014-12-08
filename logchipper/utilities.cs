using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using log4net;
using log4net.Config;
using System.Reflection;

namespace logchipper.utilities
{

    public class Sql
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static void Insert(Chip chip)
        {
            using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = new SqlConnection(ConfigurationSettings.AppSettings["sqlconnectionstring"]);
                    string datasqltable = ConfigurationSettings.AppSettings["datasqltable"];
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT into " + datasqltable + " (insert_date, date_time, client_ip, site_name, computer_name,server_ip, server_port, method, uri_stem, uri_query, status, substatus, host, user_agent,referer, bytes_out, bytes_in, [time-taken], w32status ) VALUES (@insert_date, @date_time, @client_ip, @site_name, @computer_name, @server_ip, @server_port, @method, @uri_stem, @uri_query, @status, @substatus, @host, @user_agent, @referer, @bytes_out, @bytes_in, @time_taken, @w32status)";
                    command.Parameters.AddWithValue("@insert_date", DateTime.Now.ToString()); 
                    DateTime datetime = DateTime.Parse(chip.Date + " " + chip.Time);
                        command.Parameters.AddWithValue("@date_time", datetime); 
                        command.Parameters.AddWithValue("@client_ip", chip.ClientIp); 
                        command.Parameters.AddWithValue("@site_name", chip.SiteName); 
                        command.Parameters.AddWithValue("@computer_name", chip.ServerName); 
                        command.Parameters.AddWithValue("@server_ip", chip.ClientIp); 
                        command.Parameters.AddWithValue("@server_port", chip.ServerPort); 
                        command.Parameters.AddWithValue("@method", chip.Method); 
                        command.Parameters.AddWithValue("@uri_stem", chip.UriStem); 
                        command.Parameters.AddWithValue("@uri_query", chip.UriQuery); 
                        command.Parameters.AddWithValue("@status", chip.Status); 
                        command.Parameters.AddWithValue("@substatus", chip.SubStatus); 
                        command.Parameters.AddWithValue("@host", chip.Host); 
                        command.Parameters.AddWithValue("@user_agent", chip.UserAgent); 
                        command.Parameters.AddWithValue("@referer", chip.Referer); 
                        command.Parameters.AddWithValue("@bytes_out", chip.BytesOut); 
                        command.Parameters.AddWithValue("@bytes_in", chip.BytesIn); 
                        command.Parameters.AddWithValue("@time_taken", chip.TimeTaken); 
                        command.Parameters.AddWithValue("@w32status", chip.W32Status);
                    
                    try
                    {
                        command.Connection.Open();
                        int recordsAffected = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        log.Error("A sql error occurred: '{0}'", e);
                    }
                
                    //catch (SqlException e)
                    //{
                    //    log.Error("A sql error occurred: '{0}'", e);
                    //}
                    finally
                    {
                        command.Connection.Close();
                    }
                }
        }
    }
    public class Retry
    {
        internal static void DoWithRetry(Action action, TimeSpan sleepPeriod, int retryCount = 3)
        {
            while(true) {
              try {
                action();
                break; // success!
              } catch {
                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Hit retry logic");
                if(--retryCount == 0) throw;
                else Thread.Sleep(sleepPeriod);
              }
           }
        }

    }

    public class BlockingQueue<T> where T : class
    {
        private bool closing;
        private readonly Queue<T> queue = new Queue<T>();

        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }

        public BlockingQueue()
        {
            lock (queue)
            {
                closing = false;
                Monitor.PulseAll(queue);
            }
        }

        public bool Enqueue(T item)
        {
            lock (queue)
            {
                if (closing || null == item)
                {
                    return false;
                }

                queue.Enqueue(item);

                if (queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(queue);
                }

                return true;
            }
        }
        
        public void Close()
        {
            lock (queue)
            {
                if (!closing)
                {
                    closing = true;
                    queue.Clear();
                    Monitor.PulseAll(queue);
                }
            }
        }


        public bool TryDequeue(out T value, int timeout = Timeout.Infinite)
        {
            lock (queue)
            {
                while (queue.Count == 0)
                {
                    if (closing || (timeout < Timeout.Infinite) || !Monitor.Wait(queue, timeout))
                    {
                        value = default(T);
                        return false;
                    }
                }

                value = queue.Dequeue();
                return true;
            }
        }

        public void Clear()
        {
            lock (queue)
            {
                queue.Clear();
                Monitor.Pulse(queue);
            }
        }
    }

    public class HiPerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime, stopTime;
        private long freq;

        // Constructor
        public HiPerfTimer()
        {
            startTime = 0;
            stopTime = 0;

            if (QueryPerformanceFrequency(out freq) == false)
            {
                // high-performance counter not supported
                throw new Win32Exception();
            }
        }

        // Start the timer
        public void Start()
        {
            // lets do the waiting threads work
            Thread.Sleep(0);

            QueryPerformanceCounter(out startTime);
        }

        // Stop the timer
        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        // Returns the duration of the timer (in seconds)
        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double)freq;
            }
        }

        // Returns the duration of the timer (in seconds)

    }

    public class movingAverageTime
    {
        DateTime Time { get; set; }
    }
    public class Statistics
    {
        private class Example
        {
            public double Time { get; set; }
            public string ID { get; set; }
        }
        private static Dictionary<string, List<double>> stats = new Dictionary<string, List<double>>();
        //private static Dictionary<string, ConcurrentQueue<double>> movingstats = new Dictionary<string, ConcurrentQueue<double>>();
        private static object padlock = new object();
        static Dictionary<string, List<Example>> examples = new Dictionary<string, List<Example>>();
        static Dictionary<DateTime, Dictionary<string, double>> movingAverageTimes = new Dictionary<DateTime, Dictionary<string, double>>();
        static System.Threading.Thread movingAverageCollector;
        static Statistics()
        {
            movingAverageCollector = new System.Threading.Thread(CollectMovingAverages);
            movingAverageCollector.Start();
        }
        public static void CollectMovingAverages()
        {
            int errors = 0;
            int counter = 0;

            while (true)
            {

                var ht = new logchipper.utilities.HiPerfTimer();
                DateTime now = DateTime.Now;
                now = now.AddSeconds(-now.Second);
                now = now.AddMilliseconds(-now.Millisecond);
                lock (movingAverageTimes)
                {
                    if (!movingAverageTimes.ContainsKey(now)) movingAverageTimes.Add(now, new Dictionary<string, double>());
                }
                try
                {
                    if (counter > 100)
                    {
                        counter = 0;
                        errors = 0;
                    }
                    foreach (var n in stats.Keys)
                    {
                        List<double> mostRecent = new List<double>();
                        if (stats[n].Count > 110)
                        {
                            ht.Start();
                            lock (padlock)
                            {
                                mostRecent = stats[n].Skip(stats[n].Count - 150).ToList();
                            }
                            ht.Stop();
                            if (DateTime.Now.Millisecond % 10 == 3)
                                Statistics.Add("GenerateMoving Averages stat lock", ht.Duration);
                            mostRecent.Reverse();
                        }
                        else
                        {
                            mostRecent = stats[n].ToList();
                            mostRecent.Reverse();
                        }
                        var ave = mostRecent.Take(100).ToList().Average();
                        lock (movingAverageTimes)
                        {
                            movingAverageTimes[now].Add(n, ave);
                        }
                    }
                    counter++;
                    System.Threading.Thread.Sleep(60 * 1000);
                    lock (movingAverageTimes)
                    {
                        var datesToRemove = movingAverageTimes.Where(x => x.Key < DateTime.Now.AddHours(-12)).Select(x => x.Key).ToList();
                        foreach (var d in datesToRemove)
                            movingAverageTimes.Remove(d);
                    }
                }
                catch (System.Threading.ThreadAbortException) { return; }
                catch (Exception ex)
                {
                    errors++;
                    logchipper.utilities.logging.Logger.CLogger.WriteLog(logging.ELogLevel.ERROR, "Exception while collecting Moving Averages", ex);
                    if (errors < 10)
                        System.Threading.Thread.Sleep(60 * 1000);
                    else
                        System.Threading.Thread.Sleep(10 * 60 * 1000);
                }
            }
        }


        public static Dictionary<DateTime, Dictionary<string, double>> GetMovingAverages(DateTime start, DateTime end)
        {

            IList<DateTime> dates;
            lock (movingAverageTimes)
            {
                dates = movingAverageTimes.Where(x => x.Key > start && x.Key < end).Select(x => x.Key).ToList();
            }
            Dictionary<DateTime, Dictionary<string, double>> toReturn = new Dictionary<DateTime, Dictionary<string, double>>();
            foreach (var d in dates)
                toReturn.Add(d, movingAverageTimes[d]);
            return toReturn;
        }
        public static Dictionary<DateTime, double> GetMovingAverage(DateTime start, DateTime end, string stat)
        {

            IList<DateTime> dates;
            lock (movingAverageTimes)
            {
                dates = movingAverageTimes.Where(x => x.Key > start && x.Key < end).Select(x => x.Key).ToList();
            }
            Dictionary<DateTime, double> stats = new Dictionary<DateTime, double>();
            foreach (var d in dates)
            {
                if (movingAverageTimes[d].ContainsKey(stat)) stats.Add(d, movingAverageTimes[d][stat]);
                else stats.Add(d, 0);
            }
            return stats;
        }


        public static string MovingAverageTimeTable(DateTime start, DateTime end)
        {
            return MovingAverageTimeTable(start, end, "End to End SearchTimer", "QTime", "dataWait", "Census Search Time", "Record Search Time");
        }
        public static string MovingAverageTimeTable(DateTime start, DateTime end, params string[] timers)
        {
            var movingAverages = GetMovingAverages(start, end);
            var names = movingAverages.SelectMany(x => x.Value.Select(y => y.Key)).Distinct().Where(x => timers.Contains(x)).OrderBy(x => x).ToList();
            StringBuilder stb = new StringBuilder();
            stb.Append("<table><tr>");
            stb.Append("<th>time</th>");
            foreach (var name in names)
                stb.AppendFormat("<th>{0}</th>", name);
            stb.Append("</tr>");
            foreach (var d in movingAverages.Keys.OrderBy(x => x))
            {
                stb.Append("<tr>");
                stb.AppendFormat("<td>{0}</td>", d.ToString("MM/dd/yyyy HH:mm"));
                foreach (var name in names)
                {
                    if (!movingAverages[d].ContainsKey(name)) stb.AppendFormat("<td>{0}</td>", "0");
                    else stb.AppendFormat("<td>{0,10:0.000}</td>", movingAverages[d][name]);
                }
                stb.Append("</tr>");
            }
            stb.Append("</table>");
            return stb.ToString();
        }


        public static void Abort()
        {
            if (movingAverageCollector != null) movingAverageCollector.Abort();
        }


        public static void Add(string name, double time, string id)
        {    
            lock (padlock)
            {

                if (!stats.ContainsKey(name)) stats.Add(name, new List<double>());
                if (!examples.ContainsKey(name)) examples.Add(name, new List<Example>());
                //if (!movingstats.ContainsKey(name)) movingstats.Add(name, new ConcurrentQueue<double>());
                if (stats[name].Count > 30000)
                {
                    // get references to current
                    var l = stats[name];
                    var e = examples[name];
                    //var m = movingstats[name];
                    // set values to new lists
                    stats[name] = new List<double>();
                    //movingstats[name] = new ConcurrentQueue<double>();
                    examples[name] = new List<Example>();
                    // clear current contents
                    l.Clear();
                    e.Clear();
                    //m.c();
                }
                if (stats[name].Count > 3 && !String.IsNullOrWhiteSpace(id))
                {
                    if (time > stats[name].OrderBy(x => x).Take((int)(stats[name].Count() * .95)).Average())
                        examples[name].Add(new Example { Time = time, ID = id });
                }
                stats[name].Add(time);
                //while (movingstats[name].Count() >= 100)
                //    movingstats[name].Dequeue();
                //movingstats[name].Enqueue(time);
            }
        }
        public static void Add(string name, double time)
        {
            Add(name, time, null);
        }
        public static void Summary(utilities.logging.ELogLevel level)
        {

            try
            {
                StringBuilder stb = new StringBuilder();
                string row = "{0,-40}:|{1,10:0.000}|{2,10:0.000}|{3,10:0.000}|{5,10:0.000}|{6,10:0.000}|{4,10:0.000}|{9,10:0.000}|{7} | {8} \n";
                stb.AppendFormat("\n---------------------------------------------------------------\n");
                stb.AppendFormat(row, "Name", "Max", "Min", "Ave", "Last", "N5aVE", "N5mAX", "count", "longRunning", "100 MovAve");

                foreach (var n in stats.Keys.OrderByDescending(x => stats[x].Average()))
                {
                    if (stats[n].Count > 3)
                    {
                        StringBuilder stb2 = new StringBuilder();
                        List<double> mostRecent = new List<double>();
                        if (stats[n].Count > 1000)
                        {
                            mostRecent = stats[n].Skip(stats[n].Count - 150).ToList();
                            mostRecent.Reverse();
                        }
                        else
                        {
                            mostRecent = stats[n];
                            mostRecent.Reverse();
                        }

                        foreach (var i in examples[n].OrderByDescending(x => x.Time).Take(5))
                        {
                            stb2.AppendFormat("{0}, ", i.ID, i.Time);
                        }
                        stb.AppendFormat(
                        row,
                        n, stats[n].Max(), stats[n].Min(), stats[n].Average(), stats[n].Last(),
                        stats[n].OrderBy(x => x).Take((int)(stats[n].Count() * .95)).Average(),
                        stats[n].OrderBy(x => x).Take((int)(stats[n].Count() * .95)).Max(),
                        stats[n].Count(),
                        stb2.ToString(),
                        mostRecent.Take(100).ToList().Average()
                        );
                    }
                }
                stb.AppendFormat("---------------------------------------------------------------\n");
                logchipper.utilities.logging.Logger.CLogger.WriteLog(level,
                    stb.ToString(), null);
            }
            catch { }
        }

        public static string HtmlSummary()
        {
            StringBuilder stb = new StringBuilder();
            try
            {
                stb.Append("<table>");
                stb.AppendFormat("<tr class=\"{0}\">", "header");
                string row = "<td>{0,-40}</td><td>{1,10:0.000}</td><td>{2,10:0.000}</td><td>{3,10:0.000}</td><td>{5,10:0.000}</td><td>{6,10:0.000}</td><td>{4,10:0.000}</td><td>{9,10:0.000}</td><td>{7}</td><td>{8}</td> \n";
                stb.AppendFormat(row, "Name", "Max", "Min", "Ave", "Last", "N5aVE", "N5mAX", "count", "longRunning", "100 MovAve");
                stb.Append("</tr>");
                foreach (var n in stats.Keys.OrderByDescending(x => stats[x].Average()))
                {
                    if (stats[n].Count > 3)
                    {
                        lock (padlock)
                        {
                            stb.AppendFormat("<tr class=\"{0}\">", "body");
                            StringBuilder stb2 = new StringBuilder();
                            var lastskip = stats[n].Count() > 200 ? stats.Count() - 100 : 0;
                            foreach (var i in examples[n].OrderByDescending(x => x.Time).Take(5))
                                stb2.AppendFormat("{0}, ", i.ID, i.Time);

                            stb.AppendFormat(
                            row,
                            n, stats[n].Max(), stats[n].Min(), stats[n].Average(), stats[n].Last(),
                            stats[n].OrderBy(x => x).Take((int)(stats[n].Count() * .95)).Average(),
                            stats[n].OrderBy(x => x).Take((int)(stats[n].Count() * .95)).Max(),
                            stats[n].Count(),
                            stb2.ToString(),
                            ""
                                //movingstats[n].Average()
                            );
                            stb.Append("</tr>");
                        }
                    }
                }
                stb.Append("</table>");
            }
            catch { }
            return stb.ToString();
        }
        public static void Clear()
        {
            lock (padlock)
            {
                stats.Clear();
                examples.Clear();
                //movingstats.Clear();
            }
        }
    }
}
