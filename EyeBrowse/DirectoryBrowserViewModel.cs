using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using EyeBrowse.Annotations;

namespace EyeBrowse
{
    public sealed class PhotoThumbnailViewModel
    {
        public PhotoThumbnailViewModel(StorageFile file, ImageSource source)
        {
            File = file;
            Source = source;
        }

        public StorageFile File { get; }
        public ImageSource Source { get; }

        public static async Task<PhotoThumbnailViewModel> CreateAsync(StorageFile file)
        {
            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);

                return new PhotoThumbnailViewModel(file, bitmapImage);
            }
        }
    }

    public sealed  class DirectoryThumbnailViewModel {}
    
    public class DirectoryBrowserViewModel : BaseViewModel
    {
        // StorageFolder _folder;
        
        public async Task InitializeAsync(StorageFolder folder)
        {
            Path =folder.Path;
            var folders = await folder.GetFoldersAsync();
            var files = await folder.GetFilesAsync();

            Children = folders.Select(x => x.DisplayName).ToList();
            SelectedDir = Children.FirstOrDefault();
            var makePhotos = files
                .Where(f => f.FileType == ".jpeg" || f.FileType == ".jpg" || f.FileType == ".png")
                .Select(PhotoThumbnailViewModel.CreateAsync)
                .ToArray();
            Photos = await Task.WhenAll(makePhotos);
            if (SelectedDir is null) SelectedPhoto = Photos.FirstOrDefault();

            Initialized = true;
        }

        public bool Initialized { get; private set; }

        public string Path { get; private set;  }

        public List<string> Children { get; private set; } = Enumerable.Empty<string>().ToList();

        [CanBeNull] string _selectedDir;
        [CanBeNull] public string SelectedDir
        {
            get => _selectedDir;
            set { _selectedDir = value; base.OnPropertyChanged(nameof(SelectedDir)); }
        }

        [CanBeNull] PhotoThumbnailViewModel _selectedPhoto;

        [CanBeNull]
        public PhotoThumbnailViewModel SelectedPhoto
        {
            get => _selectedPhoto;
            set { _selectedPhoto = value; base.OnPropertyChanged(nameof(SelectedPhoto)); }
        }

        public IList<PhotoThumbnailViewModel> Photos { get; private set; } = Array.Empty<PhotoThumbnailViewModel>();

        public void Select(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up when SelectedDir != null && SelectedDir != Children.First():
                    SelectedDir = Children[Children.IndexOf(SelectedDir) - 1];
                    break;
                
                case Direction.Down when SelectedDir != null && SelectedDir != Children.Last():
                    SelectedDir = Children[Children.IndexOf(SelectedDir) + 1];
                    break;
                    
                case Direction.Left when SelectedPhoto is PhotoThumbnailViewModel p && p != Photos.First():
                    SelectedPhoto = Photos[Photos.IndexOf(SelectedPhoto) - 1];
                    break;

                case Direction.Right when SelectedPhoto is PhotoThumbnailViewModel p && p != Photos.Last():
                    SelectedPhoto = Photos[Photos.IndexOf(SelectedPhoto) + 1];
                    break;
            }
        }
    }
}