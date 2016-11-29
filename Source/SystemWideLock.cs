using System;
using System.Globalization;
using System.Threading;

namespace csscript
{
    internal interface ISystemWideLock : IDisposable
    {
        bool Wait(int millisecondsTimeout);
        void Release();
    }

    internal class WinSystemWideLock : ISystemWideLock, IDisposable
    {
        Mutex mutex;

        public WinSystemWideLock(string name)
        {
            mutex = new Mutex(false, name);
        }

        public bool Wait(int millisecondsTimeout)
        {
            //even if locked Mutex will handle it. It will return immediately
            return mutex.WaitOne(millisecondsTimeout, false);
        }

        public void Release()
        {
            try { mutex.ReleaseMutex(); }
            catch { }
        }

        public void Dispose()
        {
            Release();
            try { mutex.Close(); }
            catch { }
        }
    }

    internal class LinuxSystemWideLock : ISystemWideLock, IDisposable
    {
        Mono.Unix.FileMutex mutex;
        public LinuxSystemWideLock(string file, string context)
        {
            mutex = new Mono.Unix.FileMutex(file, context);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return mutex.Wait(millisecondsTimeout);
        }

        public void Dispose()
        {
            Release();
        }

        public void Release()
        {
            try { mutex.Release(); }
            catch { }
        }
    }

    internal class SystemWideLock : ISystemWideLock, IDisposable
    {
        ISystemWideLock mutex;

        bool locked = false;

        public SystemWideLock(string file, string context)
        {
            bool isLinux = (Environment.OSVersion.Platform == PlatformID.Unix);

            if (isLinux)
            {
                mutex = new LinuxSystemWideLock(file, context);
            }
            else
            {
                file = file.ToLower(CultureInfo.InvariantCulture);
                mutex = new WinSystemWideLock("" + context + "." + CSSUtils.GetHashCodeEx(file));
            }
        }

        public bool Wait(int millisecondsTimeout)
        {
            if (!locked)
                locked = mutex.Wait(millisecondsTimeout);
            return locked;
        }

        public void Release()
        {
            if (locked)
            {
                mutex.Release();
                locked = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
                mutex.Dispose();

            disposed = true;
        }

        ~SystemWideLock()
        {
            Dispose(false);
        }

        bool disposed = false;
    }

}