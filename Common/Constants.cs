﻿namespace NetAddressWinUI.Common
{
    public static partial class Constants
    {
        public static readonly string RootDirectoryPath =
            Path.Combine(PathHelper.GetAppDataFolderPath(), ProcessInfoHelper.ProductName);

        public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
        public static readonly string ThemeConfigPath = Path.Combine(RootDirectoryPath, "ThemeConfig.json");
    }
}