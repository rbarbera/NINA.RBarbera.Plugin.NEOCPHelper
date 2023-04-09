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
using NINA.Sequencer.Container;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Core.Utility;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Astrometry.Interfaces;
using System.Net;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Data;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;
using NINA.Sequencer.Utility.DateTimeProvider;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Serialization;
using NINA.Core.Model;
using NINA.Core.Enum;
using NINA.Sequencer;
using CsvHelper.Configuration;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile;
using NINA.WPF.Base.Mediator;
using NINA.RBarbera.Plugin.NeocpHelper.Models;

namespace NINA.RBarbera.Plugin.NeocpHelper.Sequencer.Containers {
    [ExportMetadata("Name", "NEOCP object list container")]
    [ExportMetadata("Description", "A container to choose a NEOCP target.")]
    [ExportMetadata("Icon", "Plugin_Test_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    internal class NEOCPListContainer : SequenceContainer, IDeepSkyObjectContainer {

        private readonly IProfileService ProfileService;
        private readonly IFramingAssistantVM FramingAssistantVM;
        private readonly IPlanetariumFactory PlanetariumFactory;
        private readonly IApplicationMediator ApplicationMediator;
        private INighttimeCalculator NighttimeCalculator;

        [ImportingConstructor]
        public NEOCPListContainer(
                IProfileService profileService,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.ProfileService = profileService;
            this.NighttimeCalculator = nighttimeCalculator;
            this.ApplicationMediator = applicationMediator;
            this.FramingAssistantVM = framingAssistantVM;
            this.PlanetariumFactory = planetariumFactory;

            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate(DateTime.Now.AddHours(4)));

            NEOCPInputTarget = new NEOCPInputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);

        }

        private NEOCPInputTarget _target;
        public NEOCPInputTarget NEOCPInputTarget {
            get => _target;
            set {
                _target = value;
                RaisePropertyChanged();
            } 
        }

        public InputTarget Target { 
            get => NEOCPInputTarget; 
            set {
                NEOCPInputTarget.TargetName = value.TargetName;
                NEOCPInputTarget.InputCoordinates = value.InputCoordinates;
                NEOCPInputTarget.Rotation = value.Rotation;
            }
        }

        public NighttimeData NighttimeData { get; private set; }

        public override object Clone() {
            var clone = new NEOCPListContainer(ProfileService, NighttimeCalculator, FramingAssistantVM, ApplicationMediator, PlanetariumFactory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition))
            };
            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.Rotation = this.Target.Rotation;
            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }
    }
}
