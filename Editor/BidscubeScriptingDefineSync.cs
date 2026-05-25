using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BidscubeSDK.Editor
{
    internal sealed class BidscubeScriptingDefineSync : IPreprocessBuildWithReport
    {
        public int callbackOrder => -10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            BidscubeDefineApplicator.ApplyFromEffectiveSettings();
        }
    }
}
