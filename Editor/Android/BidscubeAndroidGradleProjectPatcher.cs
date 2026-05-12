using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BidscubeSDK.Android;
using UnityEditor;
using UnityEditor.Android;

namespace BidscubeSDK.Editor.Android
{
    /// <summary>Bundled core AAR filenames / native version for Gradle injection (per mediation adapter package).</summary>
    public readonly struct BidscubeAndroidBundledCoreAarNames
    {
        public BidscubeAndroidBundledCoreAarNames(string nativeVersion, string liteFileName, string fullFileName)
            : this(nativeVersion, liteFileName, null, null, fullFileName)
        {
        }

        public BidscubeAndroidBundledCoreAarNames(string nativeVersion, string liteFileName, string webViewVideoFileName,
            string legacyMediaVideoFileName, string fullFileName)
        {
            NativeVersion = nativeVersion;
            LiteFileName = liteFileName;
            WebViewVideoFileName = webViewVideoFileName;
            LegacyMediaVideoFileName = legacyMediaVideoFileName;
            FullFileName = fullFileName;
        }

        public string NativeVersion { get; }
        public string LiteFileName { get; }
        public string WebViewVideoFileName { get; }
        public string LegacyMediaVideoFileName { get; }
        public string FullFileName { get; }
    }

    /// <summary>Shared Android Gradle export logic for AppLovin MAX and LevelPlay adapter packages.</summary>
    public static class BidscubeAndroidGradleProjectPatcher
    {
        public static void OnPostGenerateGradleAndroidProject(string path, string logPrefix, Assembly packageAssembly,
            bool appendAppLovinSdkDependency, BidscubeAndroidBundledCoreAarNames names)
        {
            try
            {
                var featureSet = BidscubeAndroidExportSettingsResolver.GetEffectiveFeatureSet();
                var coreMode = BidscubeAndroidExportSettingsResolver.GetEffectiveCoreDependencyMode();
                var customLines = BidscubeAndroidExportSettingsResolver.GetEffectiveCustomGradleLines();

                UnityEngine.Debug.Log($"{logPrefix} Android feature set: {DescribeFeatureSet(featureSet)}");

                if (!TryGetUnityLibraryGradleInfo(path, out _, out var unityLibraryBuildGradle, out var libsDir))
                {
                    UnityEngine.Debug.LogWarning(
                        $"{logPrefix} Could not locate unityLibrary/build.gradle from Gradle path: " + path +
                        ". Expected either <root>/unityLibrary/build.gradle or <unityLibraryModule>/build.gradle (Unity 6+).");
                    return;
                }

                var pkgRoot = ResolvePackageRoot(packageAssembly);
                if (string.IsNullOrEmpty(pkgRoot))
                {
                    UnityEngine.Debug.LogWarning($"{logPrefix} Could not resolve UPM package root; skipping Gradle/AAR integration.");
                    return;
                }

                var plugins = Path.Combine(pkgRoot, "Runtime", "Plugins", "Android");
                var ver = names.NativeVersion;
                Directory.CreateDirectory(libsDir);
                var liteSrc = GetAarPath(plugins, names.LiteFileName);
                var webViewSrc = GetAarPath(plugins, names.WebViewVideoFileName);
                var legacySrc = GetAarPath(plugins, names.LegacyMediaVideoFileName);
                var fullSrc = GetAarPath(plugins, names.FullFileName);
                var liteDst = GetAarPath(libsDir, names.LiteFileName);
                var webViewDst = GetAarPath(libsDir, names.WebViewVideoFileName);
                var legacyDst = GetAarPath(libsDir, names.LegacyMediaVideoFileName);
                var fullDst = GetAarPath(libsDir, names.FullFileName);

                if (coreMode == BidscubeAndroidCoreDependencyMode.SkipInjectionIntegratorOwnsCore)
                {
                    UnityEngine.Debug.LogWarning($"{logPrefix} CoreDependencyMode SkipInjectionIntegratorOwnsCore — not injecting Bidscube core lines.");
                    RemoveManagedBlock(unityLibraryBuildGradle, logPrefix);
                    return;
                }

                if (coreMode == BidscubeAndroidCoreDependencyMode.CustomGradleLines)
                {
                    if (string.IsNullOrWhiteSpace(customLines))
                    {
                        UnityEngine.Debug.LogError(
                            $"{logPrefix} CoreDependencyMode is CustomGradleLines but customCoreImplementationGradleLines is empty. Set lines on BidscubeAndroidExportSettings or use another coreDependencyMode.");
                        return;
                    }

                    TryCopySelectedAarForReference(featureSet, liteSrc, webViewSrc, legacySrc, fullSrc,
                        liteDst, webViewDst, legacyDst, fullDst, logPrefix);
                    PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, customLines, ver,
                        mavenCoordinate: null, bundledAarFileName: null, appendAppLovinSdkDependency, names, logPrefix);
                    return;
                }

                TryDelete(liteDst);
                TryDelete(webViewDst);
                TryDelete(legacyDst);
                TryDelete(fullDst);

                if (coreMode == BidscubeAndroidCoreDependencyMode.MavenBidscubeSdkAar)
                {
                    PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, "", ver,
                        mavenCoordinate: GetMavenCoordinate(featureSet, ver), bundledAarFileName: null,
                        appendAppLovinSdkDependency, names, logPrefix);
                    return;
                }

