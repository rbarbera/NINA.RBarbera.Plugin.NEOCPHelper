﻿using NINA.Equipment.Model;
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

        public static void UpdateTimeSpanItems(ISequenceContainer parent, DateTime endTime) {
            if (parent == null)
                return;

            if (parent is SequentialContainer container) {
                foreach (var condition in container.Conditions) {

                    if (condition is TimeSpanCondition timeSpanCondition) {
                        var span = endTime - DateTime.Now;
                        timeSpanCondition.Hours = span.Hours;
                        timeSpanCondition.Minutes = span.Minutes;
                        timeSpanCondition.Seconds = span.Seconds;
                        continue;
                    }
                }
            } else {
                var items = parent.GetItemsSnapshot();
                foreach (var item in items) {
                    if (item is ISequenceContainer innerContainer) {
                        UpdateTimeSpanItems(innerContainer, endTime);
                    }
                }
            }
        }

        public static void UpdateTakeExposureItems(ISequenceContainer parent, double exposureTime) {
            if (parent == null)
                return;

            var items = parent.GetItemsSnapshot();
            foreach (var item in items) {
                var listItem = item as TakeExposure;
                if (listItem != null && listItem.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                    listItem.ExposureTime = exposureTime;
                    continue;
                }
                var listSubItem = item as TakeSubframeExposure;
                if (listSubItem != null && listSubItem.ImageType == CaptureSequence.ImageTypes.LIGHT) {
                    listSubItem.ExposureTime = exposureTime;
                    continue;
                }
                var container = item as ISequenceContainer;
                if (container != null) {
                    UpdateTakeExposureItems(container, exposureTime);
                }
            }
        }
    }    
}
