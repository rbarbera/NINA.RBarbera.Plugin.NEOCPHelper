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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces.Mediator;
using System.Windows.Threading;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.SequenceItem.Imaging;

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
        private readonly ISequenceMediator sequenceMediator;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IApplicationMediator applicationMediator;
        private readonly ICameraMediator cameraMediator;
        private INighttimeCalculator nighttimeCalculator;

        [ImportingConstructor]
        public NEOCPListContainer(
                IProfileService profileService,
                ISequenceMediator sequenceMediator,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                ICameraMediator cameraMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.sequenceMediator = sequenceMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;
            this.cameraMediator = cameraMediator;


            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate(DateTime.Now.AddHours(4)));

            LoadTargetsCommand = new AsyncCommand<bool>(LoadTargets);
            CreateNEOFieldsCommand = new AsyncCommand<bool>(CreateDSOContainers);

            NEOCPInputTarget = new NEOCPInputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            NEOCPDSO = new NEOCPDeepSkyObject(string.Empty, new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000), string.Empty, profileService.ActiveProfile.AstrometrySettings.Horizon);
            NEOCPDSO.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now.AddHours(4)), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);

            NEOCPTargets = new AsyncObservableCollection<NEOCPObject>();

        }
        private Dispatcher _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public ICommand LoadTargetsCommand { get; set; }
        public ICommand CreateNEOFieldsCommand { get; set; }


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

        private AsyncObservableCollection<NEOCPObject> _neocpTargets;

        public AsyncObservableCollection<NEOCPObject> NEOCPTargets {
            get {
                return _neocpTargets;
            }
            set {
                _neocpTargets = value;
                RaisePropertyChanged();
            }
        }

        private NEOCPObject _SelectedNEO;

        [JsonProperty]
        public NEOCPObject SelectedNEO {
            get {
                return _SelectedNEO;
            }
            set {
                _SelectedNEO = value;
                
                LoadSingleTarget();
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<NEOCPField> _NEOFields;

        [JsonProperty]
        public AsyncObservableCollection<NEOCPField> NEOFields {
            get {
                return _NEOFields;
            }
            set {
                _NEOFields = value;
                RaiseAllPropertiesChanged();
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

        public string RecomendedExposure {
            get {
                if (SelectedNEO == null)
                    return "--";
                return String.Format("{0:f0}s", SelectedNEO.MaxExposure(0.66, 4));
            }
        }

        private void LoadSingleTarget() {
            if (SelectedNEO != null && SelectedNEO?.Designation != null) {
                var fl = profileService.ActiveProfile.TelescopeSettings.FocalLength;
                var cameraInfo = cameraMediator.GetInfo();
                var xSize = cameraInfo.XSize * cameraInfo.PixelSize / 1000.0;
                var ySize = cameraInfo.YSize * cameraInfo.PixelSize / 1000.0;

                var xAng = 2 * Math.Atan2(xSize / 2, fl);
                var yAng = 2 * Math.Atan2(ySize / 2, fl);
                var xArcsec = AstroUtil.DegreeToArcsec(AstroUtil.ToDegree(xAng));
                var yArcsec = AstroUtil.DegreeToArcsec(AstroUtil.ToDegree(yAng));

                var rise = NighttimeData.TwilightRiseAndSet.Rise ?? DateTime.Now;
                var set = NighttimeData.TwilightRiseAndSet.Set ?? DateTime.Now;

                Core.Model.CustomHorizon horizon = profileService.ActiveProfile.AstrometrySettings.Horizon;

                var starRise = GetNextRiseTime(horizon, SelectedNEO.Coordinates(), set.ToLocalTime());
                starRise = new DateTime(Math.Min(starRise.Ticks, rise.Ticks));
                var starSet = GetNextSetTime(horizon, SelectedNEO.Coordinates(), starRise.AddMinutes(10d));
                starSet = new DateTime(Math.Min(starSet.Ticks, rise.Ticks));

                NEOFields = new AsyncObservableCollection<NEOCPField>(SelectedNEO.ComputeFields(starRise, starSet , xArcsec, yArcsec));

                Target.TargetName = SelectedNEO.Designation;
                Target.InputCoordinates.Coordinates = SelectedNEO.Coordinates();
                Target.DeepSkyObject.Coordinates = SelectedNEO.Coordinates();

                NEOCPDSO.Coordinates = SelectedNEO.Coordinates();
                NEOCPDSO.Magnitude = SelectedNEO.V;
                AfterParentChanged();
            }
        }

        public Task<bool> CreateDSOContainers() {
            return Task<bool>.Run(() => {
                if (SelectedNEO == null || NEOFields.Count == 0)
                    return false;
                IDeepSkyObjectContainer myTemplate = null;
                var templates = sequenceMediator.GetDeepSkyObjectContainerTemplates();
                myTemplate = templates.Where(tp => tp.Name == "NEOCP_FIELD_TEMPLATE_ADV").First();
                var index = 1;
                var exposureTime = Math.Floor(SelectedNEO.MaxExposure(0.66, 4));

                foreach (NEOCPField field in NEOFields) {
                    DeepSkyObjectContainer fieldContainer = (DeepSkyObjectContainer)myTemplate.Clone();
                    fieldContainer.Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon) {
                        TargetName = SelectedNEO.Designation + "_field_" + index,
                        InputCoordinates = new InputCoordinates() {
                            Coordinates = field.Center
                        },
                        Rotation = 0
                    };
                    fieldContainer.Name = SelectedNEO.Designation + "_field_" + index;
                    fieldContainer.Items.ToList().ForEach(x => {
                        if (x is WaitForTime waitForTime) {
                            waitForTime.Hours = field.StartTime.Hour;
                            waitForTime.Minutes = field.StartTime.Minute;
                            waitForTime.Seconds = field.StartTime.Second;
                        }
                        if (x is NINA.Sequencer.Container.SequentialContainer sequencialContainer) {
                            sequencialContainer.Conditions.ToList().ForEach(y => {
                                if (y is TimeCondition timeCondition) {
                                    timeCondition.Hours = field.EndTime.Hour;
                                    timeCondition.Minutes = field.EndTime.Minute;
                                    timeCondition.Seconds = field.EndTime.Second;
                                }
                            });
                            sequencialContainer.Items.ToList().ForEach(y => {
                                if (y is TakeExposure takeExposure) {
                                    takeExposure.ExposureTime = exposureTime;
                                }
                            });
                        };
                    });
                    
                    index += 1;
                    _ = _dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
                        lock (Items) {
                            this.InsertIntoSequenceBlocks(100, fieldContainer);
                            Logger.Debug("Adding target container: " + fieldContainer);
                        }
                    }));
                }
                return true;
            });           
        }

        private DateTime GetNextSetTime(CustomHorizon horizon, Coordinates coords, DateTime startTime) {
            var start = startTime;
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, profileService.ActiveProfile.AstrometrySettings.Longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, coords.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, profileService.ActiveProfile.AstrometrySettings.Latitude, coords.Dec);
                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, profileService.ActiveProfile.AstrometrySettings.Latitude, coords.Dec);

                if ((horizon != null) && altitude < horizon.GetAltitude(azimuth)) {
                    break;
                } else if (altitude < 0) {
                    break;
                }


                start = start.AddHours(0.1);
            }
            return start;
        }

        private DateTime GetNextRiseTime(CustomHorizon horizon, Coordinates coords, DateTime startTime) {
            var start = startTime;
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, profileService.ActiveProfile.AstrometrySettings.Longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, coords.RA);

            for (double angle = hourAngle; angle < hourAngle + 24; angle += 0.1) {
                var degAngle = AstroUtil.HoursToDegrees(angle);
                var altitude = AstroUtil.GetAltitude(degAngle, profileService.ActiveProfile.AstrometrySettings.Latitude, coords.Dec);
                var azimuth = AstroUtil.GetAzimuth(degAngle, altitude, profileService.ActiveProfile.AstrometrySettings.Latitude, coords.Dec);

                if ((horizon != null) && altitude > horizon.GetAltitude(azimuth)) {
                    break;
                } else if (altitude > 0) {
                    break;
                }


                start = start.AddHours(0.1);
            }
            return start;
        }
        public override object Clone() {
            var clone = new NEOCPListContainer(profileService, sequenceMediator, nighttimeCalculator, framingAssistantVM, applicationMediator, cameraMediator, planetariumFactory) {
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
                if (NEOCPTargets.Count > 0) {
                    SelectedNEO = NEOCPTargets.First();
                }
                LoadSingleTarget();
                AfterParentChanged();
                return true;
            });
        }

        private AsyncObservableCollection<NEOCPObject> getNEOCPList() {

            var request = (HttpWebRequest)WebRequest.Create("https://cgi.minorplanetcenter.net/cgi-bin/confirmeph2.cgi");

            var postData = "Parallax=0&long=1.1 W&lat=39.5d&alt=0&int=1&raty=h&mot=m&dmot=r&out=f&sun=a";
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream()) {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();


            var scanner = new NEOCPScanner(responseString);
           
            var list = new AsyncObservableCollection<NEOCPObject>(scanner.ReadNEOS());
        
            return list;
        }
    }
}
