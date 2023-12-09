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
      private DataState dataState = DataState.Unchanged;
      internal bool dirtyBit = false;
      private Dictionary<TreeGridItem, DetailsLayout> DetailPanelsCache = new();
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
      private List<TreeGridItem>? systemsWithComets = null;
      private List<TreeGridItem>? crossSectorMissions = null;
      private List<TreeGridItem>? weatherReports = null;
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
         Apply.Click += ApplyButton_Click;
         Revert.Click += RevertButton_Click;
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
            //AllowEmptySelection = true,
            //AllowMultipleSelection = true
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
            var value = item.Values[1];
            if (value is OrderedDictionary dictionary && dictionary.Count != 0)
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
                  if (((TreeGridItem)item.Parent).Tag.ToString() == "Deliveries")
                  {
                     detailsLayout.Add(CreateListBuilder(item));
                  }
                  else
                  {
                     Scrollable scrollable = new()
                     {
                        ID = "DetailScrollable",
                        Content = CreateDefaultFieldsGridView(item)
                     };
                     //detailsLayout.Add(CreateDefaultFields(item));
                     detailsLayout.Add(scrollable);
                  }
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
                     ID = "DetailScrollable",
                     Content = CreateDefaultFieldsGridView(item)
                  };
                  //detailsLayout.Add(CreateDefaultFields(item));
                  detailsLayout.Add(scrollable);
                  break;
               }
         }

         return detailsLayout;
      }
      private static List<TreeGridItem> CompileDerelictList(TreeGridItemCollection items)
      {
         List<TreeGridItem> toRemove = new List<TreeGridItem>();
         foreach (TreeGridItem item in items)
         {
            OrderedDictionary properties = (OrderedDictionary)item.Values[1];
            if (/*properties.Contains((object)"Class")
               && */properties.Contains((object)"Type"))
            {
               var s = properties[(object)"Name"];
               var t = properties[(object)"Type"];
               if (s is not null && s.ToString() != "\"Stranded Ship\"" && t is not null && t.ToString() == "Derelict")
               {
                  toRemove.Add(item);
               }

            }
         }
         return toRemove;
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
         ObservableCollection<Oncler> bar = new ObservableCollection<Oncler>();
         foreach (DictionaryEntry val in vals)
         {
            bar.Add(new Oncler(val));
         }
         GridView defaultGridView = new()
         {
            DataStore = bar,
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,
            ID = "DefaultGridView"

         };
         defaultGridView.ContextMenu = new ContextMenu(EditGridViewRow(), AddGridViewRow(), DeleteGridViewRow());
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

      private void UpdateDetailsPanel(TreeGridItem item, bool clearPreexisting = false)
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
      private void ClearItemFromCache(TreeGridItem item)
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
      private TreeGridItem? GetHud()
      {
         TreeGridItem root = GetRoot();
         foreach (TreeGridItem item in root.Children)
         {
            if (item.Tag.ToString() == "HUD")
            {
               return item;
            }
         }
         return null;
      }
      private TreeGridItem? GetMissionsNode()
      {
         TreeGridItem root = GetRoot();
         TreeGridItem? MissionsSupernode = null;
         foreach (TreeGridItem item in root.Children)
         {
            if (item.Tag.ToString() == "Missions")
            {
               MissionsSupernode = item;
               break;
            }
         }
         if (MissionsSupernode is not null)
         {
            TreeGridItem? MissionsNode = (TreeGridItem)MissionsSupernode.Children[0];
            if (MissionsNode.Tag.ToString() == "Missions")
            {
               return MissionsNode;
            }
         }
         return null;
      }
      private List<TreeGridItem> FindMissions(string[] tags)
      {
         TreeGridItem? MissionsNode = GetMissionsNode();
         List<TreeGridItem> missions = new List<TreeGridItem>();
         if (MissionsNode is not null)
         {
            foreach (TreeGridItem mission in MissionsNode.Children.Cast<TreeGridItem>())
            {
               if (ContainsOneOf(mission.Tag.ToString()!, tags))
               {
                  missions.Add(mission);
               }
            }
         }

         return missions;
      }
      private List<TreeGridItem> FindMissions(Dictionary<string, string> propertyKeysAndValues)
      {
         List<TreeGridItem> missions = new();
         if (GetMissionsNode() is not null and TreeGridItem MissionsNode)
         {
            foreach (TreeGridItem mission in MissionsNode.Children.Cast<TreeGridItem>())
            {
               TryGetProperties(mission, propertyKeysAndValues.Keys.ToArray(), out Dictionary<string, string> propertyValues);
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
      private TreeGridItem? GetGalaxyNode()
      {
         return GetChildNode(GetRoot(), "Galaxy");
      }
      private List<TreeGridItem> GetGalaxyObjects(bool all = false, Dictionary<string, string>? filters = null)
      {
         List<TreeGridItem> matchingGalaxyObjects = new List<TreeGridItem>();
         if (GetChildNode(GetGalaxyNode()!, "Objects") is not null and TreeGridItem galaxyObjects)
         {
            foreach (TreeGridItem galaxyObject in galaxyObjects.Children)
            {
               if (filters is not null && filters.Count > 0)
               {
                  //foreach (string filter in filters)
                  //{
                  if (TryGetProperties(galaxyObject, filters.Keys.ToArray(), out Dictionary<string, string> val))
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
                  //}
               }
               else
               {
                  matchingGalaxyObjects.Add(galaxyObject);
               }
            }
         }

         return matchingGalaxyObjects;
      }
      private TreeGridItem? GetChildNode(TreeGridItem item, string childname, bool looseMatch = false)
      {
         foreach (TreeGridItem child in item.Children)
         {
            if (child.Tag.ToString() == childname || (looseMatch && child.Tag.ToString()!.Contains(childname)))
            {
               return child;
            }
         }
         return null;
      }
      private List<TreeGridItem> GetChildNodes(TreeGridItem item, string childname, bool looseMatch = false)
      {
         List<TreeGridItem> children = new();
         foreach (TreeGridItem child in item.Children)
         {
            if (child.Tag.ToString() == childname || (looseMatch && child.Tag.ToString()!.Contains(childname)))
            {
               children.Add(child);
            }
         }
         return children;
      }
      private List<TreeGridItem> FindChildNodesWithProperties(TreeGridItem item, string childname, bool looseMatch = false, List<string>? properties = null)
      {
         List<TreeGridItem> children = new();
         foreach (TreeGridItem child in item.Children)
         {
            if (child.Tag.ToString() == childname || (looseMatch && child.Tag.ToString()!.Contains(childname)))
            {
               if (properties is not null)
               {
                  if (HasProperties(child, properties.ToArray()))
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
      private List<TreeGridItem> FindChildNodesWithProperty(TreeGridItem item, string propertyName, string propertyValue = "")
      {// todo: do multiples
         List<TreeGridItem> list = new();
         foreach (TreeGridItem child in item.Children.Cast<TreeGridItem>())
         {
            if (TryGetProperty(child, propertyName, out string value))
            {
               if ((propertyValue != "" && propertyValue == value) || propertyValue == "")
               {
                  list.Add(child);
               }
            }
         }
         return list;
      }
      private TreeGridItem? GetSystemArchives()
      {
         return GetChildNode(GetGalaxyNode()!, "SystemArchives");
      }
      private TreeGridItem? GetSystemArchive(string id)
      {
         TreeGridItem systemArchives = GetSystemArchives()!;
         foreach (TreeGridItem systemArchive in systemArchives.Children)
         {
            if (systemArchive.Tag.ToString()!.Contains(id)) return systemArchive;
         }
         return null;
      }
      private TreeGridItem? FindShip(string LayerId, ShipDisposition disposition = ShipDisposition.Any)
      {
         string[] propertyNames = new string[] { /*"Class",*/ "Id", "Type" };
         TreeGridItem root = GetRoot();
         foreach (TreeGridItem item in root.Children.Cast<TreeGridItem>())
         {
            TryGetProperties(item, propertyNames, out Dictionary<string, string> values);
            if (values["Id"] == LayerId
               //&& values["Class"] == "Ship"
               && values["Type"] == disposition.ToString())
            {
               return item;
            }
         }
         return null;
      }
      private TreeGridItem? FindShip(ShipDisposition disposition = ShipDisposition.Any)
      {
         string[] propertyNames = new string[] { /*"Class",*/ "Id", "Type" };
         TreeGridItem root = GetRoot();
         foreach (TreeGridItem item in root.Children)
         {
            if (TryGetProperties(item, propertyNames, out Dictionary<string, string> values)
               //&& values.ContainsKey("Class") && values["Class"] == "Ship"
               && ShipDispositionMatches(disposition, StringDescToShipDisposition(values["Type"])))
            {
               return item;
            }
         }
         return null;
      }
      public bool DerelictsPresent()
      {
         return DerelictsPresent(GetRoot());
      }
      private bool DerelictsPresent(TreeGridItem item)
      {
         if (item.Children.Count == 0)
         {
            return false;
         }
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         if (properties.Contains((object)"Type") /*&& properties.Contains((object)"Class")*/)
         {
            var t = properties[(object)"Type"];
            if (t is not null && t.ToString() == "Derelict")
            {
               return true;
            }
         }

         foreach (TreeGridItem child in item.Children.Cast<TreeGridItem>())
         {
            if (DerelictsPresent(child)) return true;
         }

         return false;
      }
      public bool ResetCamera()
      {
         bool success = false;
         TreeGridItem? Hud = GetHud();
         if (Hud is not null)
         {
            TrySetProperty(Hud, "Camera.x", "0");
            TrySetProperty(Hud, "Camera.y", "0");
            success = TrySetProperty(Hud, "ViewSize", "100");

            if (success)
            {
               UpdateDetailsPanel(Hud, true);
            }
         }
         return success;
      }
      public void CleanDerelicts(DerelictsCleaningMode mode)  // todo: this whole thing kinda sucks. Works though!
      {
         TreeGridView tree = GetTreeGridView();
         TreeGridItemCollection items = (TreeGridItemCollection)tree.DataStore;
         List<TreeGridItem> toRemove;
         TreeGridItem root = (TreeGridItem)items.First();
         TreeGridItemCollection sysArchChildren = ((TreeGridItem)((TreeGridItem)root[1])[2]).Children;
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

                  foreach (TreeGridItem sys in sysArchChildren)
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
         tree.DataStore = items;
         ClearDetails();
      }
      public bool CometExists()
      {
         TreeGridItem? galaxy = GetGalaxyNode();
         if (galaxy is not null)
         {
            TreeGridItem? galaxyObjects = GetChildNode(galaxy, "Objects");
            if (galaxyObjects is not null)
            {
               systemsWithComets = FindChildNodesWithProperty(galaxyObjects, "Comet", "true");
               if (systemsWithComets.Count > 0) return true;
            }
         }

         return false;
      }
      internal bool ResetComet()
      {
         bool success = true;
         if (systemsWithComets is not null)
         {
            foreach (var systemWithComet in systemsWithComets)
            {
               // get each ID property
               if (TryGetProperty(systemWithComet, "Id", out string systemId))
               {
                  TreeGridItem system = GetSystemArchive(systemId)!;
                  List<TreeGridItem> comets = FindChildNodesWithProperty(GetChildNode(GetChildNode(system, "FreeSpace", true)!, "Objects")!, "Type", "Comet");
                  //bool cometWasSelected = false;
                  foreach (var comet in comets)
                  {
                     success = success && TrySetProperty(comet, "Position.x", "0");
                     success = success && TrySetProperty(comet, "Position.y", "0");
                     ClearItemFromCache(comet);
                     if (comet == GetSelectedTreeGridItem())
                     {
                        UpdateDetailsPanel(comet);
                     }
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
         TreeGridItem root = GetRoot();
         weatherReports = FindChildNodesWithProperties(root, "Weather", false, new List<string> { "Meteors" });
         return weatherReports.Count > 0;
      }
      internal bool TurnOffMeteors()
      {
         if (weatherReports is not null && weatherReports.Count > 0)
         {
            int count = 0;
            foreach (var weatherReport in weatherReports)
            {
               if (TrySetProperty(weatherReport, "Meteors", "false"))
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
         if (FindMissions(new string[] { "TutorialFlightReady" }) is not null and List<TreeGridItem> missions && missions.Count > 0)
         {
            TreeGridItem mission = missions.ElementAt(0);
            TryGetProperty(mission, "AssignedLayerId", out string LayerId);
            Debug.WriteLine($"Mission is assigned to {LayerId}");
            TreeGridItem? assignedShip = FindShip(LayerId);
            if (assignedShip is null
               || (TryGetProperty(assignedShip, "Type", out string Disposition)
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
      private bool JustTakeCareOfIt(TreeGridItem mission)
      {
         Debug.WriteLine("Could not find a friendly ship with that ID; finding any friendly ship");
         TreeGridItem? assignedShip = FindShip(ShipDisposition.Friendly);
         if (assignedShip is not null)
         {
            TryGetProperty(assignedShip, "Id", out string newShipID);
            if (TrySetProperty(mission, "AssignedLayerId", newShipID))
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
         List<TreeGridItem> missions = FindMissions(property);
         crossSectorMissions = new List<TreeGridItem>();
         foreach (var mission in missions)
         {
            if (TryGetProperty(mission, "ToSectorId", out string toSectorId))
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
            TreeGridItem galaxy = GetGalaxyNode()!;
            TreeGridItem currentSystem = GetChildNode(GetChildNode(galaxy, "Objects")!, currentSystemId, true)!;
            TryGetProperties(currentSystem, new string[] { "Position.x", "Position.y" }, out Dictionary<string, string> currentSystemData);
            double currentSystemX = Double.Parse(currentSystemData["Position.x"]);
            double currentSystemY = Double.Parse(currentSystemData["Position.y"]);
            TryGetProperties(galaxy, new string[] { "VoidPosition.x", "VoidPosition.y", "VoidRadius" }, out Dictionary<string, string> voidData);
            double voidX = Double.Parse(voidData["VoidPosition.x"]);
            double voidY = Double.Parse(voidData["VoidPosition.y"]);
            double voidR = Double.Parse(voidData["VoidRadius"]);
            // get galaxy.objects where colony is true or shipyard is true
            List<TreeGridItem> habitableSystems = GetGalaxyObjects(false
               , new Dictionary<string, string> { { "Colony", "true" }, { "Shipyard", "true" } });//the "true" is not, strictly speaking, necessary, but it fits the paradigm I devised

            // loop, run the math
            foreach (var habitableSystem in habitableSystems)
            {
               TryGetProperties(habitableSystem, new string[] { "Position.x", "Position.y", "Id" }, out Dictionary<string, string> vals);
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
               ReplaceProperty(mission, "ToSectorId", "ToSystemId", reachableSystems[randomIndex]);
               ClearItemFromCache(mission);
               if (mission == GetSelectedTreeGridItem())
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
            TryGetProperty(GetGalaxyNode()!, "CurrentSystem", out sysId);
         }
         else
         {
            TreeGridItem ship;
            if (shipId is not null)
            {
               ship = FindShip(shipId, ShipDisposition.Friendly)!;
            }
            else
            {
               ship = FindShip(ShipDisposition.Friendly)!;
            }
            TryGetProperty(ship, "SystemId", out sysId);
         }
         return sysId;
      }
      private bool PropertiesContains(TreeGridItem item, string propertyname) // todo: figure out how to do multiples
      {
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         if (properties.Contains((object)propertyname))
         {
            return true;
         }
         return false;
      }
      internal void RemoveProperty(TreeGridItem item, string propertyName)
      {
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         properties.Remove(propertyName);
      }
      internal void AddProperty(TreeGridItem item, string propertyName, string propertyValue = "")
      {
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         properties.Add(propertyName, propertyValue);
      }
      internal void ReplaceProperty(TreeGridItem item, string oldPropertyName, string newPropertyName, string newPropertyValue = "")
      {
         RemoveProperty(item, oldPropertyName);
         AddProperty(item, newPropertyName, newPropertyValue);
      }
      private static bool HasProperties(TreeGridItem item, string[] propertyNames, bool all = false)
      {
         int matchCount = -1;
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         foreach (var name in propertyNames)
         {
            if (properties.Contains((object)name))
            {
               if (all)
               {
                  if (matchCount < 0) matchCount = 0;
                  matchCount++;
               }
               else
               {
                  return true;
               }
            }
         }

         return matchCount == propertyNames.Length;
      }
      private static bool TryGetProperties(TreeGridItem item, string[] propertyNames, out Dictionary<string, string> propertyValues) // add switch for any vs all?
      {
         propertyValues = new();
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         foreach (var name in propertyNames)
         {
            if (properties.Contains((object)name))
            {
               string t = properties[(object)name]!.ToString()!;
               if (t is not null)
               {
                  propertyValues.Add(name, t);
               }
            }
         }
         return propertyValues.Count > 0;
      }
      private bool TryGetProperty(TreeGridItem item, string propertyName, out string propertyValue)
      {
         propertyValue = string.Empty;
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         //foreach (var name in propertyNames)
         //{
         if (properties.Contains((object)propertyName))
         {
            string t = properties[(object)propertyName]!.ToString()!;
            if (t is not null)
            {
               propertyValue = t;
               return true;
            }
         }
         //}
         return false;
      }
      private bool TrySetProperty(TreeGridItem item, string propertyName, string propertyValue)
      {
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         if (properties.Contains((object)propertyName))
         {
            properties[(object)propertyName] = propertyValue;
            //string t = properties[(object)propertyname]!.ToString()!;
            //if (t is not null)
            //{
            //   propertyvalue = t;
            return true;
            //}
         }
         //propertyvalue = string.Empty;
         return false;
      }
      private TreeGridItem GetRoot()
      {
         TreeGridView tree = GetTreeGridView();
         TreeGridItemCollection items = (TreeGridItemCollection)tree.DataStore;
         TreeGridItem root = (TreeGridItem)items[0];
         return root;
      }
      public void Rebuild(Node root)
      {
         DetailPanelsCache.Clear();
         ClearDetails();
         Root = root;
         RebuildTreeView(Root);
         crossSectorMissions = null;
         systemsWithComets = null;
      }
      private void ClearDetails()
      {
         DynamicLayout s = GetPanel2DetailsLayout();
         s.Content = null;
         UpdateApplyRevertButtons(DetailsLayout.State.Unmodified);
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
         //x.CellFormatting += X_CellFormatting;
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
            return new TreeGridItem(node.Name, node.Properties, NodeStatus.Default)
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
            return new TreeGridItem(childItems, node.Name, node.Properties, NodeStatus.Default)
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
         //GridView grid = (GridView)sender;

         ((DetailsLayout)GetPanel2DetailsLayout().Content).Status = DetailsLayout.State.Modified;
         //return DetailsLayout.State.Modified;
      }
      private void DetailsUnmodified()
      {
         Apply.Enabled = Revert.Enabled = false;
         //GridView grid = (GridView)sender;

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
         List<KeyValuePair<TreeGridItem, DetailsLayout>> cachedPanels = DetailPanelsCache.ToList();
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
               MessageBox.Show("That key is already in use", MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
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
            TreeGridItem item = (TreeGridItem)view.SelectedItem;
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
         if (e.Item is not null and TreeGridItem item && item.Values.Length > 2
            && item == ((TreeGridView)sender!).SelectedItem && (NodeStatus)item.Values[2] != NodeStatus.Default)
         {
            Color? newColor = null;
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
            if (newColor is not null)
            {
               e.BackgroundColor = (Color)newColor;
            }

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

      private void RevertButton_Click(object? sender, EventArgs e)
      {
         TreeGridItem item = GetSelectedTreeGridItem();
         UpdateDetailsPanel(item, true);
      }

      private void ApplyButton_Click(object? sender, EventArgs e) // todo: generic way to get ahold of the current details-details panel (damn, I've really screwed up the nomenclature...)
      {
         TreeGridItem item = GetSelectedTreeGridItem();
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

      private static void ApplyChange(TreeGridItem item, Control detailControl)
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
            item.Values[1] = itemDictionary;
         }
      }

      private TreeGridItem GetSelectedTreeGridItem()
      {
         return (TreeGridItem)GetTreeGridView().SelectedItem;
      }
      private void RightGrid_Updated(object? sender, EventArgs? e)
      {
         if (sender is not null and ObservableCollection<InventoryGridItem> RightList)
         {
            if (GetTreeGridView() is not null and TreeGridView treeGrid)
            {
               if (treeGrid.SelectedItem is TreeGridItem item && item.Values[1] is OrderedDictionary initialData)
               {
                  Dictionary<string, string> currentData = new Dictionary<string, string>();
                  foreach (var entry in RightList)
                  {
                     currentData.Add(entry.Name, entry.Count.ToString());
                  }
                  var dict = initialData.Cast<DictionaryEntry>().ToDictionary(k => (string)k.Key, v => v.Value!.ToString());
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

