using System.Collections.Generic;

namespace Sheriff.Common.Plugins
{
    public class Request
    {
        public string DataSource { get; set; }
        public Dictionary<string,List<string>> Parameters { get; set; }

        public string GetParameter(string name)
        {
            if (this.Parameters.ContainsKey(name))
            {
                return this.Parameters[name][0];
            }

            return null;
        }

        public List<string> GetParameters(string name)
        {
            if (this.Parameters.ContainsKey(name))
            {
                return this.Parameters[name];
            }

            return null;
        }

        public Request(string ds,Dictionary<string,List<string>> prm)
        {
            this.DataSource = ds;
            this.Parameters = prm;
        }
    }
}
