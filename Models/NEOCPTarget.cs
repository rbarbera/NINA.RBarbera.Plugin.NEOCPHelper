using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    internal class NEOCPTarget {
        string Designation { get; set; }
        int Score { get; set; }
        DateTime Discovery { get; set; }
        string RA { get; set; }
        string Dec { get; set; }
        int V { get; set; }
        string Updated { get; set; }
        String Note { get; set; }
        int NObs { get; set; }
        double Arc { get; set; }
        double H { get; set; }
        int NotSeenIn { get; set; }
    }
}
