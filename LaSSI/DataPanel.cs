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
      private DetailsLayout? CurrentDetails;
      private List<TreeGridItem>? systemsWithComets = null;
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
         return lb;
      }
      private void ShuttleItems(ObservableCollection<InventoryGridItem> RemainingItems
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
      private List<TreeGridItem> CompileDerelictList(TreeGridItemCollection items)
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
         defaultGridView.ContextMenu = new ContextMenu(AddGridViewRow(), DeleteGridViewRow());
         //defaultGridView.
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
      private Command AddGridViewRow()
      {
         var addNewRow = new Command { MenuText = "Add row", /*Shortcut = Application.Instance.CommonModifier | Keys.Equal*/ };
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
      public bool AssertionFailureConditionExists(bool justTakeCareOfIt = false)
      {
         TreeGridItem? MissionsNode = GetMissionsNode();
         if (MissionsNode is not null)
         {
            foreach (TreeGridItem mission in MissionsNode.Children.Cast<TreeGridItem>())
            {
               if (mission.Tag.ToString()!.Contains("TutorialFlightReady")
                  && TryGetProperties(mission, "AssignedLayerId", out string LayerId))
               {
                  Debug.WriteLine($"Mission is assigned to {LayerId}");
                  TreeGridItem? assignedShip = FindShip(LayerId);
                  if (assignedShip is null
                     || (TryGetProperties(assignedShip, "Type", out string Disposition)
                     && ShipDispositionMatches(ShipDisposition.Friendly, StringDescToShipDisposition(Disposition))))
                  {
                     if (justTakeCareOfIt)
                     {
                        Debug.WriteLine("Could not find a friendly ship with that ID; finding any friendly ship");
                        assignedShip = FindShip(ShipDisposition.Friendly);
                        if (assignedShip is not null)
                        {
                           TryGetProperties(assignedShip, "Id", out string newShipID);
                           if (TrySetProperty(mission, "AssignedLayerId", newShipID))
                           {
                              Debug.WriteLine($"successfully transferred mission to {newShipID}");
                              UpdateDetailsPanel(mission, true);
                           }
                        }
                        else
                        {
                           Debug.WriteLine("Could not find any friendly ship! WTF?");
                        }
                     }
                     return true;
                  }
                  else
                  {
                     return false;
                  }
               }
            }
         }
         else
         {
            Debug.WriteLine("Could not get Missions node");
         }
         return false;
      }
      private bool ShipDispositionMatches(ShipDisposition required, ShipDisposition actual)
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
      private ShipDisposition StringDescToShipDisposition(string desc)
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
      private TreeGridItem? GetGalaxyNode()
      {
         return GetChildNode(GetRoot(), "Galaxy");
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
      private TreeGridItem? GetSystemArchives()
      {
         return GetChildNode(GetGalaxyNode(), "SystemArchives");
      }
      private TreeGridItem? GetSystemArchive(string id)
      {
         TreeGridItem systemArchives = GetSystemArchives();
         foreach (TreeGridItem systemArchive in systemArchives.Children)
         {
            if (systemArchive.Tag.ToString()!.Contains(id)) return systemArchive;
         }
         return null;
      }
      private List<TreeGridItem> FindChildNodesWithProperty(TreeGridItem item, string propertyName, string propertyValue = "")
      {
         List<TreeGridItem> list = new();
         foreach (TreeGridItem child in item.Children.Cast<TreeGridItem>())
         {
            if (TryGetProperties(child, propertyName, out string value))
            {
               if ((propertyValue != "" && propertyValue == value) || propertyValue == "")
               {
                  list.Add(child);
               }
            }
         }
         return list;
      }
      private TreeGridItem? FindShip(string LayerId, ShipDisposition disposition = ShipDisposition.Any)
      {
         TreeGridItem root = GetRoot();
         foreach (TreeGridItem item in root.Children)
         {
            if (TryGetProperties(item, "Class", out string Class) && Class == "Ship"
               && TryGetProperties(item, "Id", out string ID) && ID == LayerId
               && TryGetProperties(item, "Type", out string Disposition) && Disposition == disposition.ToString())
            {
               return item;
            }
         }
         return null;
      }
      private TreeGridItem? FindShip(ShipDisposition disposition = ShipDisposition.Any)
      {
         TreeGridItem root = GetRoot();
         foreach (TreeGridItem item in root.Children)
         {
            if (TryGetProperties(item, "Class", out string Class) && Class == "Ship"
               && TryGetProperties(item, "Id", out string ID)
               && TryGetProperties(item, "Type", out string Disposition)
               && ShipDispositionMatches(ShipDisposition.Friendly, StringDescToShipDisposition(Disposition)))
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
         if (properties.Contains((object)"Type") && properties.Contains((object)"Class"))
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
               if (TryGetProperties(systemWithComet, "Id", out string systemId))
               {
                  TreeGridItem system = GetSystemArchive(systemId);
                  List<TreeGridItem> comets = FindChildNodesWithProperty(GetChildNode(GetChildNode(system, "FreeSpace", true), "Objects"), "Type", "Comet");
                  foreach (var comet in comets)
                  {
                     success = success && TrySetProperty(comet, "Position.x", "0");
                     success = success && TrySetProperty(comet, "Position.y", "0");
                     UpdateDetailsPanel(comet, true);
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
         TreeGridItem Weather = GetChildNode(root, "Weather")!;
         if (TryGetProperties(Weather, "Meteors", out string meteorsOn))
         {
            return meteorsOn == "true";
         }
         else
         {
            return false;
         }
      }
      internal bool TurnOffMeteors()
      {
         TreeGridItem root = GetRoot();
         TreeGridItem Weather = GetChildNode(root, "Weather")!;
         bool success = TrySetProperty(Weather, "Meteors", "false");
         if (success) UpdateDetailsPanel(Weather, true);
         return success;
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
      private bool TryGetProperties(TreeGridItem item, string propertyName, out string propertyValue) // todo: figure out how to do multiples
      {
         OrderedDictionary properties = (OrderedDictionary)item.Values[1];
         if (properties.Contains((object)propertyName))
         {
            string t = properties[(object)propertyName]!.ToString()!;
            if (t is not null)
            {
               propertyValue = t;
               return true;
            }
         }
         propertyValue = string.Empty;
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
         Apply.Enabled = Revert.Enabled = false;
         //GridView grid = (GridView)sender;

         ((DetailsLayout)GetPanel2DetailsLayout().Content).Status = DetailsLayout.State.Applied;
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
      public bool ReadyForSave()
      {
         if (ChangesAreUnapplied())
         {
            DialogResult result = MessageBox.Show($"There are unapplied changes!{Environment.NewLine}Apply before saving?"
               , MessageBoxButtons.YesNoCancel, MessageBoxType.Warning, MessageBoxDefaultButton.Yes);
            switch (result)
            {
               case DialogResult.Yes:
                  {
                     ApplyAllChanges();
                     return true;
                  }
               case DialogResult.No:
                  {
                     return true;
                  }
               case DialogResult.Cancel:
                  {
                     return false;
                  }
               default:
                  {
                     return false;
                  }
            }
         }
         else
         {
            return true;
         }
      }

      private void ApplyAllChanges()
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
            ApplyChange(item, detailControl);

            UpdateApplyRevertButtons(DetailsLayout.State.Applied);
            DetailsApplied();
         }
      }

      private Control GetDetailsControl()
      {
         DynamicLayout detailsLayout = (DynamicLayout)GetPanel2DetailsLayout().Content;
         Control detailsControl = detailsLayout.FindChild("DefaultGridView");
         if (detailsControl is null)
         {
            detailsControl = GetPanel2DetailsLayout().FindChild("ListBuilder");
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
         return !this.Value.Equals(value, StringComparison.CurrentCultureIgnoreCase);
      }
      public bool IsChanged(DictionaryEntry value) // these don't work as intended--can't recognize when the value is set back to the initial value
      {
         string x = string.Empty, y = string.Empty;
         if (Column == 0)
         {
            x = deValue.Value.Key.ToString();
            y = value.Key.ToString();
         }
         else if (Column == 1)
         {
            x = deValue.Value.Value.ToString();
            y = value.Value.ToString();
         }

         return x != y;
      }
   }
}

