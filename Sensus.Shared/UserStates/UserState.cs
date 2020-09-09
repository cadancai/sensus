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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Context;
using Sensus.UI.UiProperties;

namespace Sensus.UserStates
{
    /// <summary>
    /// Each UserState represents the particular aspect of user context. It is extracted from various probe data.
    /// While datum collected by various probes gets at raw sensor data generated from smartphones, UserState gets at higher level
    /// aspect of user contexts.
    /// </summary>
    public abstract class UserState : INotifyPropertyChanged
    {

        #region static members
        public static List<UserState> GetAll()
        {
            List<UserState> userStates = null;

            // the reflection stuff we do below (at least on android) needs to be run on the main thread.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                userStates = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(UserState))).Select(t => Activator.CreateInstance(t) as UserState).OrderBy(p => p.DisplayName).ToList();
            });

            return userStates;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private Protocol _protocol;
        private string _displayName;
        private string _caption;
        private string _subcaption;
        private bool _enabled;
        private bool _storeData;
        private DateTimeOffset? _mostRecentStoreTimestamp;
        private TimeSpan _updateInterval;
        private TimeSpan _probeDataTimeRange;
        private readonly Dictionary<Type, List<IDatum>> _probeData = new Dictionary<Type, List<IDatum>>();
        private EventHandler<bool> _powerConnectionChanged;
        private CancellationTokenSource _processDataCanceller;

        private readonly object _stateLocker = new object();

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [JsonIgnore]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        [JsonIgnore]
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        [JsonIgnore]
        public string SubCaption
        {
            get { return _subcaption; }
            set { _subcaption = value; }
        }

        /// <summary>
        /// Whether the <see cref="UserState"/> should be turned on when the user starts the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Enabled:", true, 2)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    // lock the state so that enabling/disabling does not interfere with ongoing
                    // start, stop, restart, etc. operations.
                    lock (_stateLocker)
                    {
                        _enabled = value;

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
                    }
                }
            }
        }

        [JsonIgnore]
        public DateTimeOffset? MostRecentStoreTimestamp
        {
            get { return _mostRecentStoreTimestamp; }
        }

        /// <summary>
        /// Whether the UserState should store the data it represents. This might be turned off if the <see cref="UserState"/> is used to trigger 
        /// the <see cref="User.Scripts.ScriptProbe"/> but the user state data are not needed.
        /// </summary>
        /// <value><c>true</c> if store data; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Store Data:", true, 3)]
        public bool StoreData
        {
            get { return _storeData; }
            set { _storeData = value; }
        }


        /// <summary>
        /// The rate of updating the <see cref="UserState"/>.
        /// </summary>
        [JsonIgnore]
        public TimeSpan UpdateInterval
        {
            get { return _updateInterval; }
            set { _updateInterval = value; }
        }


        /// <summary>
        /// The time range from the current moment to look back to that contains all the probe data for extracting
        /// the <see cref="UserState"/>.
        /// </summary>
        [JsonIgnore]
        public TimeSpan ProbeDataTimeRange
        {
            get { return _probeDataTimeRange; }
            set { _probeDataTimeRange = value; }
        }

        public UserState()
        {
            _enabled = false;
            _storeData = true;
            _updateInterval = TimeSpan.FromSeconds(300);
            _probeDataTimeRange = TimeSpan.FromMinutes(60);

            _powerConnectionChanged = async (sender, connected) =>
            {
                _processDataCanceller = new CancellationTokenSource();
                await ProcessDataAsync(_processDataCanceller.Token);
            };
        }

        private Task ProcessDataAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
