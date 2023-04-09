using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPTarget {
        public NEOCPTarget(string neocpline) {
            this.Designation = neocpline.Substring(0, 7);
            this.Score = Int32.Parse(neocpline.Substring(8, 3));
            this.Discovery = neocpline.Substring(12, 12);
            this.RA = Double.Parse(neocpline.Substring(26, 7));
            this.Dec = Double.Parse(neocpline.Substring(34, 8));
            this.V = Double.Parse(neocpline.Substring(43, 4));
            this.Note = neocpline.Substring(48, 31);
            this.NObs = Int32.Parse(neocpline.Substring(78, 4));
            this.Arc = Double.Parse(neocpline.Substring(83,6));
            this.H = Double.Parse(neocpline.Substring(90, 4));
            this.NotSeenIn = Double.Parse(neocpline.Substring(95));
        }
        public string Designation { get; set; }
        public int Score { get; set; }
        public string Discovery { get; set; }
        public double RA { get; set; }
        public double Dec { get; set; }
        public double V { get; set; }
        public string Note { get; set; }
        public int NObs { get; set; }
        public double Arc { get; set; }
        public double H { get; set; }
        public double NotSeenIn { get; set; }

        public Coordinates Coordinates() {
            return new Coordinates(Angle.ByDegree(AstroUtil.HoursToDegrees(RA)), Angle.ByDegree(Dec), Epoch.J2000);
        }
    }
}
