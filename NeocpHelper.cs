using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NINA.Sequencer.Interfaces.Mediator;
using Settings = NINA.RBarbera.Plugin.NEOCPHelper.Properties.Settings;
using Properties = NINA.RBarbera.Plugin.NEOCPHelper.Properties;

using System.Reflection;

namespace NINA.RBarbera.Plugin.NeocpHelper {
    /// <summary>
    /// This class exports the IPluginManifest interface and will be used for the general plugin information and options
    /// The base class "PluginBase" will populate all the necessary Manifest Meta Data out of the AssemblyInfo attributes. Please fill these accoringly
    /// 
    /// An instance of this class will be created and set as datacontext on the plugin options tab in N.I.N.A. to be able to configure global plugin settings
    /// The user interface for the settings will be defined by a DataTemplate with the key having the naming convention "NeocpHelper_Options" where NeocpHelper corresponds to the AssemblyTitle - In this template example it is found in the Options.xaml
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class NeocpHelper : PluginBase, INotifyPropertyChanged {
        private readonly ISequenceMediator sequenceMediator;

        [ImportingConstructor]
        public NeocpHelper(ISequenceMediator sequenceMediator) {
            this.sequenceMediator = sequenceMediator;

            if (Properties.Settings.Default.UpgradeSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }
        }

        public int MaxLength {
            get {
                return Settings.Default.MaxLength;
            }
            set {
                Settings.Default.MaxLength = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public bool AutoExpandTemplates {
            get {
                return Settings.Default.AutoExpandTemplates;
            }
            set {
                Settings.Default.AutoExpandTemplates = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double SensorAreaUsage {
            get {
                return Settings.Default.SensorAreaUsage;
            }
            set {
                Settings.Default.SensorAreaUsage = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double MagBright {
            get {
                return Settings.Default.MagBright;
            }
            set {
                Settings.Default.MagBright = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double MagFaint {
            get {
                return Settings.Default.MagFaint;
            }
            set {
                Settings.Default.MagFaint = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double DecLow {
            get {
                return Settings.Default.DecLow;
            }
            set {
                Settings.Default.DecLow = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double DecUp {
            get {
                return Settings.Default.DecUp;
            }
            set {
                Settings.Default.DecUp = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double ScoreHigh {
            get {
                return Settings.Default.ScoreHigh;
            }
            set {
                Settings.Default.ScoreHigh = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public double ScoreLow {
            get {
                return Settings.Default.ScoreLow;
            }
            set {
                Settings.Default.ScoreLow = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string SelectedTemplate {
            get {
                return Settings.Default.SelectedTemplate;
            }
            set {
                Settings.Default.SelectedTemplate = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public bool ForceSkipOnFailure {
            get {
                return Settings.Default.ForceSkipOnFailure;
            }
            set {
                Settings.Default.ForceSkipOnFailure = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string TargetPrefix {
            get {
                return Settings.Default.TargetPrefix;
            }
            set {
                Settings.Default.TargetPrefix = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string FilterString {
            get {
                var filterDec = String.Format("dl={0}&du={1}", DecLow, DecUp);
                var filterV = String.Format("mb={0}&mf={1}", MagBright, MagFaint);
                var filterScore = String.Format("nl={0}&nu={1}", ScoreLow, ScoreHigh);

                return String.Format("{0}&{1}&{2}",filterV, filterDec,filterScore);
            }
        }

        public List<String> AvailableTemplates {
            get {
                var templates = sequenceMediator.GetDeepSkyObjectContainerTemplates();
                return templates.Select(x => x.Name).ToList();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
