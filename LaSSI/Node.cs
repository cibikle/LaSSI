using System;
using System.Collections.Generic;
using System.Collections.Specialized;

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
         this.AddAddlNameDetails();
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
      public static string GetMissionName(Node node)
      {
         string details = node.Properties["Type"]!.ToString()!;
         if (details == "Production" && node.Properties.Contains("Resource")) { details += $", {node.Properties["Resource"]}"; }
         if (node.Properties.Contains("ItemCount")) { details += $", {node.Properties["ItemCount"]}"; }
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
         return $"System {node.Properties["FromSystem"]} to System {node.Properties["ToSystem"]}";
      }
      internal string GetLayerObjectDetails()
      {
         return $"{this.Properties["Type"]}";
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
         else if (this.IsMission())
         {
            addlDetails = Node.GetMissionName(this);
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
