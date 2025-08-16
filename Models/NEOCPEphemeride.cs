using NINA.Astrometry;
using NINA.RBarbera.Plugin.NeocpHelper.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TimeInterpolation.MathUtil;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {

    internal class NEOCPEphemeride
    {

        static public NEOCPEphemeride FromNEOCP(string s) {
            if (s == null) return null;

            var dateTime = DateTime.ParseExact(s.Substring(0, 15), "yyyy MM dd HHmm", null);
            var ra = AstroUtil.HMSToDegrees(s.Substring(18, 8));
            var dec = AstroUtil.DMSToDegrees(s.Substring(28, 8));
            var v = Double.Parse(s.Substring(46, 4), CultureInfo.InvariantCulture);
            var speedRA = Double.Parse(s.Substring(51, 7), CultureInfo.InvariantCulture);
            var speedDec = Double.Parse(s.Substring(59, 5), CultureInfo.InvariantCulture);

            return new NEOCPEphemeride(dateTime, ra, dec, v, speedRA, speedDec, 0);
        }

        static public NEOCPEphemeride FromMPC(string s) {
            if (s == null) return null;

            var dateTime = DateTime.ParseExact(s.Substring(0, 15), "yyyy MM dd HHmm", null);
            var ra = AstroUtil.HMSToDegrees(s.Substring(18, 8));
            var dec = AstroUtil.DMSToDegrees(s.Substring(29, 9));
            var v = Double.Parse(s.Substring(69, 4), CultureInfo.InvariantCulture);
            var speedRA = Double.Parse(s.Substring(76, 7), CultureInfo.InvariantCulture);
            var speedDec = Double.Parse(s.Substring(85, 7), CultureInfo.InvariantCulture);

            return new NEOCPEphemeride(dateTime, ra, dec, v, speedRA, speedDec, 0);
        }

        public NEOCPEphemeride(DateTime dateTime, double rA, double dec, double v, double speedRA, double speedDec, double maxExp) {
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

        public double ExpMax { get; set; }

        public void SetScales(double pixelScale, int spotSize) {
            var ppMin = this.totalSpeed / pixelScale;
            this.ExpMax = Math.Round(spotSize * 60 / ppMin, 1);
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
                new DateTime(MathUtil.Mean(a.DateTime.Ticks, b.DateTime.Ticks)),
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

