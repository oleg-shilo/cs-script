using System;
using System.Globalization;
using System.Threading;
using CSScripting;

namespace csscript
{
    interface ISystemWideLock : IDisposable
    {
        bool Wait(int millisecondsTimeout);

        void Release();
    }

    class WinSystemWideLock : ISystemWideLock, IDisposable
    {
        Mutex mutex;
        bool signaled = false;

        public WinSystemWideLock(string name)
        {
            signaled = false;
            mutex = new Mutex(signaled, name);
        }

        public bool Wait(int millisecondsTimeout)
        {
            // even if locked Mutex will handle it. It will return immediately
            return signaled = mutex.WaitOne(millisecondsTimeout, false);
        }

        public void Release()
        {
            // `try catch` would do the job but it's better to avoid exceptions
            // See https://github.com/oleg-shilo/cs-script/issues/327#issuecomment-1479273740
            try { if (signaled) mutex.ReleaseMutex(); signaled = false; }
            catch { }
        }

        public void Dispose()
        {
            Release();
            try { mutex.Close(); }
            catch { }
        }
    }

    class LinuxSystemWideLock : WinSystemWideLock
    {
        public LinuxSystemWideLock(string context) : base(context)
        {
        }
    }

    class SystemWideLock : ISystemWideLock, IDisposable
    {
        ISystemWideLock mutex;

        bool locked = false;

        public SystemWideLock(string file, string context)
        {
            bool isLinux = (Environment.OSVersion.Platform == PlatformID.Unix);
            if (isLinux)
            {
                mutex = new LinuxSystemWideLock(context + "." + file.GetHashCodeEx());
            }
            else
            {
                file = file.ToLower(CultureInfo.InvariantCulture);
                mutex = new WinSystemWideLock(context + "." + file.GetHashCodeEx());
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