using Accord.Diagnostics;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models
{
    internal class NEOCPTarget
    {
        public NEOCPTarget(string neocpline) 
        {
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

        public double totalSpeed {
            get {
                return Math.Sqrt(speedRA * speedRA + speedDec * speedDec);
            }
        }

        public double PPMin { get; internal set; }
        public int ExpMax { get; internal set; }
        public int TMax { get; internal set; }


        public void SetScales(double pixelScale, int spotSize, int usedFieldArcmin) {
            this.PPMin = this.totalSpeed/ pixelScale;
            this.ExpMax = (int)Math.Ceiling(60.0 * spotSize / this.PPMin);
            this.TMax = (int)Math.Ceiling(usedFieldArcmin * 60 / totalSpeed);
        }

        public double MaxExposure(double pixelScale, int spotSize) {
            var speed = Math.Max(Math.Abs(speedRA), Math.Abs(speedDec));
            return (pixelScale * spotSize) / speed * 60.0;
        } 

        public List<NEOCPField> ComputeFields(DateTime startTime, DateTime endTime, double XSize, double YSize) {
            var raSpan = Math.Abs(XSize / speedRA);
            var decSpan = Math.Abs(YSize / speedDec);
            var span = Math.Min(raSpan, decSpan);
            System.Diagnostics.Debug.WriteLine("{0}", span);

            var initialTime = startTime;
            var eph = Ephemerides.First();
            var startRA = eph.RA;
            var startDec = eph.Dec;


            var fields = new List<NEOCPField>();
            do {
                var computedEnd = initialTime.AddMinutes(span);
                var finalTime = new DateTime(Math.Min(endTime.Ticks, computedEnd.Ticks));
                var realSpan = finalTime - initialTime;
                var endRA = startRA + (realSpan.TotalMinutes * speedRA)/3600.0;
                var endDec = startDec + (realSpan.TotalMinutes * speedDec)/3600.0;

                var midRA = (endRA + startRA) / 2.0;
                var midDec = (endDec + startDec) / 2.0;
                var newField = new NEOCPField(initialTime, finalTime, new Coordinates(midRA, midDec, Epoch.J2000, Astrometry.Coordinates.RAType.Degrees));

                fields.Add(newField);
                if (finalTime.Ticks >= endTime.Ticks)
                    break;
                startRA = endRA;
                startDec = endDec;
                initialTime = finalTime;
            } while (true);

            return fields;
        }
        public Coordinates Coordinates() {
            return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch.J2000);
        }

    }
}
