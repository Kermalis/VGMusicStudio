using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Kermalis.VGMusicStudio.Core
{
    internal enum PlaylistMode : byte
    {
        Random,
        Sequential
    }

    internal sealed class GlobalConfig
    {
        public static GlobalConfig Instance { get; private set; }

        public readonly bool TaskbarProgress;
        public readonly ushort RefreshRate;
        public readonly bool CenterIndicators;
        public readonly bool PanpotIndicators;
        public readonly PlaylistMode PlaylistMode;
        public readonly long PlaylistSongLoops;
        public readonly long PlaylistFadeOutMilliseconds;
        public readonly sbyte MiddleCOctave;
        public readonly HSLColor[] Colors;

        private GlobalConfig()
        {
            const string configFile = "Config.yaml";
            using (StreamReader fileStream = File.OpenText(Utils.CombineWithBaseDirectory(configFile)))
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
                    PlaylistMode = mapping.GetValidEnum<PlaylistMode>(nameof(PlaylistMode));
                    PlaylistSongLoops = mapping.GetValidValue(nameof(PlaylistSongLoops), 0, long.MaxValue);
                    PlaylistFadeOutMilliseconds = mapping.GetValidValue(nameof(PlaylistFadeOutMilliseconds), 0, long.MaxValue);
                    MiddleCOctave = (sbyte)mapping.GetValidValue(nameof(MiddleCOctave), sbyte.MinValue, sbyte.MaxValue);

                    var cmap = (YamlMappingNode)mapping.Children[nameof(Colors)];
                    Colors = new HSLColor[256];
                    foreach (KeyValuePair<YamlNode, YamlNode> c in cmap)
                    {
                        int i = (int)Utils.ParseValue(string.Format(Strings.ConfigKeySubkey, nameof(Colors)), c.Key.ToString(), 0, 127);
                        if (Colors[i] != null)
                        {
                            throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigColorRepeated, i)));
                        }
                        double h = 0, s = 0, l = 0;
                        foreach (KeyValuePair<YamlNode, YamlNode> v in ((YamlMappingNode)c.Value).Children)
                        {
                            string key = v.Key.ToString();
                            string valueName = string.Format(Strings.ConfigKeySubkey, string.Format("{0} {1}", nameof(Colors), i));
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
                                throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigColorInvalidKey, i)));
                            }
                        }
                        var co = new HSLColor(h, s, l);
                        Colors[i] = co;
                        Colors[i + 128] = co;
                    }
                    for (int i = 0; i < Colors.Length; i++)
                    {
                        if (Colors[i] == null)
                        {
                            throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigColorMissing, i)));
                        }
                    }
                }
                catch (BetterKeyNotFoundException ex)
                {
                    throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyMissing, ex.Key)));
                }
                catch (Exception ex) when (ex is InvalidValueException || ex is YamlDotNet.Core.YamlException)
                {
                    throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + ex.Message));
                }
            }
        }

        public static void Init()
        {
            Instance = new GlobalConfig();
        }
    }
}
