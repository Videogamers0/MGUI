using System;

namespace MGUI.Shared.Text
{
    /// <summary>
    /// Identifies a font by family name, point size and style.
    /// Used as a cache key and as the parameter to <see cref="Engines.ITextEngine.ResolveFont"/>.
    /// </summary>
    public readonly record struct FontSpec(string Family, int Size, CustomFontStyles Style)
    {
        /// <summary>Convenience factory: Normal style.</summary>
        public static FontSpec Normal(string family, int size) =>
            new(family, size, CustomFontStyles.Normal);

        /// <inheritdoc/>
        public override string ToString() => $"{Family}/{Size}/{Style}";
    }
}
