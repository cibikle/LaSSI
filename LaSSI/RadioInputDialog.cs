using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;

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
         Options = options; // it is _required_ to set Options before calling CommonSetop(); should that be addressed somehow?
         CommonSetup(title);
      }
      public RadioInputDialog(string title, List<object> options)
      {
         string[] strings = new string[options.Count];
         for (int i = 0; i < options.Count; i++)
         {
            strings[i] = ((Node)options[i]).Name;
         }
         Options = strings;
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

         radioButtonList.Orientation = Orientation.Vertical;
         radioButtonList.Spacing = new Size(0, 5);
         radioButtonList.SelectedIndex = -1;
         radioButtonList.SelectedIndexChanged += RadioButtonList_SelectedIndexChanged;
         foreach (var opt in Options)
         {
            radioButtonList.Items.Add(opt);
         }
         layout.AddCentered(radioButtonList);
         layout.AddCentered(ButtonsLayout());
      }

      private void RadioButtonList_SelectedIndexChanged(object? sender, EventArgs e)
      {
         if (OK is not null && !OK.Enabled)
         {
            OK.Enabled = radioButtonList.SelectedIndex >= 0;
         }
      }

      public int GetSelectedIndex()
      {
         return radioButtonList.SelectedIndex;
      }

      private StackLayout ButtonsLayout()
      {
         OK = new(OK_clicked)
         {
            Text = "OK",
            Enabled = false
         };
         //OK = ok;
         DefaultButton = OK;
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         this.AbortButton = cancel;
         return new StackLayout(OK, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }

      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }

      //public string Hint { get; set; } = string.Empty;
      //private readonly TextBox TextBox = new();
      private DialogResult Result = DialogResult.None;
      private readonly string[] Options = Array.Empty<string>();
      private Button? OK;
      readonly RadioButtonList radioButtonList = new();
   }
}

