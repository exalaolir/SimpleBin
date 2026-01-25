using System.Runtime.InteropServices;
using System.Security.Principal;
using Timer = System.Threading.Timer;

namespace SimpleBin
{
    static partial class NativeBinMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Shqueryrbinfo
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHQueryRecycleBinW(
             string? pszRootPath,
             ref Shqueryrbinfo pShQueryRbInfo);

        [Flags]
        internal enum RecycleBinFlags : uint
        {
            None = 0,
            SherbNoconfirmation = 0x00000001,
            SherbNoprogressui = 0x00000002,
            SherbNosound = 0x00000004
        }

        [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHEmptyRecycleBinW(
             IntPtr hwnd,
             string? pszRootPath,
             RecycleBinFlags dwFlags);
    }

    public class BinHelper : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = [];
        public delegate void BinUpdateHandler(object sender, FileSystemEventArgs e);
        public event BinUpdateHandler? Update;
        private Timer? _debounceTimer;
        private readonly Lock _lock = new();
        private bool _isDisposed;

        public BinHelper()
        {
            string? sid = (WindowsIdentity.GetCurrent().User?.Value)
                ?? throw new InvalidOperationException("No such user");
            string binPath = Path.Combine("$Recycle.Bin", sid);

            var bins = DriveInfo.GetDrives().Where(d => d is
            {
                IsReady: true,
                DriveType: DriveType.Fixed or DriveType.Network
            }).Select(d => Path.Combine(d.RootDirectory.FullName, binPath));

            foreach (var bin in bins)
            {
                if (!Directory.Exists(bin)) continue;

                var watcher = new FileSystemWatcher(bin)
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                    InternalBufferSize = 65536,
                    Filter = "$I*",
                    NotifyFilter = NotifyFilters.FileName
                };

                watcher.Created += OnBinChanged;
                watcher.Deleted += OnBinChanged;
                _watchers.Add(watcher);
            }
        }

        internal static (long biteSize, long itemCount) GetBinSize()
        {
            const string? pszRootPath = null; //need to watch size from all disks
            var info = new NativeBinMethods.Shqueryrbinfo();
            info.cbSize = Marshal.SizeOf(info);
            const int okCode = 0;

            var result = NativeBinMethods.SHQueryRecycleBinW(pszRootPath, ref info);

            if (result != okCode) throw new Exception("SHQueryRecycleBinW failed");

            return (info.i64Size, info.i64NumItems);
        }

        internal static bool ClearBin()
        {
            const int okCode = 0;
            var parentWindow = IntPtr.Zero;
            const string? pszRootPath = null; //need to clear data from all disks

            const NativeBinMethods.RecycleBinFlags flags = NativeBinMethods.RecycleBinFlags.SherbNoconfirmation |
                                    NativeBinMethods.RecycleBinFlags.SherbNosound;

            int resultCode = NativeBinMethods.SHEmptyRecycleBinW(parentWindow, pszRootPath, flags);

            if (resultCode != okCode) throw new Exception("SHEmptyRecycleBinW failed");

            return true;
        }

        private void OnBinChanged(object? sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new(DebounceCallback, e, 500, Timeout.Infinite);
            }
        }

        private void DebounceCallback(object? state)
        {
            lock (_lock)
            {
                Update?.Invoke(this, (FileSystemEventArgs)state!);
                _debounceTimer!.Dispose();
                _debounceTimer = null;
            }
        }

        public static bool IsBinEmpty() => GetBinSize().itemCount == 0;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_lock)
                {
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }

                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}