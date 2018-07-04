using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Sheriff.Common
{
    class Server
    {

        public byte ID { get; private set; }
        public string Description { get; private set; }
        public string Class { get; private set; }
        public string Type { get; private set; }
        public Bind[] Bind { get; private set; }
        public DivisionCollection Divisions { get; private set; }
        public Server Reditected { get; private set; }


        public Server(XmlNode node)
        {
            this.Bind = new Bind[2];
            this.ID = byte.Parse(node.Attributes["ID"].Value);
            this.Description = node.Attributes["Description"].Value;
            this.Class = node.Attributes["Class"].Value;
            int i = 0;
            foreach (XmlNode x in node)
            {
                if (x.Name != "Bind") continue;
                this.Bind[i] = new Bind(x);
                i++;
            }
               

            try
            {
                if (node["Divisions"] != null)
                {
                    this.Divisions = new DivisionCollection(node["Divisions"]);
                }
            }
            catch { }


            try
            {
                if (node["DownloadServer"] != null && node["DownloadServer"]["Reditect"] != null)
                {
                    this.Reditected = Program.config.FindServer(node["DownloadServer"]["Reditect"].Attributes["ID"].Value);
                }
            }
            catch { }
        }


    }

}
