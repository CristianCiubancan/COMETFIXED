using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Comet.Shared
{
    public sealed class XmlParser
    {
        private readonly string mPath;
        private readonly XmlDocument mXml = new();

        public XmlParser(string path)
        {
            if (!File.Exists(path))
            {
                var writer = new XmlTextWriter(path, Encoding.UTF8)
                {
                    Formatting = Formatting.Indented
                };
                writer.WriteStartDocument();
                writer.WriteStartElement("Config");
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }

            mPath = path;
            mXml.Load(path);
        }

        public bool AutoSave { get; set; } = false;

        public void AddNewNode(object value, string idNode, string node, params string[] xpath)
        {
            if (!CheckNodeExists(xpath))
            {
                CreateXPath(xpath);
            }

            string query;
            if (string.IsNullOrEmpty(idNode))
                query = TransformXPath(xpath) + $"/{node}";
            else
            {
                var tempxPath = new string[xpath.Length + 1];
                for (var i = 0; i < xpath.Length; i++)
                    tempxPath[i] = xpath[i];
                tempxPath[^1] = $"{node}[@id='{idNode}']";
                query = TransformXPath(tempxPath);
            }

            if (CheckNodeExists(query))
            {
                ChangeValue(value.ToString(), TransformXPath(xpath), node);
                return;
            }

            XmlNode appendTo = GetNode(xpath);
            XmlNode newNode = mXml.CreateNode(XmlNodeType.Element, node, "");
            newNode.InnerText = value.ToString();
            if (!string.IsNullOrEmpty(idNode))
            {
                XmlAttribute attrib = mXml.CreateAttribute("id");
                attrib.Value = idNode;

                // ReSharper disable once PossibleNullReferenceException
                newNode.Attributes.Append(attrib);
            }

            appendTo.AppendChild(newNode);
            if (AutoSave)
                mXml.Save(mPath);
        }

        public void CreateXPath(params string[] xpath)
        {
            XmlNode parent = null;
            foreach (string path in xpath)
            {
                var tempXPath = $"/{path}";
                if (!CheckNodeExists(tempXPath) && parent == null)
                    mXml.AppendChild(parent = mXml.CreateElement(path));
                if (!CheckNodeExists(tempXPath) && parent != null)
                    parent.AppendChild(parent = mXml.CreateElement(path));
                else
                    parent = GetNode(tempXPath);
            }
        }

        public void DeleteNode(params string[] xpath)
        {
            DeleteNode(mXml.SelectSingleNode(TransformXPath(xpath)));
        }

        private void DeleteNode(XmlNode node)
        {
            if (node == null)
                return;

            XmlNode parent = node.ParentNode;
            parent?.RemoveChild(node);
            if (AutoSave)
                mXml.Save(mPath);
        }

        public XmlNode GetNode(params string[] xpath)
        {
            return mXml.SelectSingleNode(TransformXPath(xpath));
        }

        public bool ChangeValue(string newValue, params string[] xpath)
        {
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                mXml.SelectSingleNode(TransformXPath(xpath)).InnerText = newValue;
                if (AutoSave)
                    mXml.Save(mPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetValue(params string[] xpath)
        {
            if (!CheckNodeExists(xpath))
                return "";
            return GetNode(xpath)?.InnerText ?? "";
        }

        public int GetIntValue(params string[] xpath)
        {
            if (!CheckNodeExists(xpath))
                return 0;
            return int.TryParse(GetNode(xpath)?.InnerText, out int result) ? result : 0;
        }

        public XmlNodeList GetAllNodes(params string[] xpath)
        {
            if (!CheckNodeExists(xpath))
                return null;
            return GetNode(xpath).ChildNodes;
        }

        public bool CheckNodeExists(params string[] xpath)
        {
            return mXml.SelectSingleNode(TransformXPath(xpath)) != null;
        }

        private static string TransformXPath(params string[] xpath)
        {
            return xpath.Aggregate("", (current, str) => current + $"/{str}");
        }

        public void Save()
        {
            mXml.Save(mPath);
        }
    }
}