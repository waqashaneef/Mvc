// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileContentResultExecutor : FileResultExecutor
    {
        private readonly ILogger _logger;

        public FileContentResultExecutor(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FileContentResultExecutor>();
        }

        public Task ExecuteAsync(ActionContext context, FileContentResult result)
        {
            SetHeaders(context, result);
            _logger.FileResultExecuting(result.FileDownloadName);

            return WriteFileAsync(context, result);
        }

        private Task WriteFileAsync(ActionContext context, FileContentResult result)
        {
            var response = context.HttpContext.Response;

            var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
            bufferingFeature?.DisableResponseBuffering();

            return response.Body.WriteAsync(result.FileContents, offset: 0, count: result.FileContents.Length);
        }
    }
}
