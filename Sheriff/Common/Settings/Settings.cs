using System;
using System.Collections.Generic;
using System.Xml;

namespace Sheriff.Common
{
    class Settings
    {

        public List<Server> Servers { get; set; }
        public Security Security { get; set; }
        public String Key { get; set; }

        private XmlDocument _xml = new XmlDocument();


        public void Load(String filename)
        {
            _xml.Load(filename);
            this.Key = _xml["Settings"].Attributes["Key"].Value;
             
            this.Servers = new List<Server>();
            this.Security = new Security(_xml["Settings"]["Security"]);

            foreach (XmlNode node in _xml["Settings"]["Servers"])
            {
                this.Servers.Add(new Server(node));
	        }

        }


        public Server FindServer(string id)
        {
            foreach (XmlNode node in _xml["Settings"]["Servers"])
            {
                if (node.Attributes["ID"].Value == id)
                {
                    return new Server(node);
                }
              
            }
            return null;
        }
      
    }
}
