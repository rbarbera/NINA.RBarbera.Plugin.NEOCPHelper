using NINA.Equipment.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Astrometry;
using Accord.IO;
using NINA.Sequencer.Conditions;
using NINA.Core.Utility;

namespace NINA.RBarbera.Plugin.NeocpHelper.Utility {
    public class ItemUtility {

        public static void UpdateDSOContainerCoordinates(ISequenceContainer parent, Coordinates coordinates) {
            if (parent == null)
                return;
 
            var container = parent as IDeepSkyObjectContainer;
            if (container != null) {
                container.Target.InputCoordinates.Coordinates = coordinates;
                container.Target.DeepSkyObject.Coordinates = coordinates;
            } else {
                UpdateDSOContainerCoordinates(parent.Parent, coordinates);
            }
        }

        public static TimeSpan UpdateTimeSpanItems(ISequenceContainer parent, TimeSpan span) {
            if (parent == null)
                return TimeSpan.Zero;

            if (parent is SequentialContainer container) {
                foreach (var condition in container.Conditions) {
                    if (condition is TimeSpanCondition timeSpanCondition) {
                        if (span.Ticks < timeSpanCondition.RemainingTime.Ticks) {
                            timeSpanCondition.Hours = span.Hours;
                            timeSpanCondition.Minutes = span.Minutes;
                            timeSpanCondition.Seconds = span.Seconds;
                            return span;
                        } 
                        else {
                            return timeSpanCondition.RemainingTime;
                        }
                    }
                }
                return TimeSpan.Zero;
            } else {
                var items = parent.GetItemsSnapshot();
                foreach (var item in items) {
                    if (item is ISequenceContainer innerContainer) {
                        return UpdateTimeSpanItems(innerContainer, span);
                    }
                }
                return TimeSpan.Zero;
            }
        }

        public static void UpdateTakeExposureItems(ISequenceContainer parent, double exposureTime) {
            if (parent == null)
                return;

            var items = parent.GetItemsSnapshot();
            foreach (var item in items) {
                var listItem = item as TakeExposure;
                if (listItem != null && listItem.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                    var binning = Math.Max(listItem.Binning.X, listItem.Binning.Y);
                    var reducedExposure = (int)Math.Ceiling(exposureTime * binning);
                    listItem.ExposureTime = Math.Min(reducedExposure, listItem.ExposureTime);
                    continue;
                }
                    
                var listSubItem = item as TakeSubframeExposure;
                if (listSubItem != null && listSubItem.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                    var binning = Math.Max(listSubItem.Binning.X, listSubItem.Binning.Y);
                    var reducedExposure = (int)Math.Ceiling(exposureTime * binning);
                    listSubItem.ExposureTime = Math.Min(reducedExposure, listSubItem.ExposureTime);
                    continue;
                }

                var manyItem = item as TakeManyExposures;
                if (manyItem != null) {
                    var manyExposure = manyItem.GetTakeExposure();
                    if (manyExposure.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                        var binning = Math.Max(manyExposure.Binning.X, manyExposure.Binning.Y);
                        var reducedExposure = (int)Math.Ceiling(exposureTime * binning);
                        manyExposure.ExposureTime = Math.Min(reducedExposure, manyExposure.ExposureTime);
                        continue;
                    }
                }

                var smartItem = item as SmartExposure;
                if (smartItem != null) {
                    var smartExposure = smartItem.GetTakeExposure();
                    if (smartExposure.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                        var binning = Math.Max(smartExposure.Binning.X, smartExposure.Binning.Y);
                        smartExposure.ExposureTime = Math.Min(exposureTime, smartExposure.ExposureTime) * binning;
                        continue;
                    }
                }

                var container = item as ISequenceContainer;
                if (container != null) {
                    UpdateTakeExposureItems(container, exposureTime);
                }
            }
        }
    }    
}
