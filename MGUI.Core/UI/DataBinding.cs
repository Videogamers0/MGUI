using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public static class DataBinding
    {
#if NEVER

TODO:
Window.DataContext

DataContextChanged event
	every element subscribes to ParentWindow.DataContextChanged

MGElement has:
	object WindowDataContext -> The datacontext, as derived from ParentWindow.DataContext
	object DataContextOverride -> if not null, this is used as the actual DataContext
	object DataContext => DataContextOverride ?? WindowDataContext
	
during initialization or if DataContextChanged subscription
	set WindowDataContext

when the actual DataContext is changed
	resolve every binding
	

ex: binding1:
	Path="Foo.Bar"
	Mode=TwoWay
	TargetPropertyName=IsChecked

	object SourceObject = ...Find the Foo object within DataContext
	set IsChecked to the value of the Bar property within SourceObject
		This completes the 'OneTime' binding
	if sourceobject implements inotifypropertychanged
		subscribe to the inotifypropertychanged's propertychanged event
		if propertyname is Bar
			set IsChecked to the value of the Bar property within SourceObject
				This completes the 'OneWay' binding
	if mode is twoway then we also need:
		subscribe to MGElement.propertychanged
			if propertyname is TargetPropertyName
			set SourceObject's Bar property to the new IsChecked value

we need to iterate the bindings to group them by their path
then foreach path, resolve the path to the SourceOBject
	and if obj is INotifypropertychanged and there's at least 1 binding with mode=oneway or twoway
		create an action<object,string>
		subscvribe to the object's propertychanged with the action
		the action's logic is basically: if propertyname is 1 of the tracked propertynames
			update target value that corresponds to the propertyname

#endif
    }
}
