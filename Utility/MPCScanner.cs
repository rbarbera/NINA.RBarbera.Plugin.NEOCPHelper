using NINA.RBarbera.Plugin.NeocpHelper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NEOCPHelper.Utility {
    internal class MPCScanner : StringReader {
        public MPCScanner(string s) : base(s) {
        }

        public Dictionary<string,List<NEOCPEphemeride>> ReadEphemerides() {
            var ephemerides = new Dictionary<string, List<NEOCPEphemeride>>();
            do {
                this.SkipTo("<b>");
                var des = this.GetUntil("</b>");
                if (des == string.Empty)
                    break;
                this.SkipTo("<pre>");
                var ep = this.GetUntil("</pre>");
                var list = new List<NEOCPEphemeride>();
                foreach (string line in ep.Split("\r\n".ToCharArray())) {
                    try {
                        list.Add(NEOCPEphemeride.FromMPC(line));
                    } catch (Exception e) {
                        Debug.WriteLine(e);
                    }
                }
                if (list.Count > 0) {
                    ephemerides.Add(des, list);
                }
            } while (true);
            return ephemerides;
        }

        public void SkipTo(string label) {
            var idx = 0;
            do {
                var current = Read();
                if (current < 0)
                    break;
                if (current == label[idx]) {
                    idx++;
                } else {
                    idx = 0;
                }
                if (idx == label.Length)
                    break;

            } while (true);
        }

        public string GetUntil(string label) {
            var idx = 0;
            var sb = new StringBuilder();
            do {
                var current = Read();
                if (current < 0)
                    break;
                if (current == label[idx]) {
                    idx++;
                } else {
                    sb.Append((char)current);
                    idx = 0;
                }
                if (idx == label.Length)
                    break;

            } while (true);
            return sb.ToString();
        }
    }
}
