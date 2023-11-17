using System;
using System.Collections.Generic;
using Eto.Forms;

namespace LaSSI
{
   public class DetailsLayout : DynamicLayout
   {
      public enum State
      {
         Unmodified,
         Modified,
         Applied,

      }
      public State Status { get; set; }
      public List<CollectionChange> Changes = new();
      public DetailsLayout()
      {
         ID = "DetailsLayout";
         Status = State.Unmodified;
         Height = 250;
         Width = 250;
      }

   }
}

