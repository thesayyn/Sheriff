using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using System.Xml;
using System.IO;
using System.Text;
using System.Linq;

using Sheriff.Emulators.Agent;
using Sheriff.Emulators.Gateway;
using Sheriff.Emulators.Download;

using Sheriff.Http;
using Sheriff.Models;
using Sheriff.Common;
using Sheriff.Common.Plugins;
using Sheriff.Common.Performance;
using Sheriff.Common.Inner;

namespace Sheriff
{
    class Program
    {
        public static Settings config = new Settings();
        public static PluginManager manager = new PluginManager();
        public static List<IServiceBase> services = new List<IServiceBase>();
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public static Logger commandlogger = LogManager.GetLogger(Utility.Title+"/"+Utility.Version);
        public static List<string> accesstokens = new List<string>();

        static void Main(string[] args)
        {
            AppDomain.MonitoringIsEnabled = true;

    
            logger.Info("Loading settings.");
            config.Load("./Settings.config");
            dynamic result;
            if (Sheriffy.Check(config.Key,out result))
            {
                logger.Info("Subscription name : {0} ",result["subscription"]["name"]);
                logger.Info("Subscription key : {0} ", result["subscription"]["key"]);
                logger.Info("Subscription remaining : {0} days", result["subscription"]["remaining"]["days"]);
                logger.Info("Subscription end date : {0} ", result["subscription"]["remaining"]["date"]);
            }
            else
            {
                logger.Info("Your subscription has expired or not found please contact to your provider.");
                Environment.Exit(100);
            }

            logger.Info("Loading librarys.");
            manager.LoadLibrarys();
            logger.Info("Loading plugins.");
            manager.Load();
            logger.Info("({0}) Plugin successfully loaded.",manager.Plugins.Count);

          

            logger.Info("Services are being loaded and starting.");
            foreach (Server module in config.Servers)
            {
  
                if (module.Class == typeof(GatewayServer).FullName)
                {
 
                    GatewayServer server = new GatewayServer();
                    server.Bind = module.Bind;
                    server.ID = module.ID;
                    server.Class = module.Class;
                    server.Description = module.Description;
                    server.Divisions = module.Divisions;
                    server.Server = module.Reditected;
                    server.Start();
                    services.Add(server);
                }
                else if (module.Class == typeof(AgentServer).FullName)
                {
                    AgentServer server = new AgentServer();
                    server.Bind = module.Bind;
                    server.ID = module.ID;
                    server.Class = module.Class;
                    server.Description = module.Description;
                    server.Start();
                    services.Add(server);
                }
                else if (module.Class == typeof(DownloadServer).FullName)
                {
                    DownloadServer server = new DownloadServer();
                    server.Bind = module.Bind;
                    server.ID = module.ID;
                    server.Class = module.Class;
                    server.Description = module.Description;
                    server.Start();
                    services.Add(server);
                }
                else if (module.Class == typeof(ApiServer).FullName)
                {
                    ApiServer server = new ApiServer();
                    server.Bind = module.Bind;
                    server.ID = module.ID;
                    server.Class = module.Class;
                    server.Description = module.Description;
                    server.OnRequestReceived += ApiServer_OnRequestReceived;
                    server.Start();
                    services.Add(server);
                }
            }

           

            logger.Info("Everything is looking fine.");
            SetupConsole();
        }
        private static void ApiServer_OnRequestReceived(ApiServer server, HttpRequest request, HttpResponse response)
        {
            logger.Info("Request received {0} ,{1}", request.Path[0], request.File);

            response.StatusCode = StatusCode.Ok;
            response.Headers.Add("Server", Utility.Title);
            response.Headers.Add("Version", Utility.Version);
            response.Headers.Add("Access-Control-Allow-Origin", "*"); 

            dynamic json = new ExpandoObject();
            json.server = Utility.Title;
            json.buildtime = Utility.BuildTime;
            json.version = Utility.Version;
            json.versionint = Utility.VersionInt;
            json.developer = Utility.Developer;
     
            json.url = $"http://{request.Url}";

 

            string access_token = request.GetParam("access_token");
            Access access = config.Security.Access.Find(x=>x.ServerID == server.ID.ToString());
            Access.Token token = access.Tokens.Find(x => x.Value == access_token);

            if(request.File == "favicon.ico" || request.File == "favicon.png")
            {
                response.Headers.Add("Content-type", "image/png");
                response.Content = Utility.ImageToByte(Sheriff.Resource1.sheriff.ToBitmap());
                response.Write();
                return;
            }

            if( request.GetPath(0) == "authorize")
            {
                if(token == null)
                {
                    response.StatusCode = StatusCode.AuthenticationFail;
                    json.code = 401;
                    json.message = "authorization has been failed.";
                }
                else if(token.ExpiresIn < DateTimeOffset.Now.ToUnixTimeSeconds())
                {
                    response.StatusCode = StatusCode.AuthenticationFail;
                    json.code = 401;
                    json.message = "access token was expired.";
                }
                else
                {
                    json.code = 200;
                    json.message = "authorization was successfull.";
                }
            }
            else
            {
                if(token == null)
                {
                    response.StatusCode = StatusCode.AuthenticationFail;
                    json.code = 401;
                    json.message = "authorization has been failed.";
                }
                else if(request.GetPath(0) == "info")
                {
                    json.Performance = new ExpandoObject();

                    json.Performance.Machine = new ExpandoObject();
                    json.Performance.Machine.processortime = MachineManager.GetGlobalCpuUsage();
                    json.Performance.Machine.usedram = MachineManager.GetGlobalRamUsage();

                    json.Performance.Application = new ExpandoObject();
                    json.Performance.Application.processortime = MachineManager.GetCpuUsage(Process.GetCurrentProcess());
                    json.Performance.Application.usedram = MachineManager.GetRamUsage(Process.GetCurrentProcess());

                    json.Performance.Network = new List<ExpandoObject>();
                    foreach (NetworkInterface item in NetworkManager.GetNetworkIntefaces())
                    {
                        json.Performance.Network.Add(NetworkManager.GetNetworkUsages(item));
                    }

                    json.Performance.AppDomain = new ExpandoObject();
                    json.Performance.AppDomain.id = AppDomain.CurrentDomain.FriendlyName;
                    json.Performance.AppDomain.processortime = MachineManager.GetAppDomainCpuUsage(AppDomain.CurrentDomain);
                    json.Performance.AppDomain.usedram = MachineManager.GetAppDomainMemoryUsage(AppDomain.CurrentDomain);

                }
                else if (request.GetPath(0) == "plugin")
                {
                    if (request.GetPath(1) == "getall")
                    {
                        json.Plugins = manager.Plugins;

                    }
                    else if (request.GetPath(1) != null && request.GetPath(2) != null)
                    {

                        string id = request.GetPath(1);
                        string method = request.GetPath(2);
                        PluginBase plugin = manager.Plugins.Find(x => x.ID == id);

                        if(plugin != null)
                        {

                            if (method == "settings")
                            {

                                string submethod = request.GetPath(3);

                                if(submethod == "getall")
                                {
                                    json.Settings = new List<ExpandoObject>();
                                    foreach (XmlNode item in plugin.GetPlugin().GetSettings().GetXml())
                                    {
                                        dynamic jit = new ExpandoObject();
                                        jit.Key = item.Name;
                                        jit.Group = item.Attributes["Group"].Value;
                                        jit.Type = item.Attributes["Type"].Value;
                                        jit.Description = item.Attributes["Description"].Value;

                                        Type type = Type.GetType(jit.Type);
                                        Type singleType = Type.GetType(jit.Type.Remove(jit.Type.Length - 2, 2));
                                    

                                        if (type.IsArray)
                                        {
                                           
                                            jit.Value = new List<dynamic>();
                                            jit.IsArray = type.IsArray;
                                            jit.SingleType = singleType.ToString();
                                            foreach (XmlNode child in item)
                                            {
                                                jit.Value.Add(Convert.ChangeType(child.InnerText, singleType));
                                            }
                                        }
                                        else
                                        {
                                            jit.IsArray = type.IsArray;
                                            jit.Value = Convert.ChangeType(item.InnerText, type);
                                        }

                                        json.Settings.Add(jit);
                                    }
                               
                                }
                                else if(submethod == "set")
                                {
                                    string key = request.GetParam("key");

                                    if (key != null)
                                    {

                                        Type type = plugin.GetPlugin().GetSettings().GetType(key);


                                        if (type != null)
                                        {
                                            if (type.IsArray)
                                            {
                                                try
                                                {
                                                    Type singleType = Type.GetType(type.ToString().Remove(type.ToString().Length - 2, 2));
                                                    List<String> values = request.GetParams("value");
                                                    List<dynamic> castedValues = new List<dynamic>();
                                                    foreach (var item in values)
                                                    {
                                                        castedValues.Add(Convert.ChangeType(item, singleType));
                                                    }
                                                    plugin.GetPlugin().GetSettings().Set(key, castedValues.ToArray(), type);
                                                    plugin.GetPlugin().OnSettingsChanged();

                                                    json.code = response.StatusCode;
                                                    json.message = "settins has been added.";

                                                }
                                                catch
                                                {
                                                    response.StatusCode = StatusCode.BadRequest;
                                                    json.code = response.StatusCode;
                                                    json.message = "invalid value received.";
                                                }
                                            }
                                            else
                                            {
                                                string value = request.GetParam("value");

                                                try
                                                {
                                                    plugin.GetPlugin().GetSettings().Set(key, Convert.ChangeType(value, type), type);
                                                    plugin.GetPlugin().OnSettingsChanged();
                                                }
                                                catch
                                                {
                                                    response.StatusCode = StatusCode.BadRequest;
                                                    json.code = response.StatusCode;
                                                    json.message = "invalid value received.";
                                                }

                                            }

                                        }

                                    }
                                    else
                                    {
                                        response.StatusCode = StatusCode.BadRequest;
                                        json.code = response.StatusCode;
                                        json.message = "settings key is required.";
                                    }
                                }
                                else if(submethod == "clear")
                                {
                                    string key = request.GetParam("key");

                                    if (key != null)
                                    {
                                        plugin.GetPlugin().GetSettings().Clear(key);
                                        plugin.GetPlugin().OnSettingsChanged();
                                    }

                                    json.code = response.StatusCode;
                                    json.message = "settings was cleared.";
                                }
                                else if(submethod == "remove")
                                {
                                    string key = request.GetParam("key");
                                    string value = request.GetParam("value");

                                    if (key != null && value != null)
                                    {
                                        plugin.GetPlugin().GetSettings().RemoveValue(key, value);
                                        plugin.GetPlugin().OnSettingsChanged();
                                        json.code = response.StatusCode;
                                        json.message = "the value removed.";
                                    }
                                    else
                                    {
                                        response.StatusCode = StatusCode.BadRequest;
                                        json.code = 404;
                                        json.message = "key and value is required.";
                                    }
                                }
                                else
                                {
                                    response.StatusCode = StatusCode.NotFound;
                                    json.code = response.StatusCode;
                                    json.message = "requested endpoint not found in server.";
                                }

                            }
                            else if (method == "stop")
                            {
                                plugin.Stop();
                                json.message = "the plugin has been stopped.";
                            }
                            else if (method == "start")
                            {
                                plugin.Start();
                                json.message = "the plugin has been started.";
                            }
                        }
                        else
                        {
                            response.StatusCode = StatusCode.NotFound;
                            json.code = 404;
                            json.message = "requested plugin not found in server.";
                        }
                    }
                    else
                    {
                        response.StatusCode = StatusCode.NotFound;
                        json.code = 404;
                        json.message = "requested endpoint not found in server.";
                    }
                }
                else if (request.GetPath(0) == "service"){

                    if (request.GetPath(1) == "getall")
                    {
                        json.Services = new List<ExpandoObject>();

                        foreach (var service in services)
                        {
                            dynamic jit = new ExpandoObject();
                            jit.ID = service.ID;
                            jit.Description = service.Description;
                            jit.Class = service.Class;
                            jit.Status = service.Status;
                            jit.Clients = service.Clients;
                            jit.Bind = new List<String>();
                            foreach (var item in service.Bind)
                            {
                                if (item == null) continue;
                                jit.Bind.Add(item.IP + ":" + item.Port);
                            }
                            json.Services.Add(jit);

                        }

                    }
                    else if (request.GetPath(1) != null && request.GetPath(2) != null)
                    {

                        string id = request.GetPath(1);
                        string method = request.GetPath(2);
                        IServiceBase service = services.Find(x => x.ID == byte.Parse(id));

                        if (service != null)
                        {
                            if(method == "start")
                            {
                                if (!service.Status)
                                {
                                    service.Start();
                                    json.message = "the service has been started.";
                                }
                                else
                                {
                                    json.message = "service already started.";
                                }
                            }
                            else if(method == "stop")
                            {
                                if (service.Status)
                                {
                                    service.Stop();
                                    json.message = "the service has been stopped.";
                                }
                                else
                                {
                                    json.message = "service already stopped.";
                                }
                               
                            }
                            
                        }
                        else
                        {
                            response.StatusCode = StatusCode.NotFound;
                            json.code = 404;
                            json.message = "requested service not found in server.";
                        }
                    }
                    else
                    {
                        response.StatusCode = StatusCode.NotFound;
                        json.code = 404;
                        json.message = "requested endpoint not found in server.";
                    }
                }
                else if(request.GetPath(0) == "logs")
                {
                    string[] logfiles = Directory.GetFiles("./logs");

                    json.Logs = new List<ExpandoObject>();

                    foreach (string file in logfiles)
                    {
                        if (!file.EndsWith(".log")) continue;
                        FileInfo inf = new FileInfo(file);
                        dynamic jit = new ExpandoObject();
                        jit.Name = inf.Name;
                        jit.Size = inf.Length;
                        jit.Content = File.ReadAllText(file,Encoding.UTF8);
                        json.Logs.Add(jit);

                    }

                }
                else
                {
                    response.StatusCode = StatusCode.NotFound;
                    json.code = 404;
                    json.message = "requested endpoint not found in server.";
                }
  
            }



            response.setJSONContent(json);

        }
        private static void SetupConsole()
        {

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.Clear();
                Program.logger.Fatal(e.ExceptionObject);
                Program.logger.Warn(@"Unhandled problem has occurred. Look at to Fatal logs.");
            };

            Console.WindowWidth = 150;
            Console.BufferHeight = 5000;
            Console.Title = string.Format("{0} {1} ({2}) [{3}] *{4}",
                Utility.Title,
                Utility.Version,
                Utility.BuildTime,
                string.IsNullOrEmpty(Utility.Configuration) ? "Undefined" : string.Format("{0}", Utility.Configuration), Utility.Developer);


            Thread.Sleep(100);

            while (true)
            {
   
                Console.Write(Utility.Title + "/" + Utility.Version+" >  ");
                
                List<string> commands = Console.ReadLine().Split(' ').ToList<string>();

                if (commands[0] == "service")
                {
                    byte id = byte.Parse(commands[1]);
                    IServiceBase service = services.Find(x => x.ID == id);

                    if (service == null)
                    {
                        commandlogger.Info("service not found");
                    }
                    else
                    {
                        if (commands[2] == "start")
                        {
                            if (service.Status) { commandlogger.Info("Service is already running."); continue; }
                            service.Start();
                            commandlogger.Info("Service is started.");
                        }
                        else if(commands[2] == "stop")
                        {
                            if (!service.Status) { commandlogger.Info("Service is already stopped."); continue; }
                            service.Stop();
                            commandlogger.Info("Service is stopped.");
                        }
                        else if (commands[2] == "restart")
                        {
                            if(service.Status) service.Stop();
                            service.Start();
                            commandlogger.Info("Service restarted.");
                        }
                    }

                    
                }
                else if(commands[0] == "help")
                {
                    Console.WriteLine("Available commands");
                    Console.WriteLine("service (serviceid) start");
                    Console.WriteLine("service (serviceid) stop");
                    Console.WriteLine("service (serviceid) restart");
                }
                else if(commands[0] == "version")
                {
                    commandlogger.Info("{0}/{1} ({2}) [{3}]", Utility.Title, Utility.Version.ToString(), Utility.VersionInt, Utility.Platform);
                }
                else
                {
                    commandlogger.Info("Invalid command. Type help to see available commands.");
                }
                Thread.Sleep(100);
            }
        }

    }
}
