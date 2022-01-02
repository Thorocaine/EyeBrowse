using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using VirtualKey = Windows.System.VirtualKey;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EyeBrowse
{
    public enum Direction { Up, Down, Left, Right }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        readonly PhotoBrowserViewModel _vm;
        readonly List<Direction> _panKeys = new List<Direction>();
        readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

        public MainPage()
        {
            InitializeComponent();
            DataContext = _vm = new PhotoBrowserViewModel(Dispatcher);
            _timer.Tick += OnTick;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        ~MainPage()
        {
            _timer.Tick -= OnTick;
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
        }

        void OnTick(object sender, object e)
        {
            if (!_panKeys.Any()) return;
            var h = Viewer.HorizontalOffset;
            var v = Viewer.VerticalOffset;
            if (_panKeys.Contains(Direction.Down)) v += 10;
            if (_panKeys.Contains(Direction.Up)) v -= 10;
            if (_panKeys.Contains(Direction.Right)) h += 10;
            if (_panKeys.Contains(Direction.Left)) h -= 10;

            var zf = Viewer.ZoomFactor;
            Viewer.ChangeView(h, v, zf, false);
        }

        void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            switch (e.VirtualKey)
            {
                case VirtualKey.W:
                    AddPan(Direction.Up);
                    break;
                case VirtualKey.A:
                    AddPan(Direction.Left);
                    break;
                case VirtualKey.S:
                    AddPan(Direction.Down);
                    break;
                case VirtualKey.D:
                    AddPan(Direction.Right);
                    break;
            }

            void AddPan(Direction direction)
            {
                _panKeys.Add(direction);
                if (!_timer.IsEnabled) _timer.Start();
            }
        }

        async void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (!(Parent is Frame frame)) return;
            switch (e.VirtualKey)
            {
                case VirtualKey.Escape when Parent is Frame f && f.CanGoBack:
                    f.GoBack();
                    break;
                    
                case VirtualKey.Escape:
                    Application.Current.Exit();
                    break;
                case VirtualKey.Z:
                    Image.Width = ActualWidth;
                    Image.Height = double.NaN;
                    Image.MaxWidth = double.MaxValue;
                    Image.MaxHeight = double.MaxValue;
                    break;
                case VirtualKey.X:
                    Image.Width = double.NaN;
                    Image.Width = double.NaN;
                    Image.MaxWidth = double.MaxValue;
                    Image.MaxHeight = double.MaxValue;
                    break;
                case VirtualKey.C:
                    Image.Width = double.NaN;
                    Image.Height = double.NaN;
                    Image.MaxWidth = ActualWidth;
                    Image.MaxHeight = ActualHeight;
                    break;


                case VirtualKey.E:
                    await _vm.Next();
                    break;
                case VirtualKey.Q:
                    await _vm.Previous();
                    break;
                case VirtualKey.W:
                    RemovePan(Direction.Up);
                    break;
                case VirtualKey.A:
                    RemovePan(Direction.Left);
                    break;
                case VirtualKey.S:
                    RemovePan(Direction.Down);
                    break;
                case VirtualKey.D:
                    RemovePan(Direction.Right);
                    break;
            }

            void RemovePan(Direction direction)
            {
                _panKeys.RemoveAll(x => x == direction);
                if (!_panKeys.Any() && _timer.IsEnabled) _timer.Stop();
            }
        }

        
        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var file = await FromFromParameter(e);
            await _vm.OpenFromFile(file).ConfigureAwait(false);
        }

        static async Task<StorageFile> FromFromParameter(NavigationEventArgs e)
        {
            switch (e.Parameter)
            {
                case PhotoThumbnailViewModel ptvm: return ptvm.File;
                case FileActivatedEventArgs fae: return fae.Files.FirstOrDefault() as StorageFile;
                default: return await PickFileDuringLaunch();
            }
        }


        public static IAsyncOperation<StorageFile> PickFileDuringLaunch()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" },
            };
            return picker.PickSingleFileAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
