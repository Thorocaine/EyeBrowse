using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using VirtualKey = Windows.System.VirtualKey;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EyeBrowse
{
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
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
        }

        void OnTick(object sender, object e)
        {
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
            switch (e.VirtualKey)
            {
                case VirtualKey.Escape:
                    Application.Current.Exit();
                    break;
                case VirtualKey.Z:
                    Image.Width = ActualWidth;
                    Image.Height = double.NaN;
                    Image.MaxWidth=double.MaxValue;
                    Image.MaxHeight=double.MaxValue;
                    break;
                case VirtualKey.X:
                    Image.Width = double.NaN;
                    Image.Width = double.NaN;
                    Image.MaxWidth=double.MaxValue;
                    Image.MaxHeight=double.MaxValue;
                    break;
                case VirtualKey.C:
                    Image.Width = double.NaN;
                    Image.Height = double.NaN;
                    Image.MaxWidth= ActualWidth;
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

        enum Direction { Up, Down, Left, Right }

        //async Task ScrollAsync(Direction direction)
        //{
        //    var (h,v) = CalculateOffset(direction);
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => ApplyOffset(h, v));
        //}

        //(double h, double v) CalculateOffset(Direction direction)
        //{
        //    var h = Viewer.HorizontalOffset;
        //    var v = Viewer.VerticalOffset;
        //    switch (direction)
        //    {
        //        case Direction.Down: return (h, v + 10);
        //        case Direction.Left: return (h - 10, v);
        //        case Direction.Right: return (h + 10, v);
        //        case Direction.Up: return (h, v - 10);
        //        default: return (h, v);
        //    }
        //}

        //void ApplyOffset(double h, double v)
        //{
        //    var zf = Viewer.ZoomFactor;
        //    Viewer.ChangeView(h, v, zf, false);
        //}

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var file = (e.Parameter as FileActivatedEventArgs)?.Files.FirstOrDefault() as StorageFile
                       ?? await PickFileDuringLaunch();
            await _vm.OpenFromFile(file).ConfigureAwait(false);
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
