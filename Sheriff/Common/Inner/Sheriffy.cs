using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace Sheriff.Common.Inner
{


    public static class Sheriffy
    {
        private static string Base = "http://api.yigitcinoglu.com.tr";

        public static bool Check(string key,out dynamic result)
        {
            result = null;

            string content = null;

            try
            {

                WebClient client = new WebClient();
                content = client.DownloadString(Base + "/subscription/" + key);
            }
            catch (WebException ex)
            {
                content = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }

            dynamic responseJson = JsonConvert.DeserializeObject(content);
            result = responseJson;


            if (responseJson["code"].ToString() == "200")
            {
                return true;
            }

            return false;
        }
    }
    
}
