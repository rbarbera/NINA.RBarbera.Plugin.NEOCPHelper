using NINA.Core.Utility;
using NINA.RBarbera.Plugin.NeocpHelper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Utility {
    internal class NEOCPDownloader {
        public static List<NEOCPTarget> Get() {
            var ephemerides = GetEphemerides();
            var neos = GetNEOCP();
            neos.ForEach(e => { e.Ephemerides = ephemerides[e.Designation]; } );
            return neos;
        }

        private static List<NEOCPTarget> GetNEOCP() {
            var url = $"https://minorplanetcenter.net/iau/NEO/neocp.txt";
            WebRequest request = WebRequest.Create(url);
            request.Timeout = 30 * 60 * 1000;
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = request.Credentials;
            WebResponse response = (WebResponse)request.GetResponse();

            var list = new List<NEOCPTarget>();
            using (var sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    list.Add(new NEOCPTarget(line));
                }
            }
            return list;
        }

        private static Dictionary<string, List<NEOCPEphemeride>> GetEphemerides() {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");

            var postData = "Parallax=0&long=1.1 W&lat=39.5d&alt=0&int=1&raty=h&mot=m&dmot=r&out=f&sun=a";
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream()) {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var scanner = new NEOCPScanner(responseString);

            return scanner.ReadEphemerides();
        }
    }
}
