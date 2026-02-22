using MGUI.Shared.Text;
using Xunit;

namespace MGUI.Tests.Text
{
    public class FontSpecTests
    {
        [Fact]
        public void Constructor_StoresAllFields()
        {
            var spec = new FontSpec("Arial", 12, CustomFontStyles.Bold);
            Assert.Equal("Arial", spec.Family);
            Assert.Equal(12, spec.Size);
            Assert.Equal(CustomFontStyles.Bold, spec.Style);
        }

        [Fact]
        public void NormalFactory_CreatesNormalStyle()
        {
            var spec = FontSpec.Normal("Verdana", 14);
            Assert.Equal(CustomFontStyles.Normal, spec.Style);
            Assert.Equal("Verdana", spec.Family);
            Assert.Equal(14, spec.Size);
        }

        [Fact]
        public void EqualityIsValueBased()
        {
            var a = new FontSpec("Arial", 12, CustomFontStyles.Normal);
            var b = new FontSpec("Arial", 12, CustomFontStyles.Normal);
            var c = new FontSpec("Arial", 14, CustomFontStyles.Normal);

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
        }

        [Fact]
        public void HashCode_ConsistentForEqualSpecs()
        {
            var a = new FontSpec("Arial", 12, CustomFontStyles.Normal);
            var b = new FontSpec("Arial", 12, CustomFontStyles.Normal);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void CanBeUsedAsDictionaryKey()
        {
            var dict = new System.Collections.Generic.Dictionary<FontSpec, string>();
            var spec = FontSpec.Normal("Arial", 12);
            dict[spec] = "hello";
            Assert.Equal("hello", dict[new FontSpec("Arial", 12, CustomFontStyles.Normal)]);
        }

        [Fact]
        public void ToString_ContainsFamilyAndSize()
        {
            var spec = new FontSpec("Arial", 12, CustomFontStyles.Bold);
            var s = spec.ToString();
            Assert.Contains("Arial", s);
            Assert.Contains("12", s);
        }
    }
}
