using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {

    public class RAConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double input = (double)value;
            return AstroUtil.DegreesToHMS(input);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    public class DecConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double input = (double)value;
            return AstroUtil.DegreesToDMS(input);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    internal class NEOCPEphemerides {

        public NEOCPEphemerides(string des) {
            this.Designation = des;
        }

        public string Designation { get; set; }
        public List<NEOCPEphemeride> Ephemerides { get; set; }
    }
    internal class NEOCPEphemeride
    {
        public NEOCPEphemeride(string s) 
        {
            this.DateTime = DateTime.ParseExact(s.Substring(0, 15),"yyyy MM dd HHmm",null);
            this.RA = AstroUtil.HMSToDegrees(s.Substring(18, 8));
            this.Dec = AstroUtil.DMSToDegrees(s.Substring(28, 8));
            this.V = Double.Parse(s.Substring(46, 4), CultureInfo.InvariantCulture);
            this.speedRA = Double.Parse(s.Substring(51, 7), CultureInfo.InvariantCulture);
            this.speedDec = Double.Parse(s.Substring(59, 7), CultureInfo.InvariantCulture);
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

        public int ExpMax { get; internal set; }
        public int TMax { get; internal set; }

        public void SetScales(double pixelScale, int spotSize, int usedFieldArcmin) {
            var ppMin = this.totalSpeed / pixelScale;
            this.ExpMax = (int)Math.Ceiling(spotSize * 60 / ppMin);
            this.TMax = (int)Math.Ceiling(usedFieldArcmin * 60 / totalSpeed);
        }

        public double MaxExposure(double pixelScale, int spotSize) {
            var speed = Math.Max(Math.Abs(speedRA), Math.Abs(speedDec));
            return (pixelScale * spotSize) / speed * 60.0;
        }
        public Coordinates Coordinates {
            get {
                return new Coordinates(Angle.ByDegree(RA), Angle.ByDegree(Dec), Epoch.J2000);
            }
        }
    }
}
