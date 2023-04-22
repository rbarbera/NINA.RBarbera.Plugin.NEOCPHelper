using System;
using NINA.Astrometry;

namespace TimeInterpolation.MathUtil
{
    public static class MathUtil {

        public static double Fraction(double x, double a, double b) {
            return (x - a) / (b - a);
        }

        public static double LERP(double a, double b, double t) {
            return a + (b-a) * t;
        }

        public static long LERP(long a, long b, double t) {
            return (long)LERP((double)a, (double)b, t);
        }

        public static int LERP(int a, int b, double t) {
            return (int)LERP((double)a, (double)b, t);
        }

        public static double Normalized(double angle) {
            return AstroUtil.EuclidianModulus(angle, 360);
        }

        public static double NormalicesDecDegrees(double decDegrees) {
            while (decDegrees > 90) decDegrees -= 90;
            while (decDegrees < -90) decDegrees += 90;
            return decDegrees;
        }

        public static double Mean(double a, double b) {
            return LERP(a, b, 0.5);
        }

        public static long Mean(long a, long b) {
            return (long)Math.Floor(LERP((double)a, (double)b, 0.5));
        }

        public static int Mean(int a, int b) {
            return (int)Math.Floor(LERP((double)a, (double)b, 0.5));
        }

        public static double CyclicMean(double aDegrees, double bDegrees) {
            return CyclicMean(Angle.ByDegree(aDegrees), Angle.ByDegree(bDegrees)).Degree;
        }

        public static Angle CyclicMean(Angle a, Angle b) {
            return CyclicLERP(a, b, 0.5);
        }

        public static double CyclicLERP(double aDegrees, double bDegrees, double t) {
            return CyclicLERP(Angle.ByDegree(aDegrees), Angle.ByDegree(bDegrees),t).Degree;
        }

        public static Angle CyclicLERP(Angle a, Angle b, double t) {
            var sumSin = (1-t)*Math.Sin(a.Radians) + t*Math.Sin(b.Radians);
            var sumCos = (1-t)*Math.Cos(a.Radians)+ t*Math.Cos(b.Radians);
            var result = Math.Atan2(sumSin, sumCos);
            return Angle.ByRadians(AstroUtil.EuclidianModulus(result, 2*Math.PI));
        }
    }
}
