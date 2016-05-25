// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="FileResult"/> on execution will write a file from disk to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class PhysicalFileResult : FileResult
    {
        private PhysicalFileResultExecutor _executor;
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
        }

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType?.ToString())
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fileName = value;
            }
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _executor = context.HttpContext.RequestServices.GetRequiredService<PhysicalFileResultExecutor>();
            return _executor.ExecuteAsync(this, context);
        }

        /// <inheritdoc />
        public override Task WriteFileAsync(HttpResponse response)
        {
            return _executor.DefaultWriteFileAsync();
        }

        /// <summary>
        /// Returns <see cref="Stream"/> for the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path for which the <see cref="FileStream"/> is needed.</param>
        /// <returns><see cref="FileStream"/> for the specified <paramref name="path"/>.</returns>
        public virtual Stream GetFileStream(string path)
        {
            return _executor.DefaultGetFileStream(path);
        }
    }
}
