using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;
using LaSSI.LassiTools;

namespace LaSSI
{
   public class ToolManager : Form
   {
      //internal Dictionary<string, LassiTool> toolBox;
      internal SubMenuItem fileMenu;
      internal MainForm mainForm;
      private Dictionary<LassiTool, TabControl> EditorCache = new();
      private TabControl blankEditor;
      internal TreeGridItemCollection? results = null;
      internal ToolManager(MainForm mainForm)
      {
         blankEditor = ToolEditorPanel(new LassiTool("", mainForm));
         fileMenu = new() { Text = "&File" };
         fileMenu.Items.AddRange(mainForm.CustomCommands!.FileCommands);
         this.mainForm = mainForm;
         Menu = new MenuBar
         {
            Items =
            {
               // File submenu
               fileMenu
            // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
            },
            //ApplicationItems =
            //{
            //   // application (OS X) or file menu (others)
            //   CustomCommands.CreatePrefsMenuItem(CustomCommands.prefsCommand),
            //},
            //QuitItem = CustomCommands.QuitCommand,
            AboutItem = new AboutCommand(this)

         };

         Content = MainLayout();
         Size = new Size(850, 600);
         Title = "Tools Manager";
      }
      internal Splitter MainLayout()
      {
         Splitter splitter = new()
         {
            Orientation = Orientation.Horizontal,
            ID = "MainLayoutSplitter"
         };
         DynamicLayout mainlayout = new();

         mainlayout.Add(ToolListLayout());
         GroupBox toolsListGroup = new()
         {
            Text = " "
         };
         toolsListGroup.Content = mainlayout;
         splitter.Panel1 = toolsListGroup;
         splitter.Panel2 = ToolEditorPanel(new LassiTool("", mainForm));
         return splitter;
      }
      internal Splitter GetMainSplitter()
      {
         return ParentWindow.FindChild<Splitter>("MainLayoutSplitter");
      }
      internal Control GetMainSplitterPanel2()
      {
         return GetMainSplitter().Panel2;
      }
      internal TabControl ToolEditorPanel(LassiTool tool)
      {
         if ((string.IsNullOrEmpty(tool.Name) && blankEditor is null) || (!string.IsNullOrEmpty(tool.Name) && !EditorCache.ContainsKey(tool)))
         {
            TabControl tabs = new()
            {

            };
            TabPage defaultEditor = new()
            {
               Text = "Default",
               ID = "DefaultEditor",
               Content = DefaultToolEditor.DefaultEditorLayout(tool)
            };
            ((Button)((DynamicLayout)defaultEditor.Content).FindChild("TestFilterButton")).Command = new Command((sender, e) =>
            {
               //results = mainForm.saveFile.Search(tool.Filter);
               //CheckBoxListDialog foo = new("results", results);
               //foo.ShowModal();
               tool.Filter.RunSearch();
            });
            //TabPage sqlEditor = new()
            //{
            //   Text = "SQL-like",
            //   ID = "SqlLikeEditor"
            //};
            //TabPage jsonEditor = new()
            //{
            //   Text = "JSON",
            //   ID = "JsonEditor"
            //};
            tabs.Pages.Add(defaultEditor);
            //tabs.Pages.Add(sqlEditor);
            //tabs.Pages.Add(jsonEditor);

            if (!string.IsNullOrEmpty(tool.Name))
            {
               EditorCache.Add(tool, tabs);
            }
            else
            {
               tabs.Enabled = false;
               return tabs;
            }
         }

         if (string.IsNullOrEmpty(tool.Name) && blankEditor is not null)
         {
            return blankEditor;
         }
         else
         {
            return EditorCache[tool];
         }
      }
      internal DynamicLayout ToolListLayout()
      {
         DynamicLayout toolListLayout = new()
         {

         };
         toolListLayout.BeginVertical();
         toolListLayout.Add(ToolListBox(), null, true);
         //toolListLayout.AddSpace();
         toolListLayout.Add(ToolsListButtons());
         //toolListLayout.AddSpace();
         toolListLayout.EndVertical();
         return toolListLayout;
      }
      internal ListBox ToolListBox()
      {
         ListBox tools = new()
         {
            ID = "ToolListBox"
         };
         tools.SelectedIndexChanged += Tools_SelectedIndexChanged;
         //List<string> toolList = new List<string>() { "Placeholder 1", "Placeholder 2", "Pl3" };

         //tools.Size = new Size(100, 400);
         foreach (var tool in mainForm.ToolBox.Keys)
         {
            tools.Items.Add(tool);
            //toolBox.Add(tool, new LassiTool(tool));
         }
         return tools;
      }
      internal Icon? LoadIcon(string iconName)
      {
         Icon icon = null;
         using (System.IO.Stream? iconstream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"LaSSI.icons.{iconName}.png"))
         {
            icon = new(iconstream);
         }

