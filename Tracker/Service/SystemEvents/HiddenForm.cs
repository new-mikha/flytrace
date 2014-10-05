using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Windows.Forms;
using System.Threading;

namespace FlyTrace.Service.SystemEvents
{
  internal class HiddenForm : Form
  {
    #region Form basic plumbing

    public HiddenForm( )
    {
      InitializeComponent( );
    }

    private readonly System.ComponentModel.IContainer components = null;

    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose( );
      }
      base.Dispose( disposing );
    }

    private void InitializeComponent( )
    {
      this.SuspendLayout( );
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size( 0, 0 );
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
      this.Name = "HiddenForm";
      this.Text = "HiddenForm";
      this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
      this.Load += new System.EventHandler( this.HiddenForm_Load );
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.HiddenForm_FormClosing );
      this.ResumeLayout( false );
    }

    #endregion

    private static readonly ILog Log = LogManager.GetLogger( "TimeChange" );

    private const int WM_TIMECHANGE = 0x1E;

    public event EventHandler TimeChanged;

    protected override void WndProc( ref Message m )
    {
      // For some unknown reasons Microsoft.Win32.SystemEvents.TimeChanged event 
      // doesn't work in this service, even with a message pump started as described 
      // here: http://msdn.microsoft.com/en-us/library/microsoft.win32.systemevents.aspx
      // So catching the message at the lower level, fortunately it works as expected:
      if ( m.Msg == WM_TIMECHANGE )
      {
        // TimeChanged can be changed between read and access, 
        // so take measures to read & use it atomically:
        EventHandler timeChanged = TimeChanged;
        Thread.MemoryBarrier( );
        
        if ( timeChanged != null )
        {
          try
          {
            timeChanged( this, EventArgs.Empty );
          }
          catch ( Exception exc )
          {
            Log.Error( "Error processing WM_TIMECHANGE event", exc );
          }
        }
      }

      base.WndProc( ref m );
    }

    private void HiddenForm_Load( object sender, EventArgs e )
    {
      Log.Info( "HiddenForm_Load" );
    }

    private void HiddenForm_FormClosing( object sender, FormClosingEventArgs e )
    {
      Log.Info( "HiddenForm_FormClosing" );
    }

    //private void SystemEvents_TimeChanged( object sender, EventArgs e )
    //{
    //  Log.Info( "SystemEvents_TimeChanged" );
    //}

    //private void SystemEvents_UPCChanged( object sender, UserPreferenceChangedEventArgs e )
    //{
    //  Log.Info( "SystemEvents_UPCChanged" );
    //}

  }
}