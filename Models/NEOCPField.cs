using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPField {
        public NEOCPField(DateTime startTime, DateTime endTime, Coordinates coordinates) {
            StartTime = startTime;
            EndTime = endTime;
            Center = coordinates;
        }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Coordinates Center { get; set; }

        public double Duration {
            get => (EndTime - StartTime).TotalMinutes;
        }

    }
}
