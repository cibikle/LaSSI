using Eto.Forms;
using System.Collections;
using System.Collections.ObjectModel;

namespace LaSSI
{
   public class InventoryGridItem : GridItem
   {
      public string Name { get; set; } = string.Empty;
      public int Count { get; set; } = -1;
      public InventoryGridItem(string name, int count)
      {
         this.Name = name;
         this.Count = count;
      }
      public static bool ListContains(ObservableCollection<InventoryGridItem> list, string name)
      {
         foreach (InventoryGridItem item in list)
         {
            if (item.Name == name) return true;
         }
         return false;
      }
      public override string ToString()
      {
         return this.Name;
      }
      public DictionaryEntry ToDictionaryEntry()
      {
         return new DictionaryEntry(this.Name, this.Count);
      }
   }
}

