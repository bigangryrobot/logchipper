using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace logchipper
{
    public class Chipper
    {
        public Chip ParseChip(string logLine, string sitename)
        {
            Chip chip;

            if (!logLine.StartsWith("#")) 
            {
                string[] splitChip = logLine.Split(new char[] { ' ' });
                chip = new Chip
                {
                    Date = splitChip[0],
                    Time = splitChip[1],
                    SiteName = sitename,
                    ServerName = splitChip[3],
                    ServerIp = splitChip[4],
                    Method = splitChip[5],
                    UriStem = splitChip[6],
                    UriQuery = splitChip[7],
                    ServerPort = splitChip[8],
                    UserName = splitChip[9],
                    ClientIp = splitChip[10],
                    Version = splitChip[11],
                    UserAgent = splitChip[12],
                    Cookie = splitChip[13],
                    Referer = splitChip[14],
                    Host = splitChip[15],
                    Status = splitChip[16],
                    SubStatus = splitChip[17],
                    W32Status = splitChip[18],
                    BytesIn = splitChip[19],
                    BytesOut = splitChip[20],
                    TimeTaken = splitChip[21], 
                };
            }
            else
            {
                chip = null;
            }

            return chip;
        }
    }
}
