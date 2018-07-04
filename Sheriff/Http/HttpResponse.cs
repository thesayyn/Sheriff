using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sheriff.Http
{
    public enum StatusCode : int
    {

        Continue = 100,
        Ok = 200,
        Created = 201,
        Accepted = 202,
        MovedPermanently = 301,
        Found = 302,
        NotModified = 304,
        BadRequest = 400,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        InternalServerError = 500,
        AuthenticationFail = 401,
        Gone = 410
    }

    class HttpResponse
    {
        public StatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public byte[] Content { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string ContentAsUTF8
        {
            set
            {
                this.setContent(value, encoding: Encoding.UTF8);
            }
        }
        public void setContent(string content, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            Content = encoding.GetBytes(content);
        }
        public void setJSONContent(object content, Encoding encoding = null)
        {
            this.Headers["Content-Type"] = "application/json; charset=UTF-8";
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            Content = encoding.GetBytes(JsonConvert.SerializeObject(content));
            Write();
        }
        public void Route(string url)
        {
            this.StatusCode = Http.StatusCode.MovedPermanently;
            this.Headers["Location"] = url;
            Write();
        }

        private Socket client;
        public HttpResponse(Socket client)
        {
            this.Headers = new Dictionary<string, string>();
            this.client = client;
        }

        public void Write()
        {
            MemoryStream writer = new MemoryStream();
            if (this.Content == null)
            {
                this.Content = new byte[] { };
            }


            if (!this.Headers.ContainsKey("Content-Type"))
            {
                this.Headers["Content-Type"] = "text/html";
            }

            this.Headers["Content-Length"] = this.Content.Length.ToString();

            byte[] firstline = Encoding.UTF8.GetBytes(string.Format("HTTP/1.0 {0} {1}\r\n", (int)this.StatusCode, this.StatusCode.ToString()));
            writer.Write(firstline,0,firstline.Length);
    
            
            byte[] secondline = Encoding.UTF8.GetBytes(string.Join("\r\n", this.Headers.Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
            writer.Write(secondline,0,secondline.Length);

                byte[] serperator = Encoding.UTF8.GetBytes("\r\n\r\n");
                writer.Write(serperator, 0, serperator.Length);

                writer.Write(this.Content, 0, this.Content.Length);
            
        

            client.Send(writer.GetBuffer(), SocketFlags.None);
            client.Close();

        }
        public override string ToString()
        {
            return string.Format("HTTP status {0} {1}", this.StatusCode, this.StatusCode.ToString());
        }
    }
}
