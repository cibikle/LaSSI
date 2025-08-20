using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class CheckBoxListDialog : Dialog
   {
      private readonly bool _allOf;
      private readonly List<int>? _indices;
      private readonly CheckBoxList list = new();
      private DialogResult Result = DialogResult.None;
      private Button? OK;
      private Button? All;
      private Button? None;
      private readonly Scrollable scrollable = new();

      public CheckBoxListDialog()
      {

      }
      public CheckBoxListDialog(string title, List<string> options
         , bool allOf = false, List<int>? indices = null)
      {
         _indices = indices;// ?? new List<int>();
         _allOf = allOf;
         CommonSetup(title, options);
         Shown += CheckBoxListDialog_Shown;
      }

      private void CheckBoxListDialog_Shown(object? sender, EventArgs e)
      {
         if (Height > Owner.Height)
         {
            Height = Owner.Height;
            scrollable.Height = scrollable.Parent.Height - 80;
         }
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
         foreach (string opt in options)
         {
            list.Items.Add(opt);
         }
         Title = title;
         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 0)
         };
         Content = layout;
         scrollable.Content = list;
         scrollable.Padding = 5;
         if (list.Items.Count > 1)
         {
            layout.BeginHorizontal();
            layout.Add(AllNoneButtonsLayout(), true);
            layout.EndHorizontal();
         }
         layout.BeginCentered(new Padding(0, 5, 0, 0));
         layout.Add(scrollable, false, false);
         layout.AddSpace();
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
         DefaultButton = ok;
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         AbortButton = cancel;
         return new StackLayout(ok, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }
      private StackLayout AllNoneButtonsLayout()
      {
         StackLayout? allOf = null;
         if (_allOf)
         {
            allOf = AllOffLayout();
         }
         Button all = new()
         {
            Text = "All",
            Enabled = true,
         };
         all.Click += (sender, e) =>
         {
            list.SelectedValues = list.Items;
            ResetAllOfDropDown(allOf);
         };
         All = all;
         Button none = new() { Text = "None", Enabled = false };
         none.Click += (sender, e) =>
         {
            list.SelectedValues = null;
            ResetAllOfDropDown(allOf);
         };
         None = none;
         return new StackLayout(all, none, allOf) { Orientation = Orientation.Horizontal, Spacing = 5, Padding = new Padding(0, 5, 0, 0) };
      }
      private StackLayout AllOffLayout()
      {
         DropDown allOfType = PrefsDialog.CreateDropDown("allOfType", OptionsToAllOfTypes().ToArray());
         Label allOf = new()
         {
            Text = "All of",
         };
         allOfType.SelectedIndexChanged += (sender, e) =>
         {
            if (allOfType.SelectedIndex >= 0) { string value = allOfType.SelectedValue.ToString()!; Regex r = new(value + @"(,|\))"); list.SelectedValues = list.Items.Where(n => r.IsMatch(n.Text)); }
         };

         return new StackLayout(allOf, allOfType) { Orientation = Orientation.Horizontal, Spacing = 5, Padding = new Padding(5, 0, 0, 0) };
      }
      private List<string> OptionsToAllOfTypes()
      {
         List<string> strings = new();
         List<List<string>> allOfOptions = new();
         bool selectIndices = _indices is not null;
         int indexCount = list.Items[0].Text.TrimEnd(')').Split('(')[1].Split(',').Length;
         int currentIndex = 0;
         if (selectIndices)
         {
            indexCount = _indices!.Count;
         }
         for (int i = 0; i < indexCount; i++)
         {
            allOfOptions.Add(new List<string>());
         }

         foreach (var option in list.Items)
         {
            currentIndex = 0;
            string[] details = option.Text.TrimEnd(')').Split('(')[1].Split(',');
            for (int i = 0; i < details.Length; i++)
            {
               if (_indices is not null) // skip indices not in the list of indices to include
               {
                  while (!_indices.Contains(i) && i < details.Length)
                  {
                     i++;
                  }
                  if (i >= details.Length)
                  {
                     continue;
                  }
               }
               string detail = details[i].Trim();
               if (selectIndices)
               {
                  currentIndex = _indices!.IndexOf(i);
               }
               else
               {
                  currentIndex = i;
               }
               if (!allOfOptions[currentIndex].Contains(detail))
               {
                  allOfOptions[currentIndex].Add(detail);
               }
            }
         }
         foreach (var detailList in allOfOptions)
         {
            strings.AddRange(detailList.OrderBy(c => c.Length).ThenBy(c => c));
         }
         return strings;
      }
      private static void ResetAllOfDropDown(StackLayout? allOf)
      {
         if (allOf is not null && allOf.FindChild("allOfType") is not null and DropDown d) { d.SelectedIndex = -1; }
      }
      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }
   }
}

