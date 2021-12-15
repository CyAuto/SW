using System;
using System.IO;
using System.Security.Cryptography;

namespace iUtility
{
    /// <summary>
    /// Security 的摘要说明。
    /// </summary>
    public class Security
    {
        const string KEY_64 = "VavicApp";//VavicApp
        const string IV_64 = "VavicApp"; //注意了，是8个字符，64位
        static readonly byte[] XorKey =new byte[] {0xAA, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xBB};//KEY字符串异或加密用  

        public Security()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        /// <summary>
        /// 字符异或加密函數 
        /// </summary>
		public static string Enc(string data) 
		{
			int j=0;
			string result=string.Empty;
			
			for (int i = 0; i < data.Length; i++) 
			{
				int charVal= Convert.ToByte(data[i]) ^ XorKey[j];//取单个字符并与KEY异或处理得到十六进制字符
				result = result +  Convert.ToString(charVal,16).PadLeft(2,'0');//十进制数值转为2位的十六进制字符串
				j = (j+1) % 8; //%取余数
			}
			return result;
		}        
		
        /// <summary>
        /// 字符异或解密函數  
        /// </summary>
		public static string Dec(string data)
		{
			int j=0;
			string result=string.Empty;
			
			for (int i = 0; i < data.Length/2; i++)
			{
				string subStr= data.Substring(i*2,2); //得到2位的十六进制字符串		
				int charVal= Convert.ToInt32(subStr,16) ^ XorKey[j]; //与KEY进行异或处理
				result = result +  Convert.ToChar(charVal).ToString();
				j = (j+1) % 8; //%取余数
			}
			return result;
		}		

        public static string Encode(string data)
        {
            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(KEY_64);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(IV_64);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);

        }

        public static string Decode(string data)
        {
            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(KEY_64);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(IV_64);

            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            return sr.ReadToEnd();
        }
    }
}
