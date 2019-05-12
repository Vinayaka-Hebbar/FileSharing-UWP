using FileSharing.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace FileSharing.ViewModels
{
    public sealed class SendViewModel : ViewModelBase
    {
        private string _filePath;
        private string _iPAddress;
        private bool _isSendEnabled;

        public SendViewModel()
        {
            Send = CreateAsyncCommand(OnSend);
            SelectFile = CreateAsyncCommand(OnSelectFile);
        }

        private async Task OnSelectFile(object obj)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                FilePath = file.Path;
                IsSendEnabled = true;
            }
        }

        private async Task OnSend(object obj)
        {
            if (string.IsNullOrEmpty(IPAddress))
            {
                await NotifyAsync("Destination IP is Empty");
                return;
            }
            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                try
                {

                    var host = new Windows.Networking.HostName(IPAddress);
                    var socket = new Windows.Networking.Sockets.StreamSocket();
                    socket.Control.KeepAlive = false;
                    await socket.ConnectAsync(host, App.ServiceName);
                    DataWriter writter = new DataWriter(socket.OutputStream);
                    writter.WriteUInt32(ConnectionState.StateSending);

                    StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
                    var info = await file.GetBasicPropertiesAsync();
                    var fileInfo = Json.Serialize(new FileInfo { FileName = file.DisplayName, Size = info.Size });
                    var fileInfoLength = writter.MeasureString(fileInfo);
                    writter.WriteUInt32(fileInfoLength);
                    writter.WriteString(fileInfo);
                    //Sending Request
                    await writter.StoreAsync();

                    using (DataReader reader = new DataReader(socket.InputStream))
                    {
                        while (true)
                        {
                            var length = await reader.LoadAsync(sizeof(uint));
                            if (length != sizeof(uint)) break;
                            uint state = reader.ReadUInt32();
                            if (state == ConnectionState.StateRecieve)
                            {
                                IBuffer buffer = await FileIO.ReadBufferAsync(file);
                                writter.WriteUInt32(ConnectionState.StateRecieving);
                                writter.WriteUInt32(fileInfoLength);
                                writter.WriteString(fileInfo);
                                writter.WriteUInt32(buffer.Length);
                                writter.WriteBuffer(buffer);
                                await writter.StoreAsync();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await NotifyAsync(ex.Message);
                }
            }
            else
            {
                await NotifyAsync("Somthing wrong.. Please Restart the app and try again");
            }
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string IPAddress
        {
            get => _iPAddress;
            set => SetProperty(ref _iPAddress, value);
        }

        public Windows.Networking.HostName HostName { get; set; }

        public bool IsSendEnabled
        {
            get => _isSendEnabled;
            set => SetProperty(ref _isSendEnabled, value);
        }

        public ICommand Send { get; }

        public ICommand SelectFile { get; }

    }
}
