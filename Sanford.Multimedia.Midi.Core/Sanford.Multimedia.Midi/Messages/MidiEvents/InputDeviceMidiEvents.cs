
using System;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// MidiSignal provides all midi events from an input device
	/// </summary>
	public class InputDeviceMidiEvents : MidiEvents
	{
		readonly InputDevice FInDevice;

		public int DeviceID {
			get {
				if (FInDevice != null) {
					return FInDevice.DeviceID;
				}
				else {
					return -1;
				}
			}
		}

		/// <summary>
		/// Create Midisignal with an input device which fires the events
		/// </summary>
		/// <param name="inDevice"></param>
		public InputDeviceMidiEvents(InputDevice inDevice)
		{
			FInDevice = inDevice;
			FInDevice.StartRecording();
		}

		public void Dispose()
		{
			FInDevice.Dispose();
		}

		public static InputDeviceMidiEvents FromDeviceID(int deviceID)
		{
			var deviceCount = InputDevice.DeviceCount;
			if (deviceCount > 0)
            {
				deviceID %= deviceCount;
				return new InputDeviceMidiEvents(new InputDevice(deviceID));
			}
			return null;
		}

        /// <summary>
		/// All incoming midi messages in short format
		/// </summary>
		public event MidiMessageEventHandler MessageReceived
        {
            add
            {
                FInDevice.MessageReceived += value;
            }
            remove
            {
                FInDevice.MessageReceived -= value;
            }
        }

        /// <summary>
        /// All incoming midi messages in short format
        /// </summary>
        public event EventHandler<ShortMessageEventArgs> ShortMessageReceived {
			add {
				FInDevice.ShortMessageReceived += value;
			}
			remove {
				FInDevice.ShortMessageReceived -= value;
			}
		}

		/// <summary>
		/// Channel messages like, note, controller, program, ...
		/// </summary>
		public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived {
			add {
				FInDevice.ChannelMessageReceived += value;
			}
			remove {
				FInDevice.ChannelMessageReceived -= value;
			}
		}

		/// <summary>
		/// SysEx messages
		/// </summary>
		public event EventHandler<SysExMessageEventArgs> SysExMessageReceived {
			add {
				FInDevice.SysExMessageReceived += value;
			}
			remove {
				FInDevice.SysExMessageReceived -= value;
			}
		}

		/// <summary>
		/// Midi timecode, song position, song select, tune request
		/// </summary>
		public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived {
			add {
				FInDevice.SysCommonMessageReceived += value;
			}
			remove {
				FInDevice.SysCommonMessageReceived -= value;
			}
		}

		/// <summary>
		/// Timing events, midi clock, start, stop, reset, active sense, tick
		/// </summary>
		public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived {
			add {
				FInDevice.SysRealtimeMessageReceived += value;
			}
			remove {
				FInDevice.SysRealtimeMessageReceived -= value;
			}
		}
	}
}


