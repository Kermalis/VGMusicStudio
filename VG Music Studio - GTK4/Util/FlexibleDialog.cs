using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Adw;
using Kermalis.VGMusicStudio.Core;

namespace Kermalis.VGMusicStudio.GTK4.Util;


internal sealed class FlexibleDialog
{
	public static ResponseSelected Response = ResponseSelected.None;
	public static event Action<ResponseSelected>? OnResponse;
	private static event Action? ClickedResult;
	private static ISOLanguageNameID LanguageID = ISOLanguageNameID.en;

	private static readonly Gdk.Clipboard Clipboard = Gdk.Display.GetDefault()!.GetClipboard();
	private static string? ExceptionDetails;
	private static readonly string[] Button_Labels_English_EN = ["OK", "Cancel", "Yes", "No", "Copy", "Abort", "Terminate", "Retry", "Ignore"];
	private static readonly string[] Button_Labels_German_DE = ["OK", "Abbrechen", "Ja", "Nein", "Kopie", "Abbrechen", "Beenden", "Wiederholen", "Ignorieren"];
	private static readonly string[] Button_Labels_Spanish_ES = ["Aceptar", "Cancelar", "Sí", "No", "Copiar", "Abortar", "Terminar", "Reintentar", "Ignorar"];
	public static readonly string[] Button_Labels_French_FR = ["Approuvé", "Annuler", "Oui", "Non", "Copie", "Avorter", "Terminer", "Refaire", "Ignorer"];
	private static readonly string[] Button_Labels_Italian_IT = ["OK", "Annulla", "Sì", "No", "Copia", "Interrompi", "Terminare", "Riprova", "Ignora"];
	public static readonly string[] Button_Labels_Russian_RU = ["Есть", "Отмена", "Да", "Нет", "Копия", "Стоп", "Прекратить", "Переделать", "Игнорировать"];

	private enum ButtonID
	{
		OK = 0,
		Cancel,
		Yes,
		No,
		Copy,
		Abort,
		Terminate,
		Retry,
		Ignore
	};

	public enum ButtonsType
	{
		AbortRetryIgnore,
		OKCancel,
		RetryCancel,
		YesNo,
		YesNoCancel,
		TerminateCopyOK,
		OK
	}

	public enum ResponseSelected
	{
		None,
		OK,
		Cancel,
		Abort,
		Retry,
		Ignore,
		Yes,
		No
	}

	public enum DialogType
	{
		Exception,
		AlertDialog
	}

	public enum DefaultButton
	{
		First,
		Second,
		Third,
		Fourth
	}

	private enum ISOLanguageNameID
	{
		en,
		de,
		es,
		fr,
		it,
		ru
	}

	public static void Show(Exception ex, string heading)
	{
		Show(string.Empty, heading, ButtonsType.OK, Gtk.MessageType.Other, DefaultButton.Third, ex);
	}
	public static void Show(string text, string heading = "", ButtonsType buttonsType = ButtonsType.OK, Gtk.MessageType icon = Gtk.MessageType.Other, DefaultButton defaultButton = DefaultButton.First, Exception ex = null!, Window? parent = null)
	{
		Response = ResponseSelected.None;
		parent ??= MainWindow.Instance!;
		CreateDialog(parent, text, heading, buttonsType, icon, ex!);
	}

	private static void CreateDialog(Window parent, string text, string heading, ButtonsType buttonsType, Gtk.MessageType icon, Exception ex)
	{
		_ = Enum.TryParse(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, out LanguageID);
		Gtk.Widget dialog;
		if (ex is not null)
		{
			dialog = Dialog.New();
			var d = dialog as Dialog;
			AddContent(d!, heading, buttonsType, ex);
			d!.Present(parent);
		}
		else
		{
			dialog = AlertDialog.New(heading, text);
			var d = dialog as AlertDialog;
			AddButtons(d!, DialogType.AlertDialog, buttonsType);
			d!.Present(parent);
		}
	}

