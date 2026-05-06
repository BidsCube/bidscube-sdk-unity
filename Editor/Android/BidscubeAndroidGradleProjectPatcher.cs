#if UNITY_ANDROID
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
        {
            NativeVersion = nativeVersion;
            LiteFileName = liteFileName;
            FullFileName = fullFileName;
        }

        public string NativeVersion { get; }
        public string LiteFileName { get; }
        public string FullFileName { get; }
    }

    /// <summary>Shared Android Gradle export logic for AppLovin MAX and LevelPlay adapter packages.</summary>
    internal static class BidscubeAndroidGradleProjectPatcher
    {
        internal static void OnPostGenerateGradleAndroidProject(string path, string logPrefix, Assembly packageAssembly,
            bool appendAppLovinSdkDependency, BidscubeAndroidBundledCoreAarNames names)
        {
            try
            {
                var featureSet = BidscubeAndroidExportSettingsResolver.GetEffectiveFeatureSet();
                var coreMode = BidscubeAndroidExportSettingsResolver.GetEffectiveCoreDependencyMode();
                var customLines = BidscubeAndroidExportSettingsResolver.GetEffectiveCustomGradleLines();

                if (featureSet == BidscubeAndroidFeatureSet.LiteNoVideo)
                    UnityEngine.Debug.Log($"{logPrefix} Android feature set: LiteNoVideo");
                else
                    UnityEngine.Debug.Log($"{logPrefix} Android feature set: FullWithVideo");

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
                var liteName = names.LiteFileName;
                var fullName = names.FullFileName;
                var liteSrc = Path.Combine(plugins, liteName);
                var fullSrc = Path.Combine(plugins, fullName);
                Directory.CreateDirectory(libsDir);
                var liteDst = Path.Combine(libsDir, liteName);
                var fullDst = Path.Combine(libsDir, fullName);

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

                    TryCopySelectedAarForReference(featureSet, liteSrc, fullSrc, liteDst, fullDst, logPrefix);
                    PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, customLines, ver, fullCoreFromMaven: false,
                        useBundledFullAar: false, useBundledLiteAar: false, appendAppLovinSdkDependency, liteName, fullName, logPrefix);
                    return;
                }

                if (featureSet == BidscubeAndroidFeatureSet.LiteNoVideo)
                {
                    TryDelete(fullDst);
                    if (!File.Exists(liteSrc))
                    {
                        UnityEngine.Debug.LogError($"{logPrefix} LiteNoVideo: missing lite AAR at {liteSrc}");
                        return;
                    }

                    File.Copy(liteSrc, liteDst, true);
                    UnityEngine.Debug.Log($"{logPrefix} Copied bundled core AAR: {liteDst}");
                    PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, "", ver, fullCoreFromMaven: false,
                        useBundledFullAar: false, useBundledLiteAar: true, appendAppLovinSdkDependency, liteName, fullName, logPrefix);
                    return;
                }

                // FullWithVideo
                TryDelete(liteDst);
                if (coreMode == BidscubeAndroidCoreDependencyMode.MavenBidscubeSdkAar)
                {
                    TryDelete(fullDst);
                    PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, "", ver, fullCoreFromMaven: true,
                        useBundledFullAar: false, useBundledLiteAar: false, appendAppLovinSdkDependency, liteName, fullName, logPrefix);
                    return;
                }

                if (!File.Exists(fullSrc))
                {
                    UnityEngine.Debug.LogError(
                        $"{logPrefix} FullWithVideo requires Runtime/Plugins/Android/{fullName}, or set coreDependencyMode to MavenBidscubeSdkAar with a reachable Maven artifact com.bidscube:sdk-full-video:" +
                        ver + "@aar. Switch to LiteNoVideo for publisher demo / CI without the full AAR.");
                    RemoveManagedBlock(unityLibraryBuildGradle, logPrefix);
                    return;
                }

                File.Copy(fullSrc, fullDst, true);
                UnityEngine.Debug.Log($"{logPrefix} Copied bundled core AAR: {fullDst}");
                PatchUnityLibraryGradle(unityLibraryBuildGradle, featureSet, coreMode, "", ver, fullCoreFromMaven: false,
                    useBundledFullAar: true, useBundledLiteAar: false, appendAppLovinSdkDependency, liteName, fullName, logPrefix);
            }
            finally
            {
                var fs = BidscubeAndroidExportSettingsResolver.GetEffectiveFeatureSet();
                if (fs == BidscubeAndroidFeatureSet.LiteNoVideo)
                    ApplyDesugaringPolicyLite(path, logPrefix);
                else
                    ApplyDesugaringPolicyFull(path, logPrefix);
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
                    $"{logPrefix} LiteNoVideo: removed coreLibraryDesugaring lines from generated launcher/unityLibrary Gradle files.");
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

        static string EnsureCoreLibraryDesugaringInGradleText(string content)
        {
            const string depLine = "    coreLibraryDesugaring 'com.android.tools:desugar_jdk_libs:2.0.4'";
            if (content.IndexOf("desugar_jdk_libs", StringComparison.OrdinalIgnoreCase) < 0)
                content = Regex.Replace(content, @"(dependencies\s*\{)\s*", $"$1\n{depLine}\n", 1, RegexOptions.Multiline);

            if (Regex.IsMatch(content, @"coreLibraryDesugaringEnabled\s+true\b"))
                return content;

            if (Regex.IsMatch(content, @"coreLibraryDesugaringEnabled\s+false\b"))
                return Regex.Replace(content, @"(\bcoreLibraryDesugaringEnabled\s+)false\b", "${1}true", RegexOptions.Multiline);

            if (Regex.IsMatch(content, @"compileOptions\s*\{"))
                return Regex.Replace(content, @"(compileOptions\s*\{)(\s*)", "$1$2        coreLibraryDesugaringEnabled true$2", 1, RegexOptions.Multiline);

            return Regex.Replace(content, @"(android\s*\{)(\s*)", "$1$2    compileOptions {\n        coreLibraryDesugaringEnabled true\n    }\n$2", 1, RegexOptions.Multiline);
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

        static void TryCopySelectedAarForReference(BidscubeAndroidFeatureSet fs, string liteSrc, string fullSrc,
            string liteDst, string fullDst, string logPrefix)
        {
            try
            {
                if (fs == BidscubeAndroidFeatureSet.LiteNoVideo && File.Exists(liteSrc))
                    File.Copy(liteSrc, liteDst, true);
                else if (fs == BidscubeAndroidFeatureSet.FullWithVideo && File.Exists(fullSrc))
                    File.Copy(fullSrc, fullDst, true);
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
            content = pattern.Replace(content, "", 1);
            File.WriteAllText(gradlePath, content);
        }

        static void PatchUnityLibraryGradle(string unityLibraryBuildGradlePath, BidscubeAndroidFeatureSet featureSet,
            BidscubeAndroidCoreDependencyMode coreMode, string customLines, string ver, bool fullCoreFromMaven,
            bool useBundledFullAar, bool useBundledLiteAar, bool appendAppLovinSdkDependency, string liteFileName,
            string fullFileName, string logPrefix)
        {
            var gradlePath = unityLibraryBuildGradlePath;
            if (!File.Exists(gradlePath))
            {
                UnityEngine.Debug.LogWarning($"{logPrefix} unityLibrary build.gradle not found at {gradlePath}");
                return;
            }

            var content = File.ReadAllText(gradlePath);

            if (useBundledLiteAar || useBundledFullAar || fullCoreFromMaven)
                content = StripHostTemplateBidscubeSdkMavenLines(content);

            var sb = new StringBuilder();
            sb.AppendLine("// __BIDSCUBE_ANDROID_MANAGED_START__");

            if (coreMode == BidscubeAndroidCoreDependencyMode.CustomGradleLines &&
                !string.IsNullOrWhiteSpace(customLines))
            {
                foreach (var line in customLines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    sb.AppendLine("    " + line.Trim());
            }
            else if (useBundledLiteAar)
            {
                sb.AppendLine($"    implementation files('libs/{liteFileName}')");
            }
            else if (fullCoreFromMaven)
            {
                sb.AppendLine($"    implementation 'com.bidscube:sdk-full-video:{ver}@aar'");
            }
            else if (useBundledFullAar)
            {
                sb.AppendLine($"    implementation files('libs/{fullFileName}')");
            }

            if (appendAppLovinSdkDependency)
                MaybeAppendAppLovinSdkLine(sb, content);

            if (featureSet == BidscubeAndroidFeatureSet.LiteNoVideo)
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
                content = pattern.Replace(content, inner, 1);
            }
            else
                content = InjectAfterDependenciesOpen(content, inner);

            File.WriteAllText(gradlePath, content);
        }

        static string StripHostTemplateBidscubeSdkMavenLines(string gradle)
        {
            return Regex.Replace(
                gradle,
                @"^\s*implementation\s+['""]com\.bidscube:(?:bidscube-sdk|sdk-lite-no-video|sdk-full-video):[^'""]+['""]\s*\r?\n",
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
#endif
