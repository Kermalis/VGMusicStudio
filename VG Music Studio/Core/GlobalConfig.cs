using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core
{
    public enum PlaylistMode : byte
    {
        Random,
        Sequential
    }

    internal class GlobalConfig
    {
        public static GlobalConfig Instance { get; private set; }

        public bool TaskbarProgress;
        public ushort RefreshRate;
        public bool CenterIndicators;
        public bool PanpotIndicators;
        public PlaylistMode PlaylistMode;
        public long PlaylistSongLoops;
        public long PlaylistFadeOutMilliseconds;
        public HSLColor[] Colors;

        private GlobalConfig()
        {
            const string configFile = "Config.yaml";
            using (StreamReader fileStream = File.OpenText(configFile))
            {
                try
                {
                    var yaml = new YamlStream();
                    yaml.Load(fileStream);

                    var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                    TaskbarProgress = mapping.GetValidBoolean(nameof(TaskbarProgress));
                    RefreshRate = (ushort)mapping.GetValidValue(nameof(RefreshRate), 1, 1000);
                    CenterIndicators = mapping.GetValidBoolean(nameof(CenterIndicators));
                    PanpotIndicators = mapping.GetValidBoolean(nameof(PanpotIndicators));
                    try
                    {
                        PlaylistMode = (PlaylistMode)Enum.Parse(typeof(PlaylistMode), mapping.Children.GetValue(nameof(PlaylistMode)).ToString());
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                    {
                        throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}\"{nameof(PlaylistMode)}\" was invalid.");
                    }
                    PlaylistSongLoops = mapping.GetValidValue(nameof(PlaylistSongLoops), 0, long.MaxValue);
                    PlaylistFadeOutMilliseconds = mapping.GetValidValue(nameof(PlaylistFadeOutMilliseconds), 0, long.MaxValue);

                    var cmap = (YamlMappingNode)mapping.Children[nameof(Colors)];
                    Colors = new HSLColor[256];
                    foreach (KeyValuePair<YamlNode, YamlNode> c in cmap)
                    {
                        int i = (int)Utils.ParseValue($"{nameof(Colors)} key", c.Key.ToString(), 0, 127);
                        if (Colors[i] != null)
                        {
                            throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}Color {i} is defined more than once between decimal and hexadecimal.");
                        }
                        double h = 0, s = 0, l = 0;
                        foreach (KeyValuePair<YamlNode, YamlNode> v in ((YamlMappingNode)c.Value).Children)
                        {
                            string key = v.Key.ToString();
                            string valueName = $"Color {i} {key}";
                            if (key == "H")
                            {
                                h = Utils.ParseValue(valueName, v.Value.ToString(), 0, 240);
                            }
                            else if (key == "S")
                            {
                                s = Utils.ParseValue(valueName, v.Value.ToString(), 0, 240);
                            }
                            else if (key == "L")
                            {
                                l = Utils.ParseValue(valueName, v.Value.ToString(), 0, 240);
                            }
                            else
                            {
                                throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}Color {i} has an invalid key.");
                            }
                        }
                        Colors[i] = Colors[i + 128] = new HSLColor(h, s, l);
                    }
                    for (int i = 0; i < Colors.Length; i++)
                    {
                        if (Colors[i] == null)
                        {
                            throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}Color {i} is not defined.");
                        }
                    }
                }
                catch (BetterKeyNotFoundException ex)
                {
                    throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}\"{ex.Key}\" is missing.");
                }
                catch (Exception ex) when (ex is InvalidValueException || ex is YamlDotNet.Core.SyntaxErrorException)
                {
                    throw new Exception($"Error parsing \"{configFile}\"{Environment.NewLine}{ex.Message}");
                }
            }
        }

        public static void Init()
        {
            Instance = new GlobalConfig();
        }
    }
}
