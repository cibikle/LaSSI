using System;
using Eto.Drawing;
using System.Collections;
using System.Collections.Specialized;
using Eto.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LaSSI
{
   public class DataPanel : Panel
   {
      private Dictionary<TreeGridItem, DetailsLayout> DetailPanelsCache = new();
      private Size DetailsPanelInitialSize = new(0, 0);
      private readonly List<InventoryGridItem>? InventoryMasterList;
      private Node? Root { get; set; }
      private readonly int ParentWidth = 0;
      private readonly Button Apply = new(ApplyButton_Click)
      {
         Text = "Apply",
         ID = "DetailsApplyButton",
         Enabled = false
      };
      private readonly Button Revert = new(RevertButton_Click)
      {
         Text = "Revert",
         ID = "DetailsRevertButton",
         Enabled = false
      };
      public DataPanel()
      {

      }
      public DataPanel(Node root, List<InventoryGridItem> inventoryMasterList, int parentWidth)
      {
         InventoryMasterList = inventoryMasterList;
         ParentWidth = parentWidth;
         TableLayout primaryLayout = InitPrimaryPanel();
         Root = root;
         Content = primaryLayout;
      }
      public DataPanel(List<InventoryGridItem> inventoryMasterList, int parentWidth)
      {
         InventoryMasterList = inventoryMasterList;
         ParentWidth = parentWidth;
         TableLayout primaryLayout = InitPrimaryPanel();
         Content = primaryLayout;
      }
      private TableLayout InitPrimaryPanel() // any particular reason this is a TableLayout?
      {
         Splitter sp = CreateSplitter();

         //TreeGridView treeView = CreateTreeView();
         sp.Panel1 = CreateTreeView();// treeView;
         sp.Panel1.Width = ParentWidth / 2;

         //var Panel2Layout = InitPanel2Layout();
         sp.Panel2 = CreatePanel2Layout();// Panel2Layout;

         TableLayout dataLayout = new();
         dataLayout.Rows.Add(sp);

         return dataLayout;
      }
      private TreeGridView CreateTreeView()
      {
         TreeGridView treeView = new()
         {
            Tag = "DataTreeView",
            ID = "DataTreeView",
            AllowEmptySelection = true,
            AllowMultipleSelection = true
         };
         treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
         return treeView;
      }
      private static Splitter CreateSplitter()
      {
         Splitter sp = new()
         {
            Tag = "Splitter",
            ID = "Splitter",
            Orientation = Orientation.Horizontal,
            SplitterWidth = 10,
         };
         return sp;
      }
      private DynamicLayout CreatePanel2Layout()
      {
         DynamicLayout Panel2Layout = new()
         {
            Spacing = new Size(0, 5),
            ID = "Panel2PrimeLayout"
         };
         DynamicLayout Panel2DetailsLayout = new()
         {
            ID = "Panel2DetailsLayout"
         };
         //Panel2Layout.Add(DetailScrollable);
         //Panel2Layout.Rows
         //Panel2Layout.AddSeparateRow(new Label { Text = "here!" });
         //Panel2Layout.AddSpace(); // neccessary?
         Panel2Layout.AddSeparateRow(CreateApplyRevertButtonsLayout());
         Panel2Layout.AddSeparateRow(Panel2DetailsLayout);
         return Panel2Layout;
      }
      private StackLayout CreateApplyRevertButtonsLayout()
      {
         StackLayout ApplyRevertLayout = new()
         {
            Orientation = Orientation.Horizontal,
            Padding = 5,
            Spacing = 5
         };

         ApplyRevertLayout.Items.Add(Apply);
         ApplyRevertLayout.Items.Add(Revert);

         return ApplyRevertLayout;
      }
      private ListBuilder CreateListBuilder(TreeGridItem item)
      {
         ObservableCollection<InventoryGridItem> RemainingItems = new ObservableCollection<InventoryGridItem>();
         ObservableCollection<InventoryGridItem> InStock = new ObservableCollection<InventoryGridItem>();
         if (InventoryMasterList is not null)
         {
            RemainingItems = new ObservableCollection<InventoryGridItem>(InventoryMasterList);
            foreach (var value in item.Values)
            {
               if (value is Dictionary<string, string> dictionary && dictionary.Count != 0)
               {
                  foreach (var entry in dictionary)
                  {
                     try
                     {
                        ShuttleItems(RemainingItems, InStock, entry);
                     }
                     catch
                     {
                        Debug.WriteLine($"Item not found in master list: {entry.Key}");
                     }
                  }
               }
            }
         }

         return new ListBuilder(RemainingItems, new List<string> { "Remaining items" }, InStock, new List<string> { "In stock", "Count" });
      }
      private void ShuttleItems(ObservableCollection<InventoryGridItem> RemainingItems
         , ObservableCollection<InventoryGridItem> InStock
         , KeyValuePair<string, string> entry)
      {
         InventoryGridItem InvItem = RemainingItems.Where(x => x.Name == entry.Key).First();
         RemainingItems.Remove(InvItem);
         InvItem.Count = int.Parse(entry.Value);
         InStock.Add(InvItem);
      }
      private DetailsLayout CreateDetailsLayout(TreeGridItem item)
      {
         DetailsLayout detailsLayout = new()
         {
            //Padding = new Padding(5, 0),
            Spacing = new Size(0, 5)
         };

         detailsLayout.Add(GetNodePathLabel(item));
         switch (item.Tag)
         {
            case "OurStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "TheirStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "Cells":
               {
                  detailsLayout.Add(CreateShipCellsLayout(item));
                  break;
               }
            default:
               {
                  Scrollable scrollable = new()
                  {
                     ID = "DetailScrollable"
                  };
                  scrollable.Content = CreateDefaultFieldsGridView(item);
                  //detailsLayout.Add(CreateDefaultFields(item));
                  detailsLayout.Add(scrollable);
                  break;
               }
         }

         return detailsLayout;
      }
      private static string GetNodePath(TreeGridItem item)
      {
         string path = item.Values[0].ToString()!;
         while (item.Parent != null && item.Parent.Parent != null)
         {
            item = (TreeGridItem)item.Parent;
            path = $"{item.Values[0]}/{path}";
         }
         return path;
      }
      private static Label GetNodePathLabel(TreeGridItem item)
      {
         Label nodePathLabel = new Label { Text = GetNodePath(item), BackgroundColor = Colors.Silver, Font = new Font("Arial", 18, FontStyle.Bold) };
         return nodePathLabel;
      }
      private static DynamicLayout CreateNodePathLayout(TreeGridItem item)
      {
         DynamicLayout layout = new DynamicLayout();
         layout.Add(GetNodePathLabel(item));
         return layout;
      }
      private GridView CreateDefaultFieldsGridView(TreeGridItem item)
      {
         OrderedDictionary vals = (OrderedDictionary)item.Values[1];
         //Dictionary<string, string> dic = new Dictionary<string, string>();
         IEnumerable<object> foo = vals.Cast<object>();
         GridView defaultGridView = new()
         {
            DataStore = foo,
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,

         };
         //defaultGridView.
         defaultGridView.Columns.Add(new GridColumn
         {
            HeaderText = "Key",
            DataCell = new TextBoxCell()
            {
               Binding = Binding.Property((DictionaryEntry i) => (string)i.Key)
            },
            Editable = true,
            //AutoSize = true,
            //Resizable = true
         });
         defaultGridView.Columns.Add(new GridColumn
         {
            HeaderText = "Value",
            DataCell = new TextBoxCell()
            {
               Binding = Binding.Property((DictionaryEntry i) => (string)i.Value!)
            },
            Editable = true,
            //AutoSize = true,
            //Resizable = true
         });
         defaultGridView.Shown += DefaultGridView_Shown;
         return defaultGridView;
      }

      private static Label CreateDetailLabel(string text)
      {
         return new Label
         {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Wrap = WrapMode.Word
         };
      }
      private static TextBox CreateDetailTextBox(string text)
      {
         int multiplicand = text.Length > 2 ? text.Length : 2;
         return new TextBox
         {
            Text = text,
            Width = multiplicand * 10
         };
      }
      /// <summary>
      /// Returns a DropDown populated from a comman-delimited string parameter
      /// </summary>
      /// <param name="options"></param>
      /// <returns></returns>
      private static DropDown CreateDetailDropMenu(string options)
      {
         DropDown dropDown = new DropDown();
         var optionsArray = options.Split(',');
         foreach (var option in optionsArray)
         {
            dropDown.Items.Add(option);
         }

         return dropDown;
      }
      /// <summary>
      /// Returns a DropDown populated from a comma-delimited string parameter and sets the SelectedIndex from an int parameter.<br/>
      /// If the int is out of bounds, the SelectedIndex is 0.
      /// </summary>
      /// <param name="text"></param>
      /// <param name="defaultIndex"></param>
      /// <returns></returns>
      private static DropDown CreateDetailDropMenu(string text, int defaultIndex)
      {
         DropDown dropDown = CreateDetailDropMenu(text);
         if (defaultIndex < dropDown.Items.Count && defaultIndex >= 0)
         {
            dropDown.SelectedIndex = defaultIndex;
         }
         else
         {
            Debug.WriteLine($"Specified default index is out of bounds: {defaultIndex}");
            dropDown.SelectedIndex = 0;
         }

         return dropDown;
      }
      /// <summary>
      /// Returns true if the string parameter equals (ignores case) "true" or "false"; otherwise, false.
      /// Used to determine if detail control should a TextBox or a DropDown.
      /// </summary>
      /// <param name="value"></param>
      /// <returns></returns>
      private static bool IsValueTrueFalse(string value)
      {
         return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                     || String.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
      }
      /// <summary>
      /// Depricated, probably. <br/>
      /// Returns a StackLayout of label-textbox pairs representing the properties of a node in a TreeGridItem.
      /// </summary>
      /// <param name="item"></param>
      /// <returns>StackLayout</returns>
      private static StackLayout CreateDefaultFields(TreeGridItem item)
      {
         StackLayout defaultLayout = new StackLayout();
         foreach (var s in item.Values)
         {
            if (s is string)
            {
               //detailsLayout.Items.Add(new StackLayoutItem(new Label().Text = s.ToString()));
            }
            else if (s is OrderedDictionary dictionary && dictionary.Count != 0)
            {
               TableLayout innerLayout = new TableLayout
               {
                  Spacing = new Size(5, 0),
                  Padding = new Padding(0, 5)
               };
               foreach (DictionaryEntry p in dictionary)
               {
                  Label label = CreateDetailLabel((string)p.Key);
                  Control value;
                  if (IsValueTrueFalse((string)p.Value!))
                  {
                     value = CreateDetailDropMenu("true,false", String.Equals((string?)p.Value, "true", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
                  }
                  else
                  {
                     value = CreateDetailTextBox((string)p.Value!);
                  }

                  innerLayout.Rows.Add(new TableRow(new TableCell(label), new TableCell(value)));
               }
               defaultLayout.Items.Add(new StackLayoutItem(innerLayout));
            }
         }
         return defaultLayout;
      }

      private static TextArea CreateShipCellsLayout(TreeGridItem item)
      {
         //string cells = string.Empty;
         ////string row = string.Empty;
         //if (item.Values[1] is OrderedDictionary dictionary && dictionary.Count != 0)
         //{
         //   foreach(var p in dictionary)
         //   {
         //      //row = p.Key + "  " + p.Value + Environment.NewLine;
         //      cells += p.Key + "  " + p.Value + Environment.NewLine;
         //   }
         //}
         ////return new(new TextArea() { ReadOnly = true, Font = new Font("Courier", 12), Text = cells, });
         //return new TextArea() { ReadOnly = true, Font = new Font("Courier", 12), Text = cells, };
         return new TextArea()
         {
            ReadOnly = true,
            Font = new Font("Courier", 12)
            ,
            Text = $"Coming soon!{Environment.NewLine}(Look, I tried, but it's going to take a whole new module to make the ship layout inspector usable and we both have way bigger fish to fry.)"
         };
      }

      private void UpdateDetailsPanel(TreeGridItem item)
      {
         DynamicLayout detailslayout = GetPanel2DetailsLayout();
         if (!DetailPanelsCache.ContainsKey(item))
         {
            DetailPanelsCache.Add(item, CreateDetailsLayout(item));
         }
         //Panel2PrimeLayout.AddSeparateRow(DetailPanelsCache[item]);
         //Label f = new Label { Text = $"{item.Tag}", Size = new Size { Height = 50, Width = 50 } };
         detailslayout.Content = DetailPanelsCache[item];
         //UpdateApplyRevertButtons(DetailPanelsCache[item].Status);
         //this.Invalidate();
      }
      private void UpdateApplyRevertButtons(DetailsLayout.State status)
      {
         switch (status)
         {
            case DetailsLayout.State.Unmodified:
               {
                  Apply.Enabled = Revert.Enabled = false;
                  break;
               }
            case DetailsLayout.State.Modified:
               {
                  Apply.Enabled = Revert.Enabled = true;
                  break;
               }
            case DetailsLayout.State.Applied:
               {
                  Apply.Enabled = false;
                  Revert.Enabled = true; // todo: handle this case
                  break;
               }
         }
      }
      private DynamicLayout GetPanel2PrimeLayout()
      {
         return (DynamicLayout)this.Children.Where<Control>(x => x.ID == "Panel2PrimeLayout").First();
         // pretty sure this blows up if the prime layout isn't found
      }
      private DynamicLayout GetPanel2DetailsLayout()
      {
         return (DynamicLayout)this.Children.Where<Control>(x => x.ID == "Panel2DetailsLayout").First();
         // pretty sure this blows up if the prime layout isn't found
      }

      public void Rebuild(Node root)
      {
         DetailPanelsCache.Clear();
         ClearDetails();
         Root = root;
         RebuildTreeView(Root);
      }
      private void ClearDetails()
      {
         DynamicLayout s = GetPanel2PrimeLayout();
         //s.Rows[0] = null;
         //UpdateApplyRevertButtons(DetailsLayout.State.Unmodified); // todo: turn this back on
      }
      private void RebuildTreeView(Node root)
      {
         TreeGridView x = (TreeGridView)this.Children.Where<Control>(x => x.ID == "DataTreeView").First();
         x.DataStore = SaveFile2TreeGridItems(root);
         if (x.Columns.Count == 0)
         {
            x.Columns.Add(new GridColumn
            {
               AutoSize = true,
               DataCell = new TextBoxCell(0)
            });
         }
         x.CellFormatting += X_CellFormatting;
      }
      private TreeGridItemCollection SaveFile2TreeGridItems(Node root)
      {
         TreeGridItemCollection treeGridItems = new()
         {
            WalkNodeTree(root)
         };
         treeGridItems[0].Expanded = true;
         return treeGridItems;
      }
      private TreeGridItem WalkNodeTree(Node node)
      {
         if (!node.HasChildren())
         {
            return new TreeGridItem(node.Name, node.Properties)
            {
               Tag = node.Name
            };
         }
         else
         {
            TreeGridItemCollection childItems = new();
            foreach (var child in node.Children)
            {
               childItems.Add(WalkNodeTree(child));
            }
            return new TreeGridItem(childItems, node.Name, node.Properties)
            {
               Tag = node.Name
            };
         }
      }
      private Size GetTheSizeUnderControl(Control control, GridView gridView) // I don't love this, but it _frelling_ works
      {
         if (control.Height < control.ParentWindow.Height)
         {
            int offset = 0;
            if (control is DynamicLayout s) // todo: does this still work?
            {
               var iter = s.Children.Where(x => x.Parent == gridView.Parent && x != this && x.Height <= s.Height).GetEnumerator();
               while (iter.MoveNext())
               {
                  offset += iter.Current.Height;
               }
            }
            return new Size(control.Width - 10, control.Height - offset);
         }
         if (control.Parent != null)
         {
            control.Size = GetTheSizeUnderControl(control.Parent, gridView);
         }
         return control.Size;
      }

      #region event handlers
      private void TreeView_SelectedItemChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView)
         {
            TreeGridView tree = (TreeGridView)sender;
            TreeGridItem item = (TreeGridItem)(tree).SelectedItem;
            //item.SetValue(2, Colors.Magenta);
            if (item is not null)
            {
               UpdateDetailsPanel(item);
            }
            //else
            //{
            //   ClearDetailsPanel();
            //}
         }
      }
      private void X_CellFormatting(object? sender, GridCellFormatEventArgs e)
      {
         //e.BackgroundColor = e.Row % 2 == 0 ? Colors.Blue : Colors.LightBlue;
         if (e.Item is not null and TreeGridItem item && item.Values.Length > 2)
         {
            Color newColor = Colors.White;
            NodeStatus s = (NodeStatus)item.Values[2];
            switch (s)
            {
               case NodeStatus.Edited:
                  {
                     newColor = Colors.Orange;
                     break;
                  }
               case NodeStatus.ChildEdited:
                  {
                     newColor = Colors.Peru;
                     break;
                  }
               case NodeStatus.Deleted:
                  {
                     newColor = Colors.OrangeRed;
                     break;
                  }
            }
            e.BackgroundColor = newColor;
         }

         //Colors.
      }
      private void DefaultGridView_Shown(object? sender, EventArgs e)
      {
         if ((DetailsPanelInitialSize.Height == 0 || DetailsPanelInitialSize.Width == 0) && sender is GridView and not null)
         {
            DetailsPanelInitialSize = GetTheSizeUnderControl((GridView)sender, (GridView)sender);
         }
         if (sender is GridView and not null)
         {
            ((GridView)sender).Size = DetailsPanelInitialSize;
         }
      }

      private static void RevertButton_Click(object? sender, EventArgs e)
      {
         MessageBox.Show("Revert button clicked");
         // remember to handle the case in which the node is applied already
      }

      private static void ApplyButton_Click(object? sender, EventArgs e)
      {
         MessageBox.Show("Apply button clicked");
      }
      #endregion event handlers
   }
}

