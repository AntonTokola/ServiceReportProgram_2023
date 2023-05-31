using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using CsvHelper;
using System.IO;

namespace ServiceReportProgram
{
    class Settings
    {
        public int setConnectionMissed = 2;
        public string setConnectionMissedInfo = "Laite ei ole ottanut yhteyttä kahteen vuorokauteen.";
        bool runDebug = false;
        //reg ON or OFF

        public class CookieAwareWebClient : WebClient
        {
            private CookieContainer cookie = new CookieContainer();

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).CookieContainer = cookie;
                }
                return request;
            }
        }


    }
}

