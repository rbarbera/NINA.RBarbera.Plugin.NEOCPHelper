using NINA.Core.Utility;
using NINA.Profile.Interfaces;
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
        public static List<NEOCPTarget> Get(IAstrometrySettings astrometrySettings) {
            var neos = GetNEOCP();
            var ephemerides = GetEphemerides(astrometrySettings);

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

        private static Dictionary<string, List<NEOCPEphemeride>> GetEphemerides(IAstrometrySettings astrometrySettings) {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");

            var strLong = String.Format("{0:f2} {1}", Math.Abs(astrometrySettings.Longitude), (astrometrySettings.Longitude > 0) ? "e" : "w");
            var strLat = String.Format("{0:f2}d", astrometrySettings.Latitude);
            var postData = String.Format("Parallax=0&long={0}&lat={1}&alt={2}&int=1&raty=h&mot=m&dmot=r&out=f&sun=a&W=a",strLong, strLat, astrometrySettings.Elevation);
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

        public static Dictionary<string, List<NEOCPEphemeride>> GetEphemerides(string obj, IAstrometrySettings astrometrySettings) {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");

            var strLong = String.Format("{0:f2} {1}", Math.Abs(astrometrySettings.Longitude), (astrometrySettings.Longitude > 0) ? "e" : "w");
            var strLat = String.Format("{0:f2}d", astrometrySettings.Latitude);
            var postData = String.Format("Parallax=0&long={0}&lat={1}&alt={2}&int=1&raty=h&mot=m&dmot=r&out=f&sun=a&W=j&obj={3}", strLong, strLat, astrometrySettings.Elevation,obj);
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
