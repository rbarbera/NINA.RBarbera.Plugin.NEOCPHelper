#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Expression.Interactivity.Core;
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
using System.Runtime.CompilerServices;
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
using System.Diagnostics;

namespace NINA.RBarbera.Plugin.NeocpHelper.Sequencer.Containers {
    [ExportMetadata("Name", "NEOCP object list container")]
    [ExportMetadata("Description", "A container to choose a NEOCP target.")]
    [ExportMetadata("Icon", "Plugin_Test_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    internal class NEOCPListContainer : SequenceContainer, IDeepSkyObjectContainer {

        private readonly IProfileService profileService;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IApplicationMediator applicationMediator;
        private INighttimeCalculator nighttimeCalculator;

        [ImportingConstructor]
        public NEOCPListContainer(
                IProfileService profileService,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;

            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate(DateTime.Now.AddHours(4)));

            LoadTargetsCommand = new AsyncCommand<bool>(LoadTargets);

            NEOCPInputTarget = new NEOCPInputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            NEOCPDSO = new NEOCPDeepSkyObject(string.Empty, new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000), string.Empty, profileService.ActiveProfile.AstrometrySettings.Horizon);
            NEOCPDSO.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now.AddHours(4)), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);

            NEOCPTargets = new AsyncObservableCollection<NEOCPTarget>();

        }

        public ICommand LoadTargetsCommand { get; set; }

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

        private AsyncObservableCollection<NEOCPTarget> _neocpTargets;

        public AsyncObservableCollection<NEOCPTarget> NEOCPTargets {
            get {
                return _neocpTargets;
            }
            set {
                _neocpTargets = value;
                RaisePropertyChanged();
            }
        }

        private NEOCPTarget _SelectedNEO;

        [JsonProperty]
        public NEOCPTarget SelectedNEO {
            get {
                return _SelectedNEO;
            }
            set {
                _SelectedNEO = value;
                LoadSingleTarget();
                RaisePropertyChanged();
            }
        }

        private NEOCPDeepSkyObject _neocpDSO;

        [JsonProperty]
        public NEOCPDeepSkyObject NEOCPDSO {
            get {
                return _neocpDSO;
            }
            set {
                _neocpDSO = value;
                RaisePropertyChanged();
            }
        }

        private void LoadSingleTarget() {
            if (SelectedNEO != null && SelectedNEO?.Designation != null) {
                Target.TargetName = SelectedNEO.Designation;
                Target.InputCoordinates.Coordinates = SelectedNEO.Coordinates();
                Target.DeepSkyObject.Coordinates = SelectedNEO.Coordinates();

                NEOCPDSO.Coordinates = SelectedNEO.Coordinates();
                NEOCPDSO.Magnitude = SelectedNEO.V;
                AfterParentChanged();
            }
        }

        public override object Clone() {
            var clone = new NEOCPListContainer(profileService, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory) {
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

        private Boolean _LoadingTargets = false;
        public Boolean LoadingTargets {
            get { return _LoadingTargets; }
            set {
                _LoadingTargets = value;
                RaisePropertyChanged();
            }
        }

        private Task<bool> LoadTargets(object obj) {
            LoadingTargets = true;

            return Task.Run(() => {
                NEOCPTargets = getNEOCPList();
                LoadingTargets = false;
                SelectedNEO = NEOCPTargets.First();
                LoadSingleTarget();
                AfterParentChanged();
                return true;
            });
        }

        private AsyncObservableCollection<NEOCPTarget> getNEOCPList() {
            var url = $"https://minorplanetcenter.net/iau/NEO/neocp.txt";
            WebRequest request = WebRequest.Create(url);
            request.Timeout = 30 * 60 * 1000;
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = request.Credentials;
            WebResponse response = (WebResponse)request.GetResponse();
           
            var list = new AsyncObservableCollection<NEOCPTarget>();
            using (var sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    list.Add(new NEOCPTarget(line));
                }
            }
            return list;
        }
    }
}
