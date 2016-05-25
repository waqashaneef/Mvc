// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="FileResult" /> that on execution writes the file specified using a virtual path to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class VirtualFileResult : FileResult
    {
        private VirtualFileResultExecutor _executor;
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with the provided <paramref name="fileName"/>
        /// and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
        }

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, MediaTypeHeaderValue contentType)
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

        /// <summary>
        /// Gets or sets the <see cref="IFileProvider"/> used to resolve paths.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _executor = context.HttpContext.RequestServices.GetService<VirtualFileResultExecutor>();
            return _executor.ExecuteResultAsync(this, context);
        }

        /// <inheritdoc />
        public override Task WriteFileAsync(HttpResponse response)
        {
            return _executor.DefaultWriteFileAsync(response);
        }

        /// <summary>
        /// Returns <see cref="Stream"/> for the specified <paramref name="fileInfo"/>.
        /// </summary>
        /// <param name="fileInfo">The <see cref="IFileInfo"/> for which the stream is needed.</param>
        /// <returns><see cref="Stream"/> for the specified <paramref name="fileInfo"/>.</returns>
        public virtual Stream GetFileStream(IFileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            return _executor.DefaultGetFileStream(fileInfo);
        }
    }
}
