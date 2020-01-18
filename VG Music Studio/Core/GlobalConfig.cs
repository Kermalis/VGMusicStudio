using Kermalis.VGMusicStudio.Properties;
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
                    try
                    {
                        PlaylistMode = (PlaylistMode)Enum.Parse(typeof(PlaylistMode), mapping.Children.GetValue(nameof(PlaylistMode)).ToString());
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                    {
                        throw new Exception(string.Format(Strings.ErrorParseConfig, configFile, Environment.NewLine + string.Format(Strings.ErrorConfigKeyInvalid, nameof(PlaylistMode))));
                    }
                    PlaylistSongLoops = mapping.GetValidValue(nameof(PlaylistSongLoops), 0, long.MaxValue);
                    PlaylistFadeOutMilliseconds = mapping.GetValidValue(nameof(PlaylistFadeOutMilliseconds), 0, long.MaxValue);

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
                        Colors[i] = Colors[i + 128] = new HSLColor(h, s, l);
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
