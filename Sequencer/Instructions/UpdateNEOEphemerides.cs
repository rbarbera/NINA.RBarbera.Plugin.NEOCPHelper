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

namespace NINA.RBarbera.Plugin.NeocpHelper.Sequencer.Instructions {
    
    [ExportMetadata("Name", "Update NEO ephemerides")]
    [ExportMetadata("Description", "Get the most recent ephemerides for the NEO defined on the container and update Coordinates, time conditions and exposure")]
    [ExportMetadata("Icon", "ImpactorSVG")]
    [ExportMetadata("Category", "NEOCP Helper")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    internal class UpdateNEOEphemerides: SequenceItem, IValidatable {

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
                Issues = issues
            };

            return clone;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.Run(() => {
                if (this.Parent is IDeepSkyObjectContainer container) {
                    this.ErrorBehavior = NINA.Sequencer.Utility.InstructionErrorBehavior.SkipInstructionSetOnError;
                    var targetName = container.Target.TargetName;
                    progress.Report(new ApplicationStatus() { Status = String.Format("Retrieving {0} ephemerides", targetName) });
                    try {
                        var ephemerides = NEOCPDownloader.GetEphemerides(targetName, profileService.ActiveProfile.AstrometrySettings);
                        if (ephemerides.Count == 0) {
                            Issues.Add("Ephemerides not available.");
                            throw new SequenceEntityFailedException(string.Join(",", Issues));
                        }
                        var newEp = ephemerides.First().Value.First().Coordinates;
                        ItemUtility.UpdateDSOContainerCoordinates(container, newEp);
                        ItemUtility.UpdateTakeExposureItems(container, 17);
                        ItemUtility.UpdateEndOfLoop(container,DateTime.Now.AddMinutes(35));
                        
                    } catch (Exception ex) {
                        Issues.Add("Ephemerides not available.");
                        throw new SequenceEntityFailedException(string.Join(",", Issues));
                    }
                }
            });
        }

        public bool Validate() {
            return true;
        }
    }
}
