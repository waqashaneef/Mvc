﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Writes to the <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponseStreamWriter : TextWriter
    {
        private const int DefaultBufferSize = 1024;
        private readonly Stream _stream;
        private Encoder _encoder;
        private byte[] _flushBuffer;
        private byte[] _byteBuffer;
        private int _byteBufferSize;
        private int _byteBufferCount;
        private char[] _charBuffer;
        private int _charBufferSize;
        private int _charBufferCount;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize)
        {
        }

        public HttpResponseStreamWriter([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize)
        {
            _stream = stream;
            Encoding = encoding;
            _encoder = encoding.GetEncoder();
            _byteBufferSize = bufferSize;
            _byteBuffer = new byte[bufferSize];
            _charBufferSize = bufferSize;
            _charBuffer = new char[bufferSize];
            _flushBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
        }

        public override Encoding Encoding { get; }

        public override void Write(object value)
        {
            var bytes = value as byte[];
            if (bytes != null)
            {
                if (_charBufferCount > 0)
                {
                    FlushCharBuffer();
                }

                if (_byteBufferCount == _byteBufferSize)
                {
                    FlushInternal();
                }

                var count = bytes.Length;
                var index = 0;
                while (count > 0)
                {
                    if (_byteBufferCount == _byteBufferSize)
                    {
                        FlushInternal();
                    }

                    CopyToByteBuffer(bytes, ref index, ref count);
                }
            }
            else
            {
                base.Write(value);
            }
        }

        public override void Write(char value)
        {
            if (_byteBufferCount > 0)
            {
                FlushByteBuffer();
            }

            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal();
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override void Write(char[] values, int index, int count)
        {
            if (values == null)
            {
                return;
            }

            if (_byteBufferCount > 0)
            {
                FlushByteBuffer();
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal();
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            if (_byteBufferCount > 0)
            {
                FlushByteBuffer();
            }

            var count = value.Length;
            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal();
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        public override async Task WriteAsync(char value)
        {
            if (_charBufferCount == _charBufferSize)
            {
                await FlushInternalAsync();
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override async Task WriteAsync(char[] values, int index, int count)
        {
            if (values == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync();
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override async Task WriteAsync(string value)
        {
            if (value == null)
            {
                return;
            }

            var count = value.Length;
            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync();
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        // We want to flush the stream when Flush/FlushAsync is explicitly
        // called by the user (example: from a Razor view).

        public override void Flush()
        {
            FlushInternal(true, true);
        }

        public override async Task FlushAsync()
        {
            await FlushInternalAsync(true, true);
        }

        // Do not flush the stream on Dispose, as this will cause response to be
        // sent in chunked encoding in case of Helios.
        protected override void Dispose(bool disposing)
        {
            FlushInternal(flushStream: false, flushEncoder: true);
        }

        private void FlushInternal(bool flushStream = false, bool flushEncoder = false)
        {
            if (_charBufferCount != 0)
            {
                var count = _encoder.GetBytes(_charBuffer, 0, _charBufferCount, _flushBuffer, 0, flushEncoder);
                if (count > 0)
                {
                    _stream.Write(_flushBuffer, 0, count);
                }

                _charBufferCount = 0;
            }

            if (_byteBufferCount != 0)
            {
                _stream.Write(_byteBuffer, 0, _byteBufferCount);

                _byteBufferCount = 0;
            }

            if (flushStream)
            {
                _stream.Flush();
            }
        }

        private void FlushCharBuffer()
        {
            if (_charBufferCount != 0)
            {
                var count = _encoder.GetBytes(_charBuffer, 0, _charBufferCount, _flushBuffer, 0, true);
                if (count > 0)
                {
                    _stream.Write(_flushBuffer, 0, count);
                }

                _charBufferCount = 0;
            }
        }

        private void FlushByteBuffer()
        {
            if (_byteBufferCount != 0)
            {
                _stream.Write(_byteBuffer, 0, _byteBufferCount);

                _byteBufferCount = 0;
            }
        }

        private async Task FlushInternalAsync(bool flushStream = false, bool flushEncoder = false)
        {
            if (_charBufferCount != 0)
            {
                var count = _encoder.GetBytes(_charBuffer, 0, _charBufferCount, _flushBuffer, 0, flushEncoder);
                if (count > 0)
                {
                    await _stream.WriteAsync(_flushBuffer, 0, count);
                }

                _charBufferCount = 0;
            }

            if (_byteBufferCount != 0)
            {
                await _stream.WriteAsync(_byteBuffer, 0, _byteBufferCount);

                _byteBufferCount = 0;
            }

            if (flushStream)
            {
                await _stream.FlushAsync();
            }
        }

        private void CopyToCharBuffer(string value, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            value.CopyTo(
                sourceIndex: index,
                destination: _charBuffer,
                destinationIndex: _charBufferCount,
                count: remaining);

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }

        private void CopyToCharBuffer(char[] values, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            Buffer.BlockCopy(
                src: values,
                srcOffset: index * sizeof(char),
                dst: _charBuffer,
                dstOffset: _charBufferCount * sizeof(char),
                count: remaining * sizeof(char));

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }

        private void CopyToByteBuffer(byte[] bytes, ref int index, ref int count)
        {
            var remaining = Math.Min(_byteBufferSize - _byteBufferCount, count);

            Buffer.BlockCopy(
                src: bytes,
                srcOffset: index,
                dst: _byteBuffer,
                dstOffset: _byteBufferCount,
                count: remaining);

            _byteBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }
    }
}
