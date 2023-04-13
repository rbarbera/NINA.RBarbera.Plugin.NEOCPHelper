using Newtonsoft.Json;
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

namespace NINA.RBarbera.Plugin.NeocpHelper.Sequencer.Instructions {
    
    [ExportMetadata("Name", "Update NEO ephemerides")]
    [ExportMetadata("Description", "Get the most recent ephemerides for the NEO defined on the container and update Coordinates, time conditions and exposure")]
    [ExportMetadata("Icon", "ImpactorSVG")]
    [ExportMetadata("Category", "NEOCP Helper")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class UpdateNEOEphemerides: SequenceItem, IValidatable {

        private readonly IProfileService profileService;
        private readonly ISequenceMediator sequenceMediator;
        private INighttimeCalculator nighttimeCalculator;
        private NeocpHelper neocpHelper;

        [ImportingConstructor]
        public UpdateNEOEphemerides(IProfileService profileService,
                ISequenceMediator sequenceMediator,
                INighttimeCalculator nighttimeCalculator) { 
            this.profileService = profileService;
            this.sequenceMediator = sequenceMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.neocpHelper = new NeocpHelper(sequenceMediator);
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public override object Clone() {
            var clone = new UpdateNEOEphemerides(profileService, sequenceMediator, nighttimeCalculator) {
                Name = Name,
                Icon = Icon,
                Category = Category,
                Description = Description
            };

            return clone;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.Run(() => {
                if (this.Parent is IDeepSkyObjectContainer container) {
                    if (neocpHelper.ForceSkipOnFailure) {
                        this.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.SkipInstructionSetOnError;
                    }
                    var targetName = container.Target.TargetName;
                    progress.Report(new ApplicationStatus() { Status = String.Format("Retrieving {0} ephemerides", targetName) });
                    try {
                        var ephemerides = NEOCPDownloader.GetEphemerides(targetName, profileService.ActiveProfile.AstrometrySettings);
                        if (ephemerides.Count == 0) {
                            var error = String.Format("Ephemerides not found for {0}", targetName);
                            Issues.Add(error);
                            Logger.Error(error);
                            throw new SequenceEntityFailedException(string.Join(",", Issues));
                        }

                        var pixelSize = profileService.ActiveProfile.CameraSettings.PixelSize;
                        var focalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength;
                        var scale = AstroUtil.DegreeToArcsec(AstroUtil.ToDegree(2 * Math.Atan2(pixelSize / 2000, focalLength)));

                        var firstEphemeride = ephemerides.First().Value.First();
                        firstEphemeride.SetScales(scale, neocpHelper.MaxLength, neocpHelper.UsedField);

                        var newEphemerides = firstEphemeride.Coordinates;
                        var newExposure = Math.Min(firstEphemeride.ExpMax, neocpHelper.MaxExposureTime);
                        var newIntegrationTime = Math.Min(firstEphemeride.TMax, neocpHelper.ExpectedIntegrationTime);

                        ItemUtility.UpdateDSOContainerCoordinates(container, newEphemerides);
                        ItemUtility.UpdateTakeExposureItems(container, newExposure);
                        ItemUtility.UpdateTimeSpanItems(container,DateTime.Now.AddMinutes(newIntegrationTime));
                        
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

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(UpdateNEOEphemerides)}";
        }
    }
}
