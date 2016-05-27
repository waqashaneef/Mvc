// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class LocalRedirectResultExecutor
    {
        private readonly ILogger _logger;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public LocalRedirectResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (urlHelperFactory == null)
            {
                throw new ArgumentNullException(nameof(urlHelperFactory));
            }

            _logger = loggerFactory.CreateLogger<LocalRedirectResultExecutor>();
            _urlHelperFactory = urlHelperFactory;
        }

        public void Execute(ActionContext context, LocalRedirectResult result)
        {
            var urlHelper = GetUrlHelper(context, result);

            // IsLocalUrl is called to handle  Urls starting with '~/'.
            var destinationUrl = result.Url;
            if (!urlHelper.IsLocalUrl(result.Url))
            {
                throw new InvalidOperationException(Resources.UrlNotLocal);
            }

            destinationUrl = urlHelper.Content(result.Url);
            _logger.LocalRedirectResultExecuting(destinationUrl);
            context.HttpContext.Response.Redirect(destinationUrl, result.Permanent);
        }

        private IUrlHelper GetUrlHelper(ActionContext context, LocalRedirectResult result)
        {
            var urlHelper = result.UrlHelper;
            if (urlHelper == null)
            {
                urlHelper = _urlHelperFactory.GetUrlHelper(context);
            }

            return urlHelper;
        }
    }
}
