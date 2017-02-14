using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

public class Form1 : Form, IApplication
{
    private PropertyGrid propertyGrid1;
    private System.ComponentModel.Container components = null;

    public Form1()
    {
        InitializeComponent();
        propertyGrid1.SelectedObject = new Customer();
    }

    Customer customer
    {
        get { return (Customer)propertyGrid1.SelectedObject; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (components != null)
            {
                components.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
        this.SuspendLayout();
        //
        // propertyGrid1
        //
        this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
        | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
        this.propertyGrid1.Name = "propertyGrid1";
        this.propertyGrid1.Size = new System.Drawing.Size(434, 265);
        this.propertyGrid1.TabIndex = 0;
        //
        // Form1
        //
        this.ClientSize = new System.Drawing.Size(435, 264);
        this.Controls.Add(this.propertyGrid1);
        this.Name = "Form1";
        this.ResumeLayout(false);
    }

    #endregion Windows Form Designer generated code

    public void SetEmail(string email)
    {
        InGuiThread(() =>
        {
            customer.Email = email;
            propertyGrid1.Refresh();
        });
    }

    public void SetDateOfBirth(DateTime dateOfBirth)
    {
        InGuiThread(() =>
        {
            customer.DateOfBirth = dateOfBirth;
            propertyGrid1.Refresh();
        });
    }

    public void SetAge(int age)
    {
        InGuiThread(() =>
            {
                customer.Age = age;
                propertyGrid1.Refresh();
            });
    }

    public void SetName(string name)
    {
        InGuiThread(() =>
        {
            customer.Name = name;
            propertyGrid1.Refresh();
        });
    }

    public void Start()
    {
        InGuiThread(Show);
    }

    public void Stop()
    {
        InGuiThread(Close);
    }

    void InGuiThread(Action action)
    {
        if (this.InvokeRequired)
            this.Invoke(action);
        else
            action();
    }
}

public interface IApplication
{
    void Start();

    void Stop();

    void SetEmail(string email);

    void SetDateOfBirth(DateTime dateOfBirth);

    void SetAge(int age);

    void SetName(string name);
}

[DefaultPropertyAttribute("Name")]
public class Customer
{
    private string name;
    private int age;
    private DateTime dateOfBirth;
    private string ssn;
    private string address;
    private string email;
    private bool frequentBuyer;

    // Name property with category attribute and
    // description attribute added
    [CategoryAttribute("ID Settings"), DescriptionAttribute("Name of the customer")]
    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            name = value;
        }
    }

    [CategoryAttribute("ID Settings"),
    DescriptionAttribute("Social Security Number of the customer")]
    public string SSN
    {
        get
        {
            return ssn;
        }
        set
        {
            ssn = value;
        }
    }

    [CategoryAttribute("ID Settings"),
    DescriptionAttribute("Address of the customer")]
    public string Address
    {
        get
        {
            return address;
        }
        set
        {
            address = value;
        }
    }

    [CategoryAttribute("ID Settings"),
    DescriptionAttribute("Date of Birth of the Customer (optional)")]
    public DateTime DateOfBirth
    {
        get
        {
            return dateOfBirth;
        }
        set
        {
            dateOfBirth = value;
        }
    }

    [CategoryAttribute("ID Settings"), DescriptionAttribute("Age of the customer")]
    public int Age
    {
        get
        {
            return age;
        }
        set
        {
            age = value;
        }
    }

    [CategoryAttribute("Marketing Settings"), DescriptionAttribute("If the customer as bought more than 10 times, this is set to true")]
    public bool FrequentBuyer
    {
        get
        {
            return frequentBuyer;
        }
        set
        {
            frequentBuyer = value;
        }
    }

    [CategoryAttribute("Marketing Settings"), DescriptionAttribute("Most current e-mail of the customer")]
    public string Email
    {
        get
        {
            return email;
        }
        set
        {
            email = value;
        }
    }

    public Customer()
    {
    }
}

class Script
{
    const string usage = "Usage: cscscript WinForm ...\nThe primitive example that demonstrates how to create WinForm application.\n";

    [STAThread]
    static public void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());

        //IApplication app = new Form1();

        //ThreadPool.QueueUserWorkItem(x =>
        //    {
        //        Thread.Sleep(1000);
        //        app.SetAge(17);
        //        Thread.Sleep(1000);
        //        app.SetEmail("john.smith@gmail.com");
        //        Thread.Sleep(1000);
        //        app.SetName("John Smith");
        //        Thread.Sleep(1000);
        //        app.SetDateOfBirth(DateTime.Now.AddYears(-17));
        //    });

        //Application.Run((Form)app);
    }
}
