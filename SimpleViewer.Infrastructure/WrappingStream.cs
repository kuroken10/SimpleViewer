using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleViewer.Infrastructure
{
    /// <summary>
    /// ストリームのWrapperクラス
    /// </summary>
    /// <remarks>
    /// Dispose 時に、内部ストリームの参照を外します
    /// https://pierre3.hatenablog.com/entry/2015/10/25/001207
    /// </remarks>
    internal class WrappingStream : Stream
    {
        Stream m_streamBase;

        public WrappingStream(Stream streamBase)
        {
            if (streamBase == null)
            {
                throw new ArgumentNullException("streamBase");
            }
            m_streamBase = streamBase; //渡したStreamを内部ストリームとして保持
        }

        public override bool CanRead => m_streamBase.CanRead;

        public override bool CanSeek => m_streamBase.CanSeek;

        public override bool CanWrite => m_streamBase.CanWrite;

        public override long Length => m_streamBase.Length;

        public override long Position { get => m_streamBase.Position; set => m_streamBase.Position = value; }

        public override void Flush()
        {
            ThrowIfDisposed();
            m_streamBase.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return m_streamBase.Read(buffer, offset, count);
        }

        //Streamクラスのメソッドをオーバーライドして、内部ストリームの同じメソッドをそのまま呼ぶだけ
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public new Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return m_streamBase.ReadAsync(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            return m_streamBase.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            m_streamBase.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            m_streamBase.Write(buffer, offset, count);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_streamBase.Dispose();
                m_streamBase = null;  //disposeしたら内部ストリームをnullにして参照を外す
            }
            base.Dispose(disposing);
        }
        private void ThrowIfDisposed()
        {
            if (m_streamBase == null)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
