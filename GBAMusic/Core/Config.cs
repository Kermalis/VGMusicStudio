using GBAMusic.Util;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace GBAMusic.Core
{
    public class Config
    {
        public readonly uint SongTable;
        public readonly string GameName, CreatorName;

        YamlStream yaml;
        public Config()
        {
            yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText("Games.yaml")));

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            var game = (YamlMappingNode)mapping.Children[new YamlScalarNode(ROM.Instance.GameCode)];

            SongTable = (uint)Utils.ParseInt(game.Children[new YamlScalarNode("songTable")].ToString());
            GameName = game.Children[new YamlScalarNode("name")].ToString();
            // Steal
            CreatorName = game.Children[new YamlScalarNode("creator")].ToString();
        }
    }
}
