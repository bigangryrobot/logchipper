using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using logchipper;

namespace logchipper.utilities.logging
{

        public enum ELogLevel
        {
            DEBUG = 1,
            ERROR,
            FATAL,
            INFO,
            WARN
        }

        public class Logger
        {

            public static class CLogger
            {

                #region Members




                #endregion


                static System.Threading.Thread LogProcessor;
                #region Constructors

                static CLogger()
                {
                    log4net.Config.XmlConfigurator.Configure();
                    LogProcessor = new System.Threading.Thread(ProcessLogs);
                    LogProcessor.Start();
                }

                #endregion


                #region Methods
                private static Queue<LogRequest> logs = new Queue<LogRequest>();
                public static int LogQueue { get { return logs.Count; } }
                private static void ProcessLogs()
                {
                    while (true)
                    {
                        try
                        {
                            if (logs.Count == 0)
                            {
                                System.Threading.Thread.Sleep(100);
                                continue;
                            }
                            LogRequest log = null;
                            lock (logs)
                                log = logs.Dequeue();
                            log.LogMe();
                        }
                        catch (System.Threading.ThreadAbortException) { break; }
                    }
                }
                private class LogRequest
                {
                    public string log;
                    public ELogLevel logLevel;
                    public Exception ex;
                    public ILog logger;
                    public void LogMe()
                    {
                        switch (logLevel)
                        {
                            case ELogLevel.DEBUG:
                                logger.Debug(log, ex);
                                break;
                            case ELogLevel.ERROR:
                                logger.Error(log, ex);
                                if (ex != null)
                                {
                                    var ex2 = ex.InnerException;
                                    while (ex2 != null)
                                    {
                                        logger.Error(log, ex2);
                                        ex2 = ex2.InnerException;
                                    }
                                }
                                break;
                            case ELogLevel.FATAL:
                                logger.Fatal(log, ex);
                                if (ex != null)
                                {
                                    var ex2 = ex.InnerException;
                                    if (ex2 != null)
                                    {
                                        logger.Error(log, ex2);
                                        ex2 = ex2.InnerException;
                                    }
                                }
                                break;
                            case ELogLevel.INFO:
                                logger.Info(log, ex);
                                break;
                            case ELogLevel.WARN:
                                logger.Warn(log, ex);
                                break;
                        }
                    }

                }



                public static void WriteLog(ELogLevel logLevel, String log, Exception ex)
                {
                    WriteLog(logLevel, log, ex, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name);
                }
                public static void WriteLog(ELogLevel logLevel, String log, Exception ex, string LoggerName)
                {
                    if (string.IsNullOrEmpty(LoggerName)) LoggerName = new System.Diagnostics.StackTrace().GetFrame(2).GetMethod().Name;
                    ILog logger = LogManager.GetLogger(LoggerName);
                    lock (logs)
                        logs.Enqueue(new LogRequest
                        {
                            ex = ex,
                            log = log,
                            logger = logger,
                            logLevel = logLevel
                        });
                }
                public static void WriteLog(ELogLevel logLevel, String log)
                {
                    WriteLog(logLevel, log, null, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name);
                }
                public static void Abort()
                {
                    LogProcessor.Abort();
                }
                #endregion

            }

        }

    }
