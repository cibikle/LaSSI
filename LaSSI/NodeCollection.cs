using System;
using System.Collections.Generic;
using Eto.Forms;

namespace LaSSI
{
   public class NodeCollection : ITreeGridStore<Node>
   {
      private List<Node> Nodes = new List<Node>();

      public NodeCollection()
      {

      }

      public Node this[int index] => Nodes[index];

      public int Count => Nodes.Count;

      public void Add(Node node)
      {
         Nodes.Add(node);
      }

      public void Remove(Node node)
      {
         Nodes.Remove(node);
      }

      public bool Contains(Node node)
      {
         return Nodes.Contains(node);
      }

      public List<Node>.Enumerator GetEnumerator()
      {
         return Nodes.GetEnumerator();
      }
   }
}

