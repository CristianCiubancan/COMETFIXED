using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Comet.Shared;

namespace Comet.Launcher.Server.States.Patches
{
    public static class UpdateManager
    {
        public static readonly string ConfigFilePath;
        private static readonly ConcurrentDictionary<int, UpdateStruct> mUpdatesDictionary;

        private static XmlParser mXmlParser { get; set; }

        static UpdateManager()
        {
            ConfigFilePath = Path.Combine(Environment.CurrentDirectory, "Config.xml");
            mUpdatesDictionary = new ConcurrentDictionary<int, UpdateStruct>();
        }

        public static int ListenPort { get; private set; }
        public static string DownloadFrom { get; private set; }
        public static string ConquerHash { get; private set; }


        private static XmlParser CreateConfigFile(string location)
        {
            var writer = new XmlTextWriter(location, Encoding.UTF8)
            {
                Formatting = Formatting.Indented
            };
            writer.WriteStartDocument();
            writer.WriteStartElement("Config");
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            var parser = new XmlParser(location);
            parser.AddNewNode("http://localhost/patches", null, "Url", "Config");
            parser.AddNewNode("9528", null, "ListenPort", "Config");
            parser.AddNewNode("", null, "ConquerSha256", "Config");
            parser.AddNewNode("", null, "Updates", "Config");
            parser.Save();
            return parser;
        }

        public static async Task LoadConfigAsync()
        {
            try
            {
                mXmlParser = !File.Exists(ConfigFilePath)
                                 ? CreateConfigFile(ConfigFilePath)
                                 : new XmlParser(ConfigFilePath);

                ListenPort = int.Parse(mXmlParser.GetValue("Config", "ListenPort"));
                DownloadFrom = mXmlParser.GetValue("Config", "Url");
                ConquerHash = mXmlParser.GetValue("Config", "ConquerSha256");

                foreach (XmlNode node in mXmlParser.GetAllNodes("Config", "Updates"))
                {
                    string id = node.Attributes?["id"]?.Value;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    string ext = mXmlParser.GetValue("Config", "Updates", $"Patch[@id='{id}']", "Extension");

                    if (!int.TryParse(mXmlParser.GetValue("Config", "Updates", $"Patch[@id='{node.Attributes["id"].Value}']",
                                                          "From"),
                                      out int @from))
                    {
                        await Log.WriteLogAsync(LogLevel.Error, $"Invalid patch [From] data for ID '{id}'.");
                        continue;
                    }

                    if (!int.TryParse(mXmlParser.GetValue("Config", "Updates", $"Patch[@id='{node.Attributes["id"].Value}']",
                                                          "To"),
                                      out int to))
                        to = from;

                    if (!IsSupportedExt(ext))
                    {
                        await Log.WriteLogAsync(LogLevel.Error, $"Invalid extension [{ext}] for ID '{id}'.");
                        continue;
                    }

                    string hash = mXmlParser.GetValue("Config", "Updates",
                                                      $"Patch[@id='{node.Attributes["id"].Value}']",
                                                      "Hash");

                    var str = new UpdateStruct
                    {
                        From = from,
                        To = to,
                        Extension = ext,
                        Hash = hash
                    };
                    if (mUpdatesDictionary.TryAdd(from, str))
                    {
                        if (str.IsBundle)
                            await Log.WriteLogAsync(LogLevel.Info, $"Bundle [{str.FullFileName}] loaded.");
                        else
                            await Log.WriteLogAsync(LogLevel.Info, $"Patch [{str.FullFileName}] loaded.");
                    }
                    else
                    {
                        await Log.WriteLogAsync(LogLevel.Error, $"Possible duplicate of patch ID -> '{id}'.");
                    }
                }
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, e.Message);
            }
        }

        public static bool AppendPatch(UpdateStruct patch)
        {
            if (patch.From == 0)
                return false;

            if (mUpdatesDictionary.ContainsKey(patch.From))
                return false;

            if (mUpdatesDictionary.TryAdd(patch.From, patch))
            {
                mXmlParser.AddNewNode("", patch.From.ToString(), "Patch", "Config", "Updates");
                mXmlParser.AddNewNode($"{patch.From}", null, "From", "Config", "Updates", $"Patch[@id='{patch.From}']");
                mXmlParser.AddNewNode($"{patch.To}", null, "To", "Config", "Updates", $"Patch[@id='{patch.From}']");
                mXmlParser.AddNewNode($"{patch.Extension}", null, "Extension", "Config", "Updates", $"Patch[@id='{patch.From}']");
                mXmlParser.AddNewNode($"{patch.Hash}", null, "Hash", "Config", "Updates", $"Patch[@id='{patch.From}']");
                mXmlParser.Save();
                return true;
            }

            return false;
        }

        public static bool ContainsUpdate(int from)
        {
            return mUpdatesDictionary.ContainsKey(from);
        }

        public static bool RemovePatch(int from)
        {
            if (from == 0)
                return false;
            if (mUpdatesDictionary.TryRemove(from, out UpdateStruct patch))
            {
                mXmlParser.DeleteNode("Config", "Updates", $"Patch[@id='{from}']");
                return true;
            }

            return false;
        }

        public static int LatestClientUpdate()
        {
            return mUpdatesDictionary.Values.Where(x => x.IsUpdate).DefaultIfEmpty(new UpdateStruct()).Max(x => x.To);
        }

        public static int LatestGameClientUpdate()
        {
            return mUpdatesDictionary.Values.Where(x => !x.IsUpdate).DefaultIfEmpty(new UpdateStruct()).Max(x => x.To);
        }

        public static List<UpdateStruct> GetUpdateSequence(int from)
        {
            bool game = from < 10000;
            var result = new List<UpdateStruct>();
            int current = game ? LatestGameClientUpdate() : LatestClientUpdate();

            if (!game)
                return new List<UpdateStruct>
                {
                    mUpdatesDictionary.Values.Where(x => x.IsUpdate).OrderByDescending(x => x.To).FirstOrDefault()
                };

            foreach (UpdateStruct patch in mUpdatesDictionary.Values
                                                             .Where(x => x.IsUpdate == false && x.From > from)
                                                             .OrderByDescending(x => x.To))
            {
                if (patch.To > current)
                    continue;

                result.Add(patch);

                if (patch.IsBundle)
                    current = patch.From;
                else current -= 1;
            }

            return result.OrderBy(x => x.To).ToList();
        }

        public static void ListUpdates()
        {
            Console.WriteLine(
                $"Last Client Update[{LatestClientUpdate():00000}] \tLast Game Client Update[{LatestGameClientUpdate():0000}]");

            Console.WriteLine("Displaying Updates");
            Console.WriteLine("Auto Patcher Client");
            foreach (UpdateStruct update in mUpdatesDictionary.Values.Where(x => x.IsUpdate).OrderBy(x => x.From))
                Console.WriteLine($"\tUpdate {update.FileName}");

            Console.WriteLine("Game Client");
            foreach (UpdateStruct update in mUpdatesDictionary.Values.Where(x => !x.IsUpdate).OrderBy(x => x.From))
                if (!update.IsBundle)
                    Console.WriteLine($"\tUpdate {update.FileName}");
                else Console.WriteLine($"\tBundle {update.FileName}");
        }

        public static bool IsSupportedExt(string ext)
        {
            switch (ext.ToLowerInvariant())
            {
                case "7z":
                case "exe":
                    return true;
                default:
                    return false;
            }
        }
    }
}