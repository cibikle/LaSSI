using Eto.Forms;
using Eto.Drawing;
using System;

namespace LaSSI
{
   public class RadioInputDialog : Dialog
   {

      public RadioInputDialog()
      {

      }
      //public RadioInputDialog(string title)
      //{
      //   CommonSetup(title);
      //}
      public RadioInputDialog(string title, string[] options)
      {
         //TextBox.PlaceholderText = hint;
         Options = options;
         CommonSetup(title);
      }
      public DialogResult GetDialogResult()
      {
         return Result;
      }
      private void CommonSetup(string title)
      {
         Title = title;
         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 5),
            Spacing = new Size(0, 5)
         };
         Content = layout;

         buttonList.Orientation = Orientation.Vertical;
         buttonList.Spacing = new Size(0, 5);
         foreach (var opt in Options)
         {
            buttonList.Items.Add(opt);
         }
         layout.AddCentered(buttonList);
         buttonList.SelectedIndex = 0;
         layout.AddCentered(ButtonsLayout());
      }

      public int GetSelectedIndex()
      {
         return buttonList.SelectedIndex;
      }

      private StackLayout ButtonsLayout()
      {
         Button ok = new(OK_clicked)
         {
            Text = "OK",
         };
         OK = ok;
         this.DefaultButton = ok; // this seems bad but I'm not fixing it now
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         this.AbortButton = cancel;
         return new StackLayout(ok, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }

      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }

      //public string Hint { get; set; } = string.Empty;
      //private readonly TextBox TextBox = new();
      private DialogResult Result = DialogResult.None;
      private string[] Options = new string[0];
      private Button? OK;
      RadioButtonList buttonList = new RadioButtonList();
   }
}

