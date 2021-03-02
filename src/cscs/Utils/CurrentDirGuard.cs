using System;

#if !class_lib

namespace csscript
#else

namespace CSScriptLib
#endif
{
    internal class CurrentDirGuard : IDisposable
    {
        string currentDir = Environment.CurrentDirectory;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
                Environment.CurrentDirectory = currentDir;

            disposed = true;
        }

        ~CurrentDirGuard() => Dispose(false);

        bool disposed = false;
    }
}