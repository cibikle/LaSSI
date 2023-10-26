using System.Collections.ObjectModel;

namespace LaSSI
{
   public class Node
   {
      public string Name { get; set; } = string.Empty;
      public string Text { get; set; } = string.Empty;
      public int Id { get; set; }
      public Node? Parent { get; set; }
      public ObservableCollection<Node> Children { get; set; }
      public Dictionary<string, object> Properties { get; }

      public Node()
      {
         Name = string.Empty;
         Id = 0;
         Parent = null;
         Children = new ObservableCollection<Node>();
         Properties = new Dictionary<string, object>();
      }
      public Node(string name, int id, Node? parent, ObservableCollection<Node> children, Dictionary<string, object> properties)
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
         Children = new ObservableCollection<Node>();
         Properties = new Dictionary<string, object>();
      }
      public Node(string name, Node? parent)
      {
         Name = name;
         Id = 0;
         Parent = parent;
         Children = new ObservableCollection<Node>();
         Properties = new Dictionary<string, object>();
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
   }
}
