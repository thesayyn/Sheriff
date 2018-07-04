using System.Collections.Generic;
using System.Xml;

namespace Sheriff.Common
{
    public class Security
    {
        public List<Access> Access { get; set; }

        public Security(XmlNode node)
        {
            this.Access = new List<Access>();

            foreach(XmlNode item in node)
            {
                if (item.Name != nameof(Access)) continue;

                this.Access.Add(new Access(item));
            }

    
        }

    }
}
