namespace LXGaming.Captain.Services.Docker.Utilities.IO;

public class MultiplexedStream(Stream stream, bool multiplexed) : Stream {

    private readonly byte[] _header = multiplexed ? new byte[8] : [];
    private int _type;
    private int _remaining;
    private bool _disposed;

    public override bool CanRead => stream.CanRead;

    public override bool CanSeek => stream.CanSeek;

    public override bool CanWrite => stream.CanWrite;

    public override long Length => stream.Length;

    public override long Position {
        get => stream.Position;
        set => stream.Position = value;
    }

    public override void Flush() {
        stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (!multiplexed) {
            return stream.Read(buffer, offset, count);
        }

        while (_remaining == 0) {
            (_type, _remaining) = ReadHeader();
            if (_type == -1) {
                return 0;
            }
        }

        var read = stream.Read(buffer, offset, Math.Min(count, _remaining));
        if (read == 0) {
            throw new EndOfStreamException();
        }

        _remaining -= read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        return stream.Seek(offset, origin);
    }

    public override void SetLength(long value) {
        stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count) {
        stream.Write(buffer, offset, count);
    }

    private (int type, int length) ReadHeader() {
        var index = 0;
        while (index < _header.Length) {
            var read = stream.Read(_header, index, _header.Length - index);
            if (read == 0) {
                if (index == 0) {
                    return (-1, 0);
                }

                throw new EndOfStreamException();
            }

            index += read;
        }

        var type = _header[0];
        var length = (_header[4] << 24) | (_header[5] << 16) | (_header[6] << 8) | _header[7];
        return (type, length);
    }

    protected override void Dispose(bool disposing) {
        if (!_disposed) {
            _disposed = true;

            if (disposing) {
                stream.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}