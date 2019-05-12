using FileSharing.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FileSharing
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string SavePathKey = "SavePath";
        private readonly ApplicationDataContainer settings;
        public MainPage()
        {
            InitializeComponent();
            settings = ApplicationData.Current.LocalSettings;
            LoadSettings();

        }

        private async void LoadSettings()
        {

            if (!settings.Values.ContainsKey(SavePathKey))
            {
                var downloadFolder = await DownloadsFolder.CreateFolderAsync("SharedFiles", CreationCollisionOption.GenerateUniqueName);
                settings.Values.Add(SavePathKey, downloadFolder.Path);
            }
            SavePathField.Text = settings.Values[SavePathKey].ToString();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (CoreApplication.Properties.TryGetValue("host", out object hostName))
                {
                    var currentHost = (HostName)hostName;
                    IPInfoField.Text = currentHost.CanonicalName;

                }
            }
            if (e.NavigationMode == NavigationMode.New)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackPressed;
                Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += OnNetworkChanged;
                CoreApplication.Exiting += OnExit;
                await CreateListener();
            }
        }

        private async Task CreateListener()
        {
            HostName hostName;
            if (!CoreApplication.Properties.ContainsKey("listener"))
            {
                try
                {
                    StreamSocketListener listener = new StreamSocketListener();
                    listener.Control.KeepAlive = false;
                    listener.ConnectionReceived += OnRecieved;
                    hostName = LoadIpInfo();
                    await listener.BindEndpointAsync(hostName, App.ServiceName);
                    CoreApplication.Properties.Add("listener", listener);
                    CoreApplication.Properties.Add("host", hostName);
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message).ShowAsync();
                }
            }
        }

        private async void OnNetworkChanged(object sender)
        {
            StopListener();
            await CreateListener();
        }

        private HostName LoadIpInfo()
        {
            HostName hostName;
            var networkId = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().NetworkAdapter.NetworkAdapterId;

            hostName = Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
                 .Where(host => host.Type == HostNameType.Ipv4 && host.IPInformation.NetworkAdapter.NetworkAdapterId == networkId).FirstOrDefault();
            IPInfoField.Text = hostName.ToString();
            return hostName;
        }

        private async void OnRecieved(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            DataReader reader = new DataReader(args.Socket.InputStream);
            while (true)
            {
                uint length = await reader.LoadAsync(sizeof(uint));
                if (length != sizeof(uint))
                    return;
                uint state = reader.ReadUInt32();
                if (state == ConnectionState.StateSending)
                {

                    var isClosed = await FileAcceptAync(reader, async (res) =>
                     {
                         DataWriter writer = new DataWriter(args.Socket.OutputStream);
                         if (res == ResponceType.Accept)
                         {
                             writer.WriteUInt32(ConnectionState.StateRecieve);
                         }
                         else if (res == ResponceType.Deny)
                         {
                             writer.WriteUInt32(ConnectionState.StateDeny);
                         }

                         await writer.StoreAsync();
                     });
                    if (isClosed)
                    {
                        return;
                    }

                }
                if (state == ConnectionState.StateRecieving)
                {
                    var res = await RecieveFileAsync(reader);
                    if (res == ResponceType.Close)
                        return;
                }

            }
        }

        private async Task<ResponceType> RecieveFileAsync(DataReader reader)
        {
            var fileInfoLength = await reader.LoadAsync(sizeof(uint));
            if (fileInfoLength != sizeof(uint)) return ResponceType.Close;
            var fileInfoCount = reader.ReadUInt32();
            var actualFileInfoCount = await reader.LoadAsync(fileInfoCount);
            if (fileInfoCount != actualFileInfoCount) return ResponceType.Close;
            var fileInfo = Json.Deserialize<FileInfo>(reader.ReadString(fileInfoCount));
            var length = await reader.LoadAsync(sizeof(uint));
            if (length != sizeof(uint)) return ResponceType.Close;
            var actualContentLength = reader.ReadUInt32();
            var contentLength = await reader.LoadAsync(actualContentLength);
            if (actualContentLength != contentLength) return ResponceType.Close;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LogField.Text += $"File {fileInfo.FileName}.{fileInfo.FileType}\n";
            });
            await SaveRecievedFile(fileInfo, reader.ReadBuffer(actualContentLength));
            return ResponceType.Accept;

        }

        private async Task SaveRecievedFile(FileInfo info, IBuffer buffer)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(settings.Values[SavePathKey].ToString());
                StorageFile file = await folder.CreateFileAsync($"{info.FileName}{info.FileType}", CreationCollisionOption.GenerateUniqueName);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await stream.WriteAsync(buffer);
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task<bool> FileAcceptAync(IDataReader reader, Action<ResponceType> action)
        {
            var length = await reader.LoadAsync(sizeof(uint));
            if (length != sizeof(uint)) return true;
            var actualContentLength = reader.ReadUInt32();
            var contentLength = await reader.LoadAsync(actualContentLength);
            if (actualContentLength != contentLength) return true;
            FileInfo fileInfo = Json.Deserialize<FileInfo>(reader.ReadString(contentLength));
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
             {
                 var dialog = new MessageDialog($"File {fileInfo.FileName}", "Do you want to recieve");
                 dialog.Commands.Add(new UICommand("Ok", (c) => { action(ResponceType.Accept); }));
                 dialog.Commands.Add(new UICommand("Cancel", (c) => { action(ResponceType.Deny); }));
                 dialog.DefaultCommandIndex = 0;
                 dialog.DefaultCommandIndex = 1;
                 await dialog.ShowAsync();
             });
            return false;
        }

        private void OnExit(object sender, object e)
        {
            StopListener();
        }

        private static void StopListener()
        {
            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                var listener = (StreamSocketListener)CoreApplication.Properties["listener"];
                listener.Dispose();
                CoreApplication.Properties.Remove("listener");
            }
        }

        private void OnBackPressed(object sender, BackRequestedEventArgs e)
        {
            Frame.GoBack();
            if (!Frame.CanGoBack)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
        }

        private void OnSendClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SendPage));
        }

        private async void OnPathChangeClick(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var seletedFolder = await folderPicker.PickSingleFolderAsync();
            if (seletedFolder != null)
            {
                settings.Values[SavePathKey] = seletedFolder.Path;
                SavePathField.Text = seletedFolder.Path;
            }
        }
    }
}
