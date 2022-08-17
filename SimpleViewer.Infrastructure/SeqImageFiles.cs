using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SimpleViewer.Infrastructure
{
    public class SeqImageFiles
    {
        private string _folderPath;

        private Dictionary<string, BitmapImage> _imageStore;

        private string[] _files;
        private Task<bool>[] _status;

        private int _cacheCountFwd;
        private int _cacheCountBwd;

        private Queue<int> _history = new Queue<int>();
        private int MAX_CACHE_COUNT = 100;

        public int CurrentIndex { get; private set; }

        public string CurrentFileName { get; private set; }

        public SeqImageFiles(string folderPath)
        {
            _folderPath = folderPath;
            _imageStore = new Dictionary<string, BitmapImage>();
        }

        public async Task<BitmapImage> GetImageAsync(int offset = 0)
        {
            var index = CurrentIndex + offset;
            if (0 <= index && index < _files.Length)
            {
                if (_status[index] == null)
                {
                    CacheImages(index);
                }

                // 読み込み完了を待つ
                await _status[index];

                var filepath = _files[index];
                if (!_imageStore.ContainsKey(filepath))
                {
                    _status[index] = ReadImageAsync(filepath);
                    await _status[index];
                }
                CurrentIndex = index;
                CurrentFileName = Path.GetFileName(_files[index]);
                CacheImages();
                _history.Enqueue(index);
                return _imageStore[filepath];
            }
            else
            {
                return null;
            }
        }

        public int Prepare(int cacheCountFwd, int cacheCountBwd)
        {
            _files = Directory.GetFiles(_folderPath);
            _status = new Task<bool>[_files.Length];
            CurrentIndex = 0;
            _cacheCountFwd = cacheCountFwd;
            _cacheCountBwd = cacheCountBwd;

            CacheImages();

            return _files.Length;
        }


        private void CacheImages(int? index = null)
        {
            var target = index ?? CurrentIndex;
            var min = Math.Max(target - _cacheCountBwd, 0);
            var max = Math.Min(target + _cacheCountFwd, _files.Length);

            for (int i = min; i < max; i++)
            {
                _status[i] = ReadImageAsync(_files[i]);
            }

            // キャッシュクリア
            while(_history.Count > MAX_CACHE_COUNT)
            {
                var oldest = _history.Dequeue();
                if (!_history.Contains(oldest))
                {
                    _imageStore.Remove(_files[oldest]);
                }
            }
        }

        private async Task<bool> ReadImageAsync(string filepath)
        {
            if (_imageStore.ContainsKey(filepath))
            {
                return true;
            }
            else
            {
                try
                {
                    using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[fs.Length];
                        var pos = await fs.ReadAsync(bytes, 0, (int)fs.Length);

                        using (var stream = new WrappingStream(new MemoryStream(bytes)))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            _imageStore.Add(filepath, bitmap);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {

                    return false;
                }
            }
        }
    }
}
