﻿using P3bble;
using P3bble.PCL;
using P3bble.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace P3bbleWP8
{
    public sealed partial class MainPage : Page
    {
        private P3bble.P3bble _pebble;
        private ProgressBar _currentProgressBar;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;

            //MediaPlayer.ActiveSongChanged += MediaPlayer_ActiveSongChanged;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Logger = new P3bble.Logger();
            await TryConnection();
        }

        private async Task TryConnection()
        {
            List<P3bble.P3bble> pebbles = await P3bble.P3bble.DetectPebbles();

            if (pebbles.Count >= 1)
            {
                _pebble = pebbles[0];
                await _pebble.ConnectAsync();

                if (_pebble != null && _pebble.IsConnected)
                {
                    //_pebble.MusicControlReceived += new MusicControlReceivedHandler(this.MusicControlReceived);
                    _pebble.InstallProgress += new InstallProgressHandler(this.InstallProgressReceived);

                    PebbleName.Text = "Connected to Pebble " + _pebble.DisplayName;
                    PebbleVersion.Text = "Version " + _pebble.FirmwareVersion.Version + " - " + _pebble.FirmwareVersion.Timestamp.ToString();
                    RetryConnection.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NotConnected();
                }
            }
        }

        private void NotConnected()
        {
            _pebble = null;
            PebbleName.Text = "Not connected";
            PebbleVersion.Text = string.Empty;
            RetryConnection.Visibility = Visibility.Visible;
        }

        private async void Retry_Click(object sender, RoutedEventArgs e)
        {
            await TryConnection();
        }

        /* Time */

        private async void GetTime_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                DateTime time = await _pebble.GetTimeAsync();
                await MessageBox.Show("Time is " + time.ToString() + " - " + Math.Abs(Convert.ToInt32((DateTime.Now - time).TotalMinutes)).ToString() + " minute(s) different from phone");
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void SetTime_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                await _pebble.SetTimeAsync(DateTime.Now);
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        /* Apps */

        private async void GetInstalledApps_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                var apps = await _pebble.GetInstalledAppsAsync();
                if (apps != null)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.AppendLine(apps.ApplicationBanks.ToString() + " app banks available");
                    msg.AppendLine(apps.ApplicationsInstalled.Count.ToString() + " apps installed");

                    foreach (var app in apps.ApplicationsInstalled)
                    {
                        msg.AppendLine(app.Name + " by " + app.Company);
                    }

                    await MessageBox.Show(msg.ToString());
                }
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void RemoveApp_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                var apps = await _pebble.GetInstalledAppsAsync();
                if (apps != null && apps.ApplicationsInstalled.Count > 0)
                {
                    if (await MessageBox.Show("This example will remove the first app found: " + apps.ApplicationsInstalled[0].Name + " - are you sure you want to continue?", "DANGER!", MessageBox.WrappedMessageBoxButton.OKCancel) == MessageBox.WrappedMessageBoxResult.OK)
                    {
                        await _pebble.RemoveAppAsync(apps.ApplicationsInstalled[0]);
                    }
                }
                else
                {
                    await MessageBox.Show("No apps installed");
                }
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void DownloadApp_Click(object sender, RoutedEventArgs e)
        {
            //const string InstallUrl = "http://pebble-static.s3.amazonaws.com/watchfaces/apps/simplicity.pbw";
            const string InstallUrl = "http://u.jdiez.me/pixel.pbw";

            this._currentProgressBar = this.InstallAppProgress;
            await InstallBundle(InstallUrl);
        }

        private async void InstallProgressReceived(int percentComplete)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this._currentProgressBar.IsIndeterminate = false;
                    this._currentProgressBar.Value = percentComplete;
                    if (percentComplete == 0 || percentComplete == 100)
                    {
                        this._currentProgressBar.Value = 0;
                    }
                });
        }

        private async void LaunchApp_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                await _pebble.LaunchAppAsync(new Guid("deadefde-acfe-efbe-99ef-beefbeefbeef"));
            }
        }

        /* Firmware */

        private async void CheckFirmware_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                if (_pebble.FirmwareVersion != null)
                {
                    var latest = await _pebble.GetLatestFirmwareVersionAsync();
                    if (latest.FirmwareVersion > _pebble.FirmwareVersion)
                    {
                        await MessageBox.Show("new version available (" + latest.FirmwareVersion.Version.ToString() + " available, you have " + _pebble.FirmwareVersion.Version.ToString() + ")");
                    }
                    else
                    {
                        await MessageBox.Show("you have the latest firmware " + _pebble.FirmwareVersion.Version.ToString());
                    }
                }
                else
                {
                    await MessageBox.Show("we did not manage to get version info from p3bble");
                }
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void DownloadFirmware_Click(object sender, RoutedEventArgs e)
        {
            //const string InstallUrl = "http://pebblebits.com/firmware/2.0.1-ev2_4-battery-sym+en.pbz";
            const string InstallUrl = "https://pebblefw.s3.amazonaws.com/pebble/ev2_4/release/pbz/normal_ev2_4_v1.14.1_release-v1.x-34.pbz";

            this._currentProgressBar = this.InstallFirmwareProgress;
            await InstallBundle(InstallUrl);

            // Reset the connection
            this.NotConnected();
        }

        /* Music Control */

        private void PlayMusic_Click(object sender, RoutedEventArgs e)
        {
            //if (_pebble != null && _pebble.IsConnected)
            //{
            //    MediaLibrary lib = new MediaLibrary();
            //    MediaPlayer.Play(lib.Songs, new Random().Next(lib.Songs.Count));
            //    MediaPlayer.IsShuffled = true;
            //}
            //else
            //{
            //    MessageBox.Show("Pebble not connected");
            //}
        }

        //private void MediaPlayer_ActiveSongChanged(object sender, EventArgs e)
        //{
        //    if (MediaPlayer.Queue.ActiveSong != null)
        //    {
        //        _pebble.SetNowPlayingAsync(MediaPlayer.Queue.ActiveSong.Artist.Name, MediaPlayer.Queue.ActiveSong.Album.Name, MediaPlayer.Queue.ActiveSong.Name);
        //    }
        //    else
        //    {
        //        _pebble.SetNowPlayingAsync(string.Empty, string.Empty, string.Empty);
        //    }
        //}

        private void MusicControlReceived(MusicControlAction action)
        {
            switch (action)
            {
                case MusicControlAction.PlayPause:
                    //if (MediaPlayer.State == MediaState.Playing)
                    //{
                    //    MediaPlayer.Pause();
                    //}
                    //else
                    //{
                    //    MediaPlayer.Resume();
                    //}

                    break;

                case MusicControlAction.Next:
                    //MediaPlayer.MoveNext();
                    break;

                case MusicControlAction.Previous:
                    //MediaPlayer.MovePrevious();
                    break;
            }
        }

        /* Notification Demos */

        private async void Ping_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
                await _pebble.PingAsync();
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void SmsNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
                await _pebble.SmsNotificationAsync("+436604908028", "wow, what a cool app :)");
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void EmailNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
                await _pebble.EmailNotificationAsync("root@p3.co.at", "P3bble", "youre sooo cooool :)");
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void FacebookNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
                await _pebble.FacebookNotificationAsync("test", "testmessage");
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        private async void PhoneCall_Click(object sender, RoutedEventArgs e)
        {
            if (_pebble != null && _pebble.IsConnected)
            {
                byte[] cookie = new byte[] { 0x00, 0xEB, 0x00, 0x00 };
                await _pebble.PhoneCallAsync("P3root", "555 555 555", cookie);
                await _pebble.RingAsync(cookie);
                await _pebble.RingAsync(cookie);
                await _pebble.RingAsync(cookie);

                await _pebble.StartCallAsync(cookie);
                await _pebble.EndCallAsync(cookie);
            }
            else
            {
                await MessageBox.Show("Pebble not connected");
            }
        }

        /* Util methods */

        private async Task InstallBundle(string bundleUrl)
        {
            this._currentProgressBar.IsIndeterminate = true;
            this._currentProgressBar.Value = 0;
            this._currentProgressBar.Visibility = Visibility.Visible;

            string errorMessage = null;

            try
            {
                var bundle = await this._pebble.DownloadBundleAsync(bundleUrl);
                if (bundle != null)
                {
                    switch (bundle.BundleType)
                    {
                        case BundleType.Application:
                            await this._pebble.InstallAppAsync(bundle);
                            break;

                        case BundleType.Firmware:
                            await this._pebble.InstallFirmwareAsync(bundle, false);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                this._currentProgressBar.IsIndeterminate = false;
                this._currentProgressBar.Value = 0;

                errorMessage = ex.Message;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                await MessageBox.Show(errorMessage);
            }

            this._currentProgressBar.IsIndeterminate = false;
            this._currentProgressBar.Value = 0;
        }
    }
}
