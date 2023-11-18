using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Eto.Forms;

namespace LaSSI
{
   public class FileWriter
   {
      private readonly int IndentationAmount = 4;
      public FileWriter()
      {

      }
      public bool WriteFile(TreeGridItem root, string Filename)
      {
         string rootdata = RenderRoot(root.Values);
         string text = string.Empty;
         foreach (TreeGridItem child in root.Children)
         {
            text += foo(child);
         }

         string data = Environment.NewLine + rootdata + text;
         using (var sw = new StreamWriter(Filename))
         {
            sw.Write(data);
         }

         return false;
      }
      private static bool IsOneliner(TreeGridItem item) // todo: this is a travesty
      {
         bool IsOneLiner;
         if (((TreeGridItem)item.Parent).Tag.ToString() == "Zones")
         {
            if (item.Values[1] is OrderedDictionary dic)
            {
               if (dic.Contains("Entities"))
               {
                  string bar = dic["Entities"]!.ToString()!;
                  int baz = bar.Count(c => c == ',');
                  if (baz < 5) // todo: get rid of the magic number!
                  {
                     IsOneLiner = true;
                  }
                  else
                  {
                     IsOneLiner = false;
                  }
               }
               else
               {
                  IsOneLiner = true;
               }
            }
            else
            {
               IsOneLiner = true;
            }
         }
         else if (item.Tag is (object)"WorkQueue" && item.Children.Count == 0)
         {
            if (item.Values[1] is OrderedDictionary dic)
            {
               if (dic.Contains("Workers"))
               {
                  string bar = dic["Workers"]!.ToString()!;
                  int baz = bar.Count(s => s == ',');
                  if (baz < 5) // todo: get rid of the magic number!
                  {
                     IsOneLiner = true;
                  }
                  else
                  {
                     IsOneLiner = false;
                  }
               }
               else
               {
                  IsOneLiner = true;
               }
            }
            else
            {
               IsOneLiner = true;
            }
         }
         else if (item.Tag is (object)"Cells")
         {
            if (item.Values[1] is OrderedDictionary dic)
            {
               if (dic.Count == 0)
               {
                  IsOneLiner = true;
               }
               else
               {
                  IsOneLiner = false;
               }
            }
            else
            {
               IsOneLiner = true;
            }

         }
         else if (item.Children.Count == 0
            && ((OrderedDictionary)item.Values[1]).Count <= 10
            && item.Tag is not (object)"Palette"
            && item.Tag is not (object)"OurStock"
            && item.Tag is not (object)"TheirStock")
         {
            IsOneLiner = true;
         }
         else if ((item.Tag.ToString() == "Palette" || item.Tag.ToString() == "OurStock" || item.Tag.ToString() == "TheirStock")
            && ((OrderedDictionary)item.Values[1]).Count == 0)
         {
            IsOneLiner = true;
         }
         else
         {
            IsOneLiner = false;
         }

         return IsOneLiner;
      }
      private string foo(TreeGridItem item, int indentationLevel = 0)
      {
         if (IsOneliner(item))
         {
            return RenderOneLiner(item, indentationLevel);
         }
         else
         {
            return RenderMultiliner(item, indentationLevel);
         }
      }
      private static string CleanName(TreeGridItem item)
      {
         string name = (string)item.Tag;
         if (name.Contains('('))
         {
            name = name[..(name.IndexOf("("))];
         }
         return name;
      }
      private string GetIndentPad(int indentationLevel = 0)
      {
         return new string(' ', indentationLevel * IndentationAmount);
      }
      private string RenderOneLiner(TreeGridItem item, int indentationLevel = 0)
      {
         string name = CleanName(item);
         string indent = GetIndentPad(indentationLevel);
         string text = $"{indent}BEGIN {name}{new string(' ', IndentationAmount)}  {RenderProperties(item)}END{Environment.NewLine}";

         return text;
      }
      private string RenderMultiliner(TreeGridItem item, int indentationLevel = 0)
      {
         string name = CleanName(item);
         string indent = GetIndentPad(indentationLevel);
         indentationLevel++;
         string text = $"{indent}BEGIN {name}{Environment.NewLine}";
         if (name == "PowerGrid" || name == "Palette")
         {
            int index = 0;
            if (name == "PowerGrid" && ((OrderedDictionary)item.Values[1]).Count == 12)
            {
               index = 1;
               text += RenderProperties(item, indentationLevel, true, index);
            }

            text += PropertiesToNodes(item, indentationLevel, index);
         }
         else
         {
            text += RenderProperties(item, indentationLevel, true);
         }

         foreach (TreeGridItem child in item.Children)
         {
            text += foo(child, indentationLevel);
         }
         text += $"{indent}END{Environment.NewLine}";
         return text;
      }
      private string RenderProperties(TreeGridItem item, int indentationLevel = 0, bool multiline = false, int truncateIndex = -1)
      {
         int counter = 0;
         string text = string.Empty;
         string indent = GetIndentPad(indentationLevel);
         var dic = (OrderedDictionary)item.Values[1];
         //if (dic.Contains("Entities"))
         //{
         //   multiline = true;
         //}
         foreach (DictionaryEntry entry in dic)
         {
            if(IsPropertyArray(entry.Key))
            {
               if (entry.Value!.ToString()!.Contains(' '))
               {
                  text += $"{indent}{entry.Key} \"[{entry.Value}]\"  ";
               }
               else
               {
                  text += $"{indent}{entry.Key} [{entry.Value}]  ";
               }
            }
            else
            {
               text += $"{indent}{entry.Key} {entry.Value}  ";
            }

            if (multiline) text += Environment.NewLine;
            counter++;
            if (truncateIndex > -1 && counter >= truncateIndex)
            {
               break;
            }
         }
         return text;
      }
      private static bool IsPropertyArray(object key)
      {
         return (key is (object)"Entities"
            or (object)"Researched"
            or (object)"Visible"
            or (object)"Workers"
            or (object)"UnloadRequests"
            or (object)"UnlockedRecipes"
            or (object)"SpecialUnlocks"
            or (object)"Items") ; // todo: clean this up
      }
      private string PropertiesToNodes(TreeGridItem item, int indentationLevel = 0, int startIndex = 0)
      {
         string name = CleanName(item);
         string text = string.Empty;
         string indent = GetIndentPad(indentationLevel);
         var dic = (OrderedDictionary)item.Values[1];
         int counter = 0;
         foreach (DictionaryEntry entry in dic)
         {
            if (counter < startIndex)
            {
               counter++;
               continue;
            }
            string key = entry.Key.ToString()!;
            string value = entry.Value!.ToString()!;
            if (name == "PowerGrid")
            {
               key = key[..(key.IndexOf(' '))];
               if(value == "Setting 0")
               {
                  value = string.Empty;
               }
            }
            text += $"{indent}BEGIN {key}{indent}{value}  END{Environment.NewLine}";
         }

         return text;
      }
      private static string RenderRoot(object[] RootValues)
      {
         string foo = string.Empty;
         var s = RootValues[1];
         if (s is OrderedDictionary dictionary && dictionary.Count != 0)
         {
            foreach (DictionaryEntry p in dictionary)
            {
               int keylen = p.Key.ToString()!.Length;
               //string pad = new string(' ', (22 - keylen - 1)); //magic numbers screwed us again!
               string pad = " "; // less pretty, but future-proof
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

