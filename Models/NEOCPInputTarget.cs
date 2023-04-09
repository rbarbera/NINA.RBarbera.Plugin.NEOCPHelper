#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Astrometry;

namespace NINA.RBarbera.Plugin.NeocpHelper.Models {
    [JsonObject(MemberSerialization.OptIn)]
    internal class NEOCPInputTarget: InputTarget {
        public NEOCPInputTarget(Angle latitude, Angle longitude, CustomHorizon horizon) : base(latitude, longitude, horizon) {
        }
    }
}
