using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPField {
        public NEOCPField( Coordinates coordinates, TimeSpan duration) {
            Duration = duration;
            Center = coordinates;
        }

        public Coordinates Center { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
