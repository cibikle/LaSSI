using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class Node : ITreeGridItem<Node>
   {
      private string name = string.Empty;
      public string Name
      {
         get { return name; }
         set { name = value; if (string.IsNullOrEmpty(BaseName)) { BaseName = name; } }
      }
      public string BaseName { get; set; } = string.Empty;
      public string Text { get; set; } = string.Empty;
      public int Id { get; set; } = 0;
      public TreeGridItemCollection Children { get; set; } = new();
      public OrderedDictionary Properties { get; set; } = new OrderedDictionary();
      public int Count => Children.Count;

      public bool Expanded { get; set; }

      public bool Expandable => Children.Count > 0;

      public ITreeGridItem? Parent { get; set; }

      public Node this[int index] => GetChild(index);
      private Node GetChild(int index)
      {
         if (index >= 0 && index < Children.Count && Children[index] is Node node)
         {
            return node;
         }
         else
         {
            throw new IndexOutOfRangeException();
         }
      }
      public Node? GetParent()
      {
         if (Parent is not null and Node p)
         {
            return p;
         }
         else
         {
            return null;
         }
      }
      public Node()
      {
         Name = string.Empty;
      }
      public Node(string name, int id, Node? parent, TreeGridItemCollection children, OrderedDictionary properties)
      {
         Name = name;
         Id = id;
         Parent = parent;
         Children = children;
         Properties = properties;
      }
      public Node(string name)
      {
         Name = name;
      }
      public Node(string name, OrderedDictionary properties) : this(name)
      {
         Properties = properties;
      }
      public Node(string name, OrderedDictionary properties, Node parent) : this(name, properties)
      {
         Parent = parent;
         AddAddlNameDetails();
      }
      public Node(string name, Node parent) : this(name)
      {
         Parent = parent;
      }
      public void RebuildName()
      {
         Name = BaseName;
         AddAddlNameDetails();
      }
      public void AddChild(Node node)
      {
         Children.Add(node);
         node.Parent = this;
      }
      public void RemoveChild(Node node)
      {
         Children.Remove(node);
         node.Parent = null;
      }
      public void AddParent(Node parent)
      {
         parent.AddChild(this);
      }
      public void RemoveParent(Node parent)
      {
         parent.RemoveChild(this);
      }
      public Node GetRoot()
      {
         Node node = this;
         while (node.GetParent() is not null and Node p)
         {
            node = p;
         }
         return node;
      }
      public bool ContainsChild(Node node, bool recurse)
      {
         if (!this.HasChildren()) return false;
         if (!this.Children.Contains(node) && !recurse) return false;
         if (this.Children.Contains(node)) return true;
         bool contains = false;
         foreach (Node child in Children.Cast<Node>())
         {
            contains = child.ContainsChild(node, recurse);
            if (contains) break;
         }
         return contains;
      }
      public bool HasChildren()
      {
         return Children.Count > 0;
      }
      public Node? FindChild(string name, bool looseMatch = false, bool recurse = false)
      {
         foreach (Node child in Children.Cast<Node>())
         {
            if (child.Name.Equals(name) || (looseMatch && Regex.IsMatch(child.Name, name, RegexOptions.IgnoreCase)))
            {
               return child;
            }
            else
            {
               if (recurse)
               {
                  return child.FindChild(name, looseMatch, recurse); // todo: this doesn;t work
               }
            }
         }
         return null;
      }
      public TreeGridItemCollection FindChildren(string searchName, bool recurse = false)
      {
         TreeGridItemCollection nodes = new();
         foreach (Node child in Children.Cast<Node>())
         {
            if ( /*child.Name.Equals(name) || (looseMatch && child.Name.StartsWith(name))*/ Regex.IsMatch(child.Name, searchName, RegexOptions.IgnoreCase))
            {
               nodes.Add(child);
            }
            else
            {
               if (recurse)
               {
                  nodes.AddRange(child.FindChildren(searchName, recurse));
               }
            }
         }
         return nodes;
      }
      public Node? FindChild(string propertyName, string propertyValue) // todo: add recurse option
      {
         foreach (Node child in Children.Cast<Node>())
         {
            if (child.Properties.Contains(propertyName) && propertyValue.Equals(child.Properties[propertyName]))
            {
               return child;
            }
         }
         return null;
      }
      public TreeGridItemCollection FindChildren(string propertyName, string propertyValue = "", bool recurse = false)
      {
         TreeGridItemCollection nodes = new();
         foreach (Node child in Children.Cast<Node>())
         {
            if (child.TryGetProperty(propertyName, out string propValue) && propValue.Equals(propertyValue, StringComparison.OrdinalIgnoreCase))
            {
               nodes.Add(child);
            }
            else
            {
               if (recurse)
               {
                  nodes.AddRange(child.FindChildren(propertyName, propertyValue, recurse));
               }
            }
         }
         return nodes;
      }
      public TreeGridItemCollection FindChildrenWithProperty(string propertyName, bool recurse = false)
      {
         TreeGridItemCollection nodes = new();
         foreach (Node child in Children.Cast<Node>())
         {
            if (child.HasProperties(new string[] { propertyName }))
            {
               nodes.Add(child);
            }
            else
            {
               if (recurse)
               {
                  nodes.AddRange(child.FindChildrenWithProperty(propertyName, recurse));
               }
            }
         }
         return nodes;
      }
      public Node? FindChild(string name, string propertyName, string propertyValue, bool looseMatch = false) // todo: add recurse option
      {
         foreach (Node child in Children.Cast<Node>())
         {
            if ((child.Name.Equals(name) || (looseMatch && child.Name.Contains(name)))
               && child.Properties.Contains(propertyName) && propertyValue.Equals(child.Properties[propertyName]))
            {
               return child;
            }
         }
         return null;
      }
      public static bool DetermineIfPropertyIsArray(string key)
      {
         //if the key starts and ends with a [ and a ], it's an array
         //if the key starts with a " and a [ and ends with a ] and a " and contains a ' ' and a ',', it's an array
         return (key.StartsWith('[') && key.EndsWith(']')) || (key.StartsWith("\"[") && key.EndsWith("]\"") && key.Contains(", "));
      }

      public bool TryGetProperty(string propertyName, out string propertyValue)
      {
         if (MatchProperty(propertyName, out string matchingPropertyName))
         {
            propertyValue = $"{Properties[matchingPropertyName]}";
            return true;
         }
         else
         {
            propertyValue = string.Empty;
            return false;
         }
      }
      public bool TryGetProperties(Dictionary<string, string> properyNamesAndValues, bool all = false)
      {
         foreach (string name in properyNamesAndValues.Keys)
         {
            if (Properties.Contains(name))
            {
               properyNamesAndValues[name] = $"{Properties[name]}";
            }
         }
         return (all && properyNamesAndValues.Keys.Count == properyNamesAndValues.Values.Count) || properyNamesAndValues.Values.Count > 0;
      }
      public bool HasProperty(string name)
      {
         return Properties.Contains(name);
      }
      public bool HasProperties(string[] propertyNames, bool all = false)
      {
         int matchCount = -1;
         foreach (var name in propertyNames)
         {
            if (PropertiesContains(name))
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
      public bool MatchProperty(string propertyName, out string matchingPropertyName)
      {
         foreach (string propertyKey in Properties.Keys)
         {
            if (Regex.IsMatch(propertyKey, propertyName, RegexOptions.IgnoreCase))
            {
               matchingPropertyName = propertyKey;
               return true;
            }
         }

         matchingPropertyName = string.Empty;
         return false;
      }
      public bool NameMatches(string[] names)
      {
         foreach (string name in names)
         {
            if (Regex.IsMatch(Name, name, RegexOptions.IgnoreCase))
            {
               return true;
            }
         }
         return false;
      }
      private bool PropertiesContains(string name)
      {
         foreach (string propertyKey in Properties.Keys)
         {
            if (Regex.IsMatch(propertyKey, name, RegexOptions.IgnoreCase))
            {
               return true;
            }
            //return propertyKey.Equals(name, StringComparison.OrdinalIgnoreCase);
         }
         return false;
      }
      public void ReplaceProperty(string oldPropertyName, string newPropertyName, string newValue)
      {
         Properties.Remove(oldPropertyName);
         Properties.Add(newPropertyName, newValue);
      }
      public bool TrySetProperty(string propertyName, string propertyValue)
      {
         if (Properties.Contains((object)propertyName))
         {
            Properties[(object)propertyName] = propertyValue;
            return true;
         }
         return false;
      }
      public static List<string> CommaSeparatedStringToList(string list)
      {
         return list.TrimStart(new char[] { '\\', '"', '[' }).TrimEnd(new char[] { ']', '"', '\\' }).Split(", ").ToList();
      }
      public List<string> GetItems()
      {
         List<string> items = new();
         if (TryGetProperty("Items", out string data))
         {
            items = CommaSeparatedStringToList(data);
         }
         return items;
      }
      public void RemoveItems(List<string> itemIds)
      {
         foreach (string itemId in itemIds)
         {
            if (FindChild("Id", itemId) is not null and Node item)
            {
               RemoveChild(item);
            }
         }
      }
      public void RemoveLayerObjects(List<string> layerObjectsToRemove)
      {
         Node layerObjectsNode = (Node)Children.First(x => ((Node)x).Name.Equals("Objects"));
         if (layerObjectsNode != null)
         {
            foreach (string layerObject in layerObjectsToRemove)
            {
               if (layerObjectsNode.FindChild(layerObject, looseMatch: true) is not null and Node item)
               {
                  layerObjectsNode.RemoveChild(item);
               }
            }
         }
      }

      public static Node? GetGalaxyNode(Node root)
      {
         return GetChildNode(root, "Galaxy");
      }
      public static List<Node> GetGalaxyObjects(Node root, bool all = false, Dictionary<string, string>? filters = null)
      {
         List<Node> matchingGalaxyObjects = new();
         if (GetGalaxyNode(root) is not null and Node galaxy && GetChildNode(galaxy, "Objects") is not null and Node galaxyObjects)
         {
            foreach (Node galaxyObject in galaxyObjects.Children.Cast<Node>())
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
      public static Node? GetChildNode(Node item, string childname, bool looseMatch = false)
      {
         foreach (Node child in item.Children.Cast<Node>())
         {
            if (child.Name == childname || (looseMatch && child.Name.Contains(childname)))
            {
               return child;
            }
         }
         return null;
      }
      public static List<Node> GetChildNodes(Node item, string childname, bool looseMatch = false)
      {
         List<Node> children = new();
         foreach (Node child in item.Children.Cast<Node>())
         {
            if (child.Name == childname || (looseMatch && child.Name.Contains(childname)))
            {
               children.Add(child);
            }
         }
         return children;
      }
      public static List<Node> FindChildNodesWithProperties(Node item, string childname, bool looseMatch = false, List<string>? properties = null, bool all = false)
      {
         List<Node> children = new();
         foreach (Node child in item.Children.Cast<Node>())
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
      public static List<Node> FindChildNodesWithProperty(Node item, string propertyName, string propertyValue = "")
      {// todo: do multiples
         List<Node> list = new();
         foreach (Node child in item.Children.Cast<Node>())
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
      public static Node? GetSystemArchives(Node root)
      {
         return GetChildNode(GetGalaxyNode(root)!, "SystemArchives");
      }
      public static Node? GetSystemArchive(Node root, string id)
      {
         Node systemArchives = GetSystemArchives(root)!;
         foreach (Node systemArchive in systemArchives.Children.Cast<Node>())
         {
            if (systemArchive.Name.Contains(id)) return systemArchive;
         }
         return null;
      }
      public static string GetNodePath(Node item)
      {
         string path = item.Name;
         while (item.Parent != null && item.Parent.Parent != null)
         {
            item = (Node)item.Parent;
            path = $"{item.Name}/{path}";
         }
         return path;
      }
      public static Label GetNodePathLabel(Node item)
      {
         Label nodePathLabel = new() { Text = GetNodePath(item), BackgroundColor = Colors.Silver, Font = new Font("Arial", 18, FontStyle.Bold) };
         return nodePathLabel;
      }

      public bool IsChildOf(string parentName)
      {
         return GetParent() is not null and Node p && p.Name == parentName;
      }
      public bool IsDescendantOf(string ancestorName)
      {
         var n = GetParent();
         bool isDescendantOf = false;
         while (n is not null && !isDescendantOf)
         {
            isDescendantOf = n.Name == ancestorName;
            n = n.GetParent();
         }
         return isDescendantOf;
      }
      public bool IsHazard()
      {
         if (this.Parent != null && ((Node)Parent).Name == "Hazards" && this.Properties.Contains("Type")) return true;
         return false;
      }
      public bool IsStarSystem()
      {
         if (GetParent() is not null and Node p && p.Name == "Objects"
            && p.GetParent() is not null and Node gp && gp.Name == "Galaxy"
            && this.Properties.Contains("Name")) return true;
         return false;
      }
      public bool IsMission()
      {
         if (GetParent() is not null and Node p && p.Name == "Missions"
            && p.GetParent() is not null and Node gp && gp.Name == "Missions"
            && this.Properties.Contains("Type")) return true;
         return false;
      }
      public bool IsCombatMission()
      {
         return IsMission() && TryGetProperty("Type", out string type) && type.Equals("Combat");
      }
      public bool IsMissionRequirement()
      {
         if (GetParent() is not null and Node p && p.Name == "Requirements"
            && p.GetParent() is not null and Node gp
            && gp.GetParent() is not null and Node ggp && ggp.Name == "Missions") return true;
         return false;
      }
      public bool IsResearch()
      {
         if (this.Name == "Research") return true;
         return false;
      }
      public bool IsTradingPost()
      {
         if (this.Name == "TradingPost") return true;
         return false;
      }
      public bool IsFtlJourney()
      {
         if (GetParent() is not null and Node p && p.Name == "Journeys") return true;
         return false;
      }
      /// <summary>
      /// Determines if the calling node is a layer or, optionally, the child of a Layer (free space/ship).
      /// </summary>
      public bool IsLayer(bool DetermineIfChild = false)
      {
         if (this.Name == "Layer") return true;
         if (GetParent() is not null and Node p && DetermineIfChild) return p.IsLayer(DetermineIfChild);
         return false;
      }
      public bool IsSystemNode()
      {
         return Name.StartsWith("System");
      }
      public bool IsLayerObject()
      {
         return IsLayer(true) && GetParent() is not null and Node p && p.Name == "Objects";
      }
      public bool IsPalette(bool DetermineIfChild = false)
      {
         if (this.Name == "Palette") return true;
         return GetParent() is not null and Node p && DetermineIfChild && p.IsPalette(DetermineIfChild);
      }
      public bool IsPowerGrid()
      {
         return this.Name == "PowerGrid";
      }
      public bool IsEditor(bool DetermineIfChild = false)
      {
         if (this.Name == "Editor") return true;
         return GetParent() is not null and Node p && DetermineIfChild && p.IsEditor(DetermineIfChild);
      }
      public bool IsPhysicsState()
      {
         return GetParent() is not null and Node p && p.Name == "Physics" && Name == "State";
      }
      public bool IsSystemArchive()
      {
         return GetParent() is not null and Node p && p.Name == "SystemArchives";
      }
      public bool IsLogisticsRequest()
      {
         return GetParent() is not null and Node p && p.Name == "Requests" && p.GetParent() is not null and Node gp && gp.Name == "Logistics";
      }
      public bool IsWeather()
      {
         return Name == "Weather";
      }
      public bool IsOrders()
      {
         return Name == "Orders";
      }
      public bool IsNetwork()
      {
         return Name == "Network";
      }
      public bool IsHabitationZone()
      {
         return GetParent() is not null and Node p && p.Name == "Zones" && p.GetParent() is not null and Node gp && gp.Name == "Habitation";
      }
      public bool IsWorkQueueJob()
      {
         return GetParent() is not null and Node p && p.Name == "Jobs" && p.GetParent() is not null and Node gp && gp.Name == "WorkQueue";
      }
      public bool IsLogisticsTransfer()
      {
         return GetParent() is not null and Node p && p.Name == "Transfers" && p.GetParent() is not null and Node gp && gp.Name == "Logistics";
      }
      public static string GetHazardName(string id) //todo: replace with enum
      {
         string HazardName = string.Empty;
         switch (id)
         {
            case "1":
               HazardName = "asteroid field";
               break;
            case "2":
               HazardName = "gas cloud, metreon";
               break;
            case "3":
               HazardName = "gas cloud, zeleon";
               break;
         }
         return HazardName;
      }
      public static string GetStarSystemSummary(Node node)
      {
         string Summary = string.Empty;
         Summary += node.Properties["Name"];
         if (node.Properties.Contains("Colony")) Summary += ", Colony";
         if (node.Properties.Contains("Shipyard")) Summary += ", Shipyard";
         if (node.Properties.Contains("Comet")) Summary += ", Comet";
         if (node.Properties.Contains("Hostiles")) Summary += ", Hostiles";
         if (node.Properties.Contains("Rescue")) Summary += ", Rescue";
         return Summary;
      }
      public string GetMissionName()
      {
         string details = Properties["Type"]!.ToString()!;
         string missionType = details;

         if ("true".Equals(Properties["Accepted"]))
         {
            details = $"Accepted: {details}";
         }
         if (missionType == "Combat")
         {
            details += GetCombatMissionDetails();
         }
         else if (missionType == "Production")
         {
            details += GetProductionMissionDetails();
         }
         else
         {
            if (Properties.Contains("ItemCount")) { details += $", {Properties["ItemCount"]}"; }
            if (missionType == "Delivery")
            {
               details += " boxes";
            }
            else if (missionType == "Industry")
            {
               details += " tilium ore";
            }
            else if (missionType == "Passengers" || missionType == "Rescue")
            {
               details += " people";
            }
            if (Properties.Contains("FromSystemId")) { details += $", pick-up: System {Properties["FromSystemId"]}"; }
            if (Properties.Contains("ToSystemId")) { details += $", drop-off: System {Properties["ToSystemId"]}"; }
            if (Properties.Contains("ToSectorId") && int.Parse((string)Properties["ToSectorId"]!) != 0) { details += $", destination: Sector {Properties["ToSectorId"]}"; }

            if (missionType == "Rescue")
            {
               if (Properties.Contains("FromSystemId")) { details = details.Replace("pick-up", "contract available"); }
               if (Properties.Contains("ToSystemId")) { details = details.Replace("drop-off", "ship location"); }
            }
         }
         return details;
      }
      public string GetProductionMissionDetails()
      {
         Dictionary<string, string> propertyNamesAndValues = new()
            {
               { "Resource", "" },
               { "ItemCount", "" }
            };
         TryGetProperties(propertyNamesAndValues);
         string details = $", {propertyNamesAndValues["Resource"]}";
         details += $", {propertyNamesAndValues["ItemCount"]}";
         return details;
      }
      public string GetCombatMissionDetails()
      {
         Dictionary<string, string> propertyNamesAndValues = new()
            {
               { "EnemyType", "" },
               { "ItemCount", "" },
               { "ToSystemId", "" }
            };
         TryGetProperties(propertyNamesAndValues);
         string details = $", {propertyNamesAndValues["EnemyType"]}";
         details += $", {propertyNamesAndValues["ItemCount"]} vessels";
         details += $", System {propertyNamesAndValues["ToSystemId"]}";
         return details;
      }
      public static string GetMissionRequirement(Node node)
      {
         string details = node.Properties["Type"]!.ToString()!;
         if (node.Properties.Contains("ObjectType")) { details += $", {node.Properties["ObjectType"]}"; }
         if (node.Properties.Contains("Count")) { details += $", {node.Properties["Count"]}"; }
         return details;
      }
      public static string GetLayerDetails(Node node)
      {
         string details = $"{node.Properties["Id"]}, {node.Properties["Name"]}, {node.Properties["Type"]}";
         if (node.Properties.Contains("SystemId")) details += $", System {node.Properties["SystemId"]}";
         return details;
      }
      public static string GetTradingPostDetails(Node node)
      {
         return $"System {node.Properties["SystemId"]}";
      }
      public static string GetFtlJourneyDetails(Node node)
      {
         return $"{node.Properties["State"]}: {node.Properties["Layers"]} from System {node.Properties["FromSystem"]} to {node.Properties["ToSystem"]}";
      }
      public string GetLayerObjectDetails()
      {
         string details = $"{Properties["Id"]}, {Properties["Type"]}";
         if (Properties.Contains("State")) details += $", {Properties["State"]}";
         if (Properties.Contains("CauseOfDeath")) details += $", Cause of death: {Properties["CauseOfDeath"]}";
         if (Properties.Contains("HomeLayer")) details += $", Home layer: {Properties["HomeLayer"]}";
         if (Properties.Contains("Resource")) details += $", {Properties["Resource"]}";
         if (Properties.Contains("Quantity")) details += $", Qty: {Properties["Quantity"]}";
         if (Properties.Contains("Recipe")) details += $", Recipe: {Properties["Recipe"]}";
         if (Properties.Contains("Contents")) details += $", Contents: {Properties["Contents"]}";
         if (Properties.Contains("Capacity"))
         {
            if (!Properties.Contains("Quantity")) details += ", Qty: 0";
            details += $", Cap.: {Properties["Capacity"]}";
         }
         return details;
      }
      public string GetPhysicsStateDetails()
      {
         return $"{this.Properties["Id"]}";
      }
      public string GetSystemArchiveDetails()
      {
         return Regex.Replace(Name, @"^\""\[i\s", "NG").Replace("]\"", "");
      }
      public string GetLogisticsRequestDetails()
      {
         Dictionary<string, string> propertyNamesAndValues = new()
            {
               { "Quantity", "" },
               { "ItemType", "" },
               { "FromLayer", "" },
               { "ToLayer", "" }
            };
         if (TryGetProperties(propertyNamesAndValues, true))
         {
            string details = $"{propertyNamesAndValues["Quantity"]}";
            details += $" {propertyNamesAndValues["ItemType"]}";
            details += $" from {propertyNamesAndValues["FromLayer"]}";
            details += $" to {propertyNamesAndValues["ToLayer"]}";
            return details;
         }
         return string.Empty;
      }
      public string GetLogisticsTransferDetails()
      {
         Dictionary<string, string> propertyNamesAndValues = new()
            {
               { "ItemId", "" },
               { "FromLayer", "" },
               { "ToLayer", "" },
               { "JobId", "" }
            };
         if (TryGetProperties(propertyNamesAndValues))
         {
            string details = $" {propertyNamesAndValues["ItemId"]}";
            details += $" from {propertyNamesAndValues["FromLayer"]}";
            details += $" to {propertyNamesAndValues["ToLayer"]}";
            if (propertyNamesAndValues["JobId"] != string.Empty)
            {
               details += $", JobId {propertyNamesAndValues["JobId"]}";
            }

            return details;
         }
         return string.Empty;
      }
      public string GetSystemId()
      {
         TryGetProperty("SystemId", out string systemId);
         return systemId;
      }
      public string GetNetworkDetails()
      {
         return $"{Properties["Type"]}, {Properties["Id"]}";
      }
      public string GetHabitationZoneDetails()
      {
         string entities = $"{Properties["Entities"]}";

         int used = 0;
         if (entities.Length > 0)
         {
            used = entities.Length - entities.Replace(",", "").Length + 1;
         }

         return $"ID {Properties["Id"]}, Capacity: {used}/{Properties["Capacity"]}";
      }
      public string GetWorkQueueJobDetails()
      {
         string details = $"{Properties["Type"]}";
         if (Properties.Contains("TargetType")) details += $" {Properties["TargetType"]}";
         return details;
      }
      private string GetAddlNameDetails()
      {
         string addlDetails = string.Empty;
         if (this.IsHazard())
         {
            if (Properties != null && Properties.Contains("Type"))
            {
               addlDetails = GetHazardName(Properties["Type"]!.ToString()!);
            }
         }
         else if (this.IsStarSystem())
         {
            addlDetails = GetStarSystemSummary(this);
         }
         else if (IsSystemArchive())
         {
            addlDetails = GetSystemArchiveDetails();
         }
         else if (this.IsMission())
         {
            addlDetails = GetMissionName();
         }
         else if (this.IsLayer()) //remember, layers include both "FreeSpace" and all ships/stations!
         {
            addlDetails = Node.GetLayerDetails(this);
         }
         else if (this.IsMissionRequirement())
         {
            addlDetails = Node.GetMissionRequirement(this);
         }
         else if (this.IsTradingPost())
         {
            addlDetails = Node.GetTradingPostDetails(this);
         }
         else if (this.IsFtlJourney())
         {
            addlDetails = GetFtlJourneyDetails(this);
         }
         else if (this.IsLayerObject())
         {
            addlDetails = this.GetLayerObjectDetails();
         }
         else if (this.IsPhysicsState())
         {
            addlDetails = this.GetPhysicsStateDetails();
         }
         else if (IsLogisticsRequest())
         {
            addlDetails = GetLogisticsRequestDetails();
         }
         else if (IsWeather() || IsOrders())
         {
            string systemId = GetSystemId();

            addlDetails = "System " + (systemId != string.Empty ? systemId : "?");
         }
         else if (IsNetwork())
         {
            addlDetails = GetNetworkDetails();
         }
         else if (IsHabitationZone())
         {
            addlDetails = GetHabitationZoneDetails();
         }
         else if (IsWorkQueueJob())
         {
            addlDetails = GetWorkQueueJobDetails();
         }
         else if (IsLogisticsTransfer())
         {
            addlDetails = GetLogisticsTransferDetails();
         }
         return addlDetails;
      }
      public void AddAddlNameDetails()
      {
         string addlDetails = GetAddlNameDetails();
         if (addlDetails != String.Empty)
         {
            addlDetails = $" ({addlDetails})";
         }
         this.Name += addlDetails;
      }
   }
}
