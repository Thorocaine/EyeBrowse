using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace EyeBrowse
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DirectoryBrowser : Page
    {
        readonly DirectoryBrowserViewModel _vm;
        StorageFolder _folder;

        public DirectoryBrowser()
        {
            this.DataContext = _vm = new DirectoryBrowserViewModel();
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyUp;
        }

        ~DirectoryBrowser()
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyUp;
        }

        async void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (!(Parent is Frame frame)) return;
            switch (e.VirtualKey)
            {
                case VirtualKey.Escape:
                    Application.Current.Exit();
                    break;

                case VirtualKey.E when _vm.SelectedDir is string dir:
                    var path = Path.Combine(_vm.Path, dir);
                    var folder = await StorageFolder.GetFolderFromPathAsync(path);
                    (Parent as Frame)?.Navigate(typeof(DirectoryBrowser), folder);
                    break;

                case VirtualKey.E when _vm.SelectedPhoto is PhotoThumbnailViewModel photo:
                    (Parent as Frame)?.Navigate(typeof(MainPage), photo);
                    break;

                case VirtualKey.Q when Parent is Frame f && f.CanGoBack:
                    f.GoBack();
                    //var backPath = Path.Combine(_vm.Path, "..");
                    //var backFolder = await StorageFolder.GetFolderFromPathAsync(backPath);
                    //(Parent as Frame)?.Navigate(typeof(DirectoryBrowser), backFolder);

                    break;


                case VirtualKey.A:
                    _vm.Select(Direction.Left);
                    break;
                case VirtualKey.D:
                    _vm.Select(Direction.Right);
                    break;
                case VirtualKey.S:
                    _vm.Select(Direction.Down);
                    break;
                case VirtualKey.W:
                    _vm.Select(Direction.Up);
                    break;
            }
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var folder = e.Parameter as StorageFolder
                         ?? await ((IAsyncOperation<StorageFolder>)e.Parameter);

            await _vm.InitializeAsync(folder).ConfigureAwait(false);
            return;

            //if (e.NavigationMode == NavigationMode.Back) return;

            if (_vm.Initialized) return;
            if (_folder is null)
            {
                _folder = e.Parameter as StorageFolder
                          ?? await (new Windows.Storage.Pickers.FolderPicker
                          {
                              ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                              SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
                          })
                              .PickSingleFolderAsync();
            }
            await _vm.InitializeAsync(_folder).ConfigureAwait(false);
        }
    }
}
