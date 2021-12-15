using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using iUtility;
using System.Windows.Threading;

namespace SW
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        //private static DispatcherOperationCallback exitFrameCallback = new DispatcherOperationCallback(ExitFrame);
        //public static void DoEvents()
        //{
        //    DispatcherFrame nestedFrame = new DispatcherFrame();
        //    DispatcherOperation exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, exitFrameCallback, nestedFrame);
        //    Dispatcher.PushFrame(nestedFrame);
        //    if (exitOperation.Status !=
        //    DispatcherOperationStatus.Completed)
        //    {
        //        exitOperation.Abort();
        //    }
        //}

        //private static Object ExitFrame(Object state)
        //{
        //    DispatcherFrame frame = state as
        //    DispatcherFrame;
        //    frame.Continue = false;
        //    return null;
        //}

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool? result = false;
            MainWindow main = new MainWindow();
#if !DEBUG
            Login login = new Login();
            result = login.ShowDialog();
#else
            result = true;
#endif
            if (result ?? false)
            {
                main.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                main.Show(); // 显示主窗口;
            }
            else
            {
                Current.Shutdown();
                //Environment.Exit(0);
            }
        }
    }
}
