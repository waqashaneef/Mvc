using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileResultExecutor
    {
        public void SetHeaders(FileResult result, ActionContext context)
        {
            SetContentType(result, context);
            SetContentDispositionHeader(result, context);
        }

        private void SetContentDispositionHeader(FileResult result, ActionContext context)
        {
            if (!string.IsNullOrEmpty(result.FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.SetHttpFileName(result.FileDownloadName);
                context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            }
        }

        private void SetContentType(FileResult result, ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = result.ContentType.ToString();
        }
    }
}
