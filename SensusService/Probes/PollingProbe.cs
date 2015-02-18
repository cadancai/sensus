// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private int _pollingSleepDurationMS;
        private bool _isPolling;
        private int _pollCallbackId;

        private readonly object _locker = new object();

        [EntryIntegerUiProperty("Sleep Duration:", true, 5)]
        public virtual int PollingSleepDurationMS
        {
            get { return _pollingSleepDurationMS; }
            set 
            {
                if (value != _pollingSleepDurationMS)
                {
                    _pollingSleepDurationMS = value; 

                    if (_pollCallbackId != -1)
                        SensusServiceHelper.Get().UpdateRepeatingCallback(_pollCallbackId, _pollingSleepDurationMS, _pollingSleepDurationMS);
                }
            }
        }

        [JsonIgnore]
        public abstract int DefaultPollingSleepDurationMS { get; }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
            _isPolling = false;
            _pollCallbackId = -1;
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails.
        /// </summary>
        public override void Start()
        {
            lock (_locker)
            {
                base.Start();

                _pollCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(() =>
                    {
                        if (Running)
                        {
                            _isPolling = true;

                            IEnumerable<Datum> data = null;
                            try
                            {
                                SensusServiceHelper.Get().Logger.Log("Polling probe \"" + GetType().FullName + "\".", LoggingLevel.Verbose);
                                data = Poll();
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Failed to poll probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal);
                            }

                            if (data != null)
                                foreach (Datum datum in data)
                                    try
                                    {
                                        StoreDatum(datum);
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Failed to store datum in probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal);
                                    }

                            _isPolling = false;
                        }
                    }, 0, _pollingSleepDurationMS);
            }
        }

        protected abstract IEnumerable<Datum> Poll();

        public override void Stop()
        {
            lock (_locker)
            {
                base.Stop();

                SensusServiceHelper.Get().CancelRepeatingCallback(_pollCallbackId);
                _pollCallbackId = -1;
            }
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                double msElapsedSincePreviousStore = (DateTimeOffset.UtcNow - MostRecentStoreTimestamp).TotalMilliseconds;
                if (!_isPolling && msElapsedSincePreviousStore > _pollingSleepDurationMS)
                    warning += "Probe \"" + GetType().FullName + "\" has not stored data in " + msElapsedSincePreviousStore + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;
            }

            return restart;
        }
    }
}