using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Eto.Forms;

namespace LaSSI
{
   public class Node : ITreeGridItem<Node>
   {
      public string Name { get; set; } = string.Empty;
      public string Text { get; set; } = string.Empty;
      public int Id { get; set; }
      public TreeGridItemCollection Children { get; set; } = new();
      public OrderedDictionary Properties { get; set; }
      public int Count => Children.Count;

      public bool Expanded { get; set; }

      public bool Expandable => Children.Count > 0;

      public ITreeGridItem Parent { get; set; }

      public Node this[int index] => GetChild(index);


      private Node? GetChild(int index)
      {
         return Children[index] is Node node ? node : null;
      }
      public Node GetParent()
      {
         return (Node)Parent;
      }
      public Node()
      {
         Name = string.Empty;
         Id = 0;
         Parent = null;
         Properties = new OrderedDictionary();
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
         Id = 0;
         Parent = null;
         Properties = new OrderedDictionary();
      }
      public Node(string name, OrderedDictionary properties)
      {
         Name = name;
         Id = 0;
         Parent = null;
         Properties = properties;
      }
      public Node(string name, OrderedDictionary properties, Node parent)
      {
         Name = name;
         Id = 0;
         Parent = parent;
         Properties = properties;
         AddAddlNameDetails();
      }
      public Node(string name, Node parent)
      {
         Name = name;
         Id = 0;
         Parent = parent;
         Properties = new OrderedDictionary();
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
         while (node.Parent is not null)
         {
            node = node.GetParent();
         }
         return node;
      }
      public bool ContainsChild(Node node, bool recurse)
      {
         if (!this.HasChildren()) return false;
         if (!this.Children.Contains(node) && !recurse) return false;
         if (this.Children.Contains(node)) return true;
         bool contains = false;
         foreach (Node child in Children)
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
         foreach (Node child in Children)
         {
            if (child.Name.Equals(name) || (looseMatch && child.Name.Contains(name)))
            {
               return child;
            }
            else
            {
               if (recurse)
               {
                  return child.FindChild(name, looseMatch, recurse);
               }
            }
         }
         return null;
      }
      public Node? FindChild(string propertyName, string propertyValue) // todo: add recurse option
      {
         foreach (Node child in Children)
         {
            if (child.Properties.Contains(propertyName) && propertyValue.Equals(child.Properties[propertyName]))
            {
               return child;
            }
         }
         return null;
      }
      public Node? FindChild(string name, string propertyName, string propertyValue, bool looseMatch = false) // todo: add recurse option
      {
         foreach (Node child in Children)
         {
            if ((child.Name.Equals(name) || (looseMatch && child.Name.Contains(name)))
               && child.Properties.Contains(propertyName) && propertyValue.Equals(child.Properties[propertyName]))
            {
               return child;
            }
         }
         return null;
      }
      public bool TryGetProperty(string propertyName, out string propertyValue)
      {
         if (Properties.Contains(propertyName))
         {
            propertyValue = $"{Properties[propertyName]}";
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
      internal bool HasProperties(string[] propertyNames, bool all = false)
      {
         int matchCount = -1;
         foreach (var name in propertyNames)
         {
            if (Properties.Contains((object)name))
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
      internal void ReplaceProperty(string oldPropertyName, string newPropertyName, string newValue)
      {
         Properties.Remove(oldPropertyName);
         Properties.Add(newPropertyName, newValue);
      }
      internal bool TrySetProperty(string propertyName, string propertyValue)
      {
         if (Properties.Contains((object)propertyName))
         {
            Properties[(object)propertyName] = propertyValue;
            return true;
         }
         return false;
      }
      internal bool IsHazard()
      {
         if (this.Parent != null && ((Node)Parent).Name == "Hazards" && this.Properties.Contains("Type")) return true;
         return false;
      }
      internal bool IsStarSystem()
      {
         if (this.Parent != null && this.GetParent().Name == "Objects"
            && this.Parent.Parent != null && this.GetParent().GetParent().Name == "Galaxy"
            && this.Properties.Contains("Name")) return true;
         return false;
      }
      internal bool IsMission()
      {
         if (this.Parent != null && this.GetParent().Name == "Missions"
            && this.Parent.Parent != null && GetParent().GetParent().Name == "Missions"
            && this.Properties.Contains("Type")) return true;
         return false;
      }
      internal bool IsMissionRequirement()
      {
         if (this.Parent != null && this.GetParent().Name == "Requirements"
            && this.Parent.Parent != null
            && this.Parent.Parent.Parent != null && this.GetParent().GetParent().GetParent().Name == "Missions") return true;
         return false;
      }
      internal bool IsResearch()
      {
         if (this.Name == "Research") return true;
         return false;
      }
      internal bool IsTradingPost()
      {
         if (this.Name == "TradingPost") return true;
         return false;
      }
      internal bool IsFtlJourney()
      {
         if (Parent != null && GetParent().Name == "Journeys") return true;
         return false;
      }
      /// <summary>
      /// Determines if the calling node is a layer or, optionally, the child of a Layer (free space/ship).
      /// </summary>
      internal bool IsLayer(bool DetermineIfChild = false)
      {
         if (this.Name == "Layer") return true;
         if (this.Parent != null && DetermineIfChild) return this.GetParent().IsLayer(DetermineIfChild);
         return false;
      }
      internal bool IsSystemNode()
      {
         return Name.StartsWith("System");
      }
      internal bool IsLayerObject()
      {
         return this.IsLayer(true) && this.Parent != null && GetParent().Name == "Objects";
      }
      internal bool IsPalette(bool DetermineIfChild = false)
      {
         if (this.Name == "Palette") return true;
         if (this.Parent != null && DetermineIfChild) return GetParent().IsPalette(DetermineIfChild);
         return false;
      }
      internal bool IsPowerGrid()
      {
         return this.Name == "PowerGrid";
      }
      internal bool IsEditor(bool DetermineIfChild = false)
      {
         if (this.Name == "Editor") return true;
         if (this.Parent != null && DetermineIfChild) return GetParent().IsEditor(DetermineIfChild);
         return false;
      }
      internal bool IsPhysicsState()
      {
         return Parent != null && GetParent().Name == "Physics" && Name == "State";
      }
      internal bool IsSystemArchive()
      {
         return Parent != null && GetParent().Name == "SystemArchives";
      }
      internal bool IsLogisticsRequest()
      {
         return Parent != null && GetParent().Name == "Requests" && Parent.Parent != null && GetParent().GetParent().Name == "Logistics";
      }
      internal bool IsWeather()
      {
         return Name == "Weather";
      }
      internal bool IsOrders()
      {
         return Name == "Orders";
      }
      internal bool IsNetwork()
      {
         return Name == "Network";
      }
      internal bool IsHabitationZone()
      {
         return Parent != null && GetParent().Name == "Zones" && Parent.Parent != null && GetParent().GetParent().Name == "Habitation";
      }
      internal bool IsWorkQueueJob()
      {
         return Parent != null && GetParent().Name == "Jobs" && Parent.Parent != null && GetParent().GetParent().Name == "WorkQueue";
      }
      internal bool IsLogisticsTransfer()
      {
         return Parent != null && GetParent().Name == "Transfers" && Parent.Parent != null && GetParent().GetParent().Name == "Logistics";
      }
      internal static string GetHazardName(string id) //todo: replace with enum
      {
         string HazardName = String.Empty;
         if (id == "1") HazardName = "asteroid field";
         else if (id == "2") HazardName = "gas cloud";
         return HazardName;
      }
      internal static string GetStarSystemSummary(Node node)
      {
         string Summary = String.Empty;
         Summary += node.Properties["Name"];
         if (node.Properties.Contains("Colony")) Summary += ", Colony";
         if (node.Properties.Contains("Shipyard")) Summary += ", Shipyard";
         if (node.Properties.Contains("Comet")) Summary += ", Comet";
         if (node.Properties.Contains("Hostiles")) Summary += ", Hostiles";
         if (node.Properties.Contains("Rescue")) Summary += ", Rescue";
         return Summary;
      }
      internal string GetMissionName()
      {
         string details = Properties["Type"]!.ToString()!;
         string missionType = details;

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
         }
         return details;
      }
      internal string GetProductionMissionDetails()
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
      internal string GetCombatMissionDetails()
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
      internal static string GetMissionRequirement(Node node)
      {
         string details = node.Properties["Type"]!.ToString()!;
         if (node.Properties.Contains("ObjectType")) { details += $", {node.Properties["ObjectType"]}"; }
         if (node.Properties.Contains("Count")) { details += $", {node.Properties["Count"]}"; }
         return details;
      }
      internal static string GetLayerDetails(Node node)
      {
         string details = $"{node.Properties["Id"]}, {node.Properties["Name"]}, {node.Properties["Type"]}";
         if (node.Properties.Contains("SystemId")) details += $", System {node.Properties["SystemId"]}";
         return details;
      }
      internal static string GetTradingPostDetails(Node node)
      {
         return $"System {node.Properties["SystemId"]}";
      }
      internal static string GetFtlJourneyDetails(Node node)
      {
         return $"{node.Properties["State"]}: {node.Properties["Layers"]} from System {node.Properties["FromSystem"]} to {node.Properties["ToSystem"]}";
      }
      internal string GetLayerObjectDetails()
      {
         string details = $"{Properties["Id"]}, {Properties["Type"]}";
         if (Properties.Contains("State")) details += $", {Properties["State"]}";
         if (Properties.Contains("CauseOfDeath")) details += $", Cause of death: {Properties["CauseOfDeath"]}";
         if (Properties.Contains("HomeLayer")) details += $", Home layer: {Properties["HomeLayer"]}";
         if (Properties.Contains("Resource")) details += $", {Properties["Resource"]}";
         if (Properties.Contains("Quantity")) details += $", Qty: {Properties["Quantity"]}";
         if (Properties.Contains("Capacity")) details += $", Cap.: {Properties["Capacity"]}";
         if (Properties.Contains("Recipe")) details += $", Recipe: {Properties["Recipe"]}";
         if (Properties.Contains("Contents")) details += $", Contents: {Properties["Contents"]}";
         return details;
      }
      internal string GetPhysicsStateDetails()
      {
         return $"{this.Properties["Id"]}";
      }
      internal string GetSystemArchiveDetails()
      {
         return Regex.Replace(Name, @"^\""\[i\s", "NG").Replace("]\"", "");
      }
      internal string GetLogisticsRequestDetails()
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
      internal string GetLogisticsTransferDetails()
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
      internal string GetSystemId()
      {
         TryGetProperty("SystemId", out string systemId);
         return systemId;
      }
      internal string GetNetworkDetails()
      {
         return $"{Properties["Type"]}, {Properties["Id"]}";
      }
      internal string GetHabitationZoneDetails()
      {
         string entities = $"{Properties["Entities"]}";
         int used = entities.Length - entities.Replace(",", "").Length + 1;
         return $"ID {Properties["Id"]}, Capacity: {used}/{Properties["Capacity"]}";
      }
      internal string GetWorkQueueJobDetails()
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
