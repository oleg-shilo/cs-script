//css dir C:\Program Files (x86)\Mono\lib\mono\4.0
//css_ref Mono.Posix.dll;
//css_ref System;
using System;
using Mono.Unix.Native;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Unix;

//http://man7.org/linux/man-pages/man2/fcntl.2.html
//https://gist.github.com/mrvaldes/6196035

namespace Mono.Unix
{
    class FileMutex
    {
        int handle;
        Flock wl;

        public FileMutex(string name, bool initiallyOwned = false)
        {
            handle = Syscall.open(name, OpenFlags.O_CREAT | OpenFlags.O_RDWR, FilePermissions.DEFFILEMODE);

            wl.l_len = 0;
            wl.l_pid = Syscall.getpid();
            wl.l_start = 0;
            wl.l_type = LockType.F_UNLCK;
            wl.l_whence = SeekFlags.SEEK_SET;

            if (initiallyOwned)
                Wait();
        }

        public bool Wait(int millisecondsTimeout = -1)
        {
            lock (typeof(FileMutex))
            {
                if (handle > 0)
                {
                    bool result = false;

                    ThreadStart placeLock = () =>
                    {
                        // a write (exclusive) lock
                        wl.l_type = LockType.F_WRLCK;
                        int res = Syscall.fcntl(handle, FcntlCommand.F_SETLKW, ref wl);

                        if (res == 0 && Syscall.GetLastError() != Errno.EAGAIN)
                            result = true;
                    };

                    if (millisecondsTimeout == -1)
                    {
                        // Console.WriteLine("waiting in the calling thread");
                        placeLock();
                    }
                    else
                    {
                        // Console.WriteLine("waiting in the separate thread");
                        var t = new Thread(placeLock) { IsBackground = true };
                        t.Start();
                        if (!t.Join(millisecondsTimeout))
                        {
                            //timeout
                            t.Abort();
                        }
                    }

                    return result;
                }
                return false;
            }
        }

        public void Release()
        {
            lock (typeof(FileMutex))
            {
                if (handle > 0)
                {
                    // a write (exclusive) unlock
                    wl.l_type = LockType.F_UNLCK;
                    Syscall.fcntl(handle, FcntlCommand.F_SETLKW, ref wl);
                }
            }
        }
    }
}

namespace ConsoleApplication14
{
    class Program
    {
        static void __Main(string[] args)
        {
            string file = @"/home/user/Desktop/krok/lock2";

            var mutex = new FileMutex(file);
            Console.WriteLine("Trying to obtain exclusive lock...");

            mutex.Wait(1000 * 5);
            Console.WriteLine("exclusive lock is obtained...");

            Console.WriteLine("Press 'Enter' to release lock.");
            Console.ReadLine();

            mutex.Release();
            Console.WriteLine("Lock is released.");

            Console.ReadLine();
            Console.WriteLine("Press 'Enter' to exit.");
        }
    }
}