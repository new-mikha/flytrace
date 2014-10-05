using System;
using System.Threading;
using System.Windows.Forms;

using log4net;

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
      // 
      // HiddenForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      this.AutoScaleMode = AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size( 300, 243 );
      this.FormBorderStyle = FormBorderStyle.None;
      this.Name = "HiddenForm";
      this.Text = "HiddenForm";
      this.WindowState = FormWindowState.Minimized;
      this.FormClosing += this.HiddenForm_FormClosing;
      this.Load += this.HiddenForm_Load;
      this.ResumeLayout( false );

    }

    #endregion

    private static readonly ILog Log = LogManager.GetLogger( "TimeChange" );

    // ReSharper disable once InconsistentNaming
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
  }
}