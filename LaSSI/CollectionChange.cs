using System;
using System.Collections.Generic;
using Eto.Forms;

namespace LaSSI
{
   public class CollectionChange
   {
      public enum ActionType
      {
         None,
         Change,
         Addition,
         Deletion
      }
      public Oncler Data;
      public ActionType Action;
      public CollectionChange(ActionType action, Oncler data)
      {
         Action = action;
         Data = data;
      }

      internal static bool ListContainsOncler(List<CollectionChange> changes, string key)
      {
         foreach (var change in changes)
         {
            if (change.Data.Key.Equals(key))
            {
               return true;
            }
         }
         return false;
      }

      internal static ActionType GetOnclersChangeAction(List<CollectionChange> changes, string key)
      {
         if (CollectionChange.ListContainsOncler(changes, key))
         {
            return FindInList(changes, key).Action;
         }
         else
         {
            return ActionType.None;
         }
      }

      internal static void UpdateChangeValue(List<CollectionChange> changes, Oncler values)
      {
         if (CollectionChange.ListContainsOncler(changes, values.Key))
         {
            FindInList(changes, values.Key).Data.Value = values.Value;
         }
      }

      internal static CollectionChange FindInList(List<CollectionChange> changes, string key)
      {
         return changes.Find(x => x.Data.Key == key);
      }

      public static void AddChange(List<CollectionChange> changes, Oncler newChange, CollectionChange.ActionType action)
      {
         if (!CollectionChange.ListContainsOncler(changes, newChange.Key))
         {
            changes.Add(new CollectionChange(action, newChange));
            return;
         }
         switch (action)
         {
            case ActionType.Addition:
               {
                  if (CollectionChange.GetOnclersChangeAction(changes, newChange.Key) == CollectionChange.ActionType.Deletion)
                  {
                     changes.Remove(CollectionChange.FindInList(changes, newChange.Key));
                  }
                  break;
               }
            case ActionType.Deletion:
               {
                  bool shouldAdd = CollectionChange.GetOnclersChangeAction(changes, newChange.Key) == ActionType.Change;
                  changes.Remove(CollectionChange.FindInList(changes, newChange.Key));
                  if (shouldAdd) changes.Add(new CollectionChange(action, newChange));
                  break;
               }
            case ActionType.Change:
               {
                  ActionType prevAction = CollectionChange.GetOnclersChangeAction(changes, newChange.Key);
                  if (prevAction == ActionType.Change || prevAction == ActionType.Addition)
                  {
                     //update value
                     CollectionChange.FindInList(changes, newChange.Key).Data.Value = newChange.Value;
                  }
                  break;
               }
         }


         //ADD
         // check if already in changes list
         // if del, remove del and continue


         //DEL

         //check if already in changes list
         //if chg, remove and then add del op
         //if add, remove add op and continue
         //List<CollectionChange> changes = ((DetailsLayout)GetPanel2DetailsLayout().Content).Changes;

         //CHG

         //check if already in list
         //if rem, return
         //if add, update value
         //if change, update value
         //otherwise, add to list
      }
      //public static bool CheckForChange(List<CollectionChange> changes, Oncler newChange)
      //{


      //   return false;
      //}
   }
}

