using Accord.Diagnostics;
using NINA.Astrometry;
using NINA.RBarbera.Plugin.NeocpHelper.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using TimeInterpolation.MathUtil;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models
{
    internal class NEOCPTarget
    {
        public NEOCPTarget(string neocpline) 
        {
            if (neocpline == null) return;

            this.Designation = neocpline.Substring(0, 7);
            this.Score = Int32.Parse(neocpline.Substring(8, 3));
            this.Discovery = neocpline.Substring(12, 12);
            this.RA = Double.Parse(neocpline.Substring(26, 7));
            this.Dec = Double.Parse(neocpline.Substring(34, 8));
            this.V = Double.Parse(neocpline.Substring(43, 4));
            this.NObs = Int32.Parse(neocpline.Substring(78, 4));
            this.Arc = Double.Parse(neocpline.Substring(83, 6));
            this.H = Double.Parse(neocpline.Substring(90, 4));
            this.NotSeenIn = Double.Parse(neocpline.Substring(95));
        }

        public NEOCPTarget(List<NEOCPEphemeride> ephemerides) {
            this.Ephemerides = ephemerides;
        }


        List<NEOCPEphemeride> _ephemerides;
        public List<NEOCPEphemeride> Ephemerides { 
            get => _ephemerides; 
            set
            {
                _ephemerides = value;
                if (_ephemerides.Count > 0 ) {
                    var first = _ephemerides.First();
                    this.RA = first.RA;
                    this.Dec = first.Dec;
                    this.V = first.V;
                    this.speedRA = first.speedRA;
                    this.speedDec = first.speedDec;
                    this.totalSpeed= first.totalSpeed;
                    this.ExpMax = first.ExpMax;
                }             
            }
        }
        public string Designation { get; set; }
        public int Score { get; set; }
        public string Discovery { get; set; }
        public double RA { get; set; }
        public double Dec { get; set; }
        public double V { get; set; }
        public int NObs { get; set; }
        public double Arc { get; set; }
        public double H { get; set; }
        public double NotSeenIn { get; set; }
        public double speedRA { get; set; }
        public double speedDec { get; set; }
        public double totalSpeed { get; set; }
        public int ExpMax { get; internal set; }

        public void SetScales(double pixelScale, int spotSize) {
            if (Ephemerides == null || Ephemerides.Count == 0)
                return;

            var first = Ephemerides[0];
            first.SetScales(pixelScale, spotSize);
            this.ExpMax = first.ExpMax;
        }

        public Coordinates Coordinates() {
            return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch.J2000);
        }

        public NEOCPEphemeride InterpolatedAtTime(DateTime atTime) {
            var aIndex = Ephemerides.FindLastIndex(ep => { return ep.DateTime.Ticks <= atTime.Ticks; });
            if (aIndex < 0)
                return Ephemerides.First();

            var bIndex = Math.Min(aIndex + 1, Ephemerides.Count - 1);

            var a = Ephemerides[aIndex];
            var b = Ephemerides[bIndex];

            var frac = MathUtil.Fraction(atTime.Ticks, a.DateTime.Ticks, b.DateTime.Ticks);
            var cRA = MathUtil.CyclicLERP(a.RA, b.RA, frac);
            var cDec = MathUtil.LERP(a.Dec, b.Dec, frac);
            var cV = MathUtil.LERP(a.V, b.V, frac);
            var cSRA = MathUtil.LERP(a.speedRA, b.speedRA, frac);
            var cSDec = MathUtil.LERP(a.speedDec, b.speedDec, frac);
            var cEMax = MathUtil.LERP(a.ExpMax, b.ExpMax, frac);

            var c = new NEOCPEphemeride(atTime,cRA,cDec,cV,cSRA,cSDec, cEMax);

            return c;
        }

        public NEOCPEphemeride LastInField(NEOCPEphemeride center, double fieldInArcMin) {
            var aIndex = Ephemerides.FindIndex(ep => { return ep.DateTime.Ticks > center.DateTime.Ticks; });
            var rest = Ephemerides.Skip(aIndex).ToList();
            if (AstroUtil.DegreeToArcmin(rest[0].Distance(center)) > fieldInArcMin)
                return rest[0];

            var bIndex = rest.FindLastIndex(ep => { return AstroUtil.DegreeToArcmin(ep.Distance(center)) <= fieldInArcMin; });
            return (bIndex >= 0) ? rest[bIndex] : rest.Last();
        }

        public NEOCPEphemeride InterpolatedAtDistance(NEOCPEphemeride start, double fieldInArcMin) {
            var d = this.LastInField(start, fieldInArcMin);
            var remainingField = fieldInArcMin - AstroUtil.DegreeToArcmin(d.Distance(start));
            var remainingTime = AstroUtil.ArcminToArcsec(remainingField) / d.totalSpeed;
            return this.InterpolatedAtTime(d.DateTime.AddMinutes(remainingTime));
        }
    }
}