                var selectedSrc = GetSelectedSourcePath(featureSet, liteSrc, webViewSrc, legacySrc, fullSrc);
                var selectedDst = GetSelectedDestinationPath(featureSet, liteDst, webViewDst, legacyDst, fullDst);
                var selectedName = GetSelectedBundledFileName(featureSet, names);
                if (string.IsNullOrEmpty(selectedSrc) || string.IsNullOrEmpty(selectedDst) || string.IsNullOrEmpty(selectedName))
                {
                    UnityEngine.Debug.LogError($"{logPrefix} No bundled AAR filename configured for {DescribeFeatureSet(featureSet)}.");
                    RemoveManagedBlock(unityLibraryBuildGradle, logPrefix);
                    return;
                }

                if (!File.Exists(selectedSrc))
                {
                    if (featureSet == BidscubeAndroidFeatureSet.FullWithVideo)
                    {
                        UnityEngine.Debug.LogError(
                            $"{logPrefix} FullWithVideo requires Runtime/Plugins/Android/{selectedName}, or set coreDependencyMode to MavenBidscubeSdkAar with a reachable Maven artifact " +
                            GetMavenCoordinate(featureSet, ver) + ". Switch to LiteNoVideo/WebViewVideoNoDesugar/LegacyMediaVideoNoDesugar if you need a no-desugar player build.");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"{logPrefix} {DescribeFeatureSet(featureSet)}: missing bundled AAR at {selectedSrc}");
                    }
                    RemoveManagedBlock(unityLibraryBuildGradle, logPrefix);
                    return;
                }

