using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using EyeBrowse.Annotations;

namespace EyeBrowse
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // public abstract Task InitializeAsync();

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class PhotoBrowserViewModel : INotifyPropertyChanged
    {
        readonly CoreDispatcher _dispatcher;
        string _currentPath = string.Empty;
        readonly Stack<IStorageFile> _previous = new Stack<IStorageFile>();
        readonly Stack<IStorageFile> _next = new Stack<IStorageFile>();
        IStorageFile _current;

        int _imageWidth = 10, _imageHeight =10;

        [CanBeNull] ImageSource _currentImage;

        public PhotoBrowserViewModel(CoreDispatcher dispatcher) => _dispatcher = dispatcher;

        [CanBeNull]
        public ImageSource CurrentImage
        {
            get => _currentImage;
            set { _currentImage = value; OnPropertyChanged(nameof(CurrentImage)); }
        }

        public string CurrentPath
        {
            get => _currentPath;
            set { _currentPath = value; OnPropertyChanged(nameof(CurrentPath)); }
        }

        public int ImageHeight
        {
            get => _imageHeight;
            set { _imageHeight = value; OnPropertyChanged(nameof(ImageHeight)); }
        }

        public int ImageWidth
        {
            get => _imageWidth;
            set { _imageWidth = value;OnPropertyChanged(nameof(ImageWidth)); }
        }

        public async Task Next()
        {
            if (!_next.Any()) return;
            _previous.Push(_current);
            _current = _next.Pop();
            CurrentPath = _current.Path;
            (CurrentImage, ImageWidth, ImageHeight) = await CreateSource(_current);
        }

        public async Task Previous()
        {
            if (!_previous.Any()) return;
            _next.Push(_current);
            _current = _previous.Pop();
            CurrentPath = _current.Path;
            (CurrentImage, ImageWidth, ImageHeight) = await CreateSource(_current);
        }

        public async Task OpenFromFile(StorageFile file)
        {
            _current = file;
            CurrentPath = file.Path;
            await SetFileStacks(file);
            (CurrentImage, ImageWidth, ImageHeight) = await CreateSource(file);
        }

        async Task SetFileStacks(StorageFile file)
        {
            _previous.Clear();
            _next.Clear();

            var dir = await file.GetParentAsync();
            if (dir == null) return; //TODO: Something
            var files = (await dir.GetFilesAsync())
                .Where(f => f.FileType == ".jpeg" || f.FileType == ".jpg" || f.FileType == ".png")
                .OrderBy(f => f.Name)
                .ToList();
            var index = files.FindIndex(f => f.Path == file.Path);
            var pre = files.Take(index);
            var nxt = files.Skip(index + 1).Reverse();
            foreach (var f in pre) _previous.Push(f);
            foreach (var f in nxt) _next.Push(f);
        }
        
        static async Task<(ImageSource img, int width, int height)> CreateSource(IStorageFile file)
        {
            var fileStream = await file.OpenAsync(FileAccessMode.Read);
            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(fileStream);
            var width = bitmapImage.PixelWidth + 2;
            var height = bitmapImage.PixelHeight + 2;
            
            return (bitmapImage, width, height);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
            );
        }
    }
}