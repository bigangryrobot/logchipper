using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Diagnostics;
using Elasticsearch.Net;
using System.IO;
using System.Configuration;
using logchipper.utilities;
using System.Web;
using System.Net;
using System.Web.Script.Serialization;
using log4net;
using log4net.Config;
using System.Reflection;
using logchipper.utilities;
using System.Threading.Tasks;

namespace logchipper
{
    public class ChipBucket
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static void MSSQL( IEnumerable<Chip> chips,int Threadid)
        {

            logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "adding chips to MSSQL bucket");
            foreach (var chip in chips)
            {
                Task.Run(() => logchipper.utilities.Sql.Insert(chip));
                //logchipper.utilities.Sql.Insert(chip);
            }
        }

        internal static void Elasticsearch(Chip chip, int Threadid)
        {
            var client = new ElasticsearchClient();

            // get the url that we are indexing under
            string elastisearchrul = ConfigurationSettings.AppSettings["elastisearchrul"];
            var uri = new Uri(elastisearchrul);
            // split that bad boy out so that we can feed it to the method correctly
            var segments =
                uri.Segments
                    .Select(s => s.EndsWith("/") ? s.Substring(0, s.Length - 1) : s)
                    .ToArray();

            var elastisearchuriparts = new[]
            {
                String.Format("{0}://{1}", uri.Scheme, uri.Host),
                segments[1],
                segments[2],
            };

            // now send to Elasticsearch
            var indexResponse = client.Index(elastisearchuriparts[1], elastisearchuriparts[2], new { ClientIp = chip.ClientIp });
        }
        internal static void RawJsonPost(Chip chip, int Threadid)
        {
            // pass chip object for conversion object to JSON string
            string jsondoc = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(chip);

            // post the json doc
            string jsonposturl = ConfigurationSettings.AppSettings["jsonposturl"];
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(jsonposturl);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string jsonpostrequirelogin = ConfigurationSettings.AppSettings["jsonpostrequirelogin"];
                if (jsonpostrequirelogin == "true" )
                {
                    string jsonpostrequireloginusername = ConfigurationSettings.AppSettings["jsonpostrequireloginusername"];
                    string jsonpostrequireloginpassword = ConfigurationSettings.AppSettings["jsonpostrequireloginpassword"];
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        user = jsonpostrequireloginusername,
                        password = jsonpostrequireloginpassword
                    });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                }
                    streamWriter.Write(jsondoc);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponsedoc = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponsedoc.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
            }
        }

        internal static void ConsoleWrite(Chip chip, int Threadid)
        {
            //logchipper.utilities.logging.Logger.CLogger.WriteLog(logchipper.utilities.logging.ELogLevel.INFO, "adding chips to CONSOLE WRITE bucket");
            Console.Write(chip.ServerIp + " " + chip.UriPort + " " + chip.ClientIp + "Thread Id= {0}", Threadid);   
        }

    }
}
