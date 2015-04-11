using System;
using System.Threading;
using System.Windows.Forms;

using log4net;

namespace FlyTrace.Tools
{
  public partial class SystemEventsHiddenForm : Form
  {
    public SystemEventsHiddenForm( )
    {
      InitializeComponent( );
    }

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

    private void SystemEventsHiddenForm_Load( object sender, EventArgs e )
    {
      Log.Debug( "SystemEventsHiddenForm_Load" );
    }

    private void SystemEventsHiddenForm_FormClosing( object sender, FormClosingEventArgs e )
    {
      Log.Debug( "SystemEventsHiddenForm_FormClosing" );
    }
  }
}
