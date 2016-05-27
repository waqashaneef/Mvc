// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileStreamResultExecutor : FileResultExecutor
    {
        // default buffer size as defined in BufferedStream type
        private const int BufferSize = 0x1000;
        private readonly ILogger _logger;

        public FileStreamResultExecutor(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FileStreamResultExecutor>();
        }

        public Task ExecuteAsync(ActionContext context, FileStreamResult result)
        {
            SetHeaders(context, result);
            _logger.FileResultExecuting(result.FileDownloadName);

            return WriteFileAsync(context, result);
        }

        private async Task WriteFileAsync(ActionContext context, FileStreamResult result)
        {
            var response = context.HttpContext.Response;
            var outputStream = response.Body;

            using (result.FileStream)
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();

                await result.FileStream.CopyToAsync(outputStream, BufferSize);
            }
        }
    }
}
