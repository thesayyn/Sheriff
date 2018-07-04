using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Sheriff.Common
{
    class Utility
    {

        public static string Title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static Version Something = Assembly.GetExecutingAssembly().GetName().Version;
        public static string BuildTime = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString();
        public static float VersionInt => float.Parse(Version.Replace(".",null));
        public static string Configuration = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;
        public static string Copyright = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public static string Developer = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

        public static string Platform = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture.ToString();

        public static string MD5(string data)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            bytes = md5.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte ba in bytes)
            {
                sb.Append(ba.ToString("x2").ToLower());
            }
            return sb.ToString();
        }
        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}
