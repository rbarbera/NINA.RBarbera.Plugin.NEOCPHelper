using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Model;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NINA.Image.FileFormat.XISF.XISFImageProperty.Observation;

namespace NINA.RBarbera.Plugin.NeocpHelper.Utility {

    public class ObservabilityWindow {
        public ObservabilityWindow(DateTime starRise, DateTime starSet) {
            StartTime = starRise;
            EndTime = starSet;
        }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
    public static class AstroUtilExtension {

        public static double Fraction(double x, double a, double b) {
            return (x - a) / (b - a);
        }
        public static double Interpolate(double a, double b, double frac) {
            return a + (b - a) * frac;
        }

        public static double ReducedRADegrees(double raDegrees) {
            while (raDegrees > 360) raDegrees -= 360;
            while (raDegrees < 0) raDegrees += 360;
            return raDegrees;
        }

        public static double ReducedDecDegrees(double decDegrees) {
            while (decDegrees > 90) decDegrees -= 90;
            while (decDegrees < -90) decDegrees += 90;
            return decDegrees;
        }

        public static DateTime GetNextSetTime(IAstrometrySettings astrometrySettings, Coordinates coords, DateTime startTime) {
            var horizon = astrometrySettings.Horizon;
            var start = startTime;
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, astrometrySettings.Longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, coords.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, astrometrySettings.Latitude, coords.Dec);
                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, astrometrySettings.Latitude, coords.Dec);

                if ((horizon != null) && altitude < horizon.GetAltitude(azimuth)) {
                    break;
                } else if (altitude < 0) {
                    break;
                }


                start = start.AddHours(0.1);
            }
            return start;
        }

        public static DateTime GetNextRiseTime(IAstrometrySettings astrometrySettings, Coordinates coords, DateTime startTime) {
            var horizon = astrometrySettings.Horizon;
            var start = startTime;
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, astrometrySettings.Longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, coords.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, astrometrySettings.Latitude, coords.Dec);
                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, astrometrySettings.Latitude, coords.Dec);

                if ((horizon != null) && altitude > horizon.GetAltitude(azimuth)) {
                    break;
                } else if (altitude > 0) {
                    break;
                }


                start = start.AddHours(0.1);
            }
            return start;
        }

        public static ObservabilityWindow ObservabilityWindow(RiseAndSetEvent riseAndSet,IAstrometrySettings astrometrySettings, Coordinates coordinates) {
            var now = DateTime.Now;
            var rise = riseAndSet.Rise ?? now;
            var set = new DateTime(Math.Max(riseAndSet.Set?.Ticks ?? now.Ticks, now.Ticks));

            Core.Model.CustomHorizon horizon = astrometrySettings.Horizon;

            var starRise = GetNextRiseTime(astrometrySettings, coordinates, set.ToLocalTime());
            starRise = new DateTime(Math.Min(starRise.Ticks, rise.Ticks));
            var starSet = GetNextSetTime(astrometrySettings, coordinates, starRise.AddMinutes(2d));
            starSet = new DateTime(Math.Min(starSet.Ticks, rise.Ticks));

            return new ObservabilityWindow(starRise, starSet);
        }

    }
}
