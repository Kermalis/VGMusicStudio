using Kermalis.VGMusicStudio.Util;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core
{
    class Config
    {
        public static Config Instance { get; } = new Config();

        public bool TaskbarProgress { get; private set; }
        public byte RefreshRate { get; private set; }
        public bool CenterIndicators { get; private set; }
        public bool PanpotIndicators { get; private set; }
        public HSLColor[] Colors { get; private set; }

        private Config()
        {
            Load();
        }
        public void Load()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Config.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            TaskbarProgress = bool.Parse(mapping.Children["TaskbarProgress"].ToString());
            RefreshRate = (byte)Utils.ParseValue(mapping.Children["RefreshRate"].ToString());
            CenterIndicators = bool.Parse(mapping.Children["CenterIndicators"].ToString());
            PanpotIndicators = bool.Parse(mapping.Children["PanpotIndicators"].ToString());

            var cmap = (YamlMappingNode)mapping.Children["Colors"];
            Colors = new HSLColor[256];
            foreach (KeyValuePair<YamlNode, YamlNode> c in cmap)
            {
                int i = (int)Utils.ParseValue(c.Key.ToString());
                IDictionary<YamlNode, YamlNode> children = ((YamlMappingNode)c.Value).Children;
                double h = 0, s = 0, l = 0;
                foreach (KeyValuePair<YamlNode, YamlNode> v in children)
                {
                    if (v.Key.ToString() == "H")
                    {
                        h = byte.Parse(v.Value.ToString());
                    }
                    else if (v.Key.ToString() == "S")
                    {
                        s = byte.Parse(v.Value.ToString());
                    }
                    else if (v.Key.ToString() == "L")
                    {
                        l = byte.Parse(v.Value.ToString());
                    }
                }
                var color = new HSLColor(h, s, l);
                Colors[i] = Colors[i + 0x80] = color;
            }
        }
    }
}
