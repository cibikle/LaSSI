using System;
using System.Linq;
using System.Collections.Generic;
using Eto.Forms;

namespace LaSSI
{
	public class AboutCommand : Command
	{
      readonly Modal m;
      readonly Control parent;
		public AboutCommand(Control parent)
		{
         MenuText = "About...";
         Executed += AboutCommand_Executed;
         this.parent = parent;

         string title = "LaSSI (Last Starship Save Inspector)";
         string author = "CIBikle, 2023";
         string tlsOwner = "'The Last Starship' is the property of Introversion Software";
         string tlsOwnerLink = "https://www.introversion.co.uk/introversion/";
         string disclaimer = "This is a fan-made tool for educational and entertainment purposes";
         string LassiGithub = "https://github.com/cibikle/LaSSI";

         m = new Modal(new List<string> { title,author,tlsOwner,disclaimer }, new List<string> { tlsOwnerLink, LassiGithub});
      }

      private void AboutCommand_Executed(object? sender, EventArgs e)
      {
         m.ShowModal(this.parent);
      }
      
   }
}

