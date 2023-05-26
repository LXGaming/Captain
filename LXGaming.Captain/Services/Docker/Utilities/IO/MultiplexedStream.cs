namespace LXGaming.Captain.Services.Docker.Utilities.IO;

public class MultiplexedStream : Stream {

    private readonly Stream _stream;
    private readonly byte[] _header;
    private readonly bool _multiplexed;
    private int _type;
    private int _remaining;
    private bool _disposed;

    public MultiplexedStream(Stream stream, bool multiplexed) {
        _stream = stream;
        _header = multiplexed ? new byte[8] : Array.Empty<byte>();
        _multiplexed = multiplexed;
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override void Flush() {
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (!_multiplexed) {
            return _stream.Read(buffer, offset, count);
        }

        while (_remaining == 0) {
            (_type, _remaining) = ReadHeader();
            if (_type == -1) {
                return 0;
            }
        }

        var read = _stream.Read(buffer, offset, Math.Min(count, _remaining));
        if (read == 0) {
            throw new EndOfStreamException();
        }

        _remaining -= read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value) {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count) {
        _stream.Write(buffer, offset, count);
    }

    private (int type, int length) ReadHeader() {
        var index = 0;
        while (index < _header.Length) {
            var read = _stream.Read(_header, index, _header.Length - index);
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
            if (disposing) {
                _stream.Dispose();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}