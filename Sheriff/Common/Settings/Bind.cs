using System.Net;
using System.Xml;

namespace Sheriff.Common
{
    public class Bind
    {
        public IPAddress IP{ get;private set; }
        public ushort Port{ get;private set; }

        public Bind(XmlNode node)
        {
            this.IP = null;
            this.IP = IPAddress.Parse(node.Attributes["IP"].Value);
            this.Port = ushort.Parse(node.Attributes["Port"].Value);
        }

        public IPEndPoint ToIpEndPoint(){
            return new IPEndPoint(this.IP, this.Port);
        }


    }
}
