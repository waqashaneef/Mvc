using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class PhysicalFileResultExecutor : FileResultExecutor
    {
        private const int DefaultBufferSize = 0x1000;
        private readonly ILogger<PhysicalFileResult> _logger;

        private ActionContext _context;
        private PhysicalFileResult _result;

        public PhysicalFileResultExecutor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PhysicalFileResult>();
        }
        public Task ExecuteAsync(PhysicalFileResult result, ActionContext context)
        {
            _result = result;
            _context = context;
            SetHeaders(result, context);
            _logger.FileResultExecuting(result.FileDownloadName);
            return result.WriteFileAsync(context.HttpContext.Response);
        }

        internal async Task DefaultWriteFileAsync()
        {
            var response = _context.HttpContext.Response;

            if (!Path.IsPathRooted(_result.FileName))
            {
                throw new NotSupportedException(Resources.FormatFileResult_PathNotRooted(_result.FileName));
            }

            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null)
            {
                await sendFile.SendFileAsync(
                    _result.FileName,
                    offset: 0,
                    count: null,
                    cancellation: default(CancellationToken));
            }
            else
            {
                var fileStream = _result.GetFileStream(_result.FileName);

                using (fileStream)
                {
                    await fileStream.CopyToAsync(response.Body, DefaultBufferSize);
                }
            }
        }

        internal Stream DefaultGetFileStream(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    DefaultBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}
