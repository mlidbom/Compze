// Reference implementation — copied from Virtual-Desktop-Grid-Switcher (Deskmancer/Dev/AgentHarness.cs) on 2026-06-12.
// Copy into the app you are instrumenting and adapt: the namespace, the UI-framework types (this copy is
// Avalonia + SkiaSharp — swap the window/bitmap APIs for your framework), and the Deskmancer.Geometry dependency.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;
using SkiaSharp;
using Deskmancer.Geometry;
using Vanara.PInvoke;

namespace Deskmancer.Dev;

///<summary>
/// Development-only visual-test harness — NOT part of the product. Lets an agent verify rendered output
/// without Computer Use: it watches for a <c>capture.trigger</c> file beside the exe and writes
/// <c>capture.png</c> of its own on-screen, DWM-composited pixels (which PrintWindow / RenderTargetBitmap
/// miss). See the windows-gui-visual-testing skill. Delete this file (and its call sites) to ship.
/// </summary>
public static class AgentHarness
{
   static readonly string Dir = AppContext.BaseDirectory;
   static readonly string LogPath = Path.Combine(Dir, "agent.log");
   static readonly string TriggerPath = Path.Combine(Dir, "capture.trigger");
   static readonly string CapturePath = Path.Combine(Dir, "capture.png");
   static readonly object Gate = new();

   ///<summary>Start a fresh harness log file.</summary>
   public static void ResetLog()
   {
      lock(Gate) File.WriteAllText(LogPath, $"=== start {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
   }

   ///<summary>Append a line to the harness log.</summary>
   public static void Log(string message)
   {
      lock(Gate) File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff}  {message}\n");
   }

   // ReSharper disable once NotAccessedField.Local — held only to keep the single-instance Mutex alive for the process; if collected, the lock releases
   static Mutex? _mutex;

   ///<summary>Ensure only one harness instance runs; if another already owns the mutex, surface that one and return false.</summary>
   public static bool EnsureSingleInstance(string id)
   {
      _mutex = new Mutex(initiallyOwned: true, $@"Local\{id}.SingleApplicationInstanceManager", out bool isFirst);
      if(isFirst) return true;

      int me = Environment.ProcessId;
      string name = Process.GetCurrentProcess().ProcessName;
      foreach(var p in Process.GetProcessesByName(name))
      {
         if(p.Id == me) continue;
         var window = new HWND(p.MainWindowHandle);
         if(window == HWND.NULL) continue;
         User32.ShowWindow(window, ShowWindowCommand.SW_RESTORE);
         User32.SetForegroundWindow(window);
      }

      return false;
   }

   static bool _armed;

   ///<summary>Capture <paramref name="area"/> (whole <see cref="VirtualScreen"/>) — the overlay window spans it, so one <see cref="Gdi32.BitBlt"/> gets the whole grid. On trigger, raise the Avalonia overlay <paramref name="window"/> topmost, then <see cref="Gdi32.BitBlt"/> the area on the next tick.</summary>
   public static void StartCapturePump(Window window, ScreenRect area) =>
      StartCapturePump(() => new HWND(window.TryGetPlatformHandle()!.Handle), area); // the HWND exists from construction in Avalonia, so this never creates one

   ///<summary>Capture pump for a plain native window (one not hosted in Avalonia) — e.g. the switch-slide render window. Same as the Avalonia overload, but raises the given <paramref name="hwnd"/> directly.</summary>
   public static void StartCapturePump(HWND hwnd, ScreenRect area) => StartCapturePump(() => hwnd, area);

   static void StartCapturePump(Func<HWND> hwnd, ScreenRect area)
   {
      var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
      timer.Tick += (_, _) =>
      {
         if(_armed)
         {
            CaptureRectToPng((int)area.Left, (int)area.Top, (int)area.Width, (int)area.Height, CapturePath);
            _armed = false;
            return;
         }

         if(!File.Exists(TriggerPath)) return;
         try { File.Delete(TriggerPath); }
         catch(IOException ex) when(ex.HResult == (HRESULT)Win32Error.ERROR_SHARING_VIOLATION)
         { /* the shell writer still holds the handle open; harmless */
         }

         ForceToTop(hwnd());
         _armed = true;
      };
      timer.Start();
   }

   ///<summary><see cref="Gdi32.BitBlt"/> the screen rectangle and save it as a PNG (written atomically via a temp file).</summary>
   public static void CaptureRectToPng(int left, int top, int w, int h, string path)
   {
      if(w <= 0 || h <= 0) return;
      SavePixelsToPng(CapturePixels(left, top, w, h), w, h, path);
   }

