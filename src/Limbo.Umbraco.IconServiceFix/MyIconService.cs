using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.DependencyInjection;
using Umbraco.Extensions;
using File = System.IO.File;
using IHostingEnvironment = Umbraco.Cms.Core.Hosting.IHostingEnvironment;
#pragma warning disable CS0618


public class MyIconService : IIconService {

    private readonly IAppPolicyCache _cache;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private GlobalSettings _globalSettings;

    [Obsolete("Use other ctor - Will be removed in Umbraco 12")]
    public MyIconService(
        IOptionsMonitor<GlobalSettings> globalSettings,
        IHostingEnvironment hostingEnvironment,
        AppCaches appCaches)
        : this(
            globalSettings,
            hostingEnvironment,
            appCaches,
            StaticServiceProvider.Instance.GetRequiredService<IWebHostEnvironment>()) {
    }

    [Obsolete("Use other ctor - Will be removed in Umbraco 12")]
    public MyIconService(
        IOptionsMonitor<GlobalSettings> globalSettings,
        IHostingEnvironment hostingEnvironment,
        AppCaches appCaches,
        IWebHostEnvironment webHostEnvironment) {
        _globalSettings = globalSettings.CurrentValue;
        _hostingEnvironment = hostingEnvironment;
        _webHostEnvironment = webHostEnvironment;
        _cache = appCaches.RuntimeCache;

        globalSettings.OnChange(x => _globalSettings = x);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? GetIcons() => GetIconDictionary();

    /// <inheritdoc />
    public IconModel? GetIcon(string iconName) {
        if (iconName.IsNullOrWhiteSpace()) {
            return null;
        }

        IReadOnlyDictionary<string, string>? allIconModels = GetIconDictionary();
        if (allIconModels?.ContainsKey(iconName) ?? false) {
            return new IconModel { Name = iconName, SvgString = allIconModels[iconName] };
        }

        return null;
    }

    /// <summary>
    ///     Gets an IconModel using values from a FileInfo model
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    private IconModel? GetIcon(FileInfo fileInfo) =>
        fileInfo == null || string.IsNullOrWhiteSpace(fileInfo.Name)
            ? null
            : CreateIconModel(fileInfo.Name.StripFileExtension(), fileInfo.FullName);

    /// <summary>
    ///     Gets an IconModel containing the icon name and SvgString
    /// </summary>
    /// <param name="iconName"></param>
    /// <param name="iconPath"></param>
    /// <returns></returns>
    private IconModel? CreateIconModel(string iconName, string iconPath) {
        try {
            var svgContent = File.ReadAllText(iconPath);

            var svg = new IconModel { Name = iconName, SvgString = svgContent };

            return svg;
        } catch {
            return null;
        }
    }

    private IEnumerable<FileInfo> GetAllIconsFiles(IFileProvider provider, string path) {

        // Iterate through all plugin folders
        foreach (var pluginDirectory in provider.GetDirectoryContents(path)) {

            // Ideally there shouldn't be any files, but we'd better check to be sure
            if (!pluginDirectory.IsDirectory) continue;

            // Iterate through the sub directories of each plugin folder
            foreach (var subDir1 in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}")) {

                // Skip files again
                if (!subDir1.IsDirectory) continue;

                // Iterate through second level sub directories
                foreach (var subDir2 in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}/{subDir1.Name}")) {

                    // Skip files again
                    if (!subDir2.IsDirectory) continue;

                    // Does the directory match the plugin icons folder? (case insensitive for legacy support)
                    if (!($"/{subDir1.Name}/{subDir2.Name}").Equals(Constants.SystemDirectories.PluginIcons, StringComparison.InvariantCultureIgnoreCase)) {
                        continue;
                    }

                    // Iterate though the files of the second level sub directory. This should be where the SVG files are located :D
                    foreach (var file in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}/{subDir1.Name}/{subDir2.Name}")) {
                        if (file.Name.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase)) {
                            yield return new FileInfo(file.PhysicalPath);
                        }
                    }

                }
            }

        }

    }

    private IEnumerable<FileInfo> GetAllIconsFiles2(IFileProvider provider, string path) {
        return from pluginDirectory
            in provider.GetDirectoryContents(path)
               where pluginDirectory.IsDirectory
               from subDir1 in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}")
               where subDir1.IsDirectory
               from subDir2 in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}/{subDir1.Name}")
               where subDir2.IsDirectory
               where ($"/{subDir1.Name}/{subDir2.Name}").Equals(Constants.SystemDirectories.PluginIcons, StringComparison.InvariantCultureIgnoreCase)
               from file in provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}/{subDir1.Name}/{subDir2.Name}")
               where file.Name.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase)
               select new FileInfo(file.PhysicalPath);
    }

    private IEnumerable<FileInfo> GetAllIconsFiles3(IFileProvider provider, string path) {
        return provider.GetDirectoryContents(path)
            .Where(pluginDirectory => pluginDirectory.IsDirectory)
            .SelectMany(pluginDirectory => provider.GetDirectoryContents($"{path}/{pluginDirectory.Name}"), (pluginDirectory, subDir1) => new { pluginDirectory, subDir1 })
            .Where(x => x.subDir1.IsDirectory)
            .SelectMany(x => provider.GetDirectoryContents($"{path}/{x.pluginDirectory.Name}/{x.subDir1.Name}"), (x, subDir2) => new { x, subDir2 })
            .Where(x => x.subDir2.IsDirectory)
            .Where(x => ($"/{x.x.subDir1.Name}/{x.subDir2.Name}").Equals(Constants.SystemDirectories.PluginIcons, StringComparison.InvariantCultureIgnoreCase))
            .SelectMany(x => provider.GetDirectoryContents($"{path}/{x.x.pluginDirectory.Name}/{x.x.subDir1.Name}/{x.subDir2.Name}"), (x, file) => new { x, file })
            .Where(x => x.file.Name.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
            .Select(x => new FileInfo(x.file.PhysicalPath));
    }

    public IEnumerable<FileInfo> GetAllIconsFiles() {
        var icons = new HashSet<FileInfo>(new CaseInsensitiveFileInfoComparer());

        // add icons from plugins
        var appPluginsDirectoryPath = _hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.AppPlugins);
        if (Directory.Exists(appPluginsDirectoryPath)) {
            var appPlugins = new DirectoryInfo(appPluginsDirectoryPath);

            // iterate sub directories of app plugins
            foreach (DirectoryInfo dir in appPlugins.EnumerateDirectories()) {
                // AppPluginIcons path was previoulsy the wrong case, so we first check for the prefered directory
                // and then check the legacy directory.
                var iconPath = _hostingEnvironment.MapPathContentRoot(
                    $"{Constants.SystemDirectories.AppPlugins}/{dir.Name}{Constants.SystemDirectories.PluginIcons}");
                var iconPathExists = Directory.Exists(iconPath);

                if (!iconPathExists) {
                    iconPath = _hostingEnvironment.MapPathContentRoot(
                        $"{Constants.SystemDirectories.AppPlugins}/{dir.Name}{Constants.SystemDirectories.AppPluginIcons}");
                    iconPathExists = Directory.Exists(iconPath);
                }

                if (iconPathExists) {
                    IEnumerable<FileInfo> dirIcons =
                        new DirectoryInfo(iconPath).EnumerateFiles("*.svg", SearchOption.TopDirectoryOnly);
                    icons.UnionWith(dirIcons);
                }
            }
        }

        icons.UnionWith(GetAllIconsFiles(_webHostEnvironment.WebRootFileProvider, Constants.SystemDirectories.AppPlugins));

        IDirectoryContents? iconFolder =
            _webHostEnvironment.WebRootFileProvider.GetDirectoryContents(_globalSettings.IconsPath);

        IEnumerable<FileInfo> coreIcons = iconFolder
            .Where(x => !x.IsDirectory && x.Name.EndsWith(".svg"))
            .Select(x => new FileInfo(x.PhysicalPath));

        icons.UnionWith(coreIcons);

        return icons;
    }

    private IReadOnlyDictionary<string, string>? GetIconDictionary() => _cache.GetCacheItem(
        $"{typeof(MyIconService).FullName}.{nameof(GetIconDictionary)}",
        () => GetAllIconsFiles()
            .Select(GetIcon)
            .WhereNotNull()
            .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SvgString, StringComparer.OrdinalIgnoreCase));

    private class CaseInsensitiveFileInfoComparer : IEqualityComparer<FileInfo> {
        public bool Equals(FileInfo? one, FileInfo? two) =>
            StringComparer.InvariantCultureIgnoreCase.Equals(one?.Name, two?.Name);

        public int GetHashCode(FileInfo item) => StringComparer.InvariantCultureIgnoreCase.GetHashCode(item.Name);
    }
}