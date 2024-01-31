using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;

namespace LaSSI
{	
	public class MyPanel1 : Dialog
	{	
		public MyPanel1()
		{
			XamlReader.Load(this);
		}
	}
}
