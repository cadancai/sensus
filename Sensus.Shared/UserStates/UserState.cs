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
using Sensus.Context;
using Sensus.UI.UiProperties;

namespace Sensus.UserStates
{
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

        private Protocol _protocol;
        private string _displayName;
        private string _caption;
        private string _subcaption;
        private bool _enabled;
        private readonly object _stateLocker = new object();

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        public string SubCaption
        {
            get { return _subcaption; }
            set { _subcaption = value; }
        }

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

        public UserState()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
