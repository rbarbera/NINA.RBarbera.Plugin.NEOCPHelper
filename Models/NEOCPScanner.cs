using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPScanner: StringReader
    {
        public NEOCPScanner(string s): base(s)
        {
        }

        public List<NEOCPObject> ReadNEOS()
        {
            var neos = new List<NEOCPObject>();
            do
            {
                this.SkipTo("<b>");
                var des = this.GetUntil("</b>");
                if (des == string.Empty)
                    break;
                this.SkipTo("<pre>");
                var ep = this.GetUntil("</pre>");
                var item = new NEOCPObject(des);
                var epList = new List<NEOCPEphemeride>();
                foreach (string line in ep.Split("\r\n".ToCharArray()))
                {
                    try
                    {
                        epList.Add(new NEOCPEphemeride(line));
                    } catch(Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                if (epList.Count > 0) {
                    item.Ephemerides = epList;
                    neos.Add(item);
                }
            } while (true);
            return neos;
        }

        public void SkipTo(string label)
        {
            var idx = 0;
            do
            {
                var current = Read();
                if (current < 0)
                    break;
                if (current == label[idx])
                {
                    idx++;
                } else
                {
                    idx = 0;
                }
                if (idx == label.Length)
                    break;

            } while (true);
        }

        public string GetUntil(string label)
        {
            var idx = 0;
            var sb = new StringBuilder();
            do
            {
                var current = Read();
                if (current < 0)
                    break;
                if (current == label[idx])
                {
                    idx++;
                }
                else
                {
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
