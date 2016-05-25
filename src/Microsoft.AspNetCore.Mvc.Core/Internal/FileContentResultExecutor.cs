using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileContentResultExecutor : FileResultExecutor
    {
        private readonly ILogger _logger;
        private ActionContext _context;
        private FileContentResult _result;

        public FileContentResultExecutor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileContentResult>();
        }

        public Task ExecuteResultAsync(FileContentResult result, ActionContext context)
        {
            _result = result;
            _context = context;
            SetHeaders(result, context);
            _logger.FileResultExecuting(result.FileDownloadName);
            return result.WriteFileAsync(context.HttpContext.Response);
        }

        internal Task DefaultWriteFileAsync(HttpResponse response)
        {
            var bufferingFeature = response.HttpContext.Features.Get<IHttpBufferingFeature>();
            bufferingFeature?.DisableResponseBuffering();

            return response.Body.WriteAsync(_result.FileContents, offset: 0, count: _result.FileContents.Length);
        }
    }
}
