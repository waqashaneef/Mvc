using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class VirtualFileResultExecutor : FileResultExecutor
    {
        private const int DefaultBufferSize = 0x1000;

        private ActionContext _context;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<VirtualFileResultExecutor> _logger;
        private VirtualFileResult _result;

        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
        {
            _logger = loggerFactory.CreateLogger<VirtualFileResultExecutor>();
            _hostingEnvironment = hostingEnvironment;            
        }

        public Task ExecuteResultAsync(VirtualFileResult result, ActionContext context)
        {
            _result = result;
            _context = context;
            SetHeaders(result, context);
            _logger.FileResultExecuting(result.FileDownloadName);
            return result.WriteFileAsync(context.HttpContext.Response);
        }

        internal async Task DefaultWriteFileAsync(HttpResponse response)
        {
            var fileProvider = GetFileProvider(response.HttpContext.RequestServices);

            var normalizedPath = _result.FileName;
            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(1);
            }

            var fileInfo = fileProvider.GetFileInfo(normalizedPath);
            if (fileInfo.Exists)
            {
                var physicalPath = fileInfo.PhysicalPath;
                var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
                if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
                {
                    await sendFile.SendFileAsync(
                        physicalPath,
                        offset: 0,
                        count: null,
                        cancellation: default(CancellationToken));
                }
                else
                {
                    var fileStream = _result.GetFileStream(fileInfo);
                    using (fileStream)
                    {
                        await fileStream.CopyToAsync(response.Body, DefaultBufferSize);
                    }
                }
            }
            else
            {
                throw new FileNotFoundException(
                    Resources.FormatFileResult_InvalidPath(_result.FileName), _result.FileName);
            }
        }

        private IFileProvider GetFileProvider(IServiceProvider requestServices)
        {
            if (_result.FileProvider != null)
            {
                return _result.FileProvider;
            }

            _result.FileProvider = _hostingEnvironment.WebRootFileProvider;

            return _result.FileProvider;
        }

        internal Stream DefaultGetFileStream(IFileInfo fileInfo)
        {
            return fileInfo.CreateReadStream();
        }
    }
}
