using Sheriff.Models;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.NetworkInformation;

namespace Sheriff.Common.Performance
{
    class NetworkManager
    {
        public static ExpandoObject GetNetworkUsages(NetworkInterface @interface) {
            dynamic @object = new ExpandoObject();
            @object.Name = @interface.Name;
            @object.BytesSent = @interface.GetIPv4Statistics().BytesSent;
            @object.BytesReceived = @interface.GetIPv4Statistics().BytesReceived;
            return @object;
        }

        public static NetworkInterface[] GetNetworkIntefaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces();
        }



    }
}
