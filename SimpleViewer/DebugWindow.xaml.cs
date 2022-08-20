using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleViewer
{
    /// <summary>
    /// DebugWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
        }

        public void ConsoleOut(string text)
        {
            TextBox1.Text += text + "\r\n";
        }

        public void ShowStatus(Task<bool>[] status, Dictionary<int, BitmapImage> imageStore)
        {
            TextBox1.Clear();
            ConsoleOut($"読み込み数 : {imageStore.Count}");
            for(int i = 0; i < status.Length; i++)
            {
                var statusStr = status[i] == null ? "null" : (status[i].IsCompleted ? status[i].Result.ToString() : "not_completed");
                var storedStr = imageStore.ContainsKey(i) ? "stored" : "-";
                ConsoleOut($"{i} : {statusStr} | {storedStr}");
            }
        }
    }
}
