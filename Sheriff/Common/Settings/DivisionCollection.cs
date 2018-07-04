using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Sheriff.Common
{
    class Division
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
        public ushort ShardID { get; set; }
        public Server Agent { get; set; }
        public Division(XmlNode node)
        {
            this.ShardID = ushort.Parse(node.Attributes["ShardID"].Value);
            this.Capacity = int.Parse(node.Attributes["Capacity"].Value);
            this.Agent = Program.config.FindServer(node["Reditect"].Attributes["ID"].Value);
            this.Name = node.Attributes["Name"].Value;
        }
    }
     class DivisionCollection : List<Division>
    {
        public string FarmID { get; set; }
        public string FarmName { get; set; }

        public DivisionCollection(XmlNode node)
        {
            this.FarmID = node.Attributes["FarmID"].Value;
            this.FarmName = node.Attributes["FarmName"].Value;
            if (node.HasChildNodes)
            {
                foreach (XmlNode item in node)
                {
                    this.Add(new Division(item));
                }
                
            }
        }

        public Division GetByShardID(uint shardid)
        {
            foreach (Division item in this)
            {
                if (item.ShardID == shardid)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
