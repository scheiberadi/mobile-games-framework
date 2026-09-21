using System.IO;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ComplianceTests
    {
        private static readonly string[] Forbidden =
        {
            "com.google.ads.mobile", "com.google.external-dependency-manager", "com.unity.purchasing",
            "com.unity.modules.unityanalytics", "com.unity.analytics", "com.unity.services",
            "com.unity.ads", "com.unity.monetization"
        };

        [Test]
        public void PackagesLockHasNoAdsAnalyticsOrPurchasing()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "packages-lock.json");
            Assert.IsTrue(File.Exists(path), "open the project once so Unity writes packages-lock.json");
            var text = File.ReadAllText(path);
            foreach (var id in Forbidden)
                Assert.That(text, Does.Not.Contain("\"" + id), "forbidden package present: " + id);
        }
    }
}
