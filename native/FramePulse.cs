using System.Diagnostics;

namespace SuanPleng;
// Schedule one UI callback at a time. WM_TIMER can be delayed behind paints/network messages.
// The farm clock remains on the authoritative server; this only paces visual frames.
sealed class FramePulse : IDisposable {
    readonly Control control;
    readonly Stopwatch clock=Stopwatch.StartNew();
    readonly long period=Stopwatch.Frequency/60;
    System.Threading.Timer? pulse;
    long next;
    int queued;
    volatile bool disposed,enabled;
    public event EventHandler? Tick;
    public FramePulse(Control control){this.control=control;control.HandleCreated+=HandleCreated;}
    void HandleCreated(object? sender,EventArgs e){if(enabled)Start();}
    public void Start(){if(disposed)return;enabled=true;if(!control.IsHandleCreated||pulse!=null)return;next=clock.ElapsedTicks;pulse=new System.Threading.Timer(Post,null,0,1);}
    void Post(object? state) {
        if(disposed||!enabled||clock.ElapsedTicks<Interlocked.Read(ref next)||Interlocked.CompareExchange(ref queued,1,0)!=0)return;
        try {
            control.BeginInvoke((MethodInvoker)(()=>{
                try {
                    if(disposed||!enabled)return;
                    long now=clock.ElapsedTicks,due=Interlocked.Read(ref next);if(now-due>period*6)due=now;
                    Interlocked.Exchange(ref next,due+period);Tick?.Invoke(this,EventArgs.Empty);
                }finally{Interlocked.Exchange(ref queued,0);}
            }));
        }catch(InvalidOperationException){Interlocked.Exchange(ref queued,0);}
    }
    public void Stop(){enabled=false;pulse?.Dispose();pulse=null;}
    public void Dispose(){if(disposed)return;disposed=true;Stop();control.HandleCreated-=HandleCreated;Tick=null;}
}
