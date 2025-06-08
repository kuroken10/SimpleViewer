using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SimpleViewer.Infrastructure
{
    public class ImageCache
    {

        private string _folderPath;

        //private Dictionary<string, BitmapImage> _imageStore = new Dictionary<string, BitmapImage>();
        private Dictionary<int, BitmapImage> _imageStore = new Dictionary<int, BitmapImage>();
        //private Dictionary<string, int> _cachedIndex = new Dictionary<string, int>();

        private string[] _files;
        private Task<bool>[] _status;


        private List<int> _cacheTarget = new List<int>();


        //private int _cacheCountFwd;
        //private int _cacheCountBwd;

        //private Queue<int> _history = new Queue<int>();
        private int MAX_CACHE_COUNT = 20;

        public int CurrentIndex { get; private set; }

        public string CurrentFileName { get; private set; }

        public ImageCache(string folderPath)
        {
            _folderPath = folderPath;
        }

        public async Task<BitmapImage> GetImageAsync(int offset = 0)
        {
            var fileIndex = CurrentIndex + offset;
            if (0 <= fileIndex && fileIndex < _files.Length)
            {
                if (_status[fileIndex] == null)
                {
                    _status[fileIndex] = ReadImageAsync(fileIndex);
                }

                // 読み込み完了を待つ
                await _status[fileIndex];

                // ファイルが無ければ読み込み
                if (!_imageStore.ContainsKey(fileIndex))
                {
                    _status[fileIndex] = ReadImageAsync(fileIndex);
                    await _status[fileIndex];
                }

                CurrentIndex = fileIndex;
                CurrentFileName = Path.GetFileName(_files[fileIndex]);

                return _imageStore[fileIndex];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 画像一覧を取得
        /// </summary>
        /// <returns></returns>
        public int Prepare()
        {
            _files = Directory.GetFiles(_folderPath);
            _status = new Task<bool>[_files.Length];
            CurrentIndex = 0;
            //_cacheCountFwd = cacheCountFwd;
            //_cacheCountBwd = cacheCountBwd;

            //CacheImages();

            return _files.Length;
        }

        public void CacheImages(int fwdCount, int bwdCount, int skipCount)
        {
            int min = Math.Max(CurrentIndex - bwdCount, 0);
            int max = Math.Min(CurrentIndex + fwdCount, _files.Length - 1);

            _cacheTarget.Clear();

            // 前後のファイルをキャッシュ対象にする
            for (int i = min; i <= max; i++)
            {
                _cacheTarget.Add(i);
            }

            // スキップ数が０より大きい場合、スキップ先のファイルをキャッシュ対象にする
            if (skipCount > 0)
            {
                var skiped = CurrentIndex;
                while (_cacheTarget.Count < MAX_CACHE_COUNT)
                {
                    skiped += skipCount;
                    if (skiped >= _files.Length)
                    {
                        break;
                    }
                    _cacheTarget.Add(skiped);
                }
            }

            // キャッシュ実行
            foreach (var target in _cacheTarget)
            {
                bool isRead = false;

                if (_status[target] == null)
                {
                    isRead = true;
                }
                else if (_status[target].IsCompleted && _status[target].Result == false)
                {
                    // 読み込み失敗ファイルを毎回読み込みチャレンジしてしまうためfalseとする
                    isRead = false;
                }
                else
                {
                    isRead = false;
                }

                if (isRead && !_imageStore.ContainsKey(target))
                {
                    _status[target] = ReadImageAsync(target);
                }
            }

        }

        public void ClearCache()
        {
            if(_status == null || _cacheTarget == null || _imageStore == null)
            {
                return;
            }

            for(int i =0; i < _status.Length; i++)
            {
                if (_status[i] != null && _status[i].IsCompleted)
                {
                    if (_cacheTarget.Contains(i))
                    {
                        continue;
                    }

                    if (_imageStore.ContainsKey(i))
                    {
                        _imageStore.Remove(i);
                        _status[i] = null;
                    }
                }
            }
        }

        public Task<bool>[] DebugGetStatus()
        {
            return _status;
        }

        public Dictionary<int, BitmapImage> DebugGetImageStore()
        {
            return _imageStore;
        }

        public string[] DebugGetFileList()
        {
            return _files;
        }


        //private void CacheImages(int? index = null)
        //{
        //    var target = index ?? CurrentIndex;
        //    var min = Math.Max(target - _cacheCountBwd, 0);
        //    var max = Math.Min(target + _cacheCountFwd, _files.Length);

        //    for (int i = min; i < max; i++)
        //    {
        //        _status[i] = ReadImageAsync(_files[i]);
        //    }

        //    // キャッシュクリア
        //    while(_history.Count > MAX_CACHE_COUNT)
        //    {
        //        var oldest = _history.Dequeue();
        //        if (!_history.Contains(oldest))
        //        {
        //            _imageStore.Remove(_files[oldest]);
        //        }
        //    }
        //}

        private async Task<bool> ReadImageAsync(int fileIndex)
        {
            if (_imageStore.ContainsKey(fileIndex))
            {
                return true;
            }
            else
            {
                try
                {
                    var filepath = _files[fileIndex];
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
                            _imageStore.Add(fileIndex, bitmap);
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
