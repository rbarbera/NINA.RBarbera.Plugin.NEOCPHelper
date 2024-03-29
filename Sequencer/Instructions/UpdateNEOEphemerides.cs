﻿using Newtonsoft.Json;
using NINA.Astrometry.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.RBarbera.Plugin.NeocpHelper.Utility;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using NINA.Sequencer.Container;
using Accord.Diagnostics;
using System.Diagnostics;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.RBarbera.Plugin.NeocpHelper.Models;
using Serilog.Debugging;

namespace NINA.RBarbera.Plugin.NeocpHelper.Sequencer.Instructions {


    [ExportMetadata("Name", "Update MPC object session")]
    [ExportMetadata("Description", "Get the most recent ephemerides for the MPC object defined on the container and update coordinates, time conditions and exposure")]
    [ExportMetadata("Icon", "ImpactorSVG")]
    [ExportMetadata("Category", "NEOCP Helper")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateNEOEphemerides : SequenceItem, IValidatable {

        private IList<string> issues = new List<string>();
        private int _maxTrackLenght;

        private readonly IProfileService profileService;
        private readonly ISequenceMediator sequenceMediator;
        private readonly ICameraMediator cameraMediator;
        private INighttimeCalculator nighttimeCalculator;
        private NeocpHelper neocpHelper;
        private double _sensorAreaUsage;
        private int _ephemerisSourceIndex;

        [ImportingConstructor]
        public UpdateNEOEphemerides(IProfileService profileService,
                ISequenceMediator sequenceMediator,
                ICameraMediator cameraMediator,
                INighttimeCalculator nighttimeCalculator) {
            this.profileService = profileService;
            this.sequenceMediator = sequenceMediator;
            this.cameraMediator = cameraMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.neocpHelper = new NeocpHelper(sequenceMediator);
            _ephemerisSourceIndex = 0;

            this.MaxTrackLenght = neocpHelper.MaxLength;
            this.SensorAreaUsage = neocpHelper.SensorAreaUsage;
        }

        private UpdateNEOEphemerides(UpdateNEOEphemerides cloneMe) : this(cloneMe.profileService, cloneMe.sequenceMediator, cloneMe.cameraMediator, cloneMe.nighttimeCalculator) {
            this.MaxTrackLenght = cloneMe.MaxTrackLenght;
            this.SensorAreaUsage = cloneMe.SensorAreaUsage;
            this.EphemerisSourceIndex = cloneMe.EphemerisSourceIndex;
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new UpdateNEOEphemerides(this);
        }

        [JsonProperty]
        public int MaxTrackLenght { get => _maxTrackLenght; set => _maxTrackLenght = value; }
        
        [JsonProperty]
        public double SensorAreaUsage {
            get => _sensorAreaUsage;
            set {
                _sensorAreaUsage = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int EphemerisSourceIndex {
            get => _ephemerisSourceIndex;
            set {
                _ephemerisSourceIndex = value;
                RaisePropertyChanged();
            }
        }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.Run(() => {
                if (this.Parent is IDeepSkyObjectContainer container) {
                    if (neocpHelper.ForceSkipOnFailure) {
                        this.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.SkipInstructionSetOnError;
                    }
                    var targetName = container.Target.TargetName;
                    if (targetName.StartsWith(neocpHelper.TargetPrefix)) {
                        targetName = targetName.Substring(neocpHelper.TargetPrefix.Length);
                    }
                    progress.Report(new ApplicationStatus() { Status = String.Format("Retrieving {0} ephemerides", targetName) });
                    try {

                        NEOCPTarget neo;

                        if (EphemerisSourceIndex == 0) {
                            var ephemerides = MPCDownloader.GetNEOEphemerides(targetName, profileService.ActiveProfile.AstrometrySettings, neocpHelper);
                            if (ephemerides.Count == 0) {
                                var error = String.Format("Ephemerides not found for {0}", targetName);
                                Issues.Add(error);
                                Logger.Error(error);
                                throw new SequenceEntityFailedException(string.Join(",", Issues));
                            }
                            neo = new NEOCPTarget(ephemerides.First().Value);
                        } else {
                            var list = MPCDownloader.GetMPCEphemerides(targetName, profileService.ActiveProfile.AstrometrySettings, neocpHelper);
                            if (list.Count == 0) {
                                var error = String.Format("Ephemerides not found for {0}", targetName);
                                Issues.Add(error);
                                Logger.Error(error);
                                throw new SequenceEntityFailedException(string.Join(",", Issues));
                            }
                            neo = new NEOCPTarget(list);
                        }  

                        var cameraInfo = cameraMediator.GetInfo();
                        var cameraSize = Math.Min(cameraInfo.XSize, cameraInfo.YSize) * SensorAreaUsage;
                        var pixelSize = cameraInfo.PixelSize;
                        var focalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength;
                        var scale = AstroUtil.DegreeToArcsec(AstroUtil.ToDegree(2 * Math.Atan2(pixelSize / 2000, focalLength)));
                        var fieldSize = AstroUtil.ArcsecToArcmin(cameraSize * scale);

                        var obseringTime = DateTime.Now.ToUniversalTime();

                        neo.SetScales(scale, MaxTrackLenght);
                        var initial = neo.InterpolatedAtTime(obseringTime);
                        var final = neo.InterpolatedAtDistance(initial, fieldSize);
                        var maxSpan = final.DateTime - initial.DateTime;
                        var usedTimeSpan = ItemUtility.UpdateTimeSpanItems(container, maxSpan);

                        if (usedTimeSpan != maxSpan) {
                            final = neo.InterpolatedAtTime(initial.DateTime + usedTimeSpan);
                        }

                        var center = NEOCPEphemeride.MidPoint(initial, final);
                        center.SetScales(scale, MaxTrackLenght);

                        ItemUtility.UpdateDSOContainerCoordinates(container,center.Coordinates);
                        ItemUtility.UpdateTakeExposureItems(container, center.ExpMax);
                        
                    } catch (Exception ex) {
                        var error = String.Format("Ephemerides not found for {0}", targetName);
                        Issues.Add(error);
                        Logger.Error(ex);
                        throw new SequenceEntityFailedException(string.Join(",", Issues));
                    }
                }
            });
        }

        public bool Validate() {
            var i = new List<string>();

            if (false == Parent is IDeepSkyObjectContainer) {
                i.Add(String.Format("Should be inside a DeepSkyObjectContainer"));
            }

            if (cameraMediator.GetDevice() == null || cameraMediator.GetDevice()?.Connected == false) {
                i.Add("Camera is not connected");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(UpdateNEOEphemerides)}";
        }
    }
}
