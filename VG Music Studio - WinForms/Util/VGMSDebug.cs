#if DEBUG
using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.WinForms.Util;

internal static class VGMSDebug
{
	// TODO: Update
	/*public static void MIDIVolumeMerger(string f1, string f2)
	{
		var midi1 = new MIDIFile(f1);
		var midi2 = new MIDIFile(f2);
		var baby = new MIDIFile(midi1.Division);

		for (int i = 0; i < midi1.Count; i++)
		{
			Track midi1Track = midi1[i];
			Track midi2Track = midi2[i];
			var babyTrack = new Track();
			baby.Add(babyTrack);

			for (int j = 0; j < midi1Track.Count; j++)
			{
				MidiEvent e1 = midi1Track.GetMidiEvent(j);
				if (e1.MidiMessage is ChannelMessage cm1 && cm1.Command == ChannelCommand.Controller && cm1.Data1 == (int)ControllerType.Volume)
				{
					MidiEvent e2 = midi2Track.GetMidiEvent(j);
					var cm2 = (ChannelMessage)e2.MidiMessage;
					babyTrack.Insert(e1.AbsoluteTicks, new ChannelMessage(ChannelCommand.Controller, cm1.MidiChannel, (int)ControllerType.Volume, Math.Max(cm1.Data2, cm2.Data2)));
				}
				else
				{
					babyTrack.Insert(e1.AbsoluteTicks, e1.MidiMessage);
				}
			}
		}

		baby.Save(f1);
		baby.Save(f2);
	}*/

	public static void EventScan(List<Config.Song> songs, bool showIndexes)
	{
		Console.WriteLine($"{nameof(EventScan)} started.");
		var scans = new Dictionary<string, List<Config.Song>>();
		Player player = Engine.Instance!.Player;
		foreach (Config.Song song in songs)
		{
			try
			{
				player.LoadSong(song.Index);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception loading {0} - {1}", showIndexes ? $"song {song.Index}" : $"\"{song.Name}\"", ex.Message);
				continue;
			}

			if (player.LoadedSong is null)
			{
				continue;
			}

			foreach (string cmd in player.LoadedSong.Events.Where(ev => ev is not null).SelectMany(ev => ev).Select(ev => ev.Command.Label).Distinct())
			{
				if (!scans.TryGetValue(cmd, out List<Config.Song>? list))
				{
					list = new List<Config.Song>();
					scans.Add(cmd, list);
				}
				list.Add(song);
			}
		}
		foreach (KeyValuePair<string, List<Config.Song>> kvp in scans.OrderBy(k => k.Key))
		{
			Console.WriteLine("{0} ({1})", kvp.Key, showIndexes ? string.Join(", ", kvp.Value.Select(s => s.Index)) : string.Join(", ", kvp.Value.Select(s => s.Name)));
		}
		Console.WriteLine($"{nameof(EventScan)} ended.");
	}

	public static void GBAGameCodeScan(string path)
	{
		Console.WriteLine($"{nameof(GBAGameCodeScan)} started.");
		string[] files = Directory.GetFiles(path, "*.gba", SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++)
		{
			string file = files[i];
			try
			{
				using (FileStream stream = File.OpenRead(file))
				{
					var reader = new EndianBinaryReader(stream, ascii: true);
					stream.Position = 0xAC;
					string gameCode = reader.ReadString_Count(3);
					stream.Position = 0xAF;
					char regionCode = reader.ReadChar();
					stream.Position = 0xBC;
					byte version = reader.ReadByte();
					files[i] = string.Format("Code: {0}\tRegion: {1}\tVersion: {2}\tFile: {3}", gameCode, regionCode, version, file);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception loading \"{0}\" - {1}", file, ex.Message);
			}
		}

		Array.Sort(files);
		for (int i = 0; i < files.Length; i++)
		{
			Console.WriteLine(files[i]);
		}
		Console.WriteLine($"{nameof(GBAGameCodeScan)} ended.");
	}

	public static void SimulateLanguage(string lang)
	{
		Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
	}
}
#endif