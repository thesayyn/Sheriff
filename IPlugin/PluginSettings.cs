using System;
using System.IO;
using System.Xml;

namespace Sheriff.Common.Plugins
{
    public class PluginSettings
    {
        
        private IPlugin _plugin;
        private XmlDocument xml;
        string FilePath;

        public PluginSettings(IPlugin plugin)
        {

            this._plugin = plugin;
            Load();
        }
        private void Load()
        {
            
            this.FilePath = $"./plugins/{_plugin.Name}.config";
            this.xml = new XmlDocument();

            if (!File.Exists(FilePath))
            {
                XmlDocument doc = new XmlDocument();
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = doc.DocumentElement;
                doc.InsertBefore(xmlDeclaration, root);
                XmlElement element1 = doc.CreateElement(string.Empty, "Settings", string.Empty);
                doc.AppendChild(element1);
                doc.Save(FilePath);
            }



            xml.Load(FilePath);
        }

        public bool IsRegistered<T>(string name)
        {
            XmlNode node = xml["Settings"][name];
            if (node != null)
            {
                if(node.Attributes["Type"].Value == null)
                {
                    return false;
                }

                if(Type.GetType(node.Attributes["Type"].Value.ToString()) != typeof(T))
                {
                    return false;
                }
            }

            return false;
        }
        public bool Register<T>(string group,string name,string description)
        {
            if (xml["Settings"][name] != null)
            {
                return false;
            }
    
            XmlNode node = xml.CreateNode(XmlNodeType.Element, name, null);

            if (typeof(T).IsArray)
            {
                node.InnerText = "";
            }
            else
            {
                node.InnerText = default(T) + "";
            }

            XmlAttribute groupAttr = xml.CreateAttribute("Group");
            XmlAttribute descAttr = xml.CreateAttribute("Description");
            XmlAttribute typeAttr = xml.CreateAttribute("Type");
            node.Attributes.SetNamedItem(groupAttr);
            node.Attributes.SetNamedItem(typeAttr);
            node.Attributes.SetNamedItem(descAttr);
            node.Attributes["Description"].Value = description;
            node.Attributes["Group"].Value = group;
            node.Attributes["Type"].Value = typeof(T).ToString();
            xml["Settings"].AppendChild(node);
            xml.Save(FilePath);
            xml.Load(FilePath);
            return true;
        }


        public bool Remove(string name)
        {
            if (xml["Settings"][name] == null)
            {
                return false;
            }

            xml.RemoveChild(xml["Settings"][name]);
            xml.Save(FilePath);
            xml.Load(FilePath);
            return true;

        }
        public void RemoveAll()
        {
            File.Delete(FilePath);
            Load();
        }

        public void Clear(string key)
        {
            Type type = GetType(key);


            if (type != null && this.xml["Settings"][key]!=null)
            {
                if (type.IsArray)
                {
                    this.xml["Settings"][key].InnerText = null;
                }
                else
                {
                    this.xml["Settings"][key].InnerText = Activator.CreateInstance(type).ToString();
                }
            }

            xml.Save(FilePath);
            xml.Load(FilePath);
        }

        public void RemoveValue(string key,string value)
        {
            Type type = GetType(key);


            if (type != null && type.IsArray  && this.xml["Settings"][key] != null)
            {
                foreach (XmlNode item in this.xml["Settings"][key])
                {
                    if(item.InnerText == value)
                    {
                        this.xml["Settings"][key].RemoveChild(item);
                    }
                }
            }

            xml.Save(FilePath);
            xml.Load(FilePath);
        }



        public T Get<T>(string name)
        {
            XmlNode node = xml["Settings"][name];
            if (node == null)
            { 
                return default(T);
            }

            string strtype = node.Attributes["Type"].Value;
            Type singleType = Type.GetType(strtype.Remove(strtype.Length - 2, 2));
            Type type = Type.GetType(strtype);


            if (type.IsArray)
            {

                object[] obj = new object[node.ChildNodes.Count];

                dynamic array = Array.CreateInstance(singleType, node.ChildNodes.Count);

                int i = 0;
                foreach (XmlNode item in node)
                {
                    obj[i] = Convert.ChangeType(item.InnerText, singleType);
                    i++;
                }

                Array.Copy(obj, array, obj.Length);

                return array;
            }


            return (T)Convert.ChangeType(node.InnerText, typeof(T));
        }
        public bool Set<T>(string name, T value)
        {
            if (xml["Settings"][name] == null)
            {

                return false;
            }

            if (typeof(T).IsArray)
            {
                
                foreach (var item in (value as dynamic))
                {
              
                    XmlNode node = xml.CreateNode(XmlNodeType.Element,nameof(item), null);
                    node.InnerText = Convert.ToString(item);
                    xml["Settings"][name].AppendChild(node);
                }
            }
            else
            {
                xml["Settings"][name].InnerText = value.ToString();
            }


           
            
            xml.Save(FilePath);
            xml.Load(FilePath);
            return true;
        }
        public bool Set(string name, dynamic value, Type type)
        {
            if (xml["Settings"][name] == null)
            {

                return false;
            }

            if (type.IsArray)
            {

                foreach (var item in (value as dynamic))
                {

                    XmlNode node = xml.CreateNode(XmlNodeType.Element, nameof(item), null);
                    node.InnerText = Convert.ToString(item);
                    xml["Settings"][name].AppendChild(node);
                }
            }
            else
            {
                xml["Settings"][name].InnerText = value.ToString();
            }




            xml.Save(FilePath);
            xml.Load(FilePath);
            return true;
        }


        public XmlElement GetXml()
        {
            return this.xml["Settings"];
        }
        public Type GetType(string key)
        {
            XmlNode node = this.xml["Settings"][key];
            if (node != null) {;
                return Type.GetType(node.Attributes["Type"].Value);
            }

            return null;
        }
    }
}