                File.Copy(selectedSrc, selectedDst, true);
                UnityEngine.Debug.Log($"{logPrefix} Copied bundled core AAR: {selectedDst}");
                PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, "", ver,
                    mavenCoordinate: null, bundledAarFileName: selectedName, appendAppLovinSdkDependency, names, logPrefix);
            }
            finally
            {
                var fs = BidscubeAndroidExportSettingsResolver.GetEffectiveFeatureSet();
                if (fs == BidscubeAndroidFeatureSet.FullWithVideo)
                    ApplyDesugaringPolicyFull(path, logPrefix);
                else
                    ApplyDesugaringPolicyLite(path, logPrefix);
            }
        }

        static void ApplyDesugaringPolicyLite(string pathFromUnity, string logPrefix)
        {
            if (!TryResolveGradleProjectRoot(pathFromUnity, out var root))
                return;

            var touched = false;
            var launcher = Path.Combine(root, "launcher", "build.gradle");
            if (File.Exists(launcher) && StripCoreLibraryDesugaringFromGradleFileIfNeeded(launcher))
                touched = true;

            var unityLib = Path.Combine(root, "unityLibrary", "build.gradle");
            if (File.Exists(unityLib) && StripCoreLibraryDesugaringFromGradleFileIfNeeded(unityLib))
                touched = true;

            if (touched)
            {
                UnityEngine.Debug.Log(
                    $"{logPrefix} No-desugar feature set: removed coreLibraryDesugaring lines from generated launcher/unityLibrary Gradle files.");
            }
        }

        static void ApplyDesugaringPolicyFull(string pathFromUnity, string logPrefix)
        {
            if (!TryResolveGradleProjectRoot(pathFromUnity, out var root))
                return;

            var launcher = Path.Combine(root, "launcher", "build.gradle");
            if (!File.Exists(launcher))
                return;

            try
            {
                var content = File.ReadAllText(launcher);
                var updated = EnsureCoreLibraryDesugaringInGradleText(content);
                if (updated != content)
                {
                    File.WriteAllText(launcher, updated);
                    UnityEngine.Debug.Log($"{logPrefix} FullWithVideo: ensured core library desugaring in launcher/build.gradle.");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"{logPrefix} Could not patch launcher desugaring: {e.Message}");
            }
        }

        /// <summary>
        /// Replaces the first regex match with a literal string (no <c>$n</c> substitution).
        /// Avoids <see cref="Regex.Replace(string, string, int)"/> overload resolution issues on some Unity / .NET profiles.
        /// </summary>
        static string ReplaceFirstMatchLiteral(Regex regex, string input, string literalReplacement)
        {
            var m = regex.Match(input);
            if (!m.Success)
                return input;
            return input.Substring(0, m.Index) + literalReplacement + input.Substring(m.Index + m.Length);
        }

        /// <summary>Replaces the first match using <see cref="Match.Result(string)"/> substitution rules.</summary>
        static string ReplaceFirstMatchSubstitution(Regex regex, string input, string substitution)
        {
            var m = regex.Match(input);
            if (!m.Success)
                return input;
            return input.Substring(0, m.Index) + m.Result(substitution) + input.Substring(m.Index + m.Length);
        }

        static string EnsureCoreLibraryDesugaringInGradleText(string content)
        {
            const string depLine = "    coreLibraryDesugaring 'com.android.tools:desugar_jdk_libs:2.0.4'";
            if (content.IndexOf("desugar_jdk_libs", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var depRx = new Regex(@"(dependencies\s*\{)\s*", RegexOptions.Multiline);
                content = ReplaceFirstMatchSubstitution(depRx, content, $"$1\n{depLine}\n");
            }

            if (Regex.IsMatch(content, @"coreLibraryDesugaringEnabled\s+true\b"))
                return content;

            if (Regex.IsMatch(content, @"coreLibraryDesugaringEnabled\s+false\b"))
                return Regex.Replace(content, @"(\bcoreLibraryDesugaringEnabled\s+)false\b", "${1}true", RegexOptions.Multiline);

            if (Regex.IsMatch(content, @"compileOptions\s*\{"))
            {
                var coRx = new Regex(@"(compileOptions\s*\{)(\s*)", RegexOptions.Multiline);
                return ReplaceFirstMatchSubstitution(coRx, content, "$1$2        coreLibraryDesugaringEnabled true$2");
            }

            var androidRx = new Regex(@"(android\s*\{)(\s*)", RegexOptions.Multiline);
            return ReplaceFirstMatchSubstitution(androidRx, content,
                "$1$2    compileOptions {\n        coreLibraryDesugaringEnabled true\n    }\n$2");
        }

        static bool TryResolveGradleProjectRoot(string pathFromUnity, out string gradleRoot)
        {
            gradleRoot = null;
            if (string.IsNullOrEmpty(pathFromUnity))
                return false;

            var p = pathFromUnity.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (File.Exists(Path.Combine(p, "launcher", "build.gradle")))
            {
                gradleRoot = p;
                return true;
            }

            var directGradle = Path.Combine(p, "build.gradle");
            if (!File.Exists(directGradle))
                return false;

            try
            {
                var head = File.ReadAllText(directGradle);
                if (head.IndexOf("com.android.library", StringComparison.Ordinal) < 0)
                    return false;
                var parent = Directory.GetParent(p)?.FullName;
                if (string.IsNullOrEmpty(parent))
                    return false;
                if (!File.Exists(Path.Combine(parent, "launcher", "build.gradle")))
                    return false;
                gradleRoot = parent;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool StripCoreLibraryDesugaringFromGradleFileIfNeeded(string gradlePath)
        {
            string content;
            try
            {
                content = File.ReadAllText(gradlePath);
            }
            catch
            {
                return false;
            }

            var updated = StripCoreLibraryDesugaringFromGradleText(content);
            if (updated == content)
                return false;
            try
            {
                File.WriteAllText(gradlePath, updated);
            }
            catch
            {
                return false;
            }

            return true;
        }

        static string StripCoreLibraryDesugaringFromGradleText(string content)
        {
            content = Regex.Replace(
                content,
                @"^\s*coreLibraryDesugaring\s+['""][^'""]+['""]\s*\r?\n",
                "",
                RegexOptions.Multiline);
            content = Regex.Replace(
                content,
                @"^\s*coreLibraryDesugaringEnabled\s+[^\r\n]+\r?\n",
                "",
                RegexOptions.Multiline);
            return content;
        }

        static string DescribeFeatureSet(BidscubeAndroidFeatureSet featureSet)
        {
            switch (featureSet)
            {
                case BidscubeAndroidFeatureSet.LiteNoVideo:
                    return "LiteNoVideo";
                case BidscubeAndroidFeatureSet.WebViewVideoNoDesugar:
                    return "WebViewVideoNoDesugar";
                case BidscubeAndroidFeatureSet.LegacyMediaVideoNoDesugar:
                    return "LegacyMediaVideoNoDesugar";
                case BidscubeAndroidFeatureSet.FullWithVideo:
                    return "FullWithVideo";
                default:
                    return featureSet.ToString();
            }
        }

        static bool RequiresFullVideoDeps(BidscubeAndroidFeatureSet featureSet)
        {
            return featureSet == BidscubeAndroidFeatureSet.FullWithVideo;
        }

        static string GetAarPath(string directory, string fileName)
        {
            return string.IsNullOrEmpty(fileName) ? null : Path.Combine(directory, fileName);
        }

        static string GetSelectedBundledFileName(BidscubeAndroidFeatureSet featureSet, BidscubeAndroidBundledCoreAarNames names)
        {
            switch (featureSet)
            {
                case BidscubeAndroidFeatureSet.LiteNoVideo:
                    return names.LiteFileName;
                case BidscubeAndroidFeatureSet.WebViewVideoNoDesugar:
                    return names.WebViewVideoFileName;
                case BidscubeAndroidFeatureSet.LegacyMediaVideoNoDesugar:
                    return names.LegacyMediaVideoFileName;
                case BidscubeAndroidFeatureSet.FullWithVideo:
                    return names.FullFileName;
                default:
                    return names.LiteFileName;
            }
        }

        static string GetSelectedSourcePath(BidscubeAndroidFeatureSet featureSet, string liteSrc, string webViewSrc,
            string legacySrc, string fullSrc)
        {
            switch (featureSet)
            {
                case BidscubeAndroidFeatureSet.LiteNoVideo:
                    return liteSrc;
                case BidscubeAndroidFeatureSet.WebViewVideoNoDesugar:
                    return webViewSrc;
                case BidscubeAndroidFeatureSet.LegacyMediaVideoNoDesugar:
                    return legacySrc;
                case BidscubeAndroidFeatureSet.FullWithVideo:
                    return fullSrc;
                default:
                    return liteSrc;
            }
        }

        static string GetSelectedDestinationPath(BidscubeAndroidFeatureSet featureSet, string liteDst, string webViewDst,
            string legacyDst, string fullDst)
        {
            switch (featureSet)
            {
                case BidscubeAndroidFeatureSet.LiteNoVideo:
                    return liteDst;
                case BidscubeAndroidFeatureSet.WebViewVideoNoDesugar:
                    return webViewDst;
                case BidscubeAndroidFeatureSet.LegacyMediaVideoNoDesugar:
                    return legacyDst;
                case BidscubeAndroidFeatureSet.FullWithVideo:
                    return fullDst;
                default:
                    return liteDst;
            }
        }

        static string GetMavenCoordinate(BidscubeAndroidFeatureSet featureSet, string version)
        {
            var artifactId = "sdk-lite-no-video";
            switch (featureSet)
            {
                case BidscubeAndroidFeatureSet.WebViewVideoNoDesugar:
                    artifactId = "sdk-webview-video";
                    break;
                case BidscubeAndroidFeatureSet.LegacyMediaVideoNoDesugar:
                    artifactId = "sdk-legacy-media-video";
                    break;
                case BidscubeAndroidFeatureSet.FullWithVideo:
                    artifactId = "sdk-full-video";
                    break;
            }
            return $"com.bidscube:{artifactId}:{version}@aar";
        }

        static void TryCopySelectedAarForReference(BidscubeAndroidFeatureSet fs, string liteSrc, string webViewSrc,
            string legacySrc, string fullSrc, string liteDst, string webViewDst, string legacyDst, string fullDst, string logPrefix)
        {
            try
            {
                var src = GetSelectedSourcePath(fs, liteSrc, webViewSrc, legacySrc, fullSrc);
                var dst = GetSelectedDestinationPath(fs, liteDst, webViewDst, legacyDst, fullDst);
                if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(dst) && File.Exists(src))
                    File.Copy(src, dst, true);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"{logPrefix} Optional AAR copy: {e.Message}");
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignored
            }
        }

        static bool TryGetUnityLibraryGradleInfo(string basePath, out string unityLibraryModuleRoot,
            out string buildGradlePath, out string libsDir)
        {
            unityLibraryModuleRoot = null;
            buildGradlePath = null;
            libsDir = null;
            if (string.IsNullOrEmpty(basePath))
                return false;

            var nested = Path.Combine(basePath, "unityLibrary", "build.gradle");
            if (File.Exists(nested))
            {
                unityLibraryModuleRoot = Path.Combine(basePath, "unityLibrary");
                buildGradlePath = nested;
                libsDir = Path.Combine(unityLibraryModuleRoot, "libs");
                return true;
            }

            var direct = Path.Combine(basePath, "build.gradle");
            if (!File.Exists(direct))
                return false;

            try
            {
                var head = File.ReadAllText(direct);
                if (head.IndexOf("com.android.library", StringComparison.Ordinal) < 0)
                    return false;
            }
            catch
            {
                return false;
            }

            unityLibraryModuleRoot = basePath;
            buildGradlePath = direct;
            libsDir = Path.Combine(unityLibraryModuleRoot, "libs");
            return true;
        }

        static void RemoveManagedBlock(string unityLibraryBuildGradlePath, string logPrefix)
        {
            var gradlePath = unityLibraryBuildGradlePath;
            if (!File.Exists(gradlePath))
                return;
            var content = File.ReadAllText(gradlePath);
            const string start = "// __BIDSCUBE_ANDROID_MANAGED_START__";
            const string end = "// __BIDSCUBE_ANDROID_MANAGED_END__";
            if (!content.Contains(start))
                return;
            var pattern = new Regex(Regex.Escape(start) + "[\\s\\S]*?" + Regex.Escape(end), RegexOptions.Multiline);
            content = ReplaceFirstMatchLiteral(pattern, content, "");
            File.WriteAllText(gradlePath, content);
        }

        static void PatchUnityLibraryGradle(string unityLibraryBuildGradlePath, BidscubeAndroidFeatureSet featureSet,
            BidscubeAndroidCoreDependencyMode coreMode, string customLines, string ver, string mavenCoordinate,
            string bundledAarFileName, bool appendAppLovinSdkDependency, BidscubeAndroidBundledCoreAarNames names, string logPrefix)
        {
            var gradlePath = unityLibraryBuildGradlePath;
            if (!File.Exists(gradlePath))
            {
                UnityEngine.Debug.LogWarning($"{logPrefix} unityLibrary build.gradle not found at {gradlePath}");
                return;
            }

            var content = File.ReadAllText(gradlePath);

            if (!string.IsNullOrEmpty(bundledAarFileName) || !string.IsNullOrEmpty(mavenCoordinate))
                content = StripHostTemplateBidscubeSdkMavenLines(content);

            var sb = new StringBuilder();
            sb.AppendLine("// __BIDSCUBE_ANDROID_MANAGED_START__");

            if (coreMode == BidscubeAndroidCoreDependencyMode.CustomGradleLines &&
                !string.IsNullOrWhiteSpace(customLines))
            {
                foreach (var line in customLines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    sb.AppendLine("    " + line.Trim());
            }
            else if (!string.IsNullOrEmpty(bundledAarFileName))
            {
                sb.AppendLine($"    implementation files('libs/{bundledAarFileName}')");
            }
            else if (!string.IsNullOrEmpty(mavenCoordinate))
            {
                sb.AppendLine($"    implementation '{mavenCoordinate}'");
            }

            if (appendAppLovinSdkDependency)
                MaybeAppendAppLovinSdkLine(sb, content);

            if (!RequiresFullVideoDeps(featureSet))
                UnityEngine.Debug.Log($"{logPrefix} Skipping Media3 and Google IMA dependencies");
            else
            {
                UnityEngine.Debug.Log($"{logPrefix} Including Media3 and Google IMA dependencies");
                AppendVideoDeps(sb, content);
            }

            sb.AppendLine("// __BIDSCUBE_ANDROID_MANAGED_END__");
            var inner = sb.ToString();
            const string start = "// __BIDSCUBE_ANDROID_MANAGED_START__";
            const string end = "// __BIDSCUBE_ANDROID_MANAGED_END__";
            if (content.Contains(start))
            {
                var pattern = new Regex(Regex.Escape(start) + "[\\s\\S]*?" + Regex.Escape(end),
                    RegexOptions.Multiline);
                content = ReplaceFirstMatchLiteral(pattern, content, inner);
            }
            else
                content = InjectAfterDependenciesOpen(content, inner);

            File.WriteAllText(gradlePath, content);
        }

        static string StripHostTemplateBidscubeSdkMavenLines(string gradle)
        {
            return Regex.Replace(
                gradle,
                @"^\s*implementation\s+['""]com\.bidscube:(?:bidscube-sdk|sdk-lite-no-video|sdk-webview-video|sdk-legacy-media-video|sdk-full-video):[^'""]+['""]\s*\r?\n",
                "",
                RegexOptions.Multiline);
        }

        static void MaybeAppendAppLovinSdkLine(StringBuilder sb, string existingGradle)
        {
            if (existingGradle.IndexOf("com.applovin:applovin-sdk", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            if (sb.ToString().IndexOf("com.applovin:applovin-sdk", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            sb.AppendLine("    implementation 'com.applovin:applovin-sdk:13.+'");
        }

        static void AppendVideoDeps(StringBuilder sb, string existingGradle)
        {
            void AddIfMissing(string coordinate)
            {
                var parts = coordinate.Split(':');
                var artifact = parts.Length > 1 ? parts[1] : coordinate;
                if (existingGradle.IndexOf(artifact, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                var cur = sb.ToString();
                if (cur.IndexOf(artifact, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                sb.AppendLine($"    implementation '{coordinate}'");
            }

            AddIfMissing("androidx.media3:media3-common:1.4.1");
            AddIfMissing("androidx.media3:media3-ui:1.4.1");
            AddIfMissing("com.google.ads.interactivemedia.v3:interactivemedia:3.33.0");
        }

        static string InjectAfterDependenciesOpen(string gradle, string block)
        {
            var idx = gradle.IndexOf("dependencies", StringComparison.Ordinal);
            if (idx < 0)
                return gradle + "\n" + block + "\n";
            var brace = gradle.IndexOf('{', idx);
            if (brace < 0)
                return gradle + "\n" + block + "\n";
            return gradle.Insert(brace + 1, "\n" + block);
        }

        static string ResolvePackageRoot(Assembly asm)
        {
            try
            {
                var info = global::UnityEditor.PackageManager.PackageInfo.FindForAssembly(asm);
                if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
                    return info.resolvedPath;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
