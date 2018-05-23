using GBAMusic.Core;
using GBAMusic.Util;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace GBAMusic.Config
{
    internal class GConfig
    {
        internal readonly uint SongTable;
        internal readonly string GameName, CreatorName;

        YamlStream yaml;
        internal GConfig()
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
