using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using logchipper.utilities;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using log4net;
using log4net.Config;

namespace logchipper
{
    public class FileReader
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool insertreportsql(string filesha)
        {
            using (SqlCommand command = new SqlCommand())
            {
                StringBuilder sBuffer = new StringBuilder();
                for (int i = 0; i < filesha.Length; i += 2)
                {
                    string hs = filesha.Substring(i, 2);
                    sBuffer.Append(Convert.ToChar(Convert.ToUInt32(hs, 16)));
                }
                string unhexsha = sBuffer.ToString();
                command.Connection = new SqlConnection(ConfigurationSettings.AppSettings["sqlconnectionstring"]);
                string reportingsqltable = ConfigurationSettings.AppSettings["reportingsqltable"];
                command.CommandType = CommandType.Text;
                command.CommandText = "INSERT into " + reportingsqltable + " (insert_date_time, filesha ) VALUES (@insert_date_time, @filesha)";
                command.Parameters.AddWithValue("@insert_date_time", DateTime.Now.ToString());
                command.Parameters.AddWithValue("@filesha", unhexsha);
                
                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "adding chips to MSSQL bucket" + command.Parameters.ToString());

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
                return true;
            }
        }

        protected bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Append, FileAccess.Write, FileShare.None);
            }
            catch
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.WARN, "file " + file.FullName + " is locked");
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        public IEnumerable<string> ParseChip(FileInfo log)
        {
                bool result = IsFileLocked(log);
                
                if (result != true)
                {
                    using (var fs = new FileStream(log.FullName, FileMode.Open))
                    {   
                        using (var sr = new StreamReader(fs))
                        {
                            string chip = null;
                            while ((chip = sr.ReadLine()) != null)
                            {
                                yield return chip;
                            }
                        }
                    }
                    using (var logfile = new FileStream(log.FullName, FileMode.Open))
                    {
                        SHA1 sha = new SHA1Managed();
                        insertreportsql(BitConverter.ToString(sha.ComputeHash(logfile)).Replace("-", ""));
                    }
                }
        }
    }

}