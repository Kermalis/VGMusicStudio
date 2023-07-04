using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Adw;

namespace Kermalis.VGMusicStudio.GTK4.Util;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * FlexibleMessageBox
 * 
 * A Message Box completely rewritten by Davin (Platinum Lucario) for use with Gir.Core (GTK4 and LibAdwaita)
 * on VG Music Studio, modified from the WinForms-based FlexibleMessageBox originally made by Jörg Reichert.
 * 
 * This uses Adw.Window to create a window similar to MessageDialog, since
 * MessageDialog and many Gtk.Dialog functions are deprecated since GTK version 4.10,
 * Adw.Window and Gtk.Window are better supported (and probably won't be deprecated until several major versions later).
 * 
 * Features include:
 * - Extra options for a dialog box style Adw.Window with the Show() function
 * - Displays a vertical scrollbar, just like the original one did
 * - Only one source file is used
 * - Much less lines of code than the original, due to built-in GTK4 and LibAdwaita functions
 * - All WinForms functions removed and replaced with GObject library functions via Gir.Core
 * 
 * GitHub: https://github.com/PlatinumLucario
 * Repository: https://github.com/PlatinumLucario/VGMusicStudio/
 * 
 *		| Original Author can be found below: |
 *		v									  v
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#region Original Author
/*  FlexibleMessageBox – A flexible replacement for the .NET MessageBox
     * 
     *  Author:         Jörg Reichert (public@jreichert.de)
     *  Contributors:   Thanks to: David Hall, Roink
     *  Version:        1.3
     *  Published at:   http://www.codeproject.com/Articles/601900/FlexibleMessageBox
     *  
     ************************************************************************************************************
     * Features:
     *  - It can be simply used instead of MessageBox since all important static "Show"-Functions are supported
     *  - It is small, only one source file, which could be added easily to each solution 
     *  - It can be resized and the content is correctly word-wrapped
     *  - It tries to auto-size the width to show the longest text row
     *  - It never exceeds the current desktop working area
     *  - It displays a vertical scrollbar when needed
     *  - It does support hyperlinks in text
     * 
     *  Because the interface is identical to MessageBox, you can add this single source file to your project 
     *  and use the FlexibleMessageBox almost everywhere you use a standard MessageBox. 
     *  The goal was NOT to produce as many features as possible but to provide a simple replacement to fit my 
     *  own needs. Feel free to add additional features on your own, but please left my credits in this class.
     * 
     ************************************************************************************************************
     * Usage examples:
     * 
     *  FlexibleMessageBox.Show("Just a text");
     * 
     *  FlexibleMessageBox.Show("A text", 
     *                          "A caption"); 
     *  
     *  FlexibleMessageBox.Show("Some text with a link: www.google.com", 
     *                          "Some caption",
     *                          MessageBoxButtons.AbortRetryIgnore, 
     *                          MessageBoxIcon.Information,
     *                          MessageBoxDefaultButton.Button2);
     *  
     *  var dialogResult = FlexibleMessageBox.Show("Do you know the answer to life the universe and everything?", 
     *                                             "One short question",
     *                                             MessageBoxButtons.YesNo);     
     * 
     ************************************************************************************************************
     *  THE SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS", WITHOUT WARRANTY
     *  OF ANY KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHOR BE
     *  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY ARISING FROM,
     *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OF THIS
     *  SOFTWARE.
     *  
     ************************************************************************************************************
     * History:
     *  Version 1.3 - 19.Dezember 2014
     *  - Added refactoring function GetButtonText()
     *  - Used CurrentUICulture instead of InstalledUICulture
     *  - Added more button localizations. Supported languages are now: ENGLISH, GERMAN, SPANISH, ITALIAN
     *  - Added standard MessageBox handling for "copy to clipboard" with <Ctrl> + <C> and <Ctrl> + <Insert>
     *  - Tab handling is now corrected (only tabbing over the visible buttons)
     *  - Added standard MessageBox handling for ALT-Keyboard shortcuts
     *  - SetDialogSizes: Refactored completely: Corrected sizing and added caption driven sizing
     * 
     *  Version 1.2 - 10.August 2013
     *   - Do not ShowInTaskbar anymore (original MessageBox is also hidden in taskbar)
     *   - Added handling for Escape-Button
     *   - Adapted top right close button (red X) to behave like MessageBox (but hidden instead of deactivated)
     * 
     *  Version 1.1 - 14.June 2013
     *   - Some Refactoring
     *   - Added internal form class
     *   - Added missing code comments, etc.
     *  
     *  Version 1.0 - 15.April 2013
     *   - Initial Version
    */
#endregion

internal class FlexibleMessageBox
{
	#region Public statics

	/// <summary>
	/// Defines the maximum width for all FlexibleMessageBox instances in percent of the working area.
	/// 
	/// Allowed values are 0.2 - 1.0 where: 
	/// 0.2 means:  The FlexibleMessageBox can be at most half as wide as the working area.
	/// 1.0 means:  The FlexibleMessageBox can be as wide as the working area.
	/// 
	/// Default is: 70% of the working area width.
	/// </summary>
	//public static double MAX_WIDTH_FACTOR = 0.7;

	/// <summary>
	/// Defines the maximum height for all FlexibleMessageBox instances in percent of the working area.
	/// 
	/// Allowed values are 0.2 - 1.0 where: 
	/// 0.2 means:  The FlexibleMessageBox can be at most half as high as the working area.
	/// 1.0 means:  The FlexibleMessageBox can be as high as the working area.
	/// 
	/// Default is: 90% of the working area height.
	/// </summary>
	//public static double MAX_HEIGHT_FACTOR = 0.9;

	/// <summary>
	/// Defines the font for all FlexibleMessageBox instances.
	/// 
	/// Default is: Theme.Font
	/// </summary>
	//public static Font FONT = Theme.Font;

	#endregion

	#region Public show functions

	public static Gtk.ResponseType Show(string text)
	{
		return FlexibleMessageBoxWindow.Show(null, text, string.Empty, Gtk.ButtonsType.Ok, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(Window owner, string text)
	{
		return FlexibleMessageBoxWindow.Show(owner, text, string.Empty, Gtk.ButtonsType.Ok, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(string text, string caption)
	{
		return FlexibleMessageBoxWindow.Show(null, text, caption, Gtk.ButtonsType.Ok, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(Exception ex, string caption)
	{
		return FlexibleMessageBoxWindow.Show(null, string.Format("Error Details:{1}{1}{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace), caption, Gtk.ButtonsType.Ok, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(Window owner, string text, string caption)
	{
		return FlexibleMessageBoxWindow.Show(owner, text, caption, Gtk.ButtonsType.Ok, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(string text, string caption, Gtk.ButtonsType buttons)
	{
		return FlexibleMessageBoxWindow.Show(null, text, caption, buttons, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(Window owner, string text, string caption, Gtk.ButtonsType buttons)
	{
		return FlexibleMessageBoxWindow.Show(owner, text, caption, buttons, Gtk.MessageType.Other, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(string text, string caption, Gtk.ButtonsType buttons, Gtk.MessageType icon)
	{
		return FlexibleMessageBoxWindow.Show(null, text, caption, buttons, icon, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(Window owner, string text, string caption, Gtk.ButtonsType buttons, Gtk.MessageType icon)
	{
		return FlexibleMessageBoxWindow.Show(owner, text, caption, buttons, icon, Gtk.ResponseType.Ok);
	}
	public static Gtk.ResponseType Show(string text, string caption, Gtk.ButtonsType buttons, Gtk.MessageType icon, Gtk.ResponseType defaultButton)
	{
		return FlexibleMessageBoxWindow.Show(null, text, caption, buttons, icon, defaultButton);
	}
	public static Gtk.ResponseType Show(Window owner, string text, string caption, Gtk.ButtonsType buttons, Gtk.MessageType icon, Gtk.ResponseType defaultButton)
	{
		return FlexibleMessageBoxWindow.Show(owner, text, caption, buttons, icon, defaultButton);
	}

	#endregion

	#region Internal form classes

	internal sealed class FlexibleButton : Gtk.Button
	{
		public Gtk.ButtonsType ButtonsType;
		public Gtk.ResponseType ResponseType;

		private FlexibleButton()
		{
			ResponseType = new Gtk.ResponseType();
		}
	}

    internal sealed class FlexibleContentBox : Gtk.Box
	{
		public Gtk.Text Text;

		private FlexibleContentBox()
		{
			Text = Gtk.Text.New();
		}
	}

	class FlexibleMessageBoxWindow : Window
	{
		//IContainer components = null;

		protected void Dispose(bool disposing)
		{
			if (disposing && richTextBoxMessage != null)
			{
				richTextBoxMessage.Dispose();
			}
			base.Dispose();
		}
		void InitializeComponent()
		{
			//components = new Container();
            richTextBoxMessage = (FlexibleContentBox)Gtk.Box.New(Gtk.Orientation.Vertical, 0);
            button1 = (FlexibleButton)Gtk.Button.New();
			//FlexibleMessageBoxFormBindingSource = new BindingSource(components);
			panel1 = (FlexibleContentBox)Gtk.Box.New(Gtk.Orientation.Vertical, 0);
			pictureBoxForIcon = Gtk.Image.New();
			button2 = (FlexibleButton)Gtk.Button.New();
			button3 = (FlexibleButton)Gtk.Button.New();
			//((ISupportInitialize)FlexibleMessageBoxFormBindingSource).BeginInit();
			//panel1.SuspendLayout();
			//((ISupportInitialize)pictureBoxForIcon).BeginInit();
			//SuspendLayout();
			// 
			// button1
			// 
			//button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			//button1.AutoSize = true;
			button1.ResponseType = Gtk.ResponseType.Ok;
			//button1.Location = new Point(11, 67);
			//button1.MinimumSize = new Size(0, 24);
			button1.Name = "button1";
			//button1.Size = new Size(75, 24);
			button1.WidthRequest = 75;
			button1.HeightRequest = 24;
			//button1.TabIndex = 2;
			button1.Label = "OK";
			//button1.UseVisualStyleBackColor = true;
			button1.Visible = false;
			// 
			// richTextBoxMessage
			// 
			//richTextBoxMessage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
			//| AnchorStyles.Left
			//| AnchorStyles.Right;
			//richTextBoxMessage.BorderStyle = BorderStyle.None;
			richTextBoxMessage.BindProperty("Text", FlexibleMessageBoxFormBindingSource, "MessageText", GObject.BindingFlags.Default);
			//richTextBoxMessage.Font = new Font(Theme.Font.FontFamily, 9);
			//richTextBoxMessage.Location = new Point(50, 26);
			//richTextBoxMessage.Margin = new Padding(0);
			richTextBoxMessage.Name = "richTextBoxMessage";
			//richTextBoxMessage.ReadOnly = true;
			richTextBoxMessage.Text.Editable = false;
			//richTextBoxMessage.ScrollBars = RichTextBoxScrollBars.Vertical;
			scrollbar = Gtk.Scrollbar.New(Gtk.Orientation.Vertical, null);
			scrollbar.SetParent(richTextBoxMessage);
			//richTextBoxMessage.Size = new Size(200, 20);
			richTextBoxMessage.WidthRequest = 200;
			richTextBoxMessage.HeightRequest = 20;
            //richTextBoxMessage.TabIndex = 0;
            //richTextBoxMessage.TabStop = false;
            richTextBoxMessage.Text.SetText("<Message>");
			//richTextBoxMessage.LinkClicked += new LinkClickedEventHandler(LinkClicked);
			// 
			// panel1
			// 
			//panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
			//| AnchorStyles.Left
			//| AnchorStyles.Right;
			//panel1.Controls.Add(pictureBoxForIcon);
			panel1.Append(pictureBoxForIcon);
			//panel1.Controls.Add(richTextBoxMessage);
			panel1.Append(richTextBoxMessage);
			//panel1.Location = new Point(-3, -4);
			panel1.Name = "panel1";
			//panel1.Size = new Size(268, 59);
			panel1.WidthRequest = 268;
			panel1.HeightRequest = 59;
			//panel1.TabIndex = 1;
			// 
			// pictureBoxForIcon
			// 
			//pictureBoxForIcon.BackColor = Color.Transparent;
			//pictureBoxForIcon.Location = new Point(15, 19);
			pictureBoxForIcon.Name = "pictureBoxForIcon";
			//pictureBoxForIcon.Size = new Size(32, 32);
			pictureBoxForIcon.WidthRequest = 32;
			pictureBoxForIcon.HeightRequest = 32;
			//pictureBoxForIcon.TabIndex = 8;
			//pictureBoxForIcon.TabStop = false;
			// 
			// button2
			// 
			//button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			button2.ResponseType = Gtk.ResponseType.Ok;
			//button2.Location = new Point(92, 67);
			//button2.MinimumSize = new Size(0, 24);
			button2.Name = "button2";
            //button2.Size = new Size(75, 24);
            button2.WidthRequest = 75;
            button2.HeightRequest = 24;
            //button2.TabIndex = 3;
            button2.Label = "OK";
			//button2.UseVisualStyleBackColor = true;
			button2.Visible = false;
			// 
			// button3
			// 
			//button3.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			//button3.AutoSize = true;
			button3.ResponseType = Gtk.ResponseType.Ok;
			//button3.Location = new Point(173, 67);
			//button3.MinimumSize = new Size(0, 24);
			button3.Name = "button3";
            //button3.Size = new Size(75, 24);
            button3.WidthRequest = 75;
            button3.HeightRequest = 24;
            //button3.TabIndex = 0;
            button3.Label = "OK";
			//button3.UseVisualStyleBackColor = true;
			button3.Visible = false;
			// 
			// FlexibleMessageBoxForm
			// 
			//AutoScaleDimensions = new SizeF(6F, 13F);
			//AutoScaleMode = AutoScaleMode.Font;
			//ClientSize = new Size(260, 102);
			//Controls.Add(button3);
			SetChild(button3);
			//Controls.Add(button2);
			SetChild(button2);
			//Controls.Add(panel1);
			SetChild(panel1);
			//Controls.Add(button1);
			SetChild(button1);
			//DataBindings.Add(new Binding("Text", FlexibleMessageBoxFormBindingSource, "CaptionText", true));
			//Icon = Properties.Resources.Icon;
			//MaximizeBox = false;
			//MinimizeBox = false;
			//MinimumSize = new Size(276, 140);
			//Name = "FlexibleMessageBoxForm";
			//SizeGripStyle = SizeGripStyle.Show;
			//StartPosition = FormStartPosition.CenterParent;
			//Text = "<Caption>";
			//Shown += new EventHandler(FlexibleMessageBoxForm_Shown);
			//((ISupportInitialize)FlexibleMessageBoxFormBindingSource).EndInit();
			//panel1.ResumeLayout(false);
			//((ISupportInitialize)pictureBoxForIcon).EndInit();
			//ResumeLayout(false);
			//PerformLayout();
		}

		private FlexibleButton button1, button2, button3;
		private GObject.Object FlexibleMessageBoxFormBindingSource;
		private FlexibleContentBox richTextBoxMessage, panel1;
		private Gtk.Scrollbar scrollbar;
		private Gtk.Image pictureBoxForIcon;

		#region Private constants

		//These separators are used for the "copy to clipboard" standard operation, triggered by Ctrl + C (behavior and clipboard format is like in a standard MessageBox)
		static readonly string STANDARD_MESSAGEBOX_SEPARATOR_LINES = "---------------------------\n";
		static readonly string STANDARD_MESSAGEBOX_SEPARATOR_SPACES = "   ";

		//These are the possible buttons (in a standard MessageBox)
		private enum ButtonID { OK = 0, CANCEL, YES, NO, ABORT, RETRY, IGNORE };

		//These are the buttons texts for different languages. 
		//If you want to add a new language, add it here and in the GetButtonText-Function
		private enum TwoLetterISOLanguageID { en, de, es, it };
		static readonly string[] BUTTON_TEXTS_ENGLISH_EN = { "OK", "Cancel", "&Yes", "&No", "&Abort", "&Retry", "&Ignore" }; //Note: This is also the fallback language
		static readonly string[] BUTTON_TEXTS_GERMAN_DE = { "OK", "Abbrechen", "&Ja", "&Nein", "&Abbrechen", "&Wiederholen", "&Ignorieren" };
		static readonly string[] BUTTON_TEXTS_SPANISH_ES = { "Aceptar", "Cancelar", "&Sí", "&No", "&Abortar", "&Reintentar", "&Ignorar" };
		static readonly string[] BUTTON_TEXTS_ITALIAN_IT = { "OK", "Annulla", "&Sì", "&No", "&Interrompi", "&Riprova", "&Ignora" };

		#endregion

		#region Private members

		Gtk.ResponseType defaultButton;
		int visibleButtonsCount;
		readonly TwoLetterISOLanguageID languageID = TwoLetterISOLanguageID.en;

		#endregion

		#region Private constructors

		private FlexibleMessageBoxWindow()
		{
			InitializeComponent();

			//Try to evaluate the language. If this fails, the fallback language English will be used
			Enum.TryParse(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, out languageID);

			//KeyPreview = true;
			//KeyUp += FlexibleMessageBoxForm_KeyUp;
		}

		#endregion

		#region Private helper functions

		static string[] GetStringRows(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return null;
			}

			string[] messageRows = message.Split(new char[] { '\n' }, StringSplitOptions.None);
			return messageRows;
		}

		string GetButtonText(ButtonID buttonID)
		{
			int buttonTextArrayIndex = Convert.ToInt32(buttonID);

			switch (languageID)
			{
				case TwoLetterISOLanguageID.de: return BUTTON_TEXTS_GERMAN_DE[buttonTextArrayIndex];
				case TwoLetterISOLanguageID.es: return BUTTON_TEXTS_SPANISH_ES[buttonTextArrayIndex];
				case TwoLetterISOLanguageID.it: return BUTTON_TEXTS_ITALIAN_IT[buttonTextArrayIndex];

				default: return BUTTON_TEXTS_ENGLISH_EN[buttonTextArrayIndex];
			}
		}

		static double GetCorrectedWorkingAreaFactor(double workingAreaFactor)
		{
			const double MIN_FACTOR = 0.2;
			const double MAX_FACTOR = 1.0;

			if (workingAreaFactor < MIN_FACTOR)
			{
				return MIN_FACTOR;
			}

			if (workingAreaFactor > MAX_FACTOR)
			{
				return MAX_FACTOR;
			}

			return workingAreaFactor;
		}

		static void SetDialogStartPosition(FlexibleMessageBoxWindow flexibleMessageBoxForm, Window owner)
		{
			//If no owner given: Center on current screen
			if (owner == null)
			{
				//var screen = Screen.FromPoint(Cursor.Position);
				//flexibleMessageBoxForm.StartPosition = FormStartPosition.Manual;
				//flexibleMessageBoxForm.Left = screen.Bounds.Left + screen.Bounds.Width / 2 - flexibleMessageBoxForm.Width / 2;
				//flexibleMessageBoxForm.Top = screen.Bounds.Top + screen.Bounds.Height / 2 - flexibleMessageBoxForm.Height / 2;
			}
		}

		static void SetDialogSizes(FlexibleMessageBoxWindow flexibleMessageBoxForm, string text, string caption)
		{
			//First set the bounds for the maximum dialog size
			//flexibleMessageBoxForm.MaximumSize = new Size(Convert.ToInt32(SystemInformation.WorkingArea.Width * GetCorrectedWorkingAreaFactor(MAX_WIDTH_FACTOR)),
			//											  Convert.ToInt32(SystemInformation.WorkingArea.Height * GetCorrectedWorkingAreaFactor(MAX_HEIGHT_FACTOR)));

			//Get rows. Exit if there are no rows to render...
			string[] stringRows = GetStringRows(text);
			if (stringRows == null)
			{
				return;
			}

			//Calculate whole text height
			//int textHeight = TextRenderer.MeasureText(text, FONT).Height;

			//Calculate width for longest text line
			//const int SCROLLBAR_WIDTH_OFFSET = 15;
			//int longestTextRowWidth = stringRows.Max(textForRow => TextRenderer.MeasureText(textForRow, FONT).Width);
			//int captionWidth = TextRenderer.MeasureText(caption, SystemFonts.CaptionFont).Width;
			//int textWidth = Math.Max(longestTextRowWidth + SCROLLBAR_WIDTH_OFFSET, captionWidth);

			//Calculate margins
			int marginWidth = flexibleMessageBoxForm.WidthRequest - flexibleMessageBoxForm.richTextBoxMessage.WidthRequest;
			int marginHeight = flexibleMessageBoxForm.HeightRequest - flexibleMessageBoxForm.richTextBoxMessage.HeightRequest;

			//Set calculated dialog size (if the calculated values exceed the maximums, they were cut by windows forms automatically)
			//flexibleMessageBoxForm.Size = new Size(textWidth + marginWidth,
			//									   textHeight + marginHeight);
		}

		static void SetDialogIcon(FlexibleMessageBoxWindow flexibleMessageBoxForm, Gtk.MessageType icon)
		{
			switch (icon)
			{
				case Gtk.MessageType.Info:
					flexibleMessageBoxForm.pictureBoxForIcon.SetFromIconName("dialog-information-symbolic");
					break;
				case Gtk.MessageType.Warning:
					flexibleMessageBoxForm.pictureBoxForIcon.SetFromIconName("dialog-warning-symbolic");
                    break;
				case Gtk.MessageType.Error:
					flexibleMessageBoxForm.pictureBoxForIcon.SetFromIconName("dialog-error-symbolic");
                    break;
				case Gtk.MessageType.Question:
					flexibleMessageBoxForm.pictureBoxForIcon.SetFromIconName("dialog-question-symbolic");
					break;
				default:
					//When no icon is used: Correct placement and width of rich text box.
					flexibleMessageBoxForm.pictureBoxForIcon.Visible = false;
					//flexibleMessageBoxForm.richTextBoxMessage.Left -= flexibleMessageBoxForm.pictureBoxForIcon.Width;
					//flexibleMessageBoxForm.richTextBoxMessage.Width += flexibleMessageBoxForm.pictureBoxForIcon.Width;
					break;
			}
		}

		static void SetDialogButtons(FlexibleMessageBoxWindow flexibleMessageBoxForm, Gtk.ButtonsType buttons, Gtk.ResponseType defaultButton)
		{
			//Set the buttons visibilities and texts
			switch (buttons)
			{
				case 0:
					flexibleMessageBoxForm.visibleButtonsCount = 3;

					flexibleMessageBoxForm.button1.Visible = true;
					flexibleMessageBoxForm.button1.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.ABORT);
					flexibleMessageBoxForm.button1.ResponseType = Gtk.ResponseType.Reject;

					flexibleMessageBoxForm.button2.Visible = true;
					flexibleMessageBoxForm.button2.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.RETRY);
					flexibleMessageBoxForm.button2.ResponseType = Gtk.ResponseType.Ok;

					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.IGNORE);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.Cancel;

					//flexibleMessageBoxForm.ControlBox = false;
					break;

				case (Gtk.ButtonsType)1:
					flexibleMessageBoxForm.visibleButtonsCount = 2;

					flexibleMessageBoxForm.button2.Visible = true;
					flexibleMessageBoxForm.button2.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.OK);
					flexibleMessageBoxForm.button2.ResponseType = Gtk.ResponseType.Ok;

					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.CANCEL);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.Cancel;

					//flexibleMessageBoxForm.CancelButton = flexibleMessageBoxForm.button3;
					break;

				case (Gtk.ButtonsType)2:
					flexibleMessageBoxForm.visibleButtonsCount = 2;

					flexibleMessageBoxForm.button2.Visible = true;
					flexibleMessageBoxForm.button2.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.RETRY);
					flexibleMessageBoxForm.button2.ResponseType = Gtk.ResponseType.Ok;

					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.CANCEL);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.Cancel;

					//flexibleMessageBoxForm.CancelButton = flexibleMessageBoxForm.button3;
					break;

				case (Gtk.ButtonsType)3:
					flexibleMessageBoxForm.visibleButtonsCount = 2;

					flexibleMessageBoxForm.button2.Visible = true;
					flexibleMessageBoxForm.button2.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.YES);
					flexibleMessageBoxForm.button2.ResponseType = Gtk.ResponseType.Yes;

					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.NO);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.No;

					//flexibleMessageBoxForm.ControlBox = false;
					break;

				case (Gtk.ButtonsType)4:
					flexibleMessageBoxForm.visibleButtonsCount = 3;

					flexibleMessageBoxForm.button1.Visible = true;
					flexibleMessageBoxForm.button1.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.YES);
					flexibleMessageBoxForm.button1.ResponseType = Gtk.ResponseType.Yes;

					flexibleMessageBoxForm.button2.Visible = true;
					flexibleMessageBoxForm.button2.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.NO);
					flexibleMessageBoxForm.button2.ResponseType = Gtk.ResponseType.No;

					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.CANCEL);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.Cancel;

					//flexibleMessageBoxForm.CancelButton = flexibleMessageBoxForm.button3;
					break;

				case (Gtk.ButtonsType)5:
				default:
					flexibleMessageBoxForm.visibleButtonsCount = 1;
					flexibleMessageBoxForm.button3.Visible = true;
					flexibleMessageBoxForm.button3.Label = flexibleMessageBoxForm.GetButtonText(ButtonID.OK);
					flexibleMessageBoxForm.button3.ResponseType = Gtk.ResponseType.Ok;

					//flexibleMessageBoxForm.CancelButton = flexibleMessageBoxForm.button3;
					break;
			}

			//Set default button (used in FlexibleMessageBoxWindow_Shown)
			flexibleMessageBoxForm.defaultButton = defaultButton;
		}

		#endregion

		#region Private event handlers

		void FlexibleMessageBoxWindow_Shown(object sender, EventArgs e)
		{
			int buttonIndexToFocus = 1;
			Gtk.Widget buttonToFocus;

			//Set the default button...
			//switch (defaultButton)
			//{
			//	case MessageBoxDefaultButton.Button1:
			//	default:
			//		buttonIndexToFocus = 1;
			//		break;
			//	case MessageBoxDefaultButton.Button2:
			//		buttonIndexToFocus = 2;
			//		break;
			//	case MessageBoxDefaultButton.Button3:
			//		buttonIndexToFocus = 3;
			//		break;
			//}

			if (buttonIndexToFocus > visibleButtonsCount)
			{
				buttonIndexToFocus = visibleButtonsCount;
			}

			if (buttonIndexToFocus == 3)
			{
				buttonToFocus = button3;
			}
			else if (buttonIndexToFocus == 2)
			{
				buttonToFocus = button2;
			}
			else
			{
				buttonToFocus = button1;
			}

			buttonToFocus.IsFocus();
		}

		//void LinkClicked(object sender, LinkClickedEventArgs e)
		//{
		//	try
		//	{
		//		Cursor.Current = Cursors.WaitCursor;
		//		Process.Start(e.LinkText);
		//	}
		//	catch (Exception)
		//	{
		//		//Let the caller of FlexibleMessageBoxWindow decide what to do with this exception...
		//		throw;
		//	}
		//	finally
		//	{
		//		Cursor.Current = Cursors.Default;
		//	}
		//}

		//void FlexibleMessageBoxWindow_KeyUp(object sender, KeyEventArgs e)
		//{
		//	//Handle standard key strikes for clipboard copy: "Ctrl + C" and "Ctrl + Insert"
		//	if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert))
		//	{
		//		string buttonsTextLine = (button1.Visible ? button1.Text + STANDARD_MESSAGEBOX_SEPARATOR_SPACES : string.Empty)
		//							+ (button2.Visible ? button2.Text + STANDARD_MESSAGEBOX_SEPARATOR_SPACES : string.Empty)
		//							+ (button3.Visible ? button3.Text + STANDARD_MESSAGEBOX_SEPARATOR_SPACES : string.Empty);

		//		//Build same clipboard text like the standard .Net MessageBox
		//		string textForClipboard = STANDARD_MESSAGEBOX_SEPARATOR_LINES
		//							 + Text + Environment.NewLine
		//							 + STANDARD_MESSAGEBOX_SEPARATOR_LINES
		//							 + richTextBoxMessage.Text + Environment.NewLine
		//							 + STANDARD_MESSAGEBOX_SEPARATOR_LINES
		//							 + buttonsTextLine.Replace("&", string.Empty) + Environment.NewLine
		//							 + STANDARD_MESSAGEBOX_SEPARATOR_LINES;

		//		//Set text in clipboard
		//		Clipboard.SetText(textForClipboard);
		//	}
		//}

		#endregion

		#region Properties (only used for binding)

		public string CaptionText { get; set; }
		public string MessageText { get; set; }

		#endregion

		#region Public show function

		public static Gtk.ResponseType Show(Window owner, string text, string caption, Gtk.ButtonsType buttons, Gtk.MessageType icon, Gtk.ResponseType defaultButton)
		{
			//Create a new instance of the FlexibleMessageBox form
			var flexibleMessageBoxForm = new FlexibleMessageBoxWindow
			{
				//ShowInTaskbar = false,

				//Bind the caption and the message text
				CaptionText = caption,
				MessageText = text
			};
			//flexibleMessageBoxForm.FlexibleMessageBoxWindowBindingSource.DataSource = flexibleMessageBoxForm;

			//Set the buttons visibilities and texts. Also set a default button.
			SetDialogButtons(flexibleMessageBoxForm, buttons, defaultButton);

			//Set the dialogs icon. When no icon is used: Correct placement and width of rich text box.
			SetDialogIcon(flexibleMessageBoxForm, icon);

			//Set the font for all controls
			//flexibleMessageBoxForm.Font = FONT;
			//flexibleMessageBoxForm.richTextBoxMessage.Font = FONT;

			//Calculate the dialogs start size (Try to auto-size width to show longest text row). Also set the maximum dialog size. 
			SetDialogSizes(flexibleMessageBoxForm, text, caption);

			//Set the dialogs start position when given. Otherwise center the dialog on the current screen.
			SetDialogStartPosition(flexibleMessageBoxForm, owner);

			//Show the dialog
			return Show(owner, text, caption, buttons, icon, defaultButton);
		}

		#endregion
	} //class FlexibleMessageBoxForm

	#endregion
}
