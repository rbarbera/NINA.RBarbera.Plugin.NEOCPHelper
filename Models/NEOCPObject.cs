using Accord.Diagnostics;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models
{
    internal class NEOCPObject
    {
        public NEOCPObject(string s) 
        {
            this.Designation= s;
        }

        List<NEOCPEphemeride> _ephemerides;
        public List<NEOCPEphemeride> Ephemerides { 
            get => _ephemerides; 
            set
            {
                _ephemerides = value;
                if (_ephemerides.Count > 0 ) {
                    var first = _ephemerides.First();
                    this.RA = AstroUtil.DegreesToHMS(first.RA);
                    this.Dec = AstroUtil.DegreesToDMS(first.Dec);
                    this.V = first.V;
                    this.speedRA = first.speedRA;
                    this.speedDec = first.speedDec;
                }             
            }
        }
        public string Designation { get; set; }
        public string RA { get; set; }
        public string Dec { get; set; }
        public double V { get; set; }
        public double speedRA { get; set; }
        public double speedDec { get; set; }

        public List<NEOCPField> ComputeFields(DateTime startTime, DateTime endTime, double XSize, double YSize) {
            var raSpan = Math.Abs(XSize / speedRA);
            var decSpan = Math.Abs(YSize / speedDec);
            var span = Math.Min(raSpan, decSpan);
            System.Diagnostics.Debug.WriteLine("{0}", span);

            var initialTime = startTime;
            var fields = new List<NEOCPField>();
            do {
                var computedEnd = initialTime.AddMinutes(span);
                var finalTime = new DateTime(Math.Min(endTime.Ticks, computedEnd.Ticks));
                var newField = new NEOCPField(initialTime, finalTime);
                fields.Add(newField);
                if (finalTime.Ticks >= endTime.Ticks)
                    break;
                initialTime = finalTime;
            } while (true);

            return fields;
        }
        public Coordinates Coordinates() {
            return new Coordinates(Angle.ByDegree(AstroUtil.HMSToDegrees(RA)), Angle.ByDegree(AstroUtil.DMSToDegrees(Dec)), Epoch.J2000);
        }
    }
}