         return icon;
      }
      internal DynamicLayout ToolsListButtons()
      {
         DynamicLayout toolListButtons = new()
         {
            Spacing = new Size(1, 0),
            Padding = new Padding(0, 5)
         };
         Button add = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("plus"),
            ToolTip = "Add"
         };
         add.Click += Add_Click;
         Button delete = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("delete"),
            ToolTip = "Delete",
            Enabled = false,
            ID = "DeleteButton"
         };
         delete.Click += Delete_Click;
         Button copy = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("copy"),
            ToolTip = "Copy",
            Enabled = false,
            ID = "CopyButton"
         };
         copy.Click += Copy_Click;
         Button import = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("import"),
            ToolTip = "Import"
         };
         import.Click += Import_Click;
         Button export = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("export"),
            ToolTip = "Export",
            Enabled = false,
            ID = "ExportButton"
         };
         export.Click += Export_Click;
         Button up = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("upload"),
            ToolTip = "Move up",
            Enabled = false,
            ID = "MoveUpButton"
         };
         up.Click += Up_Click;
         Button down = new()
         {
            Width = 30,
            Height = 30,
            Image = LoadIcon("download"),
            ToolTip = "Move down",
            Enabled = false,
            ID = "MoveDownButton"
         };
         down.Click += Down_Click;
         toolListButtons.BeginHorizontal();
         toolListButtons.Add(add, true);
         toolListButtons.Add(delete, true);
         toolListButtons.Add(copy, true);
         toolListButtons.Add(up, true);
         toolListButtons.Add(down, true);
         toolListButtons.Add(import, true);
         toolListButtons.Add(export, true);
         toolListButtons.EndHorizontal();
         return toolListButtons;
      }
      #region getters
      internal ListBox GetToolListBox()
      {
         ListBox foo = (ListBox)FindChild("ToolListBox");

         return foo;
      }
      internal Button GetDeleteButton()
      {
         Button foo = (Button)FindChild("DeleteButton");

         return foo;
      }
      internal Button GetCopyButton()
      {
         Button foo = (Button)FindChild("CopyButton");

         return foo;
      }
      internal Button GetExportButton()
      {
         Button foo = (Button)FindChild("ExportButton");

         return foo;
      }
      internal Button GetMoveUpButton()
      {
         Button foo = (Button)FindChild("MoveUpButton");

         return foo;
      }
      internal Button GetMoveDownButton()
      {
         Button foo = (Button)FindChild("MoveDownButton");

         return foo;
      }
      #endregion getters
      private void Tools_SelectedIndexChanged(object? sender, EventArgs e)
      {
         if (sender is not null and ListBox box)
         {
            GetDeleteButton().Enabled = GetCopyButton().Enabled = GetExportButton().Enabled = box.SelectedIndex >= 0;

            GetMoveUpButton().Enabled = box.SelectedIndex > 0;

            GetMoveDownButton().Enabled = box.SelectedIndex >= 0 && box.SelectedIndex < box.Items.Count - 1;

            if (box.SelectedIndex >= 0 && box.SelectedIndex < box.Items.Count)
            {
               Debug.WriteLine(box.SelectedKey);
               //Debug.WriteLine(box.SelectedValue);
               GetMainSplitter().Panel2 = ToolEditorPanel(mainForm.ToolBox[box.SelectedKey]);
            }
            else
            {
               GetMainSplitter().Panel2 = ToolEditorPanel(new LassiTool("", mainForm));
            }
         }
      }
      private void Down_Click(object? sender, EventArgs e) // todo: make the reordering stick
      {
         ListBox tools = GetToolListBox();
         int index = tools.SelectedIndex;
         int indexPlusOne = index + 1;
         if (indexPlusOne < tools.Items.Count)
         {
            tools.Items.Move(index, indexPlusOne);
            tools.SelectedIndex = indexPlusOne;
         }
      }

      private void Up_Click(object? sender, EventArgs e) // todo: make the reordering stick
      {
         ListBox tools = GetToolListBox();
         int index = tools.SelectedIndex;
         int indexMinusOne = index - 1;
         if (indexMinusOne > -1)
         {
            tools.Items.Move(index, indexMinusOne);
            tools.SelectedIndex = indexMinusOne;
         }
      }

      private void Export_Click(object? sender, EventArgs e) // todo
      {
         throw new NotImplementedException();
      }

      private void Import_Click(object? sender, EventArgs e) // todo
      {
         throw new NotImplementedException();
      }

      private void Copy_Click(object? sender, EventArgs e) // todo: actually copy the tool
      {
         ListBox tools = GetToolListBox();
         string hint = tools.SelectedKey + " - copy";
         TextInputDialog textInput = new("Name copied tool");
         textInput.SetText(hint);
         textInput.ShowModal(this);
         if (textInput.GetDialogResult() == DialogResult.Ok)
         {
            string input = textInput.GetInput();
            //ListBox tools = GetToolListBox();
            tools.Items.Add(input);
            tools.SelectedKey = input;
         }
      }

      private void Delete_Click(object? sender, EventArgs e) // todo: actually delete the tool
      {
         DialogResult result = MessageBox.Show("Deleting this tool cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No);
         if (result == DialogResult.Yes)
         {
            ListBox tools = GetToolListBox();
            int index = tools.SelectedIndex;
            tools.Items.RemoveAt(index);
            tools.SelectedIndex = -1;
            tools.SelectedIndex = index;
         }
      }

      private void Add_Click(object? sender, EventArgs e) // todo: actually create the tool
      {
         TextInputDialog textInput = new("Name new tool")
         {

         };
         textInput.ShowModal(this);
         if (textInput.GetDialogResult() == DialogResult.Ok)
         {
            string input = textInput.GetInput();
            ListBox tools = GetToolListBox();
            tools.Items.Add(input);
            tools.SelectedKey = input;
         }
      }
   }
}