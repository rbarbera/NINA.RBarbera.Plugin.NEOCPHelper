using NINA.Astrometry;
using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPDeepSkyObject: DeepSkyObject {
        public NEOCPDeepSkyObject(string id, Coordinates coords, string imageRepository, CustomHorizon customHorizon) : base(id, coords, imageRepository, customHorizon) {
        }
    }
}
