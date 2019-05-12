using FileSharing.Models;
using FileSharing.Utils;
using System;
using System.Threading;
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
        private object _selectedContentType = ContentType.Text;
        private string _content;
        private object _language;


        public SendViewModel()
        {
            Send = CreateAsyncCommand(OnSend);
            SelectFile = CreateAsyncCommand(OnSelectFile);
        }

        private async Task OnSelectFile()
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

        private async Task OnSend()
        {
            if (string.IsNullOrEmpty(IPAddress))
            {
                await NotifyAsync("Destination IP is Empty");
                return;
            }
            IsBusy = true;
            var contentType = Enum.Parse<ContentType>(_selectedContentType.ToString());
            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                Windows.Networking.Sockets.StreamSocket socket = null;
                try
                {
                    var host = new Windows.Networking.HostName(IPAddress);
                    socket = new Windows.Networking.Sockets.StreamSocket();
                    socket.Control.KeepAlive = false;
                    await socket.ConnectAsync(host, App.ServiceName);
                    switch (contentType)
                    {
                        case ContentType.File:
                            await SendFileAsync(socket.InputStream, socket.OutputStream);
                            break;
                        case ContentType.Code:
                            var language = Enum.Parse<HighlightedLanguage>(_language.ToString());
                            await SendContentAsync(socket.InputStream, socket.OutputStream, contentType);
                            break;
                        case ContentType.Text:
                            await SendContentAsync(socket.InputStream, socket.OutputStream, contentType);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    await NotifyAsync(ex.Message);
                }
                finally
                {
                    socket?.Dispose();
                    IsBusy = false;
                }
            }
            else
            {
                await NotifyAsync("Somthing wrong.. Please Restart the app and try again");
            }
        }

        private async Task SendContentAsync(IInputStream input, IOutputStream output, ContentType contentType, HighlightedLanguage language = HighlightedLanguage.PlainText)
        {
            using (DataWriter writter = new DataWriter(output))
            {
                writter.WriteUInt32(ConnectionState.StateSending);
                var fileInfo = Json.Serialize(new ContentInfo
                {
                    ContentType = contentType,
                    Extension = language.ToString(),
                });
                var fileInfoLength = writter.MeasureString(fileInfo);
                writter.WriteUInt32(fileInfoLength);
                writter.WriteString(fileInfo);
                //Sending Request
                await writter.StoreAsync();

                using (DataReader reader = new DataReader(input))
                {
                    while (true)
                    {
                        var length = await reader.LoadAsync(sizeof(uint));
                        if (length != sizeof(uint)) break;
                        uint state = reader.ReadUInt32();
                        if (state == ConnectionState.StateRecieve)
                        {
                            //Sending File
                            writter.WriteUInt32(ConnectionState.StateRecieving);
                            writter.WriteUInt32(fileInfoLength);
                            writter.WriteString(fileInfo);
                            using (DataWriter dataWriter = new DataWriter())
                            {
                                dataWriter.WriteString(Content);
                                var buffer = dataWriter.DetachBuffer();
                                writter.WriteUInt32(buffer.Length);
                                writter.WriteBuffer(buffer);
                            }
                            await writter.StoreAsync();
                            break;
                        }
                    }
                }
            }
        }

        private async Task SendFileAsync(IInputStream input, IOutputStream output)
        {
            using (DataWriter writter = new DataWriter(output))
            {
                writter.WriteUInt32(ConnectionState.StateSending);
                StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
                var info = await file.GetBasicPropertiesAsync();
                var fileInfo = Json.Serialize(new ContentInfo
                {
                    Name = file.DisplayName,
                    ContentType = ContentType.File,
                    Extension = file.FileType,
                    Size = info.Size
                });
                var fileInfoLength = writter.MeasureString(fileInfo);
                writter.WriteUInt32(fileInfoLength);
                writter.WriteString(fileInfo);
                //Sending Request
                await writter.StoreAsync();

                using (DataReader reader = new DataReader(input))
                {
                    while (true)
                    {
                        var length = await reader.LoadAsync(sizeof(uint));
                        if (length != sizeof(uint)) break;
                        uint state = reader.ReadUInt32();
                        if (state == ConnectionState.StateRecieve)
                        {
                            //Sending File
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

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public object HighlightLanguage
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public Array ContentTypes
        {
            get
            {
                return Enum.GetValues(typeof(ContentType));
            }
        }

        static readonly HighlightedLanguage[] _languages =
                {
                  HighlightedLanguage.Cpp, HighlightedLanguage.CSharp, HighlightedLanguage.Java
                };

        public Array HighlightLanguages
        {
            get
            {
                return _languages;
            }
        }

        public ICommand Send { get; }

        public ICommand SelectFile { get; }

        public object SelectedContentType
        {
            get => _selectedContentType;
            set => SetProperty(ref _selectedContentType, value);
        }

    }
}
