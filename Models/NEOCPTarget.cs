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
                    this.TMax = first.TMax;
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
        public int TMax { get; internal set; }

        public void SetScales(double pixelScale, int spotSize, int usedFieldArcmin) {
            if (Ephemerides == null || Ephemerides.Count == 0)
                return;

            var first = Ephemerides[0];
            first.SetScales(pixelScale, spotSize, usedFieldArcmin);
            this.TMax = first.TMax;
            this.ExpMax = first.ExpMax;
        }

        public Coordinates Coordinates() {
            return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch.J2000);
        }

    }
}