	private static void AddContent(Dialog dialog, string heading, ButtonsType buttonsType, Exception ex)
	{
		var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
		mainBox.SetBaselinePosition(Gtk.BaselinePosition.Center);
		mainBox.SetHalign(Gtk.Align.Center);
		mainBox.SetMarginTop(20);
		mainBox.SetMarginStart(20);
		mainBox.SetMarginBottom(20);
		mainBox.SetMarginEnd(20);

		var headerIconBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 30);
		headerIconBox.SetHalign(Gtk.Align.Center);
		var buttonBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 20);
		buttonBox.SetBaselinePosition(Gtk.BaselinePosition.Bottom);
		buttonBox.SetHalign(Gtk.Align.Center);

		var image = Gtk.Image.NewFromIconName("dialog-warning-symbolic");
		image.SetPixelSize(48);
		image.SetSizeRequest(48, 48);

		var header = Gtk.Label.New("An unhandled exception has occurred.");
		header.SetMarkup($"<span font=\"16\"><b>{header.Label_}</b></span>");
		header.SetHalign(Gtk.Align.Center);

		var scrolledWindow = Gtk.ScrolledWindow.New();

		ExceptionDetails = $"{heading}\n\n-------------\n\n {string.Format("Error Details:{1}{1}{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace)}";

		var textTag = Gtk.TextTag.New("Error");

		var textTagTable = Gtk.TextTagTable.New();
		textTagTable.Add(textTag);
		var textBuffer = Gtk.TextBuffer.New(textTagTable);
		textBuffer.SetText(ExceptionDetails, ExceptionDetails.Length);
		var textView = Gtk.TextView.NewWithBuffer(textBuffer);
		textView.SetEditable(false);

		scrolledWindow.SetChild(textView);
		scrolledWindow.SetSizeRequest(500, 180);

		AddButtons(dialog, DialogType.Exception, ButtonsType.TerminateCopyOK, buttonBox);

		headerIconBox.Append(image);
		headerIconBox.Append(header);
		mainBox.Append(headerIconBox);
		mainBox.Append(scrolledWindow);
		mainBox.Append(buttonBox);

		dialog.SetChild(mainBox);
	}

	private static string GetButtonLabel(ButtonID buttonID)
	{
		int buttonLabelIndex = Convert.ToInt32(buttonID);

		return LanguageID switch
		{
			ISOLanguageNameID.de => Button_Labels_German_DE[buttonLabelIndex],
			ISOLanguageNameID.es => Button_Labels_Spanish_ES[buttonLabelIndex],
			ISOLanguageNameID.fr => Button_Labels_French_FR[buttonLabelIndex],
			ISOLanguageNameID.it => Button_Labels_Italian_IT[buttonLabelIndex],
			ISOLanguageNameID.ru => Button_Labels_Russian_RU[buttonLabelIndex],
			_ => Button_Labels_English_EN[buttonLabelIndex],
		};
	}

	private static void AddButtons(object dialog, DialogType dialogType, ButtonsType buttonsType = ButtonsType.OK, Gtk.Box box = null!)
	{
		switch (dialogType)
		{
			case DialogType.Exception:
				{
					var d = (Dialog)dialog;

					var buttonTerminate = Gtk.Button.New();
					buttonTerminate.SetLabel(GetButtonLabel(ButtonID.Terminate));
					buttonTerminate.SetName("_buttonTerminate");
					buttonTerminate.AddCssClass("destructive-action");
					buttonTerminate.OnClicked += ResponseClicked;

					var buttonCopy = Gtk.Button.New();
					buttonCopy.SetLabel(GetButtonLabel(ButtonID.Copy));
					buttonCopy.SetName("_buttonCopy");
					buttonCopy.OnClicked += ResponseClicked;

					var buttonOK = Gtk.Button.New();
					buttonOK.SetLabel(GetButtonLabel(ButtonID.OK));
					buttonOK.SetName("_buttonOK");
					buttonOK.SetReceivesDefault(true);
					buttonOK.OnClicked += ResponseClicked;

					ClickedResult += OnClose;

					void OnClose()
					{
						ClickedResult -= OnClose;
						d!.Close();
					}

					box.Append(buttonTerminate);
					box.Append(buttonCopy);
					box.Append(buttonOK);

					dialog = d;
					break;
				}
			case DialogType.AlertDialog:
				{
					var d = (AlertDialog)dialog;
					var id = $"_{ButtonID.OK}";
					switch (buttonsType)
					{
						case ButtonsType.AbortRetryIgnore:
							{
								d.AddResponse($"_{ButtonID.Abort}", GetButtonLabel(ButtonID.Abort));
								d.AddResponse($"_{ButtonID.Retry}", GetButtonLabel(ButtonID.Retry));
								d.AddResponse($"_{ButtonID.Ignore}", GetButtonLabel(ButtonID.Ignore));
								d.SetDefaultResponse($"_{ButtonID.Abort}");
								d.SetCloseResponse($"_{ButtonID.Abort}");
								d.OnResponse += ResponseClicked;
								break;
							}
						case ButtonsType.OKCancel:
							{
								d.AddResponse($"_{ButtonID.OK}", GetButtonLabel(ButtonID.OK));
								d.AddResponse($"_{ButtonID.Cancel}", GetButtonLabel(ButtonID.Cancel));
								d.SetDefaultResponse($"_{ButtonID.OK}");
								d.SetCloseResponse($"_{ButtonID.Cancel}");
								d.OnResponse += ResponseClicked;
								break;
							}
						case ButtonsType.RetryCancel:
							{
								d.AddResponse($"_{ButtonID.Retry}", GetButtonLabel(ButtonID.Retry));
								d.AddResponse($"_{ButtonID.Cancel}", GetButtonLabel(ButtonID.Cancel));
								d.SetDefaultResponse($"_{ButtonID.Retry}");
								d.SetCloseResponse($"_{ButtonID.Cancel}");
								d.OnResponse += ResponseClicked;
								break;
							}
						case ButtonsType.YesNo:
							{
								d.AddResponse($"_{ButtonID.Yes}", GetButtonLabel(ButtonID.Yes));
								d.AddResponse($"_{ButtonID.No}", GetButtonLabel(ButtonID.No));
								d.SetDefaultResponse($"_{ButtonID.Yes}");
								d.SetCloseResponse($"_{ButtonID.No}");
								d.OnResponse += ResponseClicked;
								break;
							}
						case ButtonsType.YesNoCancel:
							{
								d.AddResponse($"_{ButtonID.Yes}", GetButtonLabel(ButtonID.Yes));
								d.AddResponse($"_{ButtonID.No}", GetButtonLabel(ButtonID.No));
								d.AddResponse($"_{ButtonID.Cancel}", GetButtonLabel(ButtonID.Cancel));
								d.SetDefaultResponse($"_{ButtonID.Yes}");
								d.SetCloseResponse($"_{ButtonID.Cancel}");
								d.OnResponse += ResponseClicked;
								break;
							}
						case ButtonsType.OK:
							{
								d.AddResponse($"_{ButtonID.OK}", GetButtonLabel(ButtonID.OK));
								d.SetDefaultResponse($"_{ButtonID.OK}");
								d.SetCloseResponse($"_{ButtonID.OK}");
								d.OnResponse += ResponseClicked;
								break;
							}
					}
					dialog = d;
					break;
				}
		}
	}

	private static void ResponseClicked(object sender, EventArgs args)
	{
		if (sender is Gtk.Button button)
		{
			switch (button.Name)
			{
				case "_buttonTerminate":
					{
						button.OnClicked -= ResponseClicked;
						ClickedResult!.Invoke();
						Engine.Instance?.Dispose();
						MainWindow.Instance!.Close();
						break;
					}
				case "_buttonCopy":
					{
						Clipboard.SetText(ExceptionDetails!);
						break;
					}
				case "_buttonOK":
					{
						button.OnClicked -= ResponseClicked;
						Response = ResponseSelected.OK;
						ClickedResult!.Invoke();
						break;
					}
			}
		}
		else if (sender is AlertDialog alertDialog)
		{
			alertDialog.OnResponse -= ResponseClicked;
			if (args is AlertDialog.ResponseSignalArgs arg)
			{
				if (arg.Response == $"_{ButtonID.Abort}")
				{
					Response = ResponseSelected.Abort;
				}
				else if (arg.Response == $"_{ButtonID.Retry}")
				{
					Response = ResponseSelected.Retry;
				}
				else if (arg.Response == $"_{ButtonID.Ignore}")
				{
					Response = ResponseSelected.Ignore;
				}
				else if (arg.Response == $"_{ButtonID.OK}")
				{
					Response = ResponseSelected.OK;
				}
				else if (arg.Response == $"_{ButtonID.Cancel}")
				{
					Response = ResponseSelected.Cancel;
				}
				else if (arg.Response == $"_{ButtonID.Yes}")
				{
					Response = ResponseSelected.Yes;
				}
				else if (arg.Response == $"_{ButtonID.No}")
				{
					Response = ResponseSelected.No;
				}
			}
			if (OnResponse is not null)
			{
				OnResponse!.Invoke(Response);
			}
			alertDialog.Close();
		}
	}
}
