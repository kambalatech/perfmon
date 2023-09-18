using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using static PerformanceMonitor.CONSTANTS;

namespace PerformanceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainViewModel viewModel;
        #region properties

        private static readonly Random Rnd = new Random();
        DispatcherTimer timer;

        private static string _logtext = "";
        private static string _logtextcsv = "";

        private static readonly string Logfile = @"\bandwidthmonitor_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" +
                                                 Environment.MachineName + "_" + Rnd.Next(10000, 99999) + ".csv";

        private static readonly string Logfilecsv = @"\bandwidthmonitor_" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                                                    "_" + Environment.MachineName + "_" + Rnd.Next(10000, 99999) +
                                                    ".csv";

        private static string brun = "true";
        public static string bmobile = "false";
        private static string bsort = "d";
        private static string bcsv = "true";
        private static string _logpath;

        static int defTimer = 5;
        private static int start = 0;

        private static string setPath = ReadConfigSetting("PATH");
        private static string AppName = ReadConfigSetting("APPNAME");
        private static int waitTime = ConvertStringToInt(ReadConfigSetting("TIMER"), defTimer)*1000;

        internal static int processLvl = ConvertStringToInt(ReadConfigSetting("LEVEL"), 1);
        public static string PortName = ReadConfigSetting("PORT");


        static PerformanceCounter machineCPU = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
        static PerformanceCounter machineRAM = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        static PerformanceCounter machineRAMCounter = new PerformanceCounter("Memory", "Available Bytes", true);
        static PerformanceCounter cpu = new PerformanceCounter("Process", "% Processor Time", AppName, true);
        static PerformanceCounter ram = new PerformanceCounter("Process", "Working Set - Private", AppName, true);

        static string AllText;
        static string filename = setPath +Logfilecsv;

        static string AppKey;

        #endregion

        public static string dateTime { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            
            viewModel = new MainViewModel ();
        
            this.DataContext = viewModel;
            
            NetworkPerformanceReporter.Create();
            //backgroundWorker.RunWorkerAsync();


            SetPath();
            
            
            Application.Current.MainWindow = this;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(waitTime);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            run();
        }

        private static void SetPath()
        {
            if (!string.IsNullOrEmpty(setPath))
            {
                if (Directory.Exists(setPath))
                {
                    if (bcsv == "true") { filename = Logfilecsv; }
                }
                else
                {
                    Directory.CreateDirectory(setPath);

                    if (bcsv == "true")
                    {
                        File.Create(Logfilecsv.Replace("\\", ""));
                        filename = Logfilecsv.Replace("\\", "");
                    }
                }
            }
            else
            {
                if (bcsv == "true")
                {
                    filename = Logfilecsv.Replace("\\", "");
                }

            }
            //_logpath = filename;
            NetworkChange.NetworkAddressChanged += AvailabilityChanged;
            //mobile();
            Console.CancelKeyPress += (sender, cancelArgs) =>
            {
                NetworkPerformanceReporter.StopSessions();
                cancelArgs.Cancel = true;
            };
            //arg();
        }
        private static void AvailabilityChanged(object sender, EventArgs e)
        {
            //mobile();
        }

        internal static int ConvertStringToInt(string timer, int defaultvalue)
        {
            try
            {
                if (string.IsNullOrEmpty(timer)) { return defaultvalue; }
                return Convert.ToInt32(timer);
            }
            catch { return defaultvalue;  }
        }
        private static void arg(object sender, EventArgs e)
        {
            
            run();
            //DispatcherTimer dispatcherTimer = (DispatcherTimer)sender;
            //dispatcherTimer.Stop();
            //BackgroundWorker backgroundWorker = new BackgroundWorker();
            //backgroundWorker.DoWork += (bs, be) => run();
            //backgroundWorker.RunWorkerCompleted += (bs, be) => dispatcherTimer.Start();
            //backgroundWorker.RunWorkerAsync();


        }

        private static void UpdateLogFile(object sender, EventArgs e)
        {
            //_logpath = Logfile;
            try
            {
                if (bcsv == "true" && start == 0)
                {
                    var headercsv = "\"DateTime\"," + "\"CPU\"," + "\"RAM\"," + "\"Received data (KB)\"," + "\"Transmitted data (KB)\"";
                    _logtextcsv = _logtextcsv + headercsv + Environment.NewLine;

                    start++;
                    String cpuMesg = String.Format("{0} is currently using {1}% of your CPU\n", AppName, cpu.NextValue());
                    String ramMesg = String.Format("{0} is currently using {1}MB of your RAM\n", AppName, Math.Round(ram.NextValue() / 1024 / 1024, 2));

                    File.AppendAllText(filename, "Time Diagonstics started at " + DateTime.Now + "\n\n");
                    AllText = cpuMesg + "\n" + ramMesg + "\n";
                    File.AppendAllText(filename, AllText);
                }
                double cCount = cpu.NextValue();
                cCount = Math.Round(cCount / Environment.ProcessorCount, 2);
                double mCount = Math.Round(ram.NextValue() / 1024 / 1024, 2);
                File.AppendAllText(filename, DateTime.Now.ToString("hh:mm:ss") + "," + cCount + "%" + "," + mCount + "MB" + "\n");

                int maxLength = NetworkPerformanceReporter.dicData.Count > 0 ? NetworkPerformanceReporter.dicData.Keys.Max(x => x.Length) : 0;
                foreach (var item in NetworkPerformanceReporter.dicData.OrderByDescending(key => key.Value.Received))
                {
                    var output = string.Format("${0,-30}{1,20}{2,25}", item.Key.PadRight(maxLength),
                        string.Format("{0:0.00} Mbps", item.Value.Received),
                        string.Format("{0:0.00} Mbps", item.Value.Sent), maxLength);
                    //Console.WriteLine(output);
                    //viewModel.applist.Add(output);
                    _logtext = _logtext + output + Environment.NewLine;
                    if (bcsv == "true")
                    {
                        var outputcsv = string.Empty; ;
                        if (item.Key.Contains(AppName))
                        {
                            outputcsv = "\"" + DateTime.Now.ToString("hh:mm:ss") + "\",\"" + cCount + "%" +
                                        "\",\"" + mCount + "MB" + "\",\"" +
                                        string.Format("{0:0.00}", item.Value.Received) + "\",\"" +
                                        string.Format("{0:0.00}", item.Value.Sent) + "\",\"" + "\"";


                        }
                        else
                        {
                            outputcsv = "\"" + DateTime.Now.ToString("hh:mm:ss") + "\",\"" + cCount + "%" +
                                        "\",\"" + mCount + "MB" + "\",\"" +
                                        0 + "\",\"" +
                                        0 + "\",\"" + "\"";
                        }
                        _logtextcsv = _logtextcsv + outputcsv + Environment.NewLine;
                    }
                }
                File.AppendAllText(filename, _logtextcsv);
            }
            catch (Exception ex)
            {
                
            }      
        }

       
        private static int help()
        {
           List<PerformanceCounter> counters = new List<PerformanceCounter>();

            foreach (Process process in Process.GetProcesses(AppName))
            {
                PerformanceCounter processorTimeCounter = new PerformanceCounter( "Process",  "% Processor Time",process.ProcessName,  AppName);

            processorTimeCounter.NextValue();
            counters.Add(processorTimeCounter);
            }
            return 1;
        }

        private static int PerformanceCounters()
        {
           List<PerformanceCounter> counters = new List<PerformanceCounter>();
            double dValue = 0;
            foreach (Process process in Process.GetProcessesByName(AppName))
            {
                PerformanceCounter processorTimeCounter = new PerformanceCounter( "Process",  "% Processor Time",AppName);

                dValue += processorTimeCounter.NextValue();
                counters.Add(processorTimeCounter);
            }
            MessageBox.Show(dValue.ToString());
            return 1;
        }

        private static void mobile()
        {
            if (IsMobileConnectionActive())
                NetworkPerformanceReporter.mobile = true;
            else
                NetworkPerformanceReporter.mobile = false;
        }

        private static bool IsMobileConnectionActive()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return false;

            var broadbandTypes = new[]
            {
                NetworkInterfaceType.Ppp,
                NetworkInterfaceType.Wwanpp,
                NetworkInterfaceType.Wwanpp2,
                NetworkInterfaceType.Wman
            };

            var mobileInterfaces = from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                where broadbandTypes.Contains(nic.NetworkInterfaceType)
                select nic;
            return mobileInterfaces.Any();
        }

        private static void run()
        {
            NetworkPerformanceReporter.Create();

            _logtext = "";
            _logtextcsv = "";
            
             _logpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Logfile;
            if (!string.IsNullOrEmpty(setPath))
            {
                if (Directory.Exists(setPath))
                {
                    _logpath = setPath + Logfilecsv;
                }
            }
            if (!string.IsNullOrEmpty(MainWindow.PortName))
            {
                AppKey = AppName + "-" + PortName;
            }
            else
            {
                AppKey = (MainWindow.processLvl == CONSTANTS.PORT_WISE_LOG) ? (AppName + "-" + PortName) : AppName;
            }

            try
            {
                if (bcsv == "true" && start == 0)
                {
                    var headercsv = "\"DateTime\"," + "\"CPU\"," + "\"RAM\"," + "\"Received data (MB)\"," + "\"Transmitted data (MB)\"," + "\"Machine CPU %\"," + "\"Machine RAM %\"," + "\"Available RAM (in MB)\"";

                    if (processLvl == CONSTANTS.PORT_WISE_LOG)
                    {
                        headercsv+= ","+  "\"Port\"";
                    }
                    _logtextcsv = _logtextcsv + headercsv + Environment.NewLine;
                    //Console.Title = "clawSoft bandwithmonitor v1.0 | Logfile: " + _logpath;

                    start++;
                    String cpuMesg = String.Format("{0} is currently using {1}% of your CPU\n", AppName, cpu.NextValue());
                    String ramMesg = String.Format("{0} is currently using {1}MB of your RAM\n", AppName, Math.Round(ram.NextValue() / 1024 / 1024, 2));

                    File.AppendAllText(_logpath, "Time Diagonstics started at " + DateTime.Now + "\n\n");
                    //AllText =  _logtextcsv;
                    File.AppendAllText(_logpath, _logtextcsv);

                    viewModel.cpuUsuage = cpu.NextValue();
                    viewModel.ramUsuage = Math.Round(ram.NextValue() / 1024 / 1024, 2);
                    _logtextcsv = string.Empty;
                }
            
                //Console.Clear();
                _logtextcsv = string.Empty;
                //Console.Title = "bandwithmonitor v1.0 | Logfile: " + _logpath;


                var header = string.Format("{0,-30}{1,20}{2,25}", "Processes", "Received data (MB)",
                    "Transmitted data (MB)");
                //_logtext = _logtext + header + Environment.NewLine;
                    



                double cCount = cpu.NextValue();
                double mCount = Math.Round(ram.NextValue() / 1024 / 1024, 2);
                double machineCPUCount = machineCPU.NextValue();

                //double Value = (cCount/(Environment.ProcessorCount * machineCPUCount))*100;
                
                //cCount = Math.Round(cCount / Environment.ProcessorCount, 2);
                //MessageBox.Show(Value.ToString());
                
                machineCPUCount = Math.Round(machineCPUCount, 2);
                double machineRAMCount = Math.Round(machineRAM.NextValue(), 2);

                double machineRAMMB = Math.Round(machineRAMCounter.NextValue()/ 1024 / 1024 , 2);
			    viewModel.IsCPUIncreased = cCount > viewModel.cpuUsuage ? true : false;
                viewModel.IsRAMIncreased = mCount > viewModel.ramUsuage ? true : false;
                viewModel.cpuUsuage = cCount;
                viewModel.ramUsuage = mCount;

                int maxLength = NetworkPerformanceReporter.dicData.Count > 0 ? NetworkPerformanceReporter.dicData.Keys.Max(x => x.Length) : 0;

                double Recvd = 0;
                double Sent = 0;

                foreach (var item in NetworkPerformanceReporter.dicData.OrderBy(key =>
                    key.Value.Received))
                {
                    var output = string.Format("{0,-30}{1,20}{2,25}", item.Key,
                        string.Format("{0:0.00}", item.Value.Received),
                        string.Format("{0:0.00}", item.Value.Sent));
                    //Console.WriteLine(output);
                    _logtext = _logtext + output + Environment.NewLine;
                    if (bcsv == "true" && item.Key.Contains(AppKey))
                    {
                        var outputcsv = "\"" + DateTime.Now.ToString("hh:mm:ss") + "\",\"" + cCount + "%" +
                                        "\",\"" + mCount + "MB" + "\",\"" + string.Format("{0:0.00}", item.Value.Received) + "\",\"" +
                                        string.Format("{0:0.00}", item.Value.Sent) + "\",\""+ 
                                        machineCPUCount + "\",\"" +
                                        machineRAMCount + "\",\"" +
                                        machineRAMMB + "\",\""; 
                        if (processLvl == PORT_WISE_LOG)
                        {
                            outputcsv += string.Format("{0:0.00}", item.Value.Port);
                        }
						
                        Recvd += item.Value.Received;
				        Sent += item.Value.Sent;
                        outputcsv += "\"";
                        _logtextcsv = _logtextcsv + outputcsv + Environment.NewLine;
                    }

                }

                viewModel.UploadBandwidthIncreased =  Recvd> viewModel.uploadeddbandwidth ? true : false;
			    viewModel.RcvdBandwidthIncreased = Sent > viewModel.rcvdbandwidth ? true : false;
			    viewModel.rcvdbandwidth = Recvd;
			    viewModel.uploadeddbandwidth = Sent;
                if (bcsv == "true")
                {
                    if (!string.IsNullOrEmpty(_logtextcsv))
                        File.AppendAllText(_logpath, _logtextcsv);
                }
                else
                    File.AppendAllText(_logpath, _logtext);
            }
            catch (Exception e) { MessageBox.Show(e.ToString()); }
            
            

              
        }

        private static void arg(string[] args)
        {
            if (args.Length == 0)
            {
                if (string.IsNullOrEmpty(setPath))
                {
                    _logpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Logfile;
                }
                else
                {
                    _logpath = setPath + Logfilecsv;
                }
                run();
            }
            else
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var argument = args[i];

                    
                    if (string.IsNullOrEmpty(setPath))
                    {
                        if (bcsv == "true")
                            _logpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Logfilecsv;
                        else
                            _logpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Logfile;

                    }
                    else
                    {
                        _logpath = Path.Combine(setPath, Logfilecsv);
                    }

                }

                if (brun == "true") run();
            }
        }
        public static string ReadConfigSetting(string strKey) { return ConfigurationManager.AppSettings[strKey]?.ToString(); }

    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private string _dateTime;

        public string dateTime
        {
            get { return _dateTime; }
            set
            {
                _dateTime = value;
                OnPropertyChanged("dateTime");
            }
        }

        private double _cpuUsuage;

        public double cpuUsuage
        {
            get { return _cpuUsuage; }

            set
            {
                _cpuUsuage = value;
                OnPropertyChanged("cpuUsuage");
                
            }
        }

        private double _ramUsuage;

        public double ramUsuage
        {
            get { return _ramUsuage; }

            set
            {
                _ramUsuage = value;
                OnPropertyChanged("ramUsuage");
            }
        }

        private double _rcvdbandwidth;

        public double rcvdbandwidth
        {
            get { return _rcvdbandwidth; }

            set
            {
                 _rcvdbandwidth = value;
                OnPropertyChanged("rcvdbandwidth");
            }
        }

        private double _uploadeddbandwidth;

        public double uploadeddbandwidth
        {
            get { return _uploadeddbandwidth; }

            set
            {
                _uploadeddbandwidth = value;
                OnPropertyChanged("uploadeddbandwidth");
            }
        }

        private bool _isCPUIncreased;

        public bool IsCPUIncreased
        {
            get { return _isCPUIncreased; }

            set
            {
                _isCPUIncreased = value;
                OnPropertyChanged("IsCPUIncreased");
            }
        }

        private bool _isRAMIncreased;

        public bool IsRAMIncreased
        {
            get { return _isRAMIncreased; }

            set
            {
                _isRAMIncreased = value;
                OnPropertyChanged("IsRAMIncreased");
            }
        }

        private bool _isuploadBandwidthIncreased;

        public bool UploadBandwidthIncreased
        {
            get { return _isuploadBandwidthIncreased; }

            set
            {
                _isuploadBandwidthIncreased = value;
                OnPropertyChanged("UploadBandwidthIncreased");
            }
        }

        private bool _isrcvdBandwidthIncreased;

        public bool RcvdBandwidthIncreased
        {
            get { return _isrcvdBandwidthIncreased; }

            set
            {
                _isrcvdBandwidthIncreased = value;
                OnPropertyChanged("RcvdBandwidthIncreased");
            }
        }

        public ObservableCollection<string> applist { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var boolValue = (bool)value;
                if (boolValue) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (((Visibility)value).Equals(Visibility.Collapsed)) return false;
                else return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
