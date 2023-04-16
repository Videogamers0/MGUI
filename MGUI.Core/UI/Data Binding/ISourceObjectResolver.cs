using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Data_Binding
{
    /// <summary>For concrete implementations, use:<br/>
    /// <see cref="SourceObjectResolverSelf"/><br/>
    /// <see cref="SourceObjectResolverElementName"/><br/>
    /// <see cref="SourceObjectResolverStaticResource"/><br/>
    /// <see cref="SourceObjectResolverElementAncestor{T}"/><br/>
    /// <see cref="SourceObjectResolverDesktop"/></summary>
    public interface ISourceObjectResolver
    {
        public object ResolveSourceObject(object TargetObject);

        private static readonly SourceObjectResolverSelf SelfResolver = new();
        /// <summary>Indicates that the source object of the binding is the same as the Targeted object</summary>
        public static ISourceObjectResolver FromSelf() => SelfResolver;

        /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGWindow.GetElementByName(string)"/><br/>
        /// (assuming the target object is of type <see cref="MGElement"/> and belongs to a <see cref="MGWindow"/>)</summary>
        public static ISourceObjectResolver FromElementName(string ElementName) => new SourceObjectResolverElementName(ElementName);

        /// <summary>Indicates that the source object of the binding should be retrieved via a particular named resource in <see cref="MGResources.StaticResources"/><br/>
        /// (assuming the target object is of type <see cref="MGElement"/> so that the resources can obtained from <see cref="MGDesktop.Resources"/>)</summary>
        public static ISourceObjectResolver FromResourceName(string ResourceName) => new SourceObjectResolverStaticResource(ResourceName);

        /// <summary>Indicates that the source object of the binding should be retrieved by traversing up the visual tree 
        /// by a certain number of hierarchical levels and looking for a parent of a particular <typeparamref name="T"/> type.<br/>
        /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
        public static ISourceObjectResolver FromElementAncestor<T>(int AncestorLevel = 1) where T : MGElement => new SourceObjectResolverElementAncestor<T>(AncestorLevel);
        public static ISourceObjectResolver FromElementAncestor(int AncestorLevel = 1) => FromElementAncestor<MGElement>(AncestorLevel);

        private static readonly SourceObjectResolverDesktop DesktopResolver = new();
        /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGElement.GetDesktop"/><br/>
        /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
        public static ISourceObjectResolver FromDesktop() => DesktopResolver;
    }

    /// <summary>Indicates that the source object of the binding is the same as the Targeted object</summary>
    public class SourceObjectResolverSelf : ISourceObjectResolver
    {
        public SourceObjectResolverSelf() { }
        public object ResolveSourceObject(object TargetObject) => TargetObject;
        public override string ToString() => $"{nameof(SourceObjectResolverSelf)}";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGWindow.GetElementByName(string)"/><br/>
    /// (assuming the target object is of type <see cref="MGElement"/> and belongs to a <see cref="MGWindow"/>)</summary>
    public class SourceObjectResolverElementName : ISourceObjectResolver
    {
        public readonly string ElementName;

        public SourceObjectResolverElementName(string ElementName)
        {
            this.ElementName = ElementName ?? throw new ArgumentNullException(nameof(ElementName));
        }

        public object ResolveSourceObject(object TargetObject)
        {
            if (TargetObject is MGElement Element && Element.SelfOrParentWindow.TryGetElementByName(ElementName, out MGElement NamedElement))
                return NamedElement;
            else
                return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverElementName)}: {ElementName}";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGResources.StaticResources"/> using the given <see cref="ResourceName"/><br/>
    /// (assuming the target object is of type <see cref="MGElement"/> so that the resources can obtained from <see cref="MGDesktop.Resources"/>)</summary>
    public class SourceObjectResolverStaticResource : ISourceObjectResolver
    {
        public readonly string ResourceName;

        public SourceObjectResolverStaticResource(string ResourceName)
        {
            this.ResourceName = ResourceName ?? throw new ArgumentNullException(nameof(ResourceName));
        }

        public object ResolveSourceObject(object TargetObject)
        {
            if (TargetObject is MGElement Element && Element.GetResources().StaticResources.TryGetValue(ResourceName, out object Resource))
                return Resource;
            else
                return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverStaticResource)}: {ResourceName}";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved by traversing up the visual tree 
    /// by a certain number of hierarchical levels and looking for a parent of a particular <typeparamref name="T"/> type.<br/>
    /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
    public class SourceObjectResolverElementAncestor<T> : ISourceObjectResolver
        where T : MGElement
    {
        /// <summary>The number of matches that must be found before ending the search.<para/>
        /// EX: If <see cref="AncestorLevel"/>=2 and <typeparamref name="T"/>=typeof(<see cref="MGBorder"/>), 
        /// this resolver will look for the 2nd <see cref="MGBorder"/> parent when traversing the visual tree upwards.</summary>
        public readonly int AncestorLevel;

        /// <param name="AncestorLevel">The number of matches that must be found before ending the search.<para/>
        /// EX: If <paramref name="AncestorLevel"/>=2 and <typeparamref name="T"/>=typeof(<see cref="MGBorder"/>), 
        /// this resolver will look for the 2nd <see cref="MGBorder"/> parent when traversing the visual tree upwards.</param>
        public SourceObjectResolverElementAncestor(int AncestorLevel = 1)
        {
            this.AncestorLevel = AncestorLevel;
        }

        public object ResolveSourceObject(object TargetObject)
        {
            if (AncestorLevel == 0)
                return TargetObject;

            if (TargetObject is MGElement Element)
            {
                int Count = AncestorLevel;
                MGElement Current = Element;

                while (Count > 0 && Current != null)
                {
                    Current = Current.Parent;
                    if (Current is T)
                    {
                        Count--;
                        if (Count == 0)
                            return Current;
                    }
                }
            }

            return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverElementAncestor<T>)}: {typeof(T).Name} ({AncestorLevel})";
    }

    /// <summary>Indicates that the source object of the binding should be retrieved via <see cref="MGElement.GetDesktop"/><br/>
    /// (assuming the target object is of type <see cref="MGElement"/>)</summary>
    public class SourceObjectResolverDesktop : ISourceObjectResolver
    {
        public SourceObjectResolverDesktop() { }

        public object ResolveSourceObject(object TargetObject)
        {
            if (TargetObject is MGElement Element)
                return Element.GetDesktop();
            else
                return null;
        }

        public override string ToString() => $"{nameof(SourceObjectResolverDesktop)}";
    }
}
