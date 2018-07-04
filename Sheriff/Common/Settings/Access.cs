using System;
using System.Collections.Generic;
using System.Xml;

namespace Sheriff.Common
{
    public class Access
    {
        public List<Token> Tokens { get; set; }
        public string ServerID { get; private set; }

        public class Token
        {
            public String Value { get; set; }
            public float ExpiresIn { get; set; }

            public Token(XmlNode node)
            {
                this.ExpiresIn = float.Parse(node.Attributes[nameof(ExpiresIn)].Value);
                this.Value = node.InnerText;
            }
        }

        public Access(XmlNode node)
        {
            this.Tokens = new List<Token>();

            foreach (XmlNode item in node)
            {
                if (item.Name != nameof(Token)) continue;

                this.Tokens.Add(new Token(item));
            }

            this.ServerID = node.Attributes[nameof(ServerID)].Value;
        }
    }
}
