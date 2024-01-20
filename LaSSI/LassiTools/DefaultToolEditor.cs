using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;
using MonoMac.AppKit;

namespace LaSSI.LassiTools
{
   public class DefaultToolEditor
   {
      //public DefaultToolEditor()
      //{

      //}
      internal static DynamicLayout DefaultEditorLayout(LassiTool tool)
      {
         DynamicLayout editorLayout = new()
         {

         };
         Label name = new() // todo: this should become a textbox, I think? so users can rename tools?
         {
            Text = tool.Name,
            TextAlignment = TextAlignment.Center
         };
         if (name.Text != string.Empty)
         {
            name.Font = new Font(FontFamilies.Sans, 14, FontStyle.None, FontDecoration.Underline);
         }
         // triggers
         GroupBox triggersGroup = new()
         {
            Text = "Triggers",
            Content = TriggersLayout(tool.Trigger)
         };
         // filters
         GroupBox filtersGroup = new()
         {
            Text = "Filter",
            Content = LassiToolFilterLayout.CreateFilterLayout(tool.Filter)
         };
         // actions
         GroupBox actionsGroup = new()
         {
            Text = "Actions"
         };
         Splitter splitter = new()
         {
            Orientation = Orientation.Vertical,
            Panel1 = filtersGroup,
            Panel2 = actionsGroup
         };
         editorLayout.Add(name);
         editorLayout.BeginHorizontal();
         editorLayout.Add(triggersGroup);
         editorLayout.EndBeginHorizontal();
         editorLayout.Add(splitter);
         editorLayout.EndHorizontal();

         return editorLayout;
      }


      internal static DynamicLayout TriggersLayout(LassiToolTrigger trigger)
      {
         DynamicLayout triggersLayout = new()
         {
            Spacing = new Size(5, 0)
         };
         CheckBox fileLoad = new()
         {
            Text = "On file load",
            ID = "FileLoadCheckBox",
         };
         fileLoad.Bind(c => c.Checked, trigger, r => r.OnLoad);
         CheckBox toolMenu = new()
         {
            Text = "Tool menu item",
            ID = "MenuItemCheckBox",
         };
         toolMenu.Bind(c => c.Checked, trigger, r => r.Enabled);
         CheckBox shortcut = new()
         {
            Text = "Shortcut",
            ID = "ShortcutCheckBox",
            Enabled = false,
            ToolTip = "Coming soon!"
         };
         triggersLayout.BeginHorizontal();
         triggersLayout.Add(fileLoad);
         triggersLayout.Add(toolMenu);
         triggersLayout.Add(shortcut);
         triggersLayout.EndHorizontal();
         return triggersLayout;
      }
   }
}

