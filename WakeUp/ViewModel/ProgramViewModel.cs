using Newtonsoft.Json.Linq;
using Notifications;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WakeUp.Model;

namespace WakeUp.ViewModel
{
    public class ProgramViewModel : BindableBase
    {
        private const string DENIED_URL = "/deniedapp";
        private readonly NotificationManager notificationManager = new NotificationManager();
        private DispatcherTimer observerTimer = new DispatcherTimer();

        public ProgramModel SelectedProgram { get; set; }

        private ObservableCollection<ProgramModel> _deniedPrograms = new ObservableCollection<ProgramModel>();
        public ObservableCollection<ProgramModel> DeniedPrograms
        {
            get => _deniedPrograms;
            set => SetProperty(ref _deniedPrograms, value);
        }

        private List<string> defaultPrograms = new List<string>();

        public ICommand AddCommand { get; set; }
        public ICommand RemoveCommand { get; set; }

        public ProgramViewModel()
        {
            AddCommand = new DelegateCommand(OnAdd);
            RemoveCommand = new DelegateCommand(OnRemove);

            Init();
        }

        private async void OnAdd()
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Title = "Select Program To Block";
                fileDialog.Filter = "응용 프로그램|*.exe";
                fileDialog.Multiselect = true;

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var fileName in fileDialog.FileNames)
                    {
                        FileVersionInfo processInfo = FileVersionInfo.GetVersionInfo(fileName);
                        string currentProgramName = (processInfo.FileDescription == "") ? processInfo.ProductName : processInfo.FileDescription;

                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.QualityLevel = 100;
                        byte[] array;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            encoder.Frames.Add(BitmapFrame.Create(GetBitmapSource(processInfo.FileName)));
                            encoder.Save(stream);
                            array = stream.ToArray();
                            stream.Close();
                        }

                        JObject jobj = new JObject();
                        jobj["app_name"] = currentProgramName;
                        jobj["app_logo"] = array;

                        await App.networkManager.GetResponse<Nothing>(App.ServerUrl + DENIED_URL, RestSharp.Method.POST, jobj.ToString());

                        DeniedPrograms.Add(new ProgramModel()
                        {
                            Name = currentProgramName,
                            Icon = array
                        });
                    }
                }
            }
        }

        private BitmapSource GetBitmapSource(string fileName)
        {
            Icon icon = Icon.ExtractAssociatedIcon(fileName);

            BitmapSource bmpSrc = Imaging.CreateBitmapSourceFromHIcon
                (
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );

            //bmpSrc = BitmapToBitmapSource(SetTransparent(bmpSrc));

            return bmpSrc;
        }

        //private BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        //{
        //    var bitmapData = bitmap.LockBits(
        //        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
        //        ImageLockMode.ReadOnly, bitmap.PixelFormat);

        //    var bitmapSource = BitmapSource.Create(
        //        bitmapData.Width, bitmapData.Height,
        //        bitmap.HorizontalResolution, bitmap.VerticalResolution,
        //        PixelFormats.Bgr24, null,
        //        bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

        //    bitmap.UnlockBits(bitmapData);

        //    return bitmapSource;
        //}

        //private Bitmap SetTransparent(BitmapSource bmpSrc)
        //{
        //    Bitmap bitmap;

        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bmpSrc));
        //        enc.Save(outStream);
        //        bitmap = new Bitmap(outStream);
        //    }

        //    bitmap.MakeTransparent();

        //    return bitmap;
        //}

        private async void OnRemove()
        {
            if (SelectedProgram != null)
            {
                await App.networkManager.GetResponse<List<ProgramModel>>($"{App.ServerUrl}{DENIED_URL}/{SelectedProgram.Name}", RestSharp.Method.DELETE);

                DeniedPrograms.Remove(DeniedPrograms.Where(x => x.Name == SelectedProgram.Name).FirstOrDefault());
            }
        }

        private async void Init()
        {
            var resp = await App.networkManager.GetResponse<List<ProgramModel>>(App.ServerUrl + DENIED_URL, RestSharp.Method.GET);
            
            if (resp != null)
            {
                DeniedPrograms = new ObservableCollection<ProgramModel>(resp);
            }

            SetDefaultPrograms();
            SetObserverTimer();
        }

        private void SetDefaultPrograms()
        {
            //defaultPrograms.Add("Microsoft Visual Studio 2019");
            defaultPrograms.Add("WakeUp");
            defaultPrograms.Add("Windows 탐색기");
            defaultPrograms.Add("Search application");
            defaultPrograms.Add("Windows Shell Experience Host");
        }

        private void SetObserverTimer()
        {
            observerTimer.Interval = TimeSpan.FromSeconds(1);
            observerTimer.Tick += ObserverTimer_Tick;
            observerTimer.Start();
        }

        private void ObserverTimer_Tick(object sender, EventArgs e)
        {
            IntPtr hwnd = W32.GetForegroundWindow();
            int pid = W32.GetWindowProcessID(hwnd);
            Process process = Process.GetProcessById(pid);

            FileVersionInfo processInfo = GetProcessExtraInfo(process.Id);

            if (processInfo == null)
                return;

            string currentProgramName = (processInfo.FileDescription == "") ? processInfo.ProductName : processInfo.FileDescription;

            if (CheckToBlock(currentProgramName))
            {
                var content = new NotificationContent
                {
                    Title = "WakeUp",
                    Message = $"{processInfo.FileDescription}이(가) 차단되었습니다.\n수업에 집중하세요!",
                    Type = NotificationType.Warning
                };

                notificationManager.Show(content);

                try
                {
                    process.Kill();
                }
                catch { }
            }
        }

        private bool CheckToBlock(string currentProgramName)
        {
            foreach (var program in defaultPrograms)
            {
                if (currentProgramName == program)
                {
                    return false;
                }
            }

            foreach (var program in DeniedPrograms)
            {
                if (currentProgramName == program.Name)
                {
                    return false;
                }
            }

            return true;
        }

        private FileVersionInfo GetProcessExtraInfo(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            using (ManagementObjectCollection processList = searcher.Get())
            {
                ManagementObject obj = processList.OfType<ManagementObject>().FirstOrDefault();

                if (obj != null && obj["ExecutablePath"] != null)
                {
                    try
                    {
                        string filePath = obj["ExecutablePath"].ToString();

                        return FileVersionInfo.GetVersionInfo(filePath);
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}
