using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;

namespace LaSSI
{
   public class CheckBoxListDialog : Dialog
   {

      public CheckBoxListDialog()
      {

      }
      public CheckBoxListDialog(string title, List<string> options)
      {
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
         if (list.Items.Count > 3)
         {
            layout.BeginHorizontal();
            layout.Add(AllNoneButtonsLayout(), true);
            layout.EndHorizontal();
         }
         layout.BeginCentered(new Padding(5, 5));
         layout.Add(list);
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
            if (All is not null)
            {
               All.Enabled = count < list.Items.Count;
            }
            if (None is not null)
            {
               None.Enabled = count > 0;
            }
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
         this.DefaultButton = ok;
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         this.AbortButton = cancel;
         return new StackLayout(ok, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }
      private StackLayout AllNoneButtonsLayout()
      {
         Button all = new()
         {
            Text = "All",
            Enabled = true,
         };
         all.Click += (sender, e) => { list.SelectedValues = list.Items; };
         All = all;
         Button none = new() { Text = "None", Enabled = false };
         none.Click += (sender, e) => { list.SelectedValues = null; };
         None = none;
         return new StackLayout(all, none) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }

      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }

      private readonly CheckBoxList list = new();
      private DialogResult Result = DialogResult.None;
      private Button? OK;
      private Button? All;
      private Button? None;
   }
}