   ///<summary>
   /// Dev probe for a fast transition: grab <paramref name="area"/> once at each offset in <paramref name="offsetsMs"/>
   /// measured from this call, so you can read a sub-100ms change frame by frame. The cheap pixel grab
   /// (<see cref="Gdi32.BitBlt"/>) is done tight to each offset; the slow PNG encoding is deferred until all frames are
   /// grabbed, so encoding never smears the timing. Each frame is saved as "<paramref name="namePrefix"/>-NNNN.Nms.png".
   ///</summary>
   ///<remarks>
   /// The UI thread is deliberately held (busy-waited to each offset) for the length of the burst — this is a dev probe,
   /// not the product. That is safe and correct for what it measures: the compositor advances frames on its own thread
   /// (DirectComposition plays committed work without our heartbeat), so a screen <see cref="Gdi32.BitBlt"/> still sees
   /// real on-screen progress while our thread is parked. Blocking actually helps — it keeps the samples tightly and
   /// evenly spaced instead of at the mercy of dispatcher scheduling.
   ///</remarks>
   public static void CaptureFrameBurst(ScreenRect area, IReadOnlyList<double> offsetsMs, string namePrefix)
   {
      int w = (int)area.Width, h = (int)area.Height;
      var frames = new List<(double Ms, byte[] Pixels)>(offsetsMs.Count);
      var sinceStart = Stopwatch.StartNew();
      foreach(double offset in offsetsMs)
      {
         while(sinceStart.Elapsed.TotalMilliseconds < offset) { } // spin to the offset; see remarks on why holding the UI thread is correct here
         frames.Add((sinceStart.Elapsed.TotalMilliseconds, CapturePixels((int)area.Left, (int)area.Top, w, h)));
      }

      foreach(var (ms, pixels) in frames)
      {
         string path = Path.Combine(Dir, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{namePrefix}-{ms:0000.0}ms.png"));
         SavePixelsToPng(pixels, w, h, path);
         Log(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{namePrefix}: frame at {ms:F1} ms -> {Path.GetFileName(path)}"));
      }
   }

   // Encode top-down BGRA bytes as a PNG, written atomically via a temp file.
   static void SavePixelsToPng(byte[] pixels, int w, int h, string path)
   {
      // Opaque: BitBlt leaves every alpha byte zeroed, so honoring alpha would encode a fully transparent image.
      using var image = SKImage.FromPixelCopy(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque), pixels, w * 4);
      using var png = image.Encode(SKEncodedImageFormat.Png, 100);
      string tmp = path + ".tmp";
      using(var fs = File.Create(tmp)) png.SaveTo(fs);
      File.Move(tmp, path, overwrite: true);
   }

   // BitBlt the screen region into top-down BGRA bytes — the SafeHandle DCs/bitmap release at scope exit.
   static byte[] CapturePixels(int left, int top, int w, int h)
   {
      using var screen = User32.GetDC(HWND.NULL);
      using var memDc = Gdi32.CreateCompatibleDC(screen);
      using var bmp = Gdi32.CreateCompatibleBitmap(screen, w, h);
      var previous = Gdi32.SelectObject(memDc, bmp);
      try
      {
         // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags — CAPTUREBLT is an independent flag bit on the ROP; Vanara just didn't mark the enum [Flags]
         Gdi32.BitBlt(memDc, 0, 0, w, h, screen, left, top, Gdi32.RasterOperationMode.SRCCOPY | Gdi32.RasterOperationMode.CAPTUREBLT);
         var info = new Gdi32.BITMAPINFO { bmiHeader = new Gdi32.BITMAPINFOHEADER { biSize = (uint)Marshal.SizeOf<Gdi32.BITMAPINFOHEADER>(), biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 } };
         var pixels = new byte[w * h * 4];
         Gdi32.GetDIBits(memDc, bmp, 0, (uint)h, pixels, ref info, Gdi32.DIBColorMode.DIB_RGB_COLORS);
         return pixels;
      }
      finally
      {
         Gdi32.SelectObject(memDc, previous); // deselect the bitmap so it can be deleted when the SafeHandle disposes
      }
   }

   ///<summary>Force <paramref name="hwnd"/> topmost and foreground, so the capture sees it unobscured.</summary>
   public static void ForceToTop(HWND hwnd)
   {
      var foregroundThread = User32.GetWindowThreadProcessId(User32.GetForegroundWindow(), out _);
      var thisThread = Kernel32.GetCurrentThreadId();
      User32.AttachThreadInput(thisThread, foregroundThread, true);
      User32.SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOACTIVATE);
      User32.BringWindowToTop(hwnd);
      User32.SetForegroundWindow(hwnd);
      User32.AttachThreadInput(thisThread, foregroundThread, false);
   }
}
