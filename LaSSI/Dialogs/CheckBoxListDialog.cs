using Eto.Forms;
using Eto.Drawing;
using System;
using System.Collections.Generic;

namespace LaSSI
{
   public class CheckBoxListDialog : Dialog
   {

      public CheckBoxListDialog()
      {

      }
      //public CheckBoxListDialog(string title)
      //{
      //   CommonSetup(title);
      //}
      public CheckBoxListDialog(string title, List<string> options)
      {
         //TextBox.PlaceholderText = hint;
         CommonSetup(title, options);
      }
      public CheckBoxListDialog(string title, TreeGridItemCollection items)
      {
         //TextBox.PlaceholderText = hint;
         List<string> options = new();
         foreach (Node item in items)
         {
            options.Add(item.Name);
         }
         CommonSetup(title, options);
      }
      public IEnumerable<string> GetSelectedItems()
      {
         return list.SelectedKeys;
      }
      public DialogResult GetDialogResult()
      {
         return Result;
      }
      private void CommonSetup(string title, List<string> options)
      {
         list.Orientation = Orientation.Vertical;
         //list.Height = 400;
         foreach (var opt in options)
         {
            list.Items.Add(opt);
         }
         Title = title;

         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 5)
         };
         Content = layout;
         Scrollable listScroll = new()
         {
            Height = 400,
            Content = list
         };
         layout.BeginCentered(new Padding(5, 5));
         layout.Add(listScroll);
         layout.EndCentered();
         layout.BeginCentered(new Padding(5, 5));
         layout.AddCentered(ButtonsLayout());
         layout.EndCentered();
         list.SelectedKeysChanged += List_SelectedKeysChanged;
      }

      private void List_SelectedKeysChanged(object? sender, EventArgs e)
      {
         if (sender is not null and CheckBoxList list && OK is not null)
         {
            int count = 0;
            var enumerator = list.SelectedKeys.GetEnumerator();
            while (enumerator.MoveNext())
            {
               count++;
            }
            OK.Enabled = count > 0;
         }
      }
      private StackLayout ButtonsLayout()
      {
         Button ok = new(OK_clicked)
         {
            Text = "OK",
            Enabled = false,
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

      public string Hint { get; set; } = string.Empty;
      private readonly CheckBoxList list = new();
      private DialogResult Result = DialogResult.None;
      private Button? OK;
   }
}