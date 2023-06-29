using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public abstract class AccessIni
    {
        public static string iniPath = AppDomain.CurrentDomain.BaseDirectory + "\\Config.ini";

        public static List<string> getKeys(string section)
        {
            byte[] buffer = new byte[1024];

            GetPrivateProfileSection(section, buffer, 1024, iniPath);

            string str = Encoding.ASCII.GetString(buffer).Trim('\0');

            if (str.Length == 0) return null;

            String[] tmp = str.Split('\0');

            List<string> result = new List<string>(tmp);

            //foreach (String entry in tmp)
            //{
            //    result.Add(entry);
            //}

            return result;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileSection")]
        public static extern uint GetPrivateProfileSection(
            string lpAppName,
            byte[] lpszReturnBuffer,
            uint nSize,
            string lpFileName
            );

        // Win32APIのGetPrivateProfileString
        // ini文字列読み込み
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        extern public static uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName
            );

        // Win32APIのGetPrivateProfileInt
        // ini数値読み込み
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileInt")]
        extern public static uint GetPrivateProfileInt(
            string lpAppName,
            string lpKeyName,
            int nDefault,
            string lpFileName
            );

        // Win32APIのWritePrivateProfileString
        // ini書き込み
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        extern public static bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName
            );
    }
}
