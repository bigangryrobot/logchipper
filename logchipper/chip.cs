using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace logchipper
{
    public class Chip
    {
        //#Fields: date time s-sitename s-computername s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username
        //  c-ip cs-version cs(User-Agent) cs(Cookie) cs(Referer) cs-host sc-status sc-substatus
        //  sc-win32-status sc-bytes cs-bytes time-taken

        public string Date { get; set; }
        public string Time { get; set; }
        public string TimeTaken { get; set; }
        public string ServerIp { get; set; }
        public string Method { get; set; }
        public string UriStem { get; set; }
        public string UriQuery { get; set; }
        public string UriPort { get; set; }
        public string UserName { get; set; }
        public string ClientIp { get; set; }
        public string ComputerName { get; set; }
        public string SiteName { get; set; }
        public string ServerName { get; set; }
        public string ServerPort { get; set; }
        public string Status { get; set; }
        public string SubStatus { get; set; }
        public string Host { get; set; }
        public string UserAgent { get; set; }
        public string Referer { get; set; }
        public string BytesOut { get; set; }
        public string BytesIn { get; set; }
        public string W32Status { get; set; }
        public string Cookie { get; set; }
        public string Version { get; set; }

    }
}
