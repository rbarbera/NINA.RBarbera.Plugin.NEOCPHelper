using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.RBarbera.Plugin.NeocpHelper.Models;
using NINA.RBarbera.Plugin.NEOCPHelper.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace NINA.RBarbera.Plugin.NeocpHelper.Utility {
    internal class MPCDownloader {
        public static List<NEOCPTarget> GetNEOList(IAstrometrySettings astrometrySettings, NeocpHelper neocpHelper) {
            var neos = GetNEOCP();
            var ephemerides = GetEphemerides(astrometrySettings, neocpHelper);

            var result = new List<NEOCPTarget>();

            neos.ForEach(e => { 
                if(ephemerides.ContainsKey(e.Designation)) {
                    e.Ephemerides = ephemerides[e.Designation];
                    result.Add(e);
                }
                    
            });
            return result;
        }

        public static Dictionary<string, List<NEOCPEphemeride>> GetNEOEphemerides(string obj, IAstrometrySettings astrometrySettings, NeocpHelper neocpHelper) {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");

            var query = NEOCPQueryString(astrometrySettings, neocpHelper) + "&" + FilterTarget(obj);
            var data = Encoding.ASCII.GetBytes(query);

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

        public static List<NEOCPEphemeride> GetMPCEphemerides(string obj, IAstrometrySettings astrometrySettings, NeocpHelper neocpHelper) {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/mpeph2.cgi");

            // Mangle names so they are accepted by MPC API
            var Type = "unknown";
            if (obj.IndexOf("(") > 1) {
                Type = "comet";
                obj = obj.Replace(" ", "+");
                obj = obj[..(obj.IndexOf("(") - 1)];
            }
            else if (obj.IndexOf(" ") > 1) {
                Type = "unnumbered body";
                obj = obj.Replace(" ", "+");
            }
            else if (obj.IndexOf("(") > 1) {
                Type = "numbered body";
                obj = obj.Replace("/", "+");
            }
            Logger.Info("Object " + obj + " identified as " + Type);

            var query = MPCQueryString(astrometrySettings, obj, neocpHelper);
            var data = Encoding.UTF8.GetBytes(query);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream()) {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var scanner = new MPCScanner(responseString);

            return scanner.ReadEphemerides().First().Value;
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


        private static string NEOCPQueryString(IAstrometrySettings astrometrySettings, NeocpHelper neocpHelper) {

            if (neocpHelper.ObservatoryCode != "") {
                return String.Format("Parallax=1&obscode={0}&int=1&raty=h&mot=m&dmot=r&out=f&sun=x&oalt=20", neocpHelper.ObservatoryCode);
            } else {
                var strLong = String.Format("{0:f2}", (astrometrySettings.Longitude > 0) ? Math.Abs(astrometrySettings.Longitude) : Math.Abs(360 + astrometrySettings.Longitude));
                var strLat = String.Format("{0:f2}", astrometrySettings.Latitude);


                return String.Format("Parallax=2&long={0}&lat={1}&alt={2}&int=1&raty=h&mot=m&dmot=r&out=f&sun=x&oalt=20", strLong, strLat, astrometrySettings.Elevation);
            }
        }

        private static string MPCQueryString(IAstrometrySettings astrometrySettings, string targetName, NeocpHelper neocpHelper) {
            var strLong = String.Format("{0:f2}", (astrometrySettings.Longitude > 0) ? Math.Abs(astrometrySettings.Longitude) : Math.Abs(360 + astrometrySettings.Longitude));
            var strLat = String.Format("{0:f2}", astrometrySettings.Latitude);

            return String.Format("ty=e&TextArea={4}&d=&l=&i=&u=h&uto=0&c={0}&long={1}&lat={2}&alt={3}&raty=a&s=c&m=m&igd=y&ibh=y&adir=S&oed=&e=-2&resoc=&tit=&bu=&ch=c&ce=f&js=f", neocpHelper.ObservatoryCode, strLong, strLat, astrometrySettings.Elevation, targetName);
        }


        private static string FilterTarget(string targetName) {
            var SelectedObject = "W=j";
            var objectByName = String.Format("obj={0}", targetName);
            return String.Format("{0}&{1}",SelectedObject, objectByName);
        }

        private static Dictionary<string, List<NEOCPEphemeride>> GetEphemerides(IAstrometrySettings astrometrySettings, NeocpHelper neocpHelper) {
            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");
            var query = NEOCPQueryString(astrometrySettings, neocpHelper) + "&" + neocpHelper.FilterString;
            var data = Encoding.ASCII.GetBytes(query);

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

