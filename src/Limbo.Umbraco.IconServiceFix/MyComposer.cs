using System;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Limbo.Umbraco.IconServiceFix; 

public class MyComposer : IComposer {

    public void Compose(IUmbracoBuilder builder) {

        Version? version = typeof(IUmbracoBuilder).Assembly.GetName().Version;
        if (version == null) return;

        if (version.Major != 10) return;
        if (version.Minor > 2) return;

        builder.Services.AddUnique<IIconService, MyIconService>();

    }

}