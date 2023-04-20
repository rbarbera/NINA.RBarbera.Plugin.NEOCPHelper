using NINA.Astrometry;
using NINA.RBarbera.Plugin.NeocpHelper.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {

    internal class NEOCPEphemeride
    {
        public NEOCPEphemeride(string s) 
        {
            if (s == null) return;

            this.DateTime = DateTime.ParseExact(s.Substring(0, 15),"yyyy MM dd HHmm",null);
            this.RA = AstroUtil.HMSToDegrees(s.Substring(18, 8));
            this.Dec = AstroUtil.DMSToDegrees(s.Substring(28, 8));
            this.V = Double.Parse(s.Substring(46, 4), CultureInfo.InvariantCulture);
            this.speedRA = Double.Parse(s.Substring(51, 7), CultureInfo.InvariantCulture);
            this.speedDec = Double.Parse(s.Substring(59, 7), CultureInfo.InvariantCulture);
        }

        public NEOCPEphemeride(DateTime dateTime, double rA, double dec, double v, double speedRA, double speedDec, int maxExp) {
            DateTime = dateTime;
            RA = rA;
            Dec = dec;
            V = v;
            this.speedRA = speedRA;
            this.speedDec = speedDec;
            this.ExpMax = maxExp;
        }

        public DateTime DateTime { get; set; }
        public double RA { get; set; }
        public double Dec { get; set; }
        public double V { get; set; }
        public double speedRA { get; set; }
        public double speedDec { get; set; }

        public double totalSpeed {
            get {
                return Math.Sqrt(speedRA * speedRA + speedDec * speedDec);
            }
        }

        public int ExpMax { get; set; }

        public void SetScales(double pixelScale, int spotSize) {
            var ppMin = this.totalSpeed / pixelScale;
            this.ExpMax = (int)Math.Ceiling(spotSize * 60 / ppMin);
        }

        public NEOCPField Field(double fieldDiameter) {
            var timeSpan = AstroUtil.ArcminToArcsec(fieldDiameter) / totalSpeed;
            var centerRA = Angle.ByDegree(AstroUtilExtension.ReducedRADegrees(RA + AstroUtil.ArcsecToDegree(timeSpan * speedRA) / 2.0));
            var centerDec = Angle.ByDegree(AstroUtilExtension.ReducedDecDegrees(Dec + AstroUtil.ArcsecToDegree(timeSpan * speedDec) / 2.0));

            return new NEOCPField(new Coordinates(centerRA, centerDec, Epoch.J2000), TimeSpan.FromMinutes(timeSpan));
        }

        /* Ψ=arccos(sinθ1sinθ2+cosθ1cosθ2cos(ϕ1−ϕ2)) */
        public double Distance(NEOCPEphemeride other) {
            var raA = Angle.ByDegree(RA);
            var raB = Angle.ByDegree(other.RA);
            var decA = Angle.ByDegree(Dec);
            var decB = Angle.ByDegree(other.Dec);
            var phi = Angle.ByRadians(Math.Acos(Math.Sin(decA.Radians) * Math.Sin(decB.Radians) + Math.Cos(decA.Radians) * Math.Cos(decB.Radians) * Math.Cos(raA.Radians - raB.Radians)));
            return phi.Degree;
        }

        public static NEOCPEphemeride MidPoint(NEOCPEphemeride a, NEOCPEphemeride b) {
            return new NEOCPEphemeride(
                new DateTime((a.DateTime.Ticks + b.DateTime.Ticks) / 2),
                (a.RA + b.RA) / 2,
                (a.Dec + b.Dec) / 2,
                (a.V + b.V) / 2,
                (a.speedRA + b.speedRA) / 2,
                (a.speedDec + b.speedDec) / 2,
                (a.ExpMax + b.ExpMax) / 2
            );
        }

        public Coordinates Coordinates {
            get {
                return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch.J2000);
            }
        }
    }
}
