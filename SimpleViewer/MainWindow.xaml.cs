using SimpleViewer.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SimpleViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageCache _imageCache;
        private DispatcherTimer _timer;
        private DispatcherTimer _cacheClearTimer;
        private bool _isPlaying = false;

        // デバッグ用
        private DebugWindow _debugWindow = new DebugWindow();
        private DispatcherTimer _debugTimer;


        public MainWindow()
        {
            InitializeComponent();

            this.thumb1.DragDelta += Thumb1_DragDelta;
            this.thumb1.SizeChanged += imageThumb1_SizeChanged;
            this.baseCanvas.PreviewMouseWheel += baseCanvas_PreviewMouseWheel;
            this.TextBoxImageFolder.TextChanged += TextBoxImageFolder_TextChanged;
            this.TextBoxImageFolder.AllowDrop = true;
            this.TextBoxImageFolder.PreviewDragOver += (s, e) =>
            {
                e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : e.Effects = DragDropEffects.None;
                e.Handled = true;
            };
            this.TextBoxImageFolder.PreviewDrop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                    TextBoxImageFolder.Text = paths[0];
                }
            };
            this.ButtonMove.Click += ButtonMove_Click;
            this.TextBoxFileNo.KeyDown += TextBoxFileNo_KeyDown;

            this.ButtonBwdFast.Click += ButtonBwdFast_Click;
            this.ButtonBwdSingle.Click += ButtonBwdSingle_Click;
            this.ButtonFwdSingle.Click += ButtonFwdSingle_Click;
            this.ButtonFwdFast.Click += ButtonFwdFast_Click;
            this.ButtonPlayPause.Click += ButtonPlayPause_Click;

            Canvas.SetLeft(thumb1, 0);
            Canvas.SetTop(thumb1, 0);

            this.TextBoxRefreshTime.Text = "0.5";
            this.TextBoxSkipCount.Text = "10";
            this.TextBoxSkipCount.TextChanged += TextBoxSkipCount_TextChanged;

            this.ButtonClearCache.Click += ButtonClearCache_Click;

            _cacheClearTimer = new DispatcherTimer();
            _cacheClearTimer.Interval = TimeSpan.FromSeconds(10);
            _cacheClearTimer.Tick += _cacheClearTimer_Tick;
            _cacheClearTimer.Start();


            // デバッグ用
            _debugWindow.Show();
            _debugTimer = new DispatcherTimer();
            _debugTimer.Interval = TimeSpan.FromSeconds(1);
            _debugTimer.Tick += _debugTimer_Tick;
            _debugTimer.Start();
        }


        private void _cacheClearTimer_Tick(object sender, EventArgs e)
        {
            if (_imageCache == null)
            {
                return;
            }

            _imageCache.ClearCache();
        }

        private void ButtonClearCache_Click(object sender, RoutedEventArgs e)
        {
            _imageCache.ClearCache();
        }

        private void _debugTimer_Tick(object sender, EventArgs e)
        {
            if (_imageCache == null)
            {
                return;
            }

            var status = _imageCache.DebugGetStatus();
            var imageStore = _imageCache.DebugGetImageStore();
            _debugWindow.ShowStatus(status, imageStore);
        }

        private void ButtonPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                TimerStop();
            }
            else
            {
                ButtonPlayPause.Content = "■";
                double interval = 0.0;
                try
                {
                    interval = double.Parse(TextBoxRefreshTime.Text.Trim());
                }
                catch
                {
                    MessageBox.Show("更新間隔は正の実数を指定して下さい");
                }
                // 前後のキャッシュは最小限にする
                _imageCache.CacheImages(2, 2, SkipCount());

                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(interval);
                _timer.Tick += _timer_Tick;
                _timer.Start();
                _isPlaying = true;
            }
        }

        private void TimerStop()
        {
            ButtonPlayPause.Content = "▶";
            _timer.Stop();
            _isPlaying = false;
        }

        private async void _timer_Tick(object sender, EventArgs e)
        {
            if (await SetImageByOffsetAsync(SkipCount()))
            {
                _imageCache.CacheImages(1, 1, SkipCount());
            }
            else
            {
                TimerStop();
            }
        }

        private async void TextBoxImageFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            var folder = TextBoxImageFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            if (!Directory.Exists(folder))
            {
                return;
            }

            _imageCache = new ImageCache(folder);

            var fileCount = _imageCache.Prepare();

            _imageCache.CacheImages(5, 5, SkipCount());

            LabelFileCount.Content = $"/ {fileCount}";

            _ = await SetImageByOffsetAsync(0);
        }

        private void TextBoxSkipCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            _imageCache.CacheImages(5, 5, SkipCount());
        }

        private void ButtonMove_Click(object sender, RoutedEventArgs e)
        {
            JumpAsync();
        }

        private void TextBoxFileNo_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                JumpAsync();
            }
        }

        private async void JumpAsync()
        {
            int no = 0;
            if (int.TryParse(TextBoxFileNo.Text.Trim(), out no))
            {
                var offset = no - 1 - _imageCache.CurrentIndex;
                _ = await SetImageByOffsetAsync(offset);
                _imageCache.CacheImages(5, 5, 0);
            }

        }


        private async void ButtonFwdFast_Click(object sender, RoutedEventArgs e)
        {
            _ = await SetImageByOffsetAsync(SkipCount());
            _imageCache.CacheImages(2, 2, SkipCount());
        }

        private async void ButtonFwdSingle_Click(object sender, RoutedEventArgs e)
        {
            _ = await SetImageByOffsetAsync(1);
            _imageCache.CacheImages(5, 5, 0);
        }

        private async void ButtonBwdSingle_Click(object sender, RoutedEventArgs e)
        {
            _ = await SetImageByOffsetAsync(-1);
            _imageCache.CacheImages(5, 5, 0);
        }

        private async void ButtonBwdFast_Click(object sender, RoutedEventArgs e)
        {
            _ = await SetImageByOffsetAsync(-1 * SkipCount());
            _imageCache.CasheImages(2, 2, SkipCount());
        }

        private int SkipCount()
        {
            try
            {
                return int.Parse(TextBoxSkipCount.Text.Trim());
            }
            catch
            {
                MessageBox.Show("スキップ数は自然数を指定してください");
                return 0;
            }
        }



        private async Task<bool> SetImageByOffsetAsync(int offset)
        {
            var imageThumb1 = thumb1.Template.FindName("imageThumb1", thumb1) as Image;
            var bitmap = await _imageCache.GetImageAsync(offset);
            if (bitmap != null)
            {
                imageThumb1.Source = bitmap;
                TextBoxFileNo.Text = (_imageCache.CurrentIndex + 1).ToString();
                TextBoxFileName.Text = _imageCache.CurrentFileName;
                return true;
            }
            else
            {
                imageThumb1.Source = null;
                TextBoxFileNo.Text = "-";
                TextBoxFileName.Text = "-";
                return false;
            }
        }


        private void Thumb1_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb1 = sender as Thumb;
            Canvas.SetLeft(thumb1, Canvas.GetLeft(thumb1) + e.HorizontalChange);
            Canvas.SetTop(thumb1, Canvas.GetTop(thumb1) + e.VerticalChange);
        }

        private void imageThumb1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            baseCanvas.Height = e.NewSize.Height;
            baseCanvas.Width = e.NewSize.Width;
        }

        private void baseCanvas_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;              // 後続のイベントを実行しないための処理

            double scale;
            if (0 < e.Delta)
            {
                scale = 1.2;
            }
            else
            {
                scale = 0.8;
            }

            var imageThumb1 = thumb1.Template.FindName("imageThumb1", thumb1) as Image;

            // 画像の拡大縮小
            imageThumb1.Height = imageThumb1.ActualHeight * scale;
            imageThumb1.Width = imageThumb1.ActualWidth * scale;

            // マウス位置が中心になるようにスクロールバーの位置を調整
            //Point mousePoint = e.GetPosition(scrollViewer);
            //double x_barOffset = (scrollViewer.HorizontalOffset + mousePoint.X) * scale - mousePoint.X;
            //scrollViewer.ScrollToHorizontalOffset(x_barOffset);

            //double y_barOffset = (scrollViewer.VerticalOffset + mousePoint.Y) * scale - mousePoint.Y;
            //scrollViewer.ScrollToVerticalOffset(y_barOffset);
        }
    }
}
