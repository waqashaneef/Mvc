using System;
using System.Collections.Generic;
using System.Linq;
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

        private ActionContext _context;
        private ILogger _logger;
        private FileStreamResult _result;

        public FileStreamResultExecutor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileStreamResult>();
        }

        public Task ExecuteResultAsync(FileStreamResult result, ActionContext context)
        {
            _result = result;
            _context = context;
            SetHeaders(result, context);
            _logger.FileResultExecuting(result.FileDownloadName);
            return result.WriteFileAsync(context.HttpContext.Response);
        }

        internal async Task DefaultWriteFileAsync(HttpResponse response)
        {
            var outputStream = response.Body;

            using (_result.FileStream)
            {
                var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
                bufferingFeature?.DisableResponseBuffering();

                await _result.FileStream.CopyToAsync(outputStream, BufferSize);
            }
        }
    }
}
