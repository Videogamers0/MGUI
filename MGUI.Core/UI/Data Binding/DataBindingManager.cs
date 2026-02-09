using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Data_Binding
{
    /// <summary>Static class that keeps track of all <see cref="DataBinding"/>s for all objects.</summary>
    public static class DataBindingManager
    {
        private static readonly List<DataBinding> _Bindings = new();
        public static IReadOnlyList<DataBinding> Bindings => _Bindings;

        private static readonly Dictionary<object, List<DataBinding>> _BindingsByTargetObject = new();

        public static DataBinding AddBinding(BindingConfig Config, object TargetObject)
        {
            if (TargetObject == null)
                throw new ArgumentNullException(nameof(TargetObject));

            DataBinding Binding = new(Config, TargetObject);
            _Bindings.Add(Binding);

            if (_BindingsByTargetObject.TryGetValue(TargetObject, out List<DataBinding> ObjectBindings))
            {
                //  Validate that there isn't already a binding for this TargetObject+TargetProperty tuple
                //  (TODO: I guess we could instead remove the existing binding to replace it with the new one)
                if (ObjectBindings.Any(x => x.Config.TargetPath == Config.TargetPath))
                {
                    throw new InvalidOperationException(
                        $"Unable to bind to target property '{Config.TargetPath}' of target object '{TargetObject}' ({TargetObject.GetType().FullName}) " +
                        $"because a binding was already created for this target.");
                }
            }
            else
            {
                ObjectBindings = new();
                _BindingsByTargetObject.Add(TargetObject, ObjectBindings);
            }

            ObjectBindings.Add(Binding);

            return Binding;
        }

        public static bool RemoveBinding(DataBinding Binding)
        {
            if (Binding == null)
                throw new ArgumentNullException(nameof(Binding));

            bool Result = _Bindings.Remove(Binding);

            if (_BindingsByTargetObject.TryGetValue(Binding.TargetObject, out List<DataBinding> ObjectBindings))
            {
                if (ObjectBindings.Remove(Binding) && ObjectBindings.Count == 0)
                    _BindingsByTargetObject.Remove(Binding.TargetObject);
            }

            if (Result)
                Binding.Dispose();
            return Result;
        }

        /// <summary>Removes all <see cref="DataBinding"/>s that use <paramref name="TargetObject"/> as their <see cref="DataBinding.TargetObject"/></summary>
        /// <returns>The count of the removed bindings</returns>
        public static int RemoveBindings(object TargetObject)
        {
            if (TargetObject == null)
                throw new ArgumentNullException(nameof(TargetObject));

            if (_BindingsByTargetObject.TryGetValue(TargetObject, out List<DataBinding> ObjectBindings))
            {
                foreach (DataBinding Binding in ObjectBindings)
                    Binding.Dispose();
                _BindingsByTargetObject.Remove(TargetObject);
                return ObjectBindings.Count;
            }
            else
                return 0;
        }
    }
}
