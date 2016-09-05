
using System.Runtime.InteropServices;
using System.Text;

namespace Utility.�洢
{
	/// <summary>
    ///  ��дINI�ļ�
	/// </summary>
    public class HINI
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        //[DllImport("kernel32.dll")]
        //public static extern int Beep(int dwFreq, int dwDuration);
 
        public static void Write(string __�ļ���, string __��, string __��, string __ֵ)
        {
            var __����·�� = H·��.��ȡ����·��(__�ļ���);

            WritePrivateProfileString(__��, __��, __ֵ, __����·��);
        }
 
        public static string Read(string __�ļ���, string __��, string __��)
        {
            var __����·�� = H·��.��ȡ����·��(__�ļ���);
            var temp = new StringBuilder(255);
            int i = GetPrivateProfileString(__��, __��, "", temp, 255, __����·��);
            if (i == 0)
            {
                return "";
            }
            return temp.ToString();
        }
    }
}