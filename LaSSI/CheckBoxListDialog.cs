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
      public CheckBoxListDialog()
      {

      }
      public CheckBoxListDialog(string title, List<string> options, bool allOf = false, List<int>? indices = null) // todo: introduce List<int> indices
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
            //Width += 40;
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
         //Scrollable scrollable = new();
         //Content = scrollable;
         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 0)
         };
         Content = layout;
         scrollable.Content = list;
         if (list.Items.Count > 3)
         {
            layout.BeginHorizontal();
            layout.Add(AllNoneButtonsLayout(), true);
            layout.EndHorizontal();
         }
         layout.BeginCentered(new Padding(5, 5, 20, 0));
         //layout.BeginScrollable();
         layout.Add(scrollable, false, false);
         layout.AddSpace();
         //layout.EndScrollable();
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

         StackLayout? allOf = null;
         if (_allOf)
         {
            allOf = AllOffLayout();
         }
         return new StackLayout(all, none, allOf) { Orientation = Orientation.Horizontal, Spacing = 5 };
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

         return new StackLayout(allOf, allOfType) { Orientation = Orientation.Horizontal, Spacing = 5 };
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
         /* List<string> missionTypes = new(); // todo: replace these three with List<List<string>>
          List<string> pickUp = new();
          List<string> dropOff = new();*/

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


               /*if (detail.StartsWith("pick-up")) // uh-oh. do we actually need a dictionary or map or something or can we do without this? like, just use the index?
               {
                  if (!pickUp.Contains(detail))
                  {
                     pickUp.Add(detail);
                  }
               }
               else if (detail.StartsWith("drop-off"))
               {
                  if (!dropOff.Contains(detail))
                  {
                     dropOff.Add(detail);
                  }
               }
               else
               {
                  if (!missionTypes.Contains(detail))
                  {
                     missionTypes.Add(detail);
                  }
               }*/
            }
         }
         foreach (var detailList in allOfOptions)
         {
            strings.AddRange(detailList.OrderBy(c => c.Length).ThenBy(c => c));
         }
         // missionTypes.Sort(); // todo: down the line maybe introduce sorting rules for each index
         /*strings.AddRange(missionTypes.OrderBy(c => c.Length).ThenBy(c => c));
         strings.AddRange(pickUp.OrderBy(c => c.Length).ThenBy(c => c));
         strings.AddRange(dropOff.OrderBy(c => c.Length).ThenBy(c => c));*/
         return strings;
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
      private readonly Scrollable scrollable = new();
   }
}

