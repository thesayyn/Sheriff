using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sheriff.Networking;
using Sheriff.Common;

namespace Sheriff.Models
{
    public abstract class IServiceBase
    {
       public abstract byte ID { get; set; }
       public abstract string Description { get; set; }
       public abstract string Class { get; set; }
       public abstract bool Status { get; set; }
       public abstract List<IClient> Clients { get; set; }
       public abstract Bind[] Bind { get; set; }
       public virtual void Stop()
       {
            this.Status = false;
       }
       public virtual void Start()
        {
            this.Status = true;
        }
    }
}
