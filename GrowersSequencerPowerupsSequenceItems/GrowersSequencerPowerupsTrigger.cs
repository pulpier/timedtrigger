using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
// using IDateTimeProvider = NINA.Sequencer.Utility.DateTimeProvider.IDateTimeProvider;
using NINA.Astrometry;
using NINA.Core.Locale;
using TimeProvider = NINA.Sequencer.Utility.DateTimeProvider.TimeProvider;
using NINA.Sequencer.SequenceItem.Utility;

namespace GrowersAstro.NINA.GrowersSequencerPowerups {
    /// <summary>
    /// This Class shows the basic principle on how to add a new Sequence Trigger to the N.I.N.A. sequencer via the plugin interface
    /// For ease of use this class inherits the abstract SequenceTrigger which already handles most of the running logic, like logging, exception handling etc.
    /// A complete custom implementation by just implementing ISequenceTrigger is possible too
    /// The following MetaData can be set to drive the initial values
    /// --> Name - The name that will be displayed for the item
    /// --> Description - a brief summary of what the item is doing. It will be displayed as a tooltip on mouseover in the application
    /// --> Icon - a string to the key value of a Geometry inside N.I.N.A.'s geometry resources
    ///
    /// If the item has some preconditions that should be validated, it shall also extend the IValidatable interface and add the validation logic accordingly.
    /// </summary>
    [ExportMetadata("Name", "Time")]
    [ExportMetadata("Description", "This will be triggered at a given time")]
    [ExportMetadata("Icon", "ClockSVG")]
    [ExportMetadata("Category", "Growers Sequencer Powerups")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class GrowersSequencerPowerupsTimedTrigger : SequenceTrigger {
        /// <summary>
        /// The constructor marked with [ImportingConstructor] will be used to import and construct the object
        /// General device interfaces can be added to the constructor parameters and will be automatically injected on instantiation by the plugin loader
        /// </summary>
     
        private IList<IDateTimeProvider> dateTimeProviders;
        private int hours;
        private int minutes;
        private int minutesOffset;
        private int seconds;
        DateTime triggerTime;

        private IDateTimeProvider selectedProvider;

        [ImportingConstructor]
        public GrowersSequencerPowerupsTimedTrigger(IList<IDateTimeProvider> dateTimeProviders) {
            this.dateTimeProviders = dateTimeProviders;
            this.selectedProvider = dateTimeProviders?.FirstOrDefault();
        }

        public override object Clone() {
            return new GrowersSequencerPowerupsTimedTrigger(dateTimeProviders) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description
            };
        }

        /// <summary>
        /// The actual running logic for when the trigger should run
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Notification.ShowSuccess("Trigger: Time");
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method will be evaluated to see if the trigger should be executed.
        /// When true - the Execute method will be called
        /// Skipped otherwise
        ///
        /// For this example the trigger will fire when the random number generator generates an even number
        /// </summary>
        /// <param name="previousItem"></param>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            DateTime currentTime = System.DateTime.Now;
            if (currentTime > triggerTime) {
                UpdateTime();
                return true;
            }
            return false;
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }



        public bool Validate() {
            var i = new List<string>();
            if (HasFixedTimeProvider) {
                var referenceDate = NighttimeCalculator.GetReferenceDate(System.DateTime.Now);
                if (lastReferenceDate != referenceDate) {
                    UpdateTime();
                }
            }
            if (!timeDeterminedSuccessfully) {
                i.Add(Loc.Instance["LblSelectedTimeSourceInvalid"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public IList<IDateTimeProvider> DateTimeProviders {
            get => dateTimeProviders;
            set {
                dateTimeProviders = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        public bool HasFixedTimeProvider {
            get {
                return selectedProvider != null && !(selectedProvider is TimeProvider);
            }
        }

        [JsonProperty]
        public int Hours {
            get => hours;
            set {
                hours = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Minutes {
            get => minutes;
            set {
                minutes = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int MinutesOffset {
            get => minutesOffset;
            set {
                minutesOffset = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Seconds {
            get => seconds;
            set {
                seconds = value;
                UpdateTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public IDateTimeProvider SelectedProvider {
            get => selectedProvider;
            set {
                selectedProvider = value;
                if (selectedProvider != null) {
                    UpdateTime();
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(HasFixedTimeProvider));
                }
            }
        }

        private bool timeDeterminedSuccessfully;
        private DateTime lastReferenceDate;
        private void UpdateTime() {
            try {
                lastReferenceDate = NighttimeCalculator.GetReferenceDate(System.DateTime.Now);
                if (HasFixedTimeProvider) {
                    var t = SelectedProvider.GetDateTime(this) + TimeSpan.FromMinutes(MinutesOffset);
                    Hours = t.Hour;
                    Minutes = t.Minute;
                    Seconds = t.Second;

                }

                DateTime currentTime = System.DateTime.Now;
                triggerTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, Hours, Minutes, Seconds);

                if (currentTime > triggerTime) {
                    triggerTime = triggerTime.AddDays(1);
                }

                timeDeterminedSuccessfully = true;
            } catch (Exception) {
                timeDeterminedSuccessfully = false;
                Validate();
            }
        }

        public ICustomDateTime DateTime { get; set; }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForTime)}, Time: {Hours}:{Minutes}:{Seconds}h, Offset: {MinutesOffset}";
        }

    }
}


namespace GrowersAstro.NINA.GrowersSequencerPowerups {
    /// <summary>
    /// </summary>
    [ExportMetadata("Name", "Interval")]
    [ExportMetadata("Description", "This will be triggered after a given time interval")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "Growers Sequencer Powerups")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class GrowersSequencerPowerupsIntervalTrigger : SequenceTrigger {
        /// <summary>
        /// The constructor marked with [ImportingConstructor] will be used to import and construct the object
        /// General device interfaces can be added to the constructor parameters and will be automatically injected on instantiation by the plugin loader
        /// </summary>
        [ImportingConstructor]
        public GrowersSequencerPowerupsIntervalTrigger() {
        }

        public override object Clone() {
            return new GrowersSequencerPowerupsIntervalTrigger() {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description
            };
        }

        /// <summary>
        /// The actual running logic for when the trigger should run
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Notification.ShowSuccess("Trigger was fired");
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method will be evaluated to see if the trigger should be executed.
        /// When true - the Execute method will be called
        /// Skipped otherwise
        ///
        /// For this example the trigger will fire when the random number generator generates an even number
        /// </summary>
        /// <param name="previousItem"></param>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return random.Next(0, 1000) % 2 == 0;
        }

        Random random = new Random();

        /// <summary>
        /// This string will be used for logging
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(GrowersSequencerPowerupsIntervalTrigger)}";
        }
    }
}