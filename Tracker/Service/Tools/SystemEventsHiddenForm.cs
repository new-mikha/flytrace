using System;
using System.Threading;
using System.Windows.Forms;

using log4net;

namespace FlyTrace.Service.Tools
{
  /// <summary>
  /// For some unknown reasons Microsoft.Win32.SystemEvents.TimeChanged event 
  /// doesn't work in this service, even with a message pump started as described 
  /// here: http://msdn.microsoft.com/en-us/library/microsoft.win32.systemevents.aspx
  /// So catching the message at the lower level, looks like it works as expected.
  /// </summary>
  internal class SystemEventsHiddenForm : Form
  {
    #region Form basic plumbing

    public SystemEventsHiddenForm( )
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
      this.SuspendLayout();
      // 
      // SystemEventsHiddenForm
      // 
      this.ClientSize = new System.Drawing.Size(264, 245);
      this.Name = "SystemEventsHiddenForm";
      this.ResumeLayout(false);

    }

    #endregion

    private static readonly ILog Log = LogManager.GetLogger( "TimeChange" );

    // ReSharper disable once InconsistentNaming
    private const int WM_TIMECHANGE = 0x1E;

    public event EventHandler TimeChanged;

    protected override void WndProc( ref Message m )
    {
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