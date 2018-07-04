using Newtonsoft.Json;
using NLog;
using Sheriff.Networking;
using Sheriff.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;

namespace Sheriff.Common.Plugins
{
    class PluginManager
    {
        public List<PluginBase> Plugins {get; private set;}
        public List<AppDomain> PluginDomains { get; private set; }
        public List<String> Librarys { get; set; }

        private Logger _logger = LogManager.GetCurrentClassLogger();


        public PluginManager()
        {
            this.Plugins = new List<PluginBase>();
            this.Librarys = new List<string>();
        }

        public void LoadLibrarys()
        {

            if (!Directory.Exists("./library"))
            {
                Directory.CreateDirectory("./library");
            }

            this.Librarys.AddRange(Directory.GetFiles("./library").ToArray());
        }

        public void Load()
        {

            if (!Directory.Exists("./plugins"))
            {
                Directory.CreateDirectory("./plugins");
            }



            string[] pluginfiles = Directory.GetFiles("./plugins");


            foreach (string library in this.Librarys)
            {
                if (!library.EndsWith(".dll")) continue;
                Assembly.LoadFrom(library);
            }


            foreach (string path in pluginfiles)
            {
                if (!path.EndsWith(".dll")) continue;

                Assembly.LoadFrom(path);

            }

            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(a => a.GetTypes())
                   .Where(p => typeof(IPlugin).IsAssignableFrom(p) && !p.IsAbstract && p.IsClass && p.IsSubclassOf(typeof(IPlugin)))
                   .ToArray();

            foreach (Type type in types)
            {
                try
                {

                    PluginBase instance = new PluginBase((Activator.CreateInstance(type) as IPlugin), type);
                    instance.GetPlugin().SetLogger(LogManager.GetLogger(instance.Class));
                    instance.GetPlugin().OnLoad();
                    this.Plugins.Add(instance);
                    _logger.Info("{0}  was successfully loaded.", instance.Name);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e);
                    _logger.Warn("{0}  failed to load.", type.Name);
                }
            }

        }

        

        public bool Notify(Packet packet, IClient client)
        {

            List<PluginBase> plugins = this.Plugins.FindAll(x => x.GetOpcodes().Contains(packet.Opcode));
            if (plugins != null && plugins.Count > 0)
            {
                foreach (PluginBase plugin in plugins)
                {
                    try
                    {
                        if (!plugin.GetPlugin().OnPacketReceived(packet, client))
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        _logger.Warn("Plugin unloaded beacue an internal error has occurred.");
                        plugin.Stop();
                        this.Plugins.Remove(plugin);
                    }
                  
                }
            }

            return true;
        }

        public object NotifyApi(PluginBase plugin, string source, Dictionary<string, List<string>> prm)
        {

            try
            {
                return plugin.GetPlugin().OnApiRequested(new Request(source, prm));
            }
            catch
            {
                _logger.Warn("Plugin unloaded beacue an internal error has occurred.");
                plugin.Stop();
                this.Plugins.Remove(plugin);
                return null;
            }

        }

        public void NotifyConnected(IClient client)
        {
            foreach (var plugin in this.Plugins)
            {
                try
                {
                    plugin.GetPlugin().OnClientConnected(client);
                        
                }
                catch
                {
                    _logger.Warn("Plugin unloaded beacue an internal error has occurred.");
                    plugin.Stop();
                    this.Plugins.Remove(plugin);
                }
            }
           
        }

        public void NotifyDisconnected(IClient client)
        {
            foreach (var plugin in this.Plugins)
            {
                try
                {
                    plugin.GetPlugin().OnClientDisconnected(client);

                }
                catch
                {
                    _logger.Warn("Plugin unloaded beacue an internal error has occurred.");
                    plugin.Stop();
                    this.Plugins.Remove(plugin);
                }
            }
        }



        public PluginBase Find(string name)
        {
            return this.Plugins.Find(x => x.Class == name);
        }
        public PluginBase GetById(string id)
        {
            return this.Plugins.Find(x => x.ID == id);
        }

    }
}