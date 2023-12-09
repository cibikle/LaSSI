using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class Node
   {
      public string Name { get; set; } = string.Empty;
      public string Text { get; set; } = string.Empty;
      public int Id { get; set; }
      public Node? Parent { get; set; }
      public List<Node> Children { get; set; }
      public OrderedDictionary Properties { get; }
      public Node()
      {
         Name = string.Empty;
         Id = 0;
         Parent = null;
         Children = new List<Node>();
         Properties = new OrderedDictionary();
      }
      public Node(string name, int id, Node? parent, List<Node> children, OrderedDictionary properties)
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
         Children = new List<Node>();
         Properties = new OrderedDictionary();
      }
      public Node(string name, OrderedDictionary properties)
      {
         Name = name;
         Id = 0;
         Parent = null;
         Children = new List<Node>();
         Properties = properties;
      }
      public Node(string name, OrderedDictionary properties, Node parent)
      {
         Name = name;
         Id = 0;
         Parent = parent;
         Children = new List<Node>();
         Properties = properties;
         AddAddlNameDetails();
      }
      public Node(string name, Node? parent)
      {
         Name = name;
         Id = 0;
         Parent = parent;
         Children = new List<Node>();
         Properties = new OrderedDictionary();
      }
      public void Add(Node node)
      {
         Children.Add(node);
         node.Parent = this;
      }
      public void Remove(Node node)
      {
         Children.Remove(node);
      }
      public Node GetRoot()
      {
         Node node = this;
         while (node!.Parent is not null)
         {
            node = node.Parent;
         }
         return node;
      }
      public bool Contains(Node node, bool recurse)
      {
         if (!this.HasChildren()) return false;
         if (!this.Children.Contains(node) && !recurse) return false;
         if (this.Children.Contains(node)) return true;
         bool contains = false;
         foreach (Node child in this.Children)
         {
            contains = child.Contains(node, recurse);
            if (contains) break;
         }
         return contains;
      }
      public bool HasChildren()
      {
         if (this.Children.Count == 0) return false;
         return true;
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
      private bool IsHazard()
      {
         if (this.Parent != null && this.Parent.Name == "Hazards" && this.Properties.Contains("Type")) return true;
         return false;
      }
      private bool IsStarSystem()
      {
         if (this.Parent != null && this.Parent.Name == "Objects"
            && this.Parent.Parent != null && this.Parent.Parent.Name == "Galaxy"
            && this.Properties.Contains("Name")) return true;
         return false;
      }
      public bool IsMission()
      {
         if (this.Parent != null && this.Parent.Name == "Missions"
            && this.Parent.Parent != null && this.Parent.Parent.Name == "Missions"
            && this.Properties.Contains("Type")) return true;
         return false;
      }
      public bool IsMissionRequirement()
      {
         if (this.Parent != null && this.Parent.Name == "Requirements"
            && this.Parent.Parent != null
            && this.Parent.Parent.Parent != null && this.Parent.Parent.Parent.Name == "Missions") return true;
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
         if (Parent != null && this.Parent.Name == "Journeys") return true;
         return false;
      }
      /// <summary>
      /// Determines if the calling node is a layer or, optionally, the child of a Layer (free space/ship).
      /// </summary>
      public bool IsLayer(bool DetermineIfChild = false)
      {
         if (this.Name == "Layer") return true;
         if (this.Parent != null && DetermineIfChild) return this.Parent.IsLayer(DetermineIfChild);
         return false;
      }
      internal bool IsLayerObject()
      {
         return this.IsLayer(true) && this.Parent != null && Parent.Name == "Objects";
      }
      internal bool IsPalette(bool DetermineIfChild = false)
      {
         if (this.Name == "Palette") return true;
         if (this.Parent != null && DetermineIfChild) return this.Parent.IsPalette(DetermineIfChild);
         return false;
      }
      internal bool IsPowerGrid()
      {
         return this.Name == "PowerGrid";
      }
      internal bool IsEditor(bool DetermineIfChild = false)
      {
         if (this.Name == "Editor") return true;
         if (this.Parent != null && DetermineIfChild) return this.Parent.IsEditor(DetermineIfChild);
         return false;
      }
      internal bool IsPhysicsState()
      {
         return Parent != null && Parent.Name == "Physics" && Name == "State";
      }
      internal bool IsSystemArchive()
      {
         return Parent != null && Parent.Name == "SystemArchives";
      }
      internal bool IsLogisticsRequest()
      {
         return Parent != null && Parent.Name == "Requests" && Parent.Parent != null && Parent.Parent.Name == "Logistics";
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
         return Parent != null && Parent.Name == "Zones" && Parent.Parent != null && Parent.Parent.Name == "Habitation";
      }
      internal bool IsWorkQueueJob()
      {
         return Parent != null && Parent.Name == "Jobs" && Parent.Parent != null && Parent.Parent.Name == "WorkQueue";
      }
      internal bool IsLogisticsTransfer()
      {
         return Parent != null && Parent.Name == "Transfers" && Parent.Parent != null && Parent.Parent.Name == "Logistics";
      }
      private static string GetHazardName(string id) //todo: replace with enum
      {
         string HazardName = String.Empty;
         if (id == "1") HazardName = "asteroid field";
         else if (id == "2") HazardName = "gas cloud";
         return HazardName;
      }
      private static string GetStarSystemSummary(Node node)
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
      public string GetMissionName()
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
      private string GetProductionMissionDetails()
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
      private string GetCombatMissionDetails()
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
         return $"{node.Properties["Id"]}, {node.Properties["Name"]}, {node.Properties["Type"]}";
      }
      public static string GetTradingPostDetails(Node node)
      {
         return $"System {node.Properties["SystemId"]}";
      }
      private static string GetFtlJourneyDetails(Node node)
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
