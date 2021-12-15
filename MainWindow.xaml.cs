using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NationalInstruments.DAQmx;
using System.Windows.Threading;
using System.Threading;
using iUtility;



//正转：	    01 06 00 00 00 01 48 0A
//反转：	    01 06 00 01 00 01 19 CA
//停止：	    01 06 00 02 00 01 E9 CA
//设置速度：	01 06 00 05 00 FF D9 8B
//设置脉冲数：	01 06 00 07 00 FF 78 4B
//设置计数：	01 06 00 04 00 FF 88 4B
//设置设备号：	01 06 00 06 00 02 E8 0A
//查询速度：	01 03 00 05 00 01 94 0B
//查询脉冲数：	01 03 00 07 00 01 35 CB
//查询计数：	01 03 00 04 00 01 C5 CB
//查询设备号：	设备上电自动发送3次当前设备号


namespace SW
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {

        DispatcherTimer timer;
        string outputCH = "Dev1/line8";
        string inputCH = "Dev2/line21";
        Task readTask = null;
        DigitalSingleChannelReader reader = null;
        SerialHelper serialPort = null;
        static bool bStop = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        private string _StepNumber;

        public string StepNumber
        {
            get { return _StepNumber; }
            set {
                _StepNumber = value;
                if(PropertyChanged!=null)
                {
                    OnPropertyChanged("StepNumber");
                };
            }
        }



        private byte[] BuildCmd(string strHead, string strFunc, string nData)
        {
            byte[] result;
            int num = Convert.ToInt32(nData);
            string strData = Convert.ToString(num, 16).PadLeft(4, '0');
            string buffer = string.Format("{0}{1}{2}", strHead, strFunc, strData);
            string checkSum = CRC.ToModbusCRC16(buffer);
            string strCmd = buffer + checkSum;
            result = CRC.StringToHexByte(strCmd);
            return result;
        }

        private void SendByteData(byte[] bCmd)
        {
            byte[] revData = new byte[8];
            serialPort = new SerialHelper(ComportCbBox.Text);
            try
            {
                serialPort.openPort();
                serialPort.sendData(bCmd, 0, bCmd.Length);
                //serialPort.sendCommand(bCmd, ref revData, 10);
                serialPort.closePort();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                serialPort = null;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<StepNumberInfo> stepNumbers = new List<StepNumberInfo>();
            for (int i = 2; i < 641; i = i + 2)
            {
                stepNumbers.Add(new StepNumberInfo { Name = i.ToString(), Value = i.ToString() });
            }
            StepNumberCbBox.ItemsSource = stepNumbers;
            StepNumberCbBox.SelectedIndex = 31;
            StepNumber = "001";


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += new EventHandler(timer1_Tick);

            UpBtn.IsEnabled = false;
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ForwardBtn.IsEnabled = false;
            ReverseBtn.IsEnabled = false;
            DelaySlider.IsEnabled = false;
            AutoStopChkBox.IsEnabled = false;
            
            AutoStopChkBox.IsChecked = true;

        }

        private void SetStepNumberBtn_Click(object sender, RoutedEventArgs e)
        {
            int num = int.Parse(StepNumberCbBox.Text);
            AngleTb.Text = string.Format("{0:F2}" ,(double)360 / 640 * num);
            //RunStepNumberTxt.Text = string.Format("{0:F0}", (double) 640 / num + 1).PadLeft(3,'0');
            arc.EndAngle = (double)360 / 640 * num;

            byte[] bStepCmd = BuildCmd("0106", "0007", StepNumberCbBox.Text);
            SendByteData(bStepCmd);

            System.Threading.Thread.Sleep(100);
            byte[] bSpeedCmd = BuildCmd("0106", "0005", SpeedSlider.Value.ToString());
            SendByteData(bSpeedCmd);
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            bStop = false;
            ForwardBtn.IsEnabled = false;
            ReverseBtn.IsEnabled = false;
            timer.Stop();
            Thread.Sleep(200);
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;

            int delayMs = Convert.ToInt32(DelaySlider.Value);
            byte[] bCmd = BuildCmd("0106", "0000", "0001");

            for (int i = 1; i <= 666; i++)
            {

                if (AutoStopChkBox.IsChecked ?? true)
                {
                    if (CheckSwDownStatus())
                    {
                        ReadyInfoBd.Background = Brushes.Green;
                        break;
                    }
                    else
                    {
                        ReadyInfoBd.Background = Brushes.Red;
                    }

                }

                Action p = () =>
                {
                    Thread.Sleep(150);
                    StepNumber = i.ToString().PadLeft(3, '0');
                };
                await System.Threading.Tasks.Task.Run(p);
                if (bStop) break;
                SendByteData(bCmd);
               
                //RunStepNumberTxt.Text = i.ToString().PadLeft(3, '0');
                //App.DoEvents();
                System.Threading.Thread.Sleep(delayMs);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //定时执行的内容
            if (CheckSwDownStatus())
            {
                ReadyInfoBd.Background = Brushes.Green;
            }
            else
            {
                ReadyInfoBd.Background = Brushes.Red;
            }
        }

        private bool CheckSwDownStatus()
        {
            bool result = false;
            readTask = new Task();
            readTask.DIChannels.CreateChannel(inputCH, "", ChannelLineGrouping.OneChannelForEachLine);
            reader = new DigitalSingleChannelReader(readTask.Stream);
            result = !reader.ReadSingleSampleSingleLine();
            readTask = null;
            reader = null;
            return result;
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            bStop = true;
            RunStepNumberTxt.Text = "001";
            ForwardBtn.IsEnabled = true;
            ReverseBtn.IsEnabled = true;
            timer.Start();
            ReadyInfoBd.Background = Brushes.LightGray;
            StopBtn.IsEnabled = false;
            StartBtn.IsEnabled = true;
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            int num = int.Parse(RunStepNumberTxt.Text) + 1;
            int delayMs = Convert.ToInt32(DelaySlider.Value);

            byte[] bCmd = BuildCmd("0106", "0000", "0001");
            SendByteData(bCmd);
            StepNumber = num.ToString().PadLeft(3, '0');

            //RunStepNumberTxt.Text = num.ToString().PadLeft(3, '0');
            System.Threading.Thread.Sleep(delayMs);            

            Mouse.OverrideCursor = null;
        }

        private void ReverseBtn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            int num = int.Parse(RunStepNumberTxt.Text);
            if (num > 0) num = num - 1;
            int delayMs = Convert.ToInt32(DelaySlider.Value);

            byte[] bCmd = BuildCmd("0106", "0001", "0001");
            SendByteData(bCmd);
            StepNumber = num.ToString().PadLeft(3, '0');

            //RunStepNumberTxt.Text = num.ToString().PadLeft(3, '0');
            System.Threading.Thread.Sleep(delayMs);
        

            Mouse.OverrideCursor = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopBtn_Click(sender, new RoutedEventArgs());
            string defaultCmd = "01 06 00 07 00 40 39 FB";
            byte[] bCmd = CRC.StringToHexByte(defaultCmd);
            SendByteData(bCmd);
            SwitchOff(true);
        }

        private void DownBtn_Click(object sender, RoutedEventArgs e)
        {
            SwitchOff(false);
            timer.Start();
            RunStepNumberTxt.Text = "001";

            ForwardBtn.IsEnabled = true;
            ReverseBtn.IsEnabled = true;
            StartBtn.IsEnabled = true;
            AutoStopChkBox.IsEnabled = true;
            DelaySlider.IsEnabled = true;

            DownBtn.IsEnabled = false;
            UpBtn.IsEnabled = true;
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {
            SwitchOff(true);
            timer.Stop();
            ReadyInfoBd.Background = Brushes.LightGray;

            ForwardBtn.IsEnabled = false;
            ReverseBtn.IsEnabled = false;
            StartBtn.IsEnabled = false;
            AutoStopChkBox.IsEnabled = false;
            DelaySlider.IsEnabled = false;

            DownBtn.IsEnabled = true;
            UpBtn.IsEnabled = false;
        }

        private void SwitchOff(bool bData)
        {
            try
            {
                Task writeTask = new Task();
                writeTask.DOChannels.CreateChannel(outputCH, "", ChannelLineGrouping.OneChannelForEachLine);
                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(writeTask.Stream);
                writer.WriteSingleSampleSingleLine(true, bData);
                writer = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }

    public class StepNumberInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
