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
      public enum DerelictsCleaningMode
      {
         CurrentSystem,
         SectorWide,
         SpecificSystem
      }
      public enum ShipDisposition
      {
         Any,
         Friendly,
         Neutral,
         Hostile,
         Derelict,
         ForSale
      }
      public enum DataState
      {
         Unchanged,
         Unapplied,
         Unsaved,
         UnsavedAndUnapplied
      }
      private MainForm? mainForm;
      private DataState dataState = DataState.Unchanged;
      internal bool dirtyBit = false;
      private Dictionary<Node, DetailsLayout> DetailPanelsCache = new();
      private Size DetailsPanelInitialSize = new(0, 0);
      private readonly List<InventoryGridItem>? InventoryMasterList;
      private Node? Root { get; set; }
      private readonly int ParentWidth = 0;
      private PreviousEntry? CurrentValues;
      private readonly Button Apply = new()
      {
         Text = "Apply",
         ID = "DetailsApplyButton",
         Enabled = false
      };
      private readonly Button Revert = new()
      {
         Text = "Revert",
         ID = "DetailsRevertButton",
         Enabled = false
      };
      //private DetailsLayout? CurrentDetails;
      private List<Node>? freespaceObjectsWithComet = null;
      private List<Node>? crossSectorMissions = null;
      private List<Node>? weatherReports = null;
      private List<Node>? deadCrew = null;
      private List<Node>? friendlyShips = null;
      public DataPanel()
      {

      }
      public DataPanel(MainForm mainform, Node root, List<InventoryGridItem> inventoryMasterList, int parentWidth)
      {
         mainForm = mainform;
         InventoryMasterList = inventoryMasterList;
         ParentWidth = parentWidth;
         TableLayout primaryLayout = InitPrimaryPanel();
         Root = root;
         Content = primaryLayout;
      }
      public DataPanel(MainForm mainform, List<InventoryGridItem> inventoryMasterList, int parentWidth)
      {
         mainForm = mainform;
         InventoryMasterList = inventoryMasterList;
         ParentWidth = parentWidth;
         TableLayout primaryLayout = InitPrimaryPanel();
         Content = primaryLayout;
      }
      private TableLayout InitPrimaryPanel() // any particular reason this is a TableLayout?
      {
         Apply.Click += ApplyButton_Click;
         Revert.Click += RevertButton_Click;

         Splitter sp = CreateSplitter();
         sp.Panel1 = CreateTreeView(ParentWidth / 2);
         sp.Panel1.Width = ParentWidth / 2;
         sp.Panel2 = CreatePanel2Layout();

         GroupBox box = new();
         box.Content = sp;

         TableLayout dataLayout = new();
         dataLayout.Rows.Add(box);

         return dataLayout;
      }
      internal TextBox GetSearchBox()
      {
         TableLayout lefthandLayout = (TableLayout)GetTreeGridView().Parent;
         return (TextBox)lefthandLayout.FindChild("searchTextbox");
         //return GetTreeGridView().Parent.Children.Where(x => x.ID == "searchTextBox");
         //return null;
      }
      private TableLayout CreateTreeView(int width)
      {
         TableLayout lefthandLayout = new()
         {
            Spacing = new Size(5, 5),
            Size = new Size(-1, -1)
         };
         DynamicLayout searchLayout = new()
         {
            Spacing = new Size(5, 5),
            Padding = new Padding(2, 0)
         };
         TextBox search = new()
         {
            ID = "searchTextbox",
            Enabled = true,
            PlaceholderText = "Search...",
         };
         search.TextChanged += Search_TextChanged;

         Button clearSearch = new()
         {
            Text = "Clear"
         };
         clearSearch.Click += ClearSearch_Click;
         searchLayout.BeginHorizontal();
         searchLayout.Add(search, true);
         searchLayout.Add(clearSearch, false);
         searchLayout.EndHorizontal();
         lefthandLayout.Rows.Add(searchLayout);

         TreeGridView treeView = new()
         {
            Tag = "DataTreeView",
            ID = "DataTreeView",
            ShowHeader = false,
            AllowEmptySelection = true,
            //Width = width
            //AllowMultipleSelection = true
         };
         treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
         treeView.CellFormatting += TreeView_CellFormatting;

         lefthandLayout.Rows.Add(treeView);

         //stack.Width = width;
         //stack.Size = new Size(-1, -1);
         //stack.Items.Add(search);
         //stack.Items.Add(treeView);
         return lefthandLayout;
      }

      private void ClearSearch_Click(object? sender, EventArgs e)
      {
         if (sender is not null and Button clear && clear.Parent.FindChild("searchTextbox") is TextBox searchBox)
         {
            searchBox.Text = string.Empty;
         }
      }

      private void Search_TextChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TextBox textBox)
         {
            string searchtext = textBox.Text;
            if (searchtext != string.Empty && mainForm is not null)
            {
               GetTreeGridView().DataStore = mainForm.saveFile.Search(searchtext.Trim());
            }
            else
            {
               GetTreeGridView().DataStore = new TreeGridItemCollection() { GetRoot() };
            }
         }
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
         Panel2Layout.AddSeparateRow(CreateApplyRevertButtonsLayout());
         Panel2Layout.AddSeparateRow(Panel2DetailsLayout);
         return Panel2Layout;
      }
      private StackLayout CreateApplyRevertButtonsLayout()
      {
         StackLayout ApplyRevertLayout = new()
         {
            Orientation = Orientation.Horizontal,
            //Padding = 5,
            Spacing = 5
         };

         ApplyRevertLayout.Items.Add(Apply);
         ApplyRevertLayout.Items.Add(Revert);

         return ApplyRevertLayout;
      }
      private ListBuilder CreateListBuilder(Node item)
      {
         ObservableCollection<InventoryGridItem> RemainingItems = new ObservableCollection<InventoryGridItem>();
         ObservableCollection<InventoryGridItem> InStock = new ObservableCollection<InventoryGridItem>();
         if (InventoryMasterList is not null)
         {
            RemainingItems = new ObservableCollection<InventoryGridItem>(InventoryMasterList);
            OrderedDictionary dictionary = item.Properties;
            if (dictionary.Count != 0)
            {
               foreach (DictionaryEntry entry in dictionary)
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

         ListBuilder lb = new ListBuilder(RemainingItems, new List<string> { "Remaining items" }, InStock, new List<string> { "In stock", "Count" });
         lb.RightGridUpdated += RightGrid_Updated;
         lb.ID = "ListBuilder";
         lb.Tag = "ListBuilder";
         return lb;
      }
      private static void ShuttleItems(ObservableCollection<InventoryGridItem> RemainingItems
         , ObservableCollection<InventoryGridItem> InStock
         , DictionaryEntry entry)
      {
         InventoryGridItem InvItem = RemainingItems.Where(x => x.Name == entry.Key.ToString()).First();
         RemainingItems.Remove(InvItem);
         InvItem.Count = int.Parse(entry.Value!.ToString()!);
         InStock.Add(InvItem);
      }
      private DetailsLayout CreateDetailsLayout(Node item)
      {
         DetailsLayout detailsLayout = new()
         {
            //Padding = new Padding(5, 0),
            Spacing = new Size(0, 5)
         };
         //detailsLayout.Add(GetNodePathLabel(item));
         Scrollable s = new Scrollable();
         s.Content = GetNodePathButtons(item);

         //s.Shown += (sender, e) => s.ScrollPosition = new Point(s.Width / 2, 0);
         detailsLayout.Add(s);
         // right here pal
         switch (item.Name)
         {
            case "OurStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  ListBuilder lb = (ListBuilder)detailsLayout.Children.First(x => x.ID == "ListBuilder");
                  //lb.Enabled = false;
                  lb.ContextMenu = new ContextMenu(new Command { MenuText = "Disabled/unfinished" });
                  lb.ToolTip = "Disabled/unfinished";
                  break;
               }
            case "TheirStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "Stock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "Trade":
               {
                  if (item.Parent is not null and Node parent && parent.Name == "Deliveries")
                  {
                     detailsLayout.Add(CreateListBuilder(item));
                  }
                  else
                  {
                     detailsLayout.Add(CreateDefaultFieldsGridView(item));
                  }
                  break;
               }
            case "Cells":
               {
                  //detailsLayout.Add(CreateShipCellsLayout(item));
                  break;
               }
            default:
               {
                  detailsLayout.Add(CreateDefaultFieldsGridView(item));
                  break;
               }
         }

         return detailsLayout;
      }
      //internal Scrollable CreateDefaultPanel(Node item)
      //{
      //   Scrollable scrollable = new()
      //   {
      //      ID = "DetailScrollable",
      //      Content = CreateDefaultFieldsGridView(item)
      //   };
      //   return scrollable;
      //}
      private static List<Node> CompileDerelictList(TreeGridItemCollection items)
      {
         List<Node> toRemove = new List<Node>();
         foreach (Node item in items)
         {
            if (item.Properties.Contains((object)"Type"))
            {
               var s = item.Properties[(object)"Name"];
               var t = item.Properties[(object)"Type"];
               if (s is not null && s.ToString() != "\"Stranded Ship\"" && t is not null && t.ToString() == "Derelict")
               {
                  toRemove.Add(item);
               }

            }
         }
         return toRemove;
      }
      private static string GetNodePath(Node item)
      {
         string path = item.Name;
         while (item.Parent != null && item.Parent.Parent != null)
         {
            item = (Node)item.Parent;
            path = $"{item.Name}/{path}";
         }
         return path;
      }
      private static Label GetNodePathLabel(Node item)
      {
         Label nodePathLabel = new Label { Text = GetNodePath(item), BackgroundColor = Colors.Silver, Font = new Font("Arial", 18, FontStyle.Bold) };
         return nodePathLabel;
      }
      private DynamicLayout GetNodePathButtons(Node item)
      {
         List<Button> buttonList = new();
         DynamicLayout pathButtonLayout = new();
         pathButtonLayout.BeginHorizontal();
         Button b = new()
         {
            Tag = item,
            Text = item.Name
         };
         buttonList.Add(b);
         while (item.Parent != null && item.Parent.Parent != null)
         {
            item = (Node)item.Parent;
            b = new()
            {
               Tag = item,
               Text = item.Name
            };
            buttonList.Add(b);
         }
         for (int i = buttonList.Count - 1; i >= 0; i--)
         {
            pathButtonLayout.Add(buttonList[i]);
            if (i > 0)
            {
               pathButtonLayout.Add(new Label() { Text = "/" });
            }
            buttonList[i].Click += NodePathNavClick;
         }
         pathButtonLayout.AddSpace();
         pathButtonLayout.EndHorizontal();
         return pathButtonLayout;
      }

      private void NodePathNavClick(object? sender, EventArgs e)
      {
         if (sender is not null and Button navButton && navButton.Tag is not null and Node ancester)
         {
            ExpandBranch(ancester);
            GetSearchBox().Text = string.Empty;
            GetTreeGridView().SelectedItem = ancester;
         }
      }
      private void ExpandBranch(Node node)
      {
         if (node.GetParent() is not null and Node p)
         {
            ExpandBranch(p);
         }
         node.Expanded = true;
      }

      //private static DynamicLayout CreateNodePathLayout(Node item)
      //{
      //   DynamicLayout layout = new DynamicLayout();
      //   layout.Add(GetNodePathLabel(item));
      //   return layout;
      //}
      private GridView CreateDefaultFieldsGridView(Node item)
      {
         OrderedDictionary vals = item.Properties;

         ObservableCollection<Oncler> bar = new();
         foreach (DictionaryEntry val in vals)
         {
            bar.Add(new Oncler(val));
         }
         GridView defaultGridView = new()
         {
            DataStore = bar,
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,
            ID = "DefaultGridView",
            ContextMenu = new ContextMenu(EditGridViewRow(), AddGridViewRow(), DeleteGridViewRow())
         };
         defaultGridView.Columns.Add(new GridColumn
         {
            HeaderText = "Key",
            DataCell = new TextBoxCell()
            {
               Binding = Binding.Property((DictionaryEntry i) => (string)i.Key)
            },
            Editable = false,
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
         });
         defaultGridView.Shown += DefaultGridView_Shown;
         defaultGridView.CellEditing += DefaultGridView_CellEditing;
         defaultGridView.CellEdited += DefaultGridView_CellEdited;
         bar.CollectionChanged += Bar_CollectionChanged;
         return defaultGridView;
      }
      private Command EditGridViewRow()
      {
         var editNewRow = new Command { MenuText = "Edit row" };
         editNewRow.Executed += EditRow_Executed;
         return editNewRow;
      }
      private Command AddGridViewRow()
      {
         var addNewRow = new Command { MenuText = "Add row" };
         addNewRow.Executed += AddRow_Executed;
         return addNewRow;
      }
      private Command DeleteGridViewRow()
      {
         var deleteRow = new Command { MenuText = "Delete row", /*Shortcut = Application.Instance.CommonModifier | Keys.Backspace*/ };
         deleteRow.Executed += DeleteRow_Executed;
         return deleteRow;
      }
      private GridView GetDefaultGridView()
      {
         return (GridView)((DetailsLayout)GetPanel2DetailsLayout().Content).FindChild("DefaultGridView");
      }
      private ListBuilder GetListBuilder()
      {
         return (ListBuilder)((DetailsLayout)GetPanel2DetailsLayout().Content).FindChild("ListBuilder");
      }
      private void Bar_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
      {
         UpdateApplyRevertButtons(DetailsLayout.State.Modified);
         // todo: register changes
         //if (e != null)
         //{
         //   switch (e.Action)
         //   {
         //      case NotifyCollectionChangedAction.Add:
         //         {
         //            break;
         //         }
         //      case NotifyCollectionChangedAction.Remove:
         //         {
         //            break;
         //         }
         //      default:
         //         {
         //            break;
         //         }
         //   }
         //}
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

      private void UpdateDetailsPanel(Node item, bool clearPreexisting = false)
      {
         if (clearPreexisting)
         {
            ClearItemFromCache(item);
         }
         DynamicLayout detailslayout = GetPanel2DetailsLayout();
         if (!DetailPanelsCache.ContainsKey(item))
         {
            DetailPanelsCache.Add(item, CreateDetailsLayout(item));
         }
         detailslayout.Content = DetailPanelsCache[item];
         UpdateApplyRevertButtons(DetailPanelsCache[item].Status);
      }
      private void ClearItemFromCache(Node item)
      {
         if (DetailPanelsCache.ContainsKey(item))
         {
            DetailPanelsCache.Remove(item);
         }
      }
      //private void UpdateTreeGridItemStatus(TreeGridItem item, NodeStatus status)
      //{
      //   item.Values[2] = status;
      //   if (item.Parent is not null and TreeGridItem parent)
      //   {
      //      NodeStatus parentStatus = (NodeStatus)parent.Values[2];
      //      if(status == NodeStatus.Default && parentStatus == NodeStatus.ChildEdited)
      //      {
      //         UpdateTreeGridItemStatus(parent, NodeStatus.Default);
      //      }
      //      else if () // just realized that this would clobber the ChildEdited status of a parent with a second child that's been edited
      //         // screw it--colors are back on the backburner
      //      {

      //      }
      //      switch((NodeStatus)parent.Values[2])
      //      {
      //         case NodeStatus.Default:
      //            {
      //               break;
      //            }
      //      }

      //   }
      //}
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
                  Apply.Enabled = Revert.Enabled = false;//todo: if the user wants to undo an applied change, they need to reload the node
                                                         //todo: add a way to reload the node
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
         return (DynamicLayout)this.FindChild("Panel2DetailsLayout");
         // pretty sure this blows up if the details layout isn't found
      }
      private TreeGridView GetTreeGridView()
      {
         return (TreeGridView)this.Children.Where<Control>(x => x.ID == "DataTreeView").First();
         // pretty sure this blows up if the data tree isn't found
      }
      private static bool ShipDispositionMatches(ShipDisposition required, ShipDisposition actual)
      {
         if (required == actual || required == ShipDisposition.Any)
         {
            return true;
         }
         else
         {
            return false;
         }
      }
      private static ShipDisposition StringDescToShipDisposition(string desc)
      {
         switch (desc)
         {
            case "FriendlyShip":
               {
                  return ShipDisposition.Friendly;
               }
            case "NeutralShip":
               {
                  return ShipDisposition.Neutral;
               }
            case "HostileShip":
               {
                  return ShipDisposition.Hostile;
               }
            case "Derelict":
               {
                  return ShipDisposition.Derelict;
               }
            case "ForSale":
               {
                  return ShipDisposition.ForSale;
               }
            default:
               {
                  return ShipDisposition.Any;
               }
         }
      }
      private Node? GetHud()
      {
         if (GetRoot() is not null and Node root)
         {
            foreach (Node item in root.Children)
            {
               if (item.Name == "HUD")
               {
                  return item;
               }
            }
         }
         return null;
      }
      private Node? GetMissionsNode()
      {
         if (GetRoot() is not null and Node root && root.FindChild("Missions", false, true) is not null and Node MissionsSupernode)
         {
            Node? MissionsNode = MissionsSupernode.FindChild("Missions");
            return MissionsNode;
         }
         //}
         return null;
      }
      private List<Node> FindMissions(string[] tags)
      {
         Node? MissionsNode = GetMissionsNode();
         List<Node> missions = new List<Node>();
         if (MissionsNode is not null)
         {
            foreach (Node mission in MissionsNode.Children)
            {
               if (ContainsOneOf(mission.Name, tags))
               {
                  missions.Add(mission);
               }
            }
         }

         return missions;
      }
      private List<Node> FindMissions(Dictionary<string, string> propertyKeysAndValues)
      {
         List<Node> missions = new();
         if (GetMissionsNode() is not null and Node MissionsNode)
         {
            foreach (Node mission in MissionsNode.Children)
            {
               Dictionary<string, string> propertyValues = new();
               foreach (var key in propertyKeysAndValues.Keys)
               {
                  propertyValues.Add(key, "");
               }
               mission.TryGetProperties(propertyValues);
               foreach (var propertyValue in propertyValues)
               {
                  if (propertyValue.Value.Equals(propertyKeysAndValues[propertyValue.Key]))
                  {
                     missions.Add(mission);
                  }
               }
            }
         }

         return missions;
      }
      private static bool ContainsOneOf(string stringToCheck, string[] stringsToCheckAgainst)
      {
         foreach (string stringToCheckAgainst in stringsToCheckAgainst)
         {
            if (stringToCheckAgainst.Equals(stringToCheck)) return true;
         }
         return false;
      }
      private Node? GetGalaxyNode()
      {
         if (GetRoot() is not null and Node root)
         {
            return GetChildNode(root, "Galaxy");
         }
         return null;
      }
      private List<Node> GetGalaxyObjects(bool all = false, Dictionary<string, string>? filters = null)
      {
         List<Node> matchingGalaxyObjects = new List<Node>();
         if (GetChildNode(GetGalaxyNode()!, "Objects") is not null and Node galaxyObjects)
         {
            foreach (Node galaxyObject in galaxyObjects.Children)
            {
               if (filters is not null && filters.Count > 0)
               {
                  Dictionary<string, string> val = new();
                  foreach (var key in filters.Keys)
                  {
                     val.Add(key, "");
                  }
                  if (galaxyObject.TryGetProperties(val))
                  {
                     int matchCount = 0;
                     foreach (var entry in val)
                     {
                        if (entry.Value.Equals(filters[entry.Key]))
                        {
                           matchCount++;
                        }
                     }
                     if ((all && matchCount == filters.Count) || matchCount > 0)
                     {
                        matchingGalaxyObjects.Add(galaxyObject);
                     }
                  }
               }
               else
               {
                  matchingGalaxyObjects.Add(galaxyObject);
               }
            }
         }

         return matchingGalaxyObjects;
      }
      private static Node? GetChildNode(Node item, string childname, bool looseMatch = false)
      {
         foreach (Node child in item.Children)
         {
            if (child.Name == childname || (looseMatch && child.Name.Contains(childname)))
            {
               return child;
            }
         }
         return null;
      }
      private List<Node> GetChildNodes(Node item, string childname, bool looseMatch = false)
      {
         List<Node> children = new();
         foreach (Node child in item.Children)
         {
            if (child.Name == childname || (looseMatch && child.Name.Contains(childname)))
            {
               children.Add(child);
            }
         }
         return children;
      }
      private List<Node> FindChildNodesWithProperties(Node item, string childname, bool looseMatch = false, List<string>? properties = null, bool all = false)
      {
         List<Node> children = new();
         foreach (Node child in item.Children)
         {
            if (child.Name == childname || (looseMatch && child.Name.Contains(childname)))
            {
               if (properties is not null)
               {
                  if (child.HasProperties(properties.ToArray(), all))
                  {
                     children.Add(child);
                  }
               }
               else
               {
                  children.Add(child);
               }
            }
         }
         return children;
      }
      private static List<Node> FindChildNodesWithProperty(Node item, string propertyName, string propertyValue = "")
      {// todo: do multiples
         List<Node> list = new();
         foreach (Node child in item.Children)
         {
            if (child.TryGetProperty(propertyName, out string value))
            {
               if ((propertyValue != "" && propertyValue == value) || propertyValue == "")
               {
                  list.Add(child);
               }
            }
         }
         return list;
      }
      private Node? GetSystemArchives()
      {
         return GetChildNode(GetGalaxyNode()!, "SystemArchives");
      }
      private Node? GetSystemArchive(string id)
      {
         Node systemArchives = GetSystemArchives()!;
         foreach (Node systemArchive in systemArchives.Children)
         {
            if (systemArchive.Name.Contains(id)) return systemArchive;
         }
         return null;
      }
      private Node? FindShip(string LayerId, ShipDisposition disposition = ShipDisposition.Any)
      {
         Dictionary<string, string> properyNamesAndValues = new()
         {
            {"Id", "" },
            { "Type", "" }
         };
         if (GetRoot() is not null and Node root)
         {
            foreach (Node item in root.Children)
            {
               if (item.TryGetProperties(properyNamesAndValues)
                  && properyNamesAndValues["Id"] == LayerId
                  //&& values["Class"] == "Ship"
                  && (properyNamesAndValues["Type"] == disposition.ToString() || disposition == ShipDisposition.Any))
               {
                  return item;
               }
            }
         }
         return null;
      }
      private Node? FindShip(ShipDisposition disposition = ShipDisposition.Any)
      {
         Dictionary<string, string> properyNamesAndValues = new()
         {
            {"Id", "" },
            { "Type", "" }
         };
         if (GetRoot() is not null and Node root)
         {
            foreach (Node item in root.Children)
            {
               if (item.TryGetProperties(properyNamesAndValues)
                  //&& values.ContainsKey("Class") && values["Class"] == "Ship"
                  && ShipDispositionMatches(disposition, StringDescToShipDisposition(properyNamesAndValues["Type"])))
               {
                  return item;
               }
            }
         }
         return null;
      }
      public bool DerelictsPresent()
      {
         if (GetRoot() is not null and Node root)
         {
            return DerelictsPresent(root);
         }
         return false;
      }
      private bool DerelictsPresent(Node item)
      {
         if (item.Children.Count == 0)
         {
            return false;
         }
         if (item.Properties.Contains((object)"Type") /*&& properties.Contains((object)"Class")*/)
         {
            var t = item.Properties[(object)"Type"];
            if (t is not null && t.ToString() == "Derelict")
            {
               return true;
            }
         }

         foreach (Node child in item.Children)
         {
            if (DerelictsPresent(child)) return true;
         }

         return false;
      }
      public bool ResetCamera()
      {
         bool success = false;
         Node? Hud = GetHud();
         if (Hud is not null)
         {
            Hud.TrySetProperty("Camera.x", "0");
            Hud.TrySetProperty("Camera.y", "0");
            success = Hud.TrySetProperty("ViewSize", "100");

            if (success)
            {
               UpdateDetailsPanel(Hud, true);
            }
         }
         return success;
      }
      public void CleanDerelicts(DerelictsCleaningMode mode)  // todo: this whole thing kinda sucks. Works though!
      {
         TreeGridItemCollection items = (TreeGridItemCollection)GetTreeGridView().DataStore;
         List<Node> toRemove;
         Node root = (Node)items.First();
         TreeGridItemCollection sysArchChildren = root[1][2].Children;
         switch (mode)
         {
            case DerelictsCleaningMode.CurrentSystem:
               {
                  toRemove = CompileDerelictList(root.Children);
                  foreach (var v in toRemove)
                  {
                     root.Children.Remove(v);
                     ClearItemFromCache(v);
                  }
                  break;
               }
            case DerelictsCleaningMode.SectorWide:
               {

                  foreach (Node sys in sysArchChildren)
                  {
                     toRemove = CompileDerelictList(sys.Children);
                     foreach (var v in toRemove)
                     {
                        sys.Children.Remove(v);
                        ClearItemFromCache(v);
                     }
                  }

                  toRemove = CompileDerelictList(root.Children);
                  foreach (var v in toRemove)
                  {
                     root.Children.Remove(v);
                     ClearItemFromCache(v);
                  }
                  break;
               }
               //case DerelictsCleaningMode.SpecificSystem:
               //   {
               //      // I don't know what to do here
               //      break;
               //   }
         }
         GetTreeGridView().DataStore = items;
         ClearDetails();
      }
      public bool CometExists()
      {
         List<Node> systemsWithComets = GetGalaxyObjects(false, new Dictionary<string, string> { { "Comet", "true" } });
         foreach (var systemWithComet in systemsWithComets)
         {
            Node? systemFreeSpace = null;
            systemWithComet.TryGetProperty("Id", out string systemId);
            if (GetSystemArchive(systemId) is not null and Node system)
            {
               if (GetChildNode(system, "FreeSpace", true) is not null and Node freeSpaceLayer)
               {
                  systemFreeSpace = GetChildNode(freeSpaceLayer, "Objects");
               }
            }
            else
            {
               if (FindCurrentSystemLayer("FreeSpace", systemId) is not null and Node freeSpaceLayer)
               {
                  systemFreeSpace = GetChildNode(freeSpaceLayer, "Objects");
               }
            }
            if (systemFreeSpace is not null)
            {
               if (this.freespaceObjectsWithComet is null)
               {
                  this.freespaceObjectsWithComet = new List<Node>();
               }
               this.freespaceObjectsWithComet.Add(systemFreeSpace);
            }
         }
         if (this.freespaceObjectsWithComet is not null && this.freespaceObjectsWithComet.Count > 0) return true;

         return false;
      }
      internal Node? FindCurrentSystemLayer(string layerName, string systemId = "")
      {
         if (GetRoot() is not null and Node root)
         {
            List<Node> currentSystemLayers = FindChildNodesWithProperties(root, layerName, true);
            if (!string.IsNullOrEmpty(systemId))
            {
               foreach (var currentSystemLayer in currentSystemLayers)
               {
                  if (currentSystemLayer.TryGetProperty("SystemId", out string layerSystemId))
                  {
                     if (systemId == layerSystemId)
                     {
                        return currentSystemLayer;
                     }
                  }

               }
            }
            return currentSystemLayers[0];
         }
         return null;
      }
      internal bool ResetComet()
      {
         bool success = true;
         if (freespaceObjectsWithComet is not null)
         {
            foreach (var freespaceWithComet in freespaceObjectsWithComet)
            {

               List<Node> comets = FindChildNodesWithProperty(freespaceWithComet, "Type", "Comet");
               //bool cometWasSelected = false;
               foreach (var comet in comets)
               {
                  success = success && comet.TrySetProperty("Position.x", "0");
                  success = success && comet.TrySetProperty("Position.y", "0");
                  ClearItemFromCache(comet);
                  if (comet == GetSelectedNode())
                  {
                     UpdateDetailsPanel(comet);
                  }
               }
            }
         }
         else
         {
            success = false;
         }

         return success;
      }
      internal bool DetectMeteors()
      {
         if (GetRoot() is not null and Node root)
         {
            weatherReports = FindChildNodesWithProperties(root, "Weather", false, new List<string> { "Meteors" });
            return weatherReports.Count > 0;
         }
         return false;
      }
      internal bool TurnOffMeteors()
      {
         if (weatherReports is not null && weatherReports.Count > 0)
         {
            int count = 0;
            foreach (var weatherReport in weatherReports)
            {
               if (weatherReport.TrySetProperty("Meteors", "false"))
               {
                  count++;
                  UpdateDetailsPanel(weatherReport, true);
               }
            }
            return count == weatherReports.Count;
         }
         else
         {
            return false;
         }
      }
      public bool AssertionFailureConditionExists(bool justTakeCareOfIt = false)
      {
         if (FindMissions(new string[] { "TutorialFlightReady" }) is not null and List<Node> missions && missions.Count > 0)
         {
            Node mission = missions.ElementAt(0);
            mission.TryGetProperty("AssignedLayerId", out string LayerId);
            Debug.WriteLine($"Mission is assigned to {LayerId}");
            Node? assignedShip = FindShip(LayerId);
            if (assignedShip is null
               || (assignedShip.TryGetProperty("Type", out string Disposition)
               && ShipDispositionMatches(ShipDisposition.Friendly, StringDescToShipDisposition(Disposition))))
            {
               if (justTakeCareOfIt)
               {
                  return JustTakeCareOfIt(mission);
               }
               else
               {
                  return true;
               }
            }
            else
            {
               return false;
            }
         }
         else
         {
            return false;
         }
      }
      private bool JustTakeCareOfIt(Node mission)
      {
         Debug.WriteLine("Could not find a friendly ship with that ID; finding any friendly ship");
         Node? assignedShip = FindShip(ShipDisposition.Friendly);
         if (assignedShip is not null)
         {
            assignedShip.TryGetProperty("Id", out string newShipID);
            if (mission.TrySetProperty("AssignedLayerId", newShipID))
            {
               Debug.WriteLine($"successfully transferred mission to {newShipID}");
               UpdateDetailsPanel(mission, true);
               return true;
            }
            else
            {
               Debug.WriteLine("Failed to reset assigned ship!");
               return false;
            }
         }
         else
         {
            Debug.WriteLine("Could not find any friendly ship! WTF?");
            return false;
         }
      }
      internal bool CrossSectorMissionsExist()
      {
         var property = new Dictionary<string, string> { { "Title", "mission_passengers_titlefurther" } };
         List<Node> missions = FindMissions(property);
         crossSectorMissions = new List<Node>();
         foreach (var mission in missions)
         {
            if (mission.TryGetProperty("ToSectorId", out string toSectorId))
            {
               crossSectorMissions.Add(mission);
            }
         }
         return crossSectorMissions.Count > 0;
      }
      internal bool SetCrossSectorMissionsDestination()
      {
         string[] options = new string[] { "Fair (selects random habitable systems)", "Fast (right here, right now)" };
         var y = new RadioInputDialog("Choose resolution type", options);
         int selectedOption;
         y.ShowModal(this.ParentWindow);
         if (y.GetDialogResult() == DialogResult.Ok)
         {
            selectedOption = y.GetSelectedIndex();
            //MessageBox.Show($"{options[selectedOption]}");
         }
         else
         {
            return false;
         }
         string currentSystemId = GetCurrentSystemId(null);
         List<string> reachableSystems = new List<string>();
         if (options[selectedOption].StartsWith("Fair"))
         {
            // var d = Math.sqrt((x - h)^2+(y - k)^2);
            // if(d <= r) unreachable
            Node galaxy = GetGalaxyNode()!;
            Node currentSystem = GetChildNode(GetChildNode(galaxy, "Objects")!, currentSystemId, true)!;
            Dictionary<string, string> currentSystemData = new()
            {
               {"Position.x","" },
               { "Position.y",""}
            };
            currentSystem.TryGetProperties(currentSystemData);
            double currentSystemX = Double.Parse(currentSystemData["Position.x"]);
            double currentSystemY = Double.Parse(currentSystemData["Position.y"]);
            Dictionary<string, string> voidData = new()
            {
               {"VoidPosition.x","" },
               { "VoidPosition.y", "" },
               { "VoidRadius","" }
            };
            galaxy.TryGetProperties(voidData);
            double voidX = Double.Parse(voidData["VoidPosition.x"]);
            double voidY = Double.Parse(voidData["VoidPosition.y"]);
            double voidR = Double.Parse(voidData["VoidRadius"]);
            // get galaxy.objects where colony is true or shipyard is true
            List<Node> habitableSystems = GetGalaxyObjects(false
               , new Dictionary<string, string> { { "Colony", "true" }, { "Shipyard", "true" } });//the "true" is not, strictly speaking, necessary, but it fits the paradigm I devised

            // loop, run the math
            foreach (var habitableSystem in habitableSystems)
            {
               Dictionary<string, string> vals = new()
               {
                  { "Position.x", "" },
                  { "Position.y", "" },
                  { "Id", "" }
               };
               habitableSystem.TryGetProperties(vals);
               double habitableSystemX = Double.Parse(vals["Position.x"]);
               double habitableSystemY = Double.Parse(vals["Position.y"]);
               string habitableSystemId = vals["Id"];

               // calc distance from current system to habitableSystem
               var distanceToHabitableSystem = CalculateDistance(currentSystemX, currentSystemY, habitableSystemX, habitableSystemY);
               Debug.WriteLine($"Distance from Sys {currentSystemId} to Sys {habitableSystemId}: {distanceToHabitableSystem}");
               // calc void expansion for travel
               var projectedVoidExpansion = distanceToHabitableSystem * .5;
               Debug.WriteLine($"Projected Void expansion: {projectedVoidExpansion}");
               // check if habitableSystem is reachable
               if (IsSystemReachable(voidX, voidY, voidR + projectedVoidExpansion, habitableSystemX, habitableSystemY)) { reachableSystems.Add(habitableSystemId); }
            }
         }
         else if (options[selectedOption].StartsWith("Fast"))
         {
            reachableSystems.Add(currentSystemId);
         }

         if (crossSectorMissions is not null)
         {
            Random rand = new();
            foreach (var mission in crossSectorMissions)
            {
               int randomIndex = rand.Next(reachableSystems.Count);
               mission.ReplaceProperty("ToSectorId", "ToSystemId", reachableSystems[randomIndex]);
               ClearItemFromCache(mission);
               if (mission == GetSelectedNode())
               {
                  UpdateDetailsPanel(mission);
               }
            }
            return true;
         }

         return false;
      }
      internal static bool IsSystemReachable(double voidX, double voidY, double voidRadius, double systemX, double systemY)
      {
         return CalculateDistance(voidX, voidY, systemX, systemY) > voidRadius;
      }
      internal static double CalculateDistance(double point1X, double point1Y, double point2X, double point2Y)
      {
         return Math.Sqrt(Math.Pow(point2X - point1X, 2) + Math.Pow((point2Y - point1Y), 2));
      }
      internal bool FindDeadCrew()
      {
         if (friendlyShips is null && GetRoot() is not null and Node root)
         {
            friendlyShips = FindChildNodesWithProperty(root, "Type", "FriendlyShip");
         }
         foreach (var layer in friendlyShips!)
         {
            if (GetChildNode(layer, "Objects") is not null and Node objects)
            {
               deadCrew = FindChildNodesWithProperties(objects, "\"[i ", true, new List<string> { "Type", "State", "CauseOfDeath" }, true);
            }
         }
         return deadCrew is not null && deadCrew.Count > 0;
         //return false;
      }
      internal List<Node> GetFriendlyShips()
      {
         if (friendlyShips is null && GetRoot() is not null and Node root)
         {
            friendlyShips = FindChildNodesWithProperty(root, "Type", "FriendlyShip");
         }
         return friendlyShips!;
      }
      internal bool ClearDeadCrew()
      {
         if (friendlyShips is null && GetRoot() is not null and Node root)
         {
            friendlyShips = FindChildNodesWithProperty(root, "Type", "FriendlyShip");
         }
         foreach (var layer in friendlyShips!)
         {
            if (GetChildNode(layer, "Objects") is not null and Node objects && deadCrew is not null)
            {
               foreach (var deadCrewmember in deadCrew)
               {
                  if (objects.Children.Contains(deadCrewmember))
                  {
                     objects.Children.Remove(deadCrewmember);
                     ClearItemFromCache(deadCrewmember);
                  }
               }
            }
         }
         return true; // todo: this should probably actually be tied to something succeeding
      }
      internal bool RemoveHab(string shipId)
      {
         Node ship = FindShip(shipId)!;
         Node? palette = ship.FindChild("Palette", false, true);
         if (palette is not null)
         {
            string[] keys = new string[palette.Properties.Count];
            palette.Properties.Keys.CopyTo(keys, 0);
            foreach (string key in keys)
            {
               if (palette.Properties[key] is not null and string value)
               {
                  palette.Properties[key] = value.Replace("Habitation true", "");
               }
            }
            return true;
         }
         return false;
      }
      /// <summary>
      /// Pass nothing to get the CurrentSystem from the Galaxy node, pass null to get the current system of the first friendly ship, pass a LayerId to get the current system of a particular friendly ship
      /// </summary>
      /// <param name="shipId"></param>
      /// <returns></returns>
      internal string GetCurrentSystemId(string? shipId = "")
      {
         string sysId = string.Empty;
         if (shipId == string.Empty)
         {
            GetGalaxyNode()!.TryGetProperty("CurrentSystem", out sysId);
         }
         else
         {
            Node ship;
            if (shipId is not null)
            {
               ship = FindShip(shipId, ShipDisposition.Friendly)!;
            }
            else
            {
               ship = FindShip(ShipDisposition.Friendly)!;
            }
            ship.TryGetProperty("SystemId", out sysId);
         }
         return sysId;
      }
      private Node? GetRoot()
      {
         if (mainForm is not null)
         {
            return mainForm.saveFile.Root;
         }
         return null;
      }
      public void Rebuild(Node root)
      {
         DetailPanelsCache.Clear();
         ClearDetails();
         Root = root;
         RebuildTreeView(Root);
         crossSectorMissions = null;
         freespaceObjectsWithComet = null;
      }
      private void ClearDetails()
      {
         DynamicLayout s = GetPanel2DetailsLayout();
         s.Content = null;
         UpdateApplyRevertButtons(DetailsLayout.State.Unmodified);
      }
      private void RebuildTreeView(Node root)
      {
         TreeGridView treeView = GetTreeGridView();
         TreeGridItemCollection collection = new()
         {
            root
         };
         collection[0].Expanded = true;
         treeView.DataStore = collection;

         if (treeView.Columns.Count == 0)
         {
            GridColumn column = new()
            {
               AutoSize = true,
               DataCell = new TextBoxCell("Name")
            };
            treeView.Columns.Add(column);
         }
      }
      public void RefreshTree()
      {
         TreeGridView treeView = GetTreeGridView();
         treeView.DataStore = (TreeGridItemCollection)treeView.DataStore;
      }
      private void TreeView_CellFormatting(object? sender, GridCellFormatEventArgs e)
      {
         if (sender is not null and TreeGridView tree)
         {
            if (mainForm is not null)
            {
               if (mainForm.prefs.holidayFun.value is not null and yesno holidayfun && holidayfun == yesno.yes)
               {
                  var today = DateTime.Today;

                  DateTime ChristmasDay = new(DateTime.Now.Year, 12, 25, 0, 0, 0);
                  DateTime NewYearDay = new(DateTime.Now.Year, 1, 1, 0, 0, 0);

                  //today = ChristmasDay.AddDays(8);
                  if ((today >= ChristmasDay.AddDays(-5)) && (today <= ChristmasDay.AddDays(7)))
                  {
                     if (e.Item is not null /*and Node item*/)
                     {
                        e.ForegroundColor = e.Row % 2 == 0 ? Colors.Green : Colors.Red;
                        //e.BackgroundColor = (Color)newColor;
                     }
                  }
                  else if (today >= NewYearDay && today < NewYearDay.AddDays(3))
                  {
                     e.ForegroundColor = Colors.SaddleBrown;
                  }
               }
            }

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
            return new Size(control.Width /*- 10*/, control.Height - offset);
         }
         if (control.Parent != null)
         {
            control.Size = GetTheSizeUnderControl(control.Parent, gridView);
         }
         return control.Size;
      }
      private void DetailsModified()
      {
         Apply.Enabled = Revert.Enabled = true;

         ((DetailsLayout)GetPanel2DetailsLayout().Content).Status = DetailsLayout.State.Modified;
      }
      private void DetailsUnmodified()
      {
         Apply.Enabled = Revert.Enabled = false;

         ((DetailsLayout)GetPanel2DetailsLayout().Content).Status = DetailsLayout.State.Unmodified;
      }
      private void DetailsApplied()
      {
         ((DetailsLayout)GetPanel2DetailsLayout().Content).Status = DetailsLayout.State.Applied;
         UpdateApplyRevertButtons(DetailsLayout.State.Applied);
      }
      public bool ChangesAreUnapplied()
      {
         foreach (var v in DetailPanelsCache)
         {
            if (((DetailsLayout)v.Value).Status == DetailsLayout.State.Modified)
            {
               return true;
            }
         }
         return false;
      }
      internal void ApplyAllChanges()
      {
         foreach (var v in DetailPanelsCache)
         {
            if (((DetailsLayout)v.Value).Status == DetailsLayout.State.Modified)
            {
               ApplyChange(v.Key, v.Value.Children.First(x => x.ID == "DefaultGridView" || x.ID == "ListBuilder")); // todo: _really_ need to genericize this!
               v.Value.Status = DetailsLayout.State.Applied;
            }
         }
         UpdateApplyRevertButtons(DetailsLayout.State.Applied);
         dataState = DataState.Unsaved;
      }
      internal void RevertAllUnappliedChanges()
      {
         List<KeyValuePair<Node, DetailsLayout>> cachedPanels = DetailPanelsCache.ToList();
         foreach (var panel in cachedPanels)
         {
            if (((DetailsLayout)panel.Value).Status == DetailsLayout.State.Modified)
            {
               UpdateDetailsPanel(panel.Key, true);
            }
         }
         UpdateApplyRevertButtons(DetailsLayout.State.Unmodified);
         SubtractUnappliedFromDataState();
      }
      /// <summary>
      /// Returns -1 if no columns are editable
      /// </summary>
      /// <param name="grid"></param>
      /// <returns></returns>
      private static int GetFirstEditableColumn(GridView grid)
      {
         foreach (var column in grid.Columns)
         {
            if (column.Editable) return column.DisplayIndex;
         }

         return -1;
      }
      internal void ResetDataState()
      {
         dataState = DataState.Unchanged;
      }
      internal bool DataStateMatches(DataState state)
      {
         return dataState == state;
      }
      internal void AddUnappliedToDataState()
      {
         if (dataState == DataState.Unchanged)
         {
            dataState = DataState.Unapplied;
         }
         else if (dataState == DataState.Unsaved)
         {
            dataState = DataState.UnsavedAndUnapplied;
         }
      }
      internal void AddUnsavedToDataState()
      {
         if (dataState == DataState.Unchanged)
         {
            dataState = DataState.Unsaved;
         }
         else if (dataState == DataState.Unapplied)
         {
            dataState = DataState.UnsavedAndUnapplied;
         }
      }
      internal void SubtractUnappliedFromDataState()
      {
         if (dataState == DataState.Unapplied)
         {
            dataState = DataState.Unchanged;
         }
         else if (dataState == DataState.UnsavedAndUnapplied)
         {
            dataState = DataState.Unsaved;
         }
      }
      internal void SubtractUnsavedFromDataState()
      {
         if (dataState == DataState.Unsaved)
         {
            dataState = DataState.Unchanged;
         }
         else if (dataState == DataState.UnsavedAndUnapplied)
         {
            dataState = DataState.Unapplied;
         }
      }
      #region event handlers
      private void DeleteRow_Executed(object? sender, EventArgs e)
      {
         GridView grid = GetDefaultGridView();
         if (grid.SelectedRow >= 0)
         {
            Oncler row = (Oncler)grid.SelectedItem;
            CollectionChange.AddChange(((DetailsLayout)GetPanel2DetailsLayout().Content).Changes, row, CollectionChange.ActionType.Deletion);

            ((ObservableCollection<Oncler>)grid.DataStore).Remove(row);
            DetailsModified();
            AddUnappliedToDataState();
         }
      }

      private void EditRow_Executed(object? sender, EventArgs e)
      {
         GridView grid = GetDefaultGridView();
         int selectedRow = grid.SelectedRow;
         int firstEditableColumn = GetFirstEditableColumn(grid);
         if (selectedRow >= 0 && firstEditableColumn >= 0)
         {
            grid.BeginEdit(selectedRow, firstEditableColumn);
         }
      }

      private void AddRow_Executed(object? sender, EventArgs e)
      {
         GridView grid = GetDefaultGridView();
         ObservableCollection<Oncler> onclers = ((ObservableCollection<Oncler>)grid.DataStore);
         int i = onclers.Count;

         TextInputDialog dialog = new("New row", "Key");
         dialog.ShowModal(this);
         if (dialog.GetDialogResult() == DialogResult.Ok)
         {
            string newkey = dialog.GetInput();
            bool alreadyInUse = false;
            foreach (var f in onclers)
            {
               if (f.Key == newkey) alreadyInUse = true;
            }
            if (alreadyInUse)
            {
               MessageBox.Show("That key is already in use", "Error", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
            }
            else
            {
               Oncler newRow = new(newkey);
               CollectionChange.AddChange(((DetailsLayout)GetPanel2DetailsLayout().Content).Changes, newRow, CollectionChange.ActionType.Addition);
               onclers.Add(newRow);
               grid.SelectRow(i);
               grid.BeginEdit(i, 1);
            }
         }
      }
      private void DefaultGridView_CellEdited(object? sender, GridViewCellEventArgs e)
      {
         //DetailsLayout.State state = DetailsLayout.State.Unmodified;
         if (CurrentValues is not null && CurrentValues.IsChanged(((Oncler)e.Item).ToDictionaryEntry())) // todo: revise this so manually setting changes back runs Unmodified!
         {

            //Changes.Add(new CollectionChange(CollectionChange.ActionType.Change, CurrentValues.deV));
            //state = DetailsModified();
            DetailsModified();
            AddUnappliedToDataState();
         }
         else
         {
            DetailsUnmodified();
         }
         //if (state == DetailsLayout.State.Modified)
         //{
         //   TreeGridView tree = GetTreeGridView();
         //   UpdateTreeGridItemStatus((TreeGridItem)tree.SelectedItem, NodeStatus.Edited);
         //   //tree.TriggerStyleChanged();
         //}
      }

      private void DefaultGridView_CellEditing(object? sender, GridViewCellEventArgs e)
      {
         CurrentValues = new PreviousEntry(e.Column, e.Row, ((Oncler)e.Item).ToDictionaryEntry());
         //Oncler values = ((Oncler)e.Item);
         //if (CollectionChange.ListContainsOncler(Changes, values))
         //{
         //   CollectionChange.ActionType action = CollectionChange.GetOnclersChangeAction(Changes, values);
         //   if (action == CollectionChange.ActionType.Change
         //      || action == CollectionChange.ActionType.Addition)
         //   {
         //      CollectionChange.UpdateChangeValue(Changes, values);
         //   }
         //}
         //else
         //{
         //   Changes.Add(new CollectionChange(CollectionChange.ActionType.Change, values));
         //}

      }

      private void TreeView_SelectedItemChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView view)
         {
            Node item = (Node)view.SelectedItem;
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
      //private void X_CellFormatting(object? sender, GridCellFormatEventArgs e)
      //{
      //   //e.BackgroundColor = e.Row % 2 == 0 ? Colors.Blue : Colors.LightBlue;
      //   if (e.Item is not null and TreeGridItem item && item.Values.Length > 2
      //      && item == ((TreeGridView)sender!).SelectedItem && (NodeStatus)item.Values[2] != NodeStatus.Default)
      //   {
      //      Color? newColor = null;
      //      NodeStatus s = (NodeStatus)item.Values[2];
      //      switch (s)
      //      {
      //         case NodeStatus.Edited:
      //            {
      //               newColor = Colors.Orange;
      //               break;
      //            }
      //         case NodeStatus.ChildEdited:
      //            {
      //               newColor = Colors.Peru;
      //               break;
      //            }
      //         case NodeStatus.Deleted:
      //            {
      //               newColor = Colors.OrangeRed;
      //               break;
      //            }
      //      }
      //      if (newColor is not null)
      //      {
      //         e.BackgroundColor = (Color)newColor;
      //      }

      //   }
      //   //Colors.
      //}
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

      private void RevertButton_Click(object? sender, EventArgs e)
      {
         Node item = GetSelectedNode();
         UpdateDetailsPanel(item, true);
      }

      private void ApplyButton_Click(object? sender, EventArgs e) // todo: generic way to get ahold of the current details-details panel (damn, I've really screwed up the nomenclature...)
      {
         Node item = GetSelectedNode();
         Control detailControl = GetDetailsControl();
         if (detailControl is not null)
         {
            dirtyBit = true;
            dataState = DataState.Unsaved;
            ApplyChange(item, detailControl);

            DetailsApplied();
         }
      }

      private Control GetDetailsControl()
      {
         DynamicLayout detailsLayout = (DynamicLayout)GetPanel2DetailsLayout().Content;
         Control detailsControl = detailsLayout.FindChild("DefaultGridView");
         if (detailsControl is null)
         {
            detailsControl = detailsLayout.FindChild("ListBuilder");
         }
         return detailsControl;
      }

      private static void ApplyChange(Node item, Control detailControl)
      {
         OrderedDictionary itemDictionary = new OrderedDictionary();

         if (detailControl is not null)
         {
            if (detailControl is GridView gridView)
            {
               ObservableCollection<Oncler> gridCollection = (ObservableCollection<Oncler>)gridView.DataStore;
               foreach (var oncler in gridCollection)
               {
                  itemDictionary.Add(oncler.Key, oncler.Value);
               }
            }
            else if (detailControl is ListBuilder lb)
            {
               ObservableCollection<InventoryGridItem> listItems = lb.GetRightList();
               foreach (var entry in listItems)
               {
                  itemDictionary.Add(entry.Name, entry.Count);
               }
            }
            item.Properties = itemDictionary;
         }
      }

      private Node GetSelectedNode()
      {
         return (Node)GetTreeGridView().SelectedItem;
      }
      private void RightGrid_Updated(object? sender, EventArgs? e)
      {
         if (sender is not null and ObservableCollection<InventoryGridItem> RightList)
         {
            if (GetTreeGridView() is not null and TreeGridView treeGrid)
            {
               if (treeGrid.SelectedItem is Node item)
               {
                  Dictionary<string, string> currentData = new Dictionary<string, string>();
                  foreach (var entry in RightList)
                  {
                     currentData.Add(entry.Name, entry.Count.ToString());
                  }
                  var dict = item.Properties.Cast<DictionaryEntry>().ToDictionary(k => (string)k.Key, v => v.Value!.ToString());
                  if (ListBuilder.IsGridModified(dict!, currentData))
                  {
                     DetailsModified();
                  }
                  else
                  {
                     DetailsUnmodified();
                  }
               }
            }
         }
      }

      #endregion event handlers
   }


   public class PreviousEntry // todo: revise this
   {
      public int Column { get; set; }
      public int Row { get; set; }
      public string? Value { get; set; }
      DictionaryEntry? deValue { get; set; }
      public PreviousEntry(int col, int row, string value)
      {
         Column = col;
         Row = row;
         Value = value;
      }
      public PreviousEntry(int col, int row, DictionaryEntry entry)
      {
         Column = col;
         Row = row;
         deValue = entry;
      }
      public bool IsChanged(string value) // these don't work as intended--can't recognize when the value is set back to the initial value
      {
         return !this.Value!.Equals(value, StringComparison.CurrentCultureIgnoreCase);
      }
      public bool IsChanged(DictionaryEntry value) // these don't work as intended--can't recognize when the value is set back to the initial value
      {
         string x = string.Empty, y = string.Empty;
         if (Column == 0)
         {
            x = deValue!.Value.Key.ToString()!;
            y = value.Key.ToString()!;
         }
         else if (Column == 1)
         {
            x = deValue!.Value.Value!.ToString()!;
            y = value.Value!.ToString()!;
         }

         return x != y;
      }
   }
}

