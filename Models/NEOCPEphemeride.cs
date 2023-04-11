using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPEphemeride
    {
        public NEOCPEphemeride(string s) 
        {
            this.DateTime = DateTime.ParseExact(s.Substring(0, 15),"yyyy MM dd HHmm",null);
            this.RA = s.Substring(18, 8);
            this.Dec = s.Substring(28, 8);
            this.V = Double.Parse(s.Substring(46, 4), CultureInfo.InvariantCulture);
            this.speedRA = Double.Parse(s.Substring(51, 7), CultureInfo.InvariantCulture);
            this.speedDec = Double.Parse(s.Substring(59, 7), CultureInfo.InvariantCulture);
        }

        public DateTime DateTime { get; set; }
        public string RA { get; set; }
        public string Dec { get; set; }
        public double V { get; set; }
        public double speedRA { get; set; }
        public double speedDec { get; set; }
    }
}
