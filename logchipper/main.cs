using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using System.Net.NetworkInformation;
using log4net;
using log4net.Config;
using System.Reflection;
using Ionic.Zip;
using logchipper.utilities;

namespace logchipper
{

    public static class main
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
 
        public const string ServiceName = "logchipper";

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                main.Start(args);
            }

            protected override void OnStop()
            {
                main.Stop();
            }
        }

        static void Main(string[] args)
        {
            int elementsprocessed = 0;
            int totalfilesprocessed = 0;
            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, @"System init logchipper v1

 __                        ____    __                                                      _     
/\ \                      /\  _`\ /\ \      __                                           /' \    
\ \ \       ___     __    \ \ \/\_\ \ \___ /\_\  _____   _____     __  _ __       __  __/\_, \   
 \ \ \  __ / __`\ /'_ `\   \ \ \/_/\ \  _ `\/\ \/\ '__`\/\ '__`\ /'__`/\`'__\    /\ \/\ \/_/\ \  
  \ \ \L\ /\ \L\ /\ \L\ \   \ \ \L\ \ \ \ \ \ \ \ \ \L\ \ \ \L\ /\  __\ \ \/     \ \ \_/ | \ \ \ 
   \ \____\ \____\ \____ \   \ \____/\ \_\ \_\ \_\ \ ,__/\ \ ,__\ \____\ \_\      \ \___/   \ \_\
    \/___/ \/___/ \/___L\ \   \/___/  \/_/\/_/\/_/\ \ \/  \ \ \/ \/____/\/_/       \/__/     \/_/
                    /\____/                        \ \_\   \ \_\                                 
                    \_/__/                          \/_/    \/_/                                 

            ");

            // create new stopwatch
            var mainstopwatch = new Stopwatch();
            // begin timing
            mainstopwatch.Start();

            if (!Environment.UserInteractive)
                // running as service
                using (var service = new Service())
                    ServiceBase.Run(service);
            else
            {   
                // parse the csv and make arrays
                string CsvFile = ConfigurationSettings.AppSettings["csvfile"];
                var reader = new StreamReader(File.OpenRead(CsvFile));
                List<string> elementA = new List<string>(); // site name
                List<string> elementB = new List<string>(); // server name
                List<string> elementC = new List<string>(); // log path
                List<string> elementD = new List<string>(); // keep flag
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    elementA.Add(values[0]);
                    elementB.Add(values[1]);
                    elementC.Add(values[2]);
                    elementD.Add(values[3]);
                }

                // bring the arrays together with linq awesome sauce
                var serverlistelements = (from n1 in elementA.Select((item, index) => new { item, index })
                                          join n2 in elementB.Select((item, index) => new { item, index }) on n1.index equals n2.index
                                          join n3 in elementC.Select((item, index) => new { item, index }) on n2.index equals n3.index
                                          join n4 in elementD.Select((item, index) => new { item, index }) on n3.index equals n4.index
                                          select new { sitename = n1.item, servername = n2.item, filepath = n3.item, keep = n4.item }).ToList();

                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, serverlistelements.Count + " element(s) found in the csv file " + CsvFile );
                
                // set max paralellelism for server threads
                int serverparallellism = Convert.ToInt32(ConfigurationSettings.AppSettings["maxdegreeofparallelismserver"]);
                var serveroptions = new ParallelOptions { MaxDegreeOfParallelism = serverparallellism };
                int maxthreadlifetime = Convert.ToInt32(ConfigurationSettings.AppSettings["maxthreadlifetime"]);

                // start creating threads
                try
                {
                    Parallel.ForEach(serverlistelements, serveroptions, element =>
                    {
                        int threadfileprocessed = 0;
                        // Try to set global site and server name additions to the logging mechanic
                        log4net.GlobalContext.Properties["sitename"] = element.sitename;
                        log4net.GlobalContext.Properties["servername"] = element.servername;
                        logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "THREAD INIT");
                               
                        // create new stopwatch
                        var threadstopwatch = new Stopwatch();
                        // begin timing
                        threadstopwatch.Start();

                        try
                        {
                            // check if server and path exists
                            var ping = new Ping();
                            var pingReply = ping.Send(element.servername);
                            if (pingReply.Status == IPStatus.Success)
                            {
                                // set max paralellelism for file threads
                                int fileparallellism = Convert.ToInt32(ConfigurationSettings.AppSettings["maxdegreeofparallelismfile"]);
                                var fileoptions = new ParallelOptions { MaxDegreeOfParallelism = fileparallellism };
                
                                DirectoryInfo dirInfo = new DirectoryInfo(element.filepath);
                                FileInfo[] fileInfoArr = dirInfo.GetFiles("*.log");
                                if (fileInfoArr.Length == 0 )
                                {
                                    logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, fileInfoArr.Length + " files found in the remote directory " + element.filepath);
                                } 
                                else
                                {
                                    logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, fileInfoArr.Length + " files found in the remote directory " + element.filepath);
                                    Parallel.ForEach(fileInfoArr, fileoptions, file =>
                                    {
                                        // make a pile
                                        var chipPile = GetChipPile(file, element.sitename, element.servername, element.keep);
                                
                                        // now dump that pile in the bucket
                                        //foreach (var chip in chipPile)
                                        //{
                                            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "File" + file + " parsing for sitename " + element.sitename + "completed, now passing to the bucket for data entry");
                                            Task.Run(() => ChipBucket.MSSQL(chipPile, Thread.CurrentThread.ManagedThreadId));
                                        //}
                                    
                                        threadfileprocessed += 1;
                                        totalfilesprocessed += 1;
                                        log4net.GlobalContext.Properties["threadlifetime"] = threadstopwatch.Elapsed;
                                        log4net.GlobalContext.Properties["threadfileprocessed"] = threadfileprocessed;
                                        log4net.GlobalContext.Properties["totalfilesprocessed"] = totalfilesprocessed;
                                    });
                                }
                            }
                            else
                            {
                                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "sitename" + "`t Server does not exist");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine("Cancellation exception caught!");
                        }
                        catch (Exception e)
                        {
                            log.Error("An error occured within the thread", e);
                        }

                        // Stop timing
                        threadstopwatch.Stop();
                        // Write result
                        logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Thread time elapsed: " + threadstopwatch.Elapsed + "and " + threadfileprocessed);
                        logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Thread finished");
                        //logchipper.utilities.Statistics.Summary(logchipper.utilities.logging.ELogLevel.INFO);
                    });

                    // Stop timing
                    mainstopwatch.Stop();
                    // Write result
                    logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Main time elapsed: " + mainstopwatch.Elapsed + "and " + totalfilesprocessed);
                    Console.ReadLine();
                }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation exception caught!");
            }
        }
        }

        private static IEnumerable<Chip> GetChipPile(FileInfo file, string sitename, string servername, string keep)
        {
            // var for processing error catching
            int processingerrors = 0;

            // Try to set global site and server name additions to the logging mechanic
            log4net.GlobalContext.Properties["sitename"] = sitename;
            log4net.GlobalContext.Properties["servername"] = servername;

            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Begining to chip file " + file.FullName );
            List<Chip> chipPile = new List<Chip>();

            if (keep != "keep")
            {
                try
                {
                    logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "File " + file.FullName + " not marked keep so it is now being deleted");
                    //File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    log.Error("An error occured while deleting the log :", e);
                }
            }
            else
            {
                var logLineReader = new FileReader();
                var logLines = logLineReader.ParseChip(file);
                var logLineParser = new Chipper();
                foreach (var logLine in logLines)
                {
                    try
                    {
                        var chip = logLineParser.ParseChip(logLine, sitename);
                            if (chip != null)
                            {
                                chipPile.Add(chip);
                                //ChipBucket.MSSQL(chip, Thread.CurrentThread.ManagedThreadId);
                            }
                    }
                    catch (Exception e)
                    {
                        log.Error("An error occured while chipping the log :", e);
                    }
                }
                
                // zip the file
                try
                {
                    string ziparchive = ConfigurationSettings.AppSettings["ziparchive"];
                    // check if zip file exists, if it exists then add the file to it
                    if (File.Exists(ziparchive + sitename + ".zip") == true) {
                        using (var zip = ZipFile.Read(ziparchive + sitename + ".zip"))
                        {
                            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Adding file " + file.FullName + " to existing archive " + ziparchive + sitename + ".zip");
                            zip.AddFile(file.FullName, "");
                            Retry.DoWithRetry(zip.Save, TimeSpan.FromSeconds(2), retryCount: 10);
                        }
                    } else
                    {
                        // if it does not exist, then create a new one and add the file to it
                        using (ZipFile zip = new ZipFile())
                        {
                            int zipcompressionlevel = Convert.ToInt32(ConfigurationSettings.AppSettings["zipcompressionlevel"]);
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Level5;
                            zip.AddFile(file.FullName, "");
                            
                            // save to the main logging location
                            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Adding file " + file.FullName + " to the archive " + ziparchive + sitename + ".zip");
                            zip.Save(ziparchive + sitename + ".zip");
                            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "Adding file " + file.FullName + " to the archive " + ziparchive + "smarterstats\\" + DateTime.Now.ToString("ddd") + sitename + ".zip");
                            zip.Save(ziparchive + "smarterstats\\" + DateTime.Now.ToString("ddd") + "\\" + sitename + ".zip");
                        }
                    }
                }
                catch(Exception e)
                {
                    string ziparchive = ConfigurationSettings.AppSettings["ziparchive"];
                    log.Error("An error occured while adding to the zip archive for file " + file.FullName + " to the archive " + ziparchive + sitename + ".zip", e);
                    processingerrors = 1;
                }
            }
            if (processingerrors != 1)
            {
                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "File " + file.FullName + " sucessfully processed and so it can now be deleted");
                File.Delete(file.FullName);                
            }
            return chipPile;
        }
        
        private static void Start(string[] args)
        {
            // onstart code here
        }

        private static void Stop()
        {
            // onstop code here
        }
    }
}
