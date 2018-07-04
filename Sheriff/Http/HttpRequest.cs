using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheriff.Http
{
    class HttpRequest
    {
        public string Method { get; set; }
        public string File { get; set; }
        public string Version { get; set; }
        public string[] Path { get; set; }
        public string Url { get; set; }
        public byte[] Content { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, List<string>> Parameters { get; set; }

        public HttpRequest(StreamReader reader)
        {
            this.Headers = new Dictionary<string, string>();
            this.Parameters = new Dictionary<string, List<string>>();

            string request = reader.ReadLine();
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3) return;

            this.Method = tokens[0].ToUpper();

            string url = tokens[1];
            this.Url = url;

            if (tokens[1].Contains('?'))
            {
                url= tokens[1].Split('?')[0];
                string[] p = tokens[1].Split('?')[1].Split('&');
                foreach (string item in p)
                {
                    string key = item.Split('=')[0].Replace("[]", "");
                    string value = Uri.UnescapeDataString(item.Split('=')[1]);
                    if (!this.Parameters.ContainsKey(key)) this.Parameters[key] = new List<string>();
                    this.Parameters[key].Add(value);
                }

            }
          
            this.Version = tokens[2];

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Equals(""))
                {
                    break;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                string name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++;
                }

                string value = line.Substring(pos, line.Length - pos);
                this.Headers.Add(name.ToLower(), value);
            }

            if (this.Headers.ContainsKey("content-length"))
            {
                int totalBytes = Convert.ToInt32(this.Headers["content-length"]);
                int bytesLeft = totalBytes;
                byte[] bytes = new byte[totalBytes];

                while (bytesLeft > 0)
                {
                    byte[] buffer = new byte[bytesLeft > 1024 ? 1024 : bytesLeft];
                    int n = reader.Read(Encoding.UTF8.GetChars(buffer), 0, buffer.Length);
                    buffer.CopyTo(bytes, totalBytes - bytesLeft);

                    bytesLeft -= n;
                }

                this.Content = bytes;
            }

            this.Url = this.Headers["host"]+this.Url;
           
            url = url.Replace('\\', '/');
            url = url.Replace("\\\\", "/");
            url = url.Replace("//", "/");
            if (url.StartsWith("/"))
            {
               url = url.Remove(0, 1);
            }
            string[] urls = url.Split('/');

            if (url == "/")
            {
                this.Path[0] = "/"; this.File = null;
            }

            else
            {
                this.Path = urls;

                if (urls[urls.Length - 1].Contains('.'))
                {
                    this.File = urls[urls.Length - 1];
                    urls[urls.Length - 1] = null;
                    this.Path = urls;
                }
            }



      
        }

        public string GetParam(string key)
        {
            try { return this.Parameters[key][0]; }
            catch { return null; }
        }

        public List<string> GetParams(string key)
        {
            try
            {
                return this.Parameters[key];
            }
            catch { return null; }
        }


 
        public string GetPath(int key)
        {
            try { return this.Path[key]; }
            catch { return null; }
        }

    }
}
