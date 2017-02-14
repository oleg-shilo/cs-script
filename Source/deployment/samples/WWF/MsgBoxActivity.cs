using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Drawing;
using System.Workflow.ComponentModel;
using System.Workflow.ComponentModel.Design;
using System.Workflow.ComponentModel.Compiler;
using System.Workflow.ComponentModel.Serialization;
using System.Workflow.Runtime;
using System.Workflow.Activities;
using System.Workflow.Activities.Rules;
using System.Collections.Generic;
using System.Text;
using System.Workflow;
using System.IO;
using System.Windows.Forms;

namespace Scripting
{
	public class MsgBox : Activity
	{
		public MsgBox()
		{
			base.Name = "MsgBox1";
		}
		public static DependencyProperty MessageProperty = DependencyProperty.Register
		    ("Message", typeof(string), typeof(MsgBox));

		public string Message
		{
			get { return Convert.ToString(base.GetValue(MessageProperty)); }
			set { base.SetValue(MessageProperty, value); }
		}

		protected override ActivityExecutionStatus Execute(ActivityExecutionContext executionContext)
		{
			try
			{
				MessageBox.Show(Message);
				return ActivityExecutionStatus.Closed;
			}
			catch
			{
				return ActivityExecutionStatus.Faulting;
			}
		}
	}
}
