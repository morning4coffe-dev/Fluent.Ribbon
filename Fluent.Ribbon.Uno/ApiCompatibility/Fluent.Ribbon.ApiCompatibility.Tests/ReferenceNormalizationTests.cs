using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests
{
    [TestFixture]
    public sealed class ReferenceNormalizationTests
    {
        [Test]
        public void Reader_NormalizesApprovedFrameworkTypesOnlyForReferenceAssembly()
        {
            var reader = new MetadataApiReader(new FrameworkTypeNormalizer());
            var path = typeof(ReferenceNormalizationFixture).Assembly.Location;
            var reference = reader.ReadReference(path);
            var candidate = reader.ReadCandidate(path);
            var typeName = typeof(ReferenceNormalizationFixture).FullName!;

            var referenceMethod = reference.Types[typeName].Members.Values.Single(
                member => member.Identity.Contains("AcceptDependencyObject", StringComparison.Ordinal));
            var candidateMethod = candidate.Types[typeName].Members.Values.Single(
                member => member.Identity.Contains("AcceptDependencyObject", StringComparison.Ordinal));

            Assert.Multiple(() =>
            {
                Assert.That(
                    referenceMethod.Parameters[0].Type,
                    Is.EqualTo("Microsoft.UI.Xaml.DependencyObject"));
                Assert.That(
                    candidateMethod.Parameters[0].Type,
                    Is.EqualTo("System.Windows.DependencyObject"));
            });
        }
    }

    public sealed class ReferenceNormalizationFixture
    {
        public void AcceptDependencyObject(System.Windows.DependencyObject value)
        {
        }
    }
}

namespace System.Windows
{
    public sealed class DependencyObject;
}
