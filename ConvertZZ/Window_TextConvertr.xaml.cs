﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace ConvertZZ
{
    /// <summary>
    /// TextConvertr.xaml 的互動邏輯
    /// </summary>
    public partial class Window_TextConvertr : Window, INotifyPropertyChanged
    {
        public Window_TextConvertr()
        {
            DataContext = this;
            InitializeComponent();
        }
        /// <summary>
        /// 編碼轉換 [0]:來源編碼   [1]:輸出編碼
        /// </summary>
        Encoding[] encoding = new Encoding[2];
        /// <summary>
        /// 輸出簡繁轉換：0:一般  1:繁體中文 2:簡體中文
        /// </summary>
        int ToChinese = 0;
        /// <summary>
        /// 模式   true:檔案模式   false:剪貼簿模式
        /// </summary>
        bool FileMode=false;
        string OutputPath = "";
        private string Convert(string origin)
        {
            switch (ToChinese)
            {
                case 1:
                    origin = ChineseConverter.ToTraditional(origin);
                    if (App.Settings.VocabularyCorrection)
                        origin = App.ChineseConverter.Convert(origin);
                    break;
                case 2:
                    origin = ChineseConverter.ToSimplified(origin);
                    if (App.Settings.VocabularyCorrection)
                        origin = App.ChineseConverter.Convert(origin);
                    break;
            }
           return encoding[1].GetString(encoding[0].GetBytes(origin));
        }
        private void Button_Convert_Click(object sender, RoutedEventArgs e)
        {
            switch (FileMode)
            {
                case true:
                    var temp = FileList.Where(x => x.isChecked).ToList();
                    foreach (var _temp in temp)
                    {
                        string str = "";
                        using (StreamReader sr = new StreamReader(_temp.FilePath, encoding[0]))
                        {
                            str = sr.ReadToEnd();
                            sr.Close();
                        }
                        str = Convert(str);
                        using (StreamWriter sw = new StreamWriter(Path.Combine(OutputPath, _temp.FileName), false, encoding[0]))
                        {
                            sw.Write(str);
                            sw.Flush();
                        }
                    }
                    break;
                case false:
                    string Clip = ClipBoardHelper.GetClipBoard_UnicodeText();
                    InputPreviewText = Clip;
                    Clip = Convert(Clip);
                    OutputPreviewText = Clip;
                    break;
            }
        }
        private void Button_Clear_Clicked(object sender, RoutedEventArgs e)
        {
            FileList.Clear();
        }
        private void Button_SelectFile_Clicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog() { Multiselect = true, CheckFileExists = false, CheckPathExists = true, ValidateNames = false };
            fileDialog.InitialDirectory = App.Settings.FileConvert.DefaultPath;
            fileDialog.FileName = "　";
            if (fileDialog.ShowDialog() == true)
            {
                foreach (string str in fileDialog.FileNames)
                {
                    if (Path.GetFileName(str) == "　" && System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(str)))
                    {
                        string folderpath = System.IO.Path.GetDirectoryName(str);
                        List<string> childFileList = System.IO.Directory.GetFiles(folderpath).ToList();
                        childFileList.ForEach(x => FileList.Add(new FileList_Line() { isChecked = true, FileName = System.IO.Path.GetFileName(x), FilePath = folderpath }));
                    }
                    else if (File.Exists(str))
                    {
                        FileList.Add(new FileList_Line() { isChecked = true, FileName = System.IO.Path.GetFileName(str), FilePath = Path.GetDirectoryName(str) });
                    }
                }
                listview.ItemsSource = FileList;
            }
        }


        private string _ClipBoard = "";
        public string ClipBoard { get => _ClipBoard; set { _ClipBoard = value; OnPropertyChanged("ClipBoard"); } }
        private string _InputPreviewText = "";
        public string InputPreviewText { get => _InputPreviewText; set { _InputPreviewText = value; OnPropertyChanged("InputPreviewText"); } }
        private string _OutputPreviewText = "";
        public string OutputPreviewText { get => _OutputPreviewText; set { _OutputPreviewText = value; OnPropertyChanged("OutputPreviewText"); } }
       

        private ObservableCollection<FileList_Line> _FileList = new ObservableCollection<FileList_Line>();

        public ObservableCollection<FileList_Line> FileList { get => _FileList; set { _FileList = value; OnPropertyChanged("FileList"); } }

        public class FileList_Line
        {
            public int ID { get; set; }
            public bool isChecked { get; set; }     //or IsSelected maybe? whichever name you want  
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #region Definitions  
        //Constants for API Calls...  
        private const int WM_DRAWCLIPBOARD = 0x308;
        private const int WM_CHANGECBCHAIN = 0x30D;

        //Handle for next clipboard viewer...  
        private IntPtr mNextClipBoardViewerHWnd;

        //API declarations...  
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern bool ChangeClipboardChain(IntPtr HWnd, IntPtr HWndNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        #endregion


        private IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_CHANGECBCHAIN:
                    if (wParam == mNextClipBoardViewerHWnd)
                    {
                        // clipboard viewer chain changed, need to fix it. 
                        mNextClipBoardViewerHWnd = lParam;
                    }
                    else if (mNextClipBoardViewerHWnd != IntPtr.Zero)
                    {
                        // pass the message to the next viewer. 
                        SendMessage(mNextClipBoardViewerHWnd, msg, wParam, lParam);
                    }
                    break;

                case WM_DRAWCLIPBOARD:
                    // clipboard content changed 
                    if (Clipboard.ContainsText())
                    {
                        ClipBoard=ClipBoardHelper.GetClipBoard_UnicodeText();
                    }


                    // pass the message to the next viewer. 
                    SendMessage(mNextClipBoardViewerHWnd, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }
        HwndSource hWndSource;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region 註冊Hook並監聽剪貼簿
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);
            hWndSource.AddHook(this.WinProc);   // start processing window messages 
            mNextClipBoardViewerHWnd = SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer            
            #endregion
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ChangeClipboardChain(hWndSource.Handle, mNextClipBoardViewerHWnd);
        }
        private void Encoding_Selected(object sender, RoutedEventArgs e)
        {
            RadioButton radiobutton = ((RadioButton)sender);
            switch (radiobutton.GroupName)
            {
                case "origin":
                    encoding[0] = Encoding.GetEncoding((string)radiobutton.Content);
                    break;
                case "target":
                    encoding[1] = Encoding.GetEncoding((string)radiobutton.Content);
                    break;
            }
            listview_SelectionChanged(null, null);
        }
        private void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listview.SelectedItem != null)
            {
                FileList_Line line = ((FileList_Line)listview.SelectedItem);
                string path = Path.Combine(line.FilePath, line.FileName);
                if (File.Exists(path))
                {
                    using (StreamReader sr = new StreamReader(path, encoding[0]))
                    {
                        InputPreviewText = sr.ReadToEnd();
                    }
                    OutputPreviewText = Convert(InputPreviewText);
                }
            }
        }
        private void Chinese_Click(object sender, RoutedEventArgs e)
        {
            switch(((RadioButton)sender).Uid)
            {
                case "NChinese":
                    ToChinese = 0;
                    break;
                case "TChinese":
                    ToChinese = 1;
                    break;
                case "CChinese":
                    ToChinese = 2;
                    break;
            }
        }

        private void TabItem_Selected(object sender, RoutedEventArgs e)
        {
            switch(((TabItem)sender).Uid)
            {
                case "TabItem_File":
                    FileMode = true;
                    break;
                case "TabItem_ClipBoard":
                    FileMode = false;
                    break;
            }
        }
    }
}