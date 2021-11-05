using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ScanBeacons
{
    public sealed partial class MainPage : Page
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private List<ulong> beaconIDs = new List<ulong>();
        private ObservableCollection<BeaconInfo> Beacons = new ObservableCollection<BeaconInfo>();
        private bool isScanning = false;

        #region UI Code

        public MainPage()
        {
            InitializeComponent();
            CopiedPanel.Visibility = Visibility.Collapsed;
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += OnAdvertisementReceived;
            ChangeScanState();
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher,
                                                   BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            const ushort AppleCompanyId = 0x004C;
            foreach (var adv in eventArgs.Advertisement.ManufacturerData.Where(x => x.CompanyId == AppleCompanyId))
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Debug.WriteLine($"LocalNmae: {eventArgs.Advertisement.LocalName}");

                    UpdateBeacons(adv.Data,
                                  eventArgs.BluetoothAddress,
                                  eventArgs.RawSignalStrengthInDBm);

                    Debug.WriteLine($"> Count : {beaconIDs.Count}, {Beacons.Count}");
                });
            }
        }

        private void UpdateBeacons(IBuffer buffer, ulong address, short rssi)
        {
            byte[] bytes = new byte[buffer.Length];
            using (var reader = DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(bytes);
            }

            if (bytes[0] != 0x02 || bytes[1] != 0x15 || bytes.Length != 23)
            {
                return;
            }

            var beaconInfo = BeaconInfo.Create(bytes, rssi);
            Debug.WriteLine($"addr: {address}");
            Debug.WriteLine($"beaconInfo: {beaconInfo}");

            int index = beaconIDs.IndexOf(address);
            Debug.WriteLine($"index: {index}");
            if (index >= 0)
            {
                // update
                Beacons[index] = beaconInfo;
            }
            else
            {
                // add
                beaconIDs.Add(address);
                Beacons.Add(beaconInfo);
            }
            Debug.WriteLine("-----------------");
        }

        private void ChangeScanState()
        {
            isScanning = !isScanning;
            if (isScanning)
            {
                // clear
                beaconIDs.Clear();
                Beacons.Clear();

                // start
                watcher.Start();
                ScanButton.Content = "Stop";
                StateProgressBar.Visibility = Visibility.Visible;
                Debug.WriteLine("Bluetooth LE Advertisement Watcher Started.");
            }
            else
            {
                watcher.Stop();
                ScanButton.Content = "Scan";
                StateProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Click Events

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeScanState();
        }

        private void BeaconListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Debug.WriteLine($">> Copy string: {e.ClickedItem}");

            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetText(e.ClickedItem.ToString());
            Clipboard.SetContent(dataPackage);

            // show CopiedPanel
            if (CopiedPanel.Visibility == Visibility.Collapsed)
            {
                CopiedPanel.Visibility = Visibility.Visible;
                var delay = TimeSpan.FromSeconds(2);
                var DelayTimer = ThreadPoolTimer.CreateTimer((source) =>
                {
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        // hide CopiedPanel
                        CopiedPanel.Visibility = Visibility.Collapsed;
                    });
                }, delay);
            }
        }

        #endregion
    }
}
