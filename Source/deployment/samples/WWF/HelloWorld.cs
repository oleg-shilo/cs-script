//css_inc MsgBoxActivity.cs;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Threading;
using System.Workflow.ComponentModel.Compiler;
using System.Workflow.ComponentModel.Serialization;
using System.Workflow.ComponentModel;
using System.Workflow.ComponentModel.Design;
using System.Workflow.Runtime;
using System.Workflow.Activities;
using System.Workflow.Activities.Rules;
using System.Windows.Forms;

namespace Scripting
{
   

    public class HelloWorldWorkflow : SequentialWorkflowActivity
    {
        private MsgBox MsgBox1;

        public HelloWorldWorkflow()
        {
            InitializeComponent();
        }

        #region Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private void InitializeComponent()
        {
            this.CanModifyActivities = true;
            this.MsgBox1 = new Scripting.MsgBox();
            // 
            // MsgBox1
            // 
            this.MsgBox1.Message = "Hello World!";
            this.MsgBox1.Name = "MsgBox1";
            // 
            // HelloWorldWorkflow
            // 
            this.Activities.Add(this.MsgBox1);
            this.Name = "HelloWorldWorkflow";
            this.CanModifyActivities = false;

        }

        #endregion

        private void checkContinue(object sender, ConditionalEventArgs e)
        {
            e.Result = true;
        }
    }
  
    class Program
    {
        static void Main(string[] args)
        {
            using (WorkflowRuntime workflowRuntime = new WorkflowRuntime())
            {
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                workflowRuntime.WorkflowCompleted += delegate(object sender, WorkflowCompletedEventArgs e)
                {
                    waitHandle.Set();
                };
                workflowRuntime.WorkflowTerminated += delegate(object sender, WorkflowTerminatedEventArgs e)
                {
                    Console.WriteLine(e.Exception.Message);
                    waitHandle.Set();
                };

                workflowRuntime.CreateWorkflow(typeof(Scripting.HelloWorldWorkflow)).Start();
                waitHandle.WaitOne();
            }
        }
    }
}

