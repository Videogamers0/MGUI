using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
using System.Windows.Data;
#else
using MGUI.Core.UI.Data_Binding.Converters;
#endif

namespace MGUI.Core.UI.Data_Binding
{
    public readonly record struct BindingConfig(string _TargetPath, string _SourcePath, DataBindingMode BindingMode = DataBindingMode.OneWay,
        ISourceObjectResolver SourceResolver = null, DataContextResolver DataContextResolver = DataContextResolver.DataContext,
        IValueConverter Converter = null, object ConverterParameter = null, object FallbackValue = null, string StringFormat = null)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _TargetPath = _TargetPath ?? throw new ArgumentNullException(nameof(_TargetPath));
        public string TargetPath
        {
            get => _TargetPath;
            init
            {
                _TargetPath = value;
                TargetPaths = GetPaths(TargetPath);
            }
        }
        public readonly ReadOnlyCollection<string> TargetPaths = GetPaths(_TargetPath);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _SourcePath = _SourcePath ?? throw new ArgumentNullException(nameof(_SourcePath));
        public string SourcePath
        {
            get => _SourcePath;
            init
            {
                _SourcePath = value;
                SourcePaths = GetPaths(SourcePath);
            }
        }

        public readonly ReadOnlyCollection<string> SourcePaths = GetPaths(_SourcePath);

        private static ReadOnlyCollection<string> GetPaths(string Path)
            => Path == string.Empty ? new List<string>() { "" }.AsReadOnly() : Path.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();

        public ISourceObjectResolver SourceResolver { get; init; } = SourceResolver ?? ISourceObjectResolver.FromSelf();
    }
}
