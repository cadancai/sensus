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
using Sensus.Exceptions;
using Xamarin.Forms;
using Sensus.UserStates;

namespace Sensus.UI
{
    public class UserStatesPage : ContentPage
    {

        private class StateTextColorValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (bool)value ? Color.Green : Color.Red;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                SensusException.Report("Invalid call to " + GetType().FullName + ".ConvertBack.");
                return null;
            }
        }

        private Protocol _protocol;
        private ListView _userStatesList;

        protected Protocol Protocol
        {
            get
            {
                return _protocol;
            }
        }

        protected ListView StatesList
        {
            get
            {
                return _userStatesList;
            }
        }

        public UserStatesPage(Protocol protocol, string title)
        {
            _protocol = protocol;

            Title = title;

            _userStatesList = new ListView(ListViewCachingStrategy.RecycleElement);
            _userStatesList.ItemTemplate = new DataTemplate(typeof(TextCell));
            _userStatesList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(UserState.Caption));
            _userStatesList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(nameof(UserState.Enabled), converter: new StateTextColorValueConverter()));
            _userStatesList.ItemTemplate.SetBinding(TextCell.DetailProperty, nameof(UserState.SubCaption));
            _userStatesList.ItemsSource = _protocol.UserStates;
            _userStatesList.ItemTapped += UserStateTappedAsync;

            Content = _userStatesList;
        }

        protected async void UserStateTappedAsync(object sender, ItemTappedEventArgs e)
        {
            UserStatePage UserStatePage = new UserStatePage(e.Item as UserState);
            await Navigation.PushAsync(UserStatePage);
            StatesList.SelectedItem = null;
        }

    }
}
