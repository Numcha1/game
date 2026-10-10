using System.Runtime.InteropServices;

namespace SuanPleng;
// Balance the native request on disposal. On Windows 10 2004+ this request is process-local.
sealed class FrameClock : IDisposable {
    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
    bool acquired=timeBeginPeriod(1)==0;
    public void Dispose(){if(acquired){timeEndPeriod(1);acquired=false;}}
}
