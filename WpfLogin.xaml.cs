using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Data;
using System.Data.OleDb;
using System.Windows.Documents;
using System.Windows.Input;

namespace iUtility
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        bool isLogin = false;
        bool bLocal = false;
        string dbDir = "";
        string dbLocDir = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dbName = "uData.accdb";
        string dbPwd = "c96b05644b"; //de765167
        string dbRemoteDir = @"\\10.21.100.48\TestData\tde\Data";
        string strFolder = @"\\10.21.100.48\TestData";
        const string strIp = "10.21.100.48";
        const string strUser = "teuser";
        const string strPassword = "teuser123";
        const int maxCount =3;
        static int index = 1;

        public Login()
        {
            InitializeComponent();
            this.txt_password.Focus();
//#if DEBUG
//            bLocal = true;
//#else
            if (SharedTools.PingHost(strIp))
                bLocal = false;
            else
                bLocal = true;
//#endif
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
#if (DEBUG)
            isLogin = false;
#endif
            //string buf = Security.Enc("test1");
            string userName = txt_userName.Text;
            string password = Security.Enc(txt_password.Password);

            if (bLocal)
            {
                dbDir = dbLocDir;
            }
            else
            {
                if (password == string.Empty)
                {
                    txt_password.Focus();
                    return;
                }
                string dbRemoteFileName = Path.Combine(dbRemoteDir + "\\" + dbName);
                string _msg = SharedTools.ConnectToShare(strFolder, strUser, strPassword);
                if ( _msg != null)
                {
                    MessageBox.Show(_msg);
                    dbDir = dbLocDir;
                }
                else if (System.IO.File.Exists(dbRemoteFileName))
                {
                    string fileName = Path.Combine(dbLocDir, dbName);
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            File.Delete(fileName);
                            File.Copy(dbRemoteFileName, fileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                    }
                    else
                    {
                        File.Copy(dbRemoteFileName, fileName);
                    }
                    dbDir = dbRemoteDir;
                }                
            }
            DbHelperAccess db = new DbHelperAccess(dbDir + "\\" + dbName, dbPwd);
            string sql = string.Format("SELECT [Id] FROM [user] WHERE [Name] = '{0}' and [Password] = '{1}'", userName, password);

            if (!isLogin)
            {
                if (!db.Exists(sql))
                {
                    if (index >= maxCount)
                    {
                        this.DialogResult = false;
                    }
                    MessageBox.Show("用户名或密码错误","提示", MessageBoxButton.OK, MessageBoxImage.Stop);
                    txt_password.Password = "";
                    txt_password.Focus();
                    index++;
                }
                else
                {
                    isLogin = true;
                }
                
            }
            if (isLogin)
            {
                this.DialogResult = true;
            }
            
        }
    }
}
