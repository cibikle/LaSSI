using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Eto.Forms;

namespace LaSSI
{
   public class FileWriter
   {
      public FileWriter()
      {

      }
      public static bool WriteFile(TreeGridItem root, string Filename)
      {
         string rootdata = RenderRoot(root.Values);
         string hudData = string.Empty;
         var iter = root.Children.GetEnumerator();
         //Debug.WriteLine($"{root.Values[0]}");
         while (iter.MoveNext())
         {
            TreeGridItem cur = (TreeGridItem)iter.Current;
            //Debug.WriteLine($"{cur.Values[0]}");
            string name = cur.Values[0].ToString()!;
            switch (name)
            {
               case "Hud":
               {
                  hudData = RenderHud(cur.Values);
                  break;
               }
            }
         }
         string data = Environment.NewLine + rootdata + hudData;
         using (var sw = new StreamWriter(Filename))
         {
            sw.Write(data);
         }
            
         return false;
      }
      private static string RenderRoot(object[] RootValues)
      {
         string foo = string.Empty;
         var s = RootValues[1];
         if (s is OrderedDictionary dictionary && dictionary.Count != 0)
         {
            foreach(DictionaryEntry p in dictionary)
            {
               int keylen = p.Key.ToString()!.Length;
               string pad = new string(' ', (22 - keylen - 1));
               foo += $"{p.Key}{pad}{p.Value}  {Environment.NewLine}";
            }
         }

         return foo;
      }
      private static string RenderHud(object[] HudValues)
      {
         string foo = "BEGIN HUD";
         int keylen = foo.Length;
         foo += new string(' ', (18 - keylen - 1));
         var s = HudValues[1];
         if (s is OrderedDictionary dictionary && dictionary.Count != 0)
         {
            foreach (DictionaryEntry p in dictionary)
            {
               foo += $"{p.Key} {p.Value}  ";
            }
         }
         foo += "END" + Environment.NewLine;
         return foo;
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
            TreeGridItemCollection childItems = new TreeGridItemCollection();
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
   }
}

