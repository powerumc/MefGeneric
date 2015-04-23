Junil Um  
http://blog.powerumc.kr

Introduction
============

MEFGeneric is a framework to support CLR Generic types in MEF (Managed Extensibility Framework).

This is require Mef Framework if you need code on .NET Framework 3.0 or C# 2.0 under.

Background
==========

MEF does not support open generic types. Therefore, open generic type classes cannot utilize MEF Composition. But, via MEFGeneric, changing and/or extending current MEF source code allows use of open generic types in MEF.

###Example

The following source code does not work in MEF, but using MEFGeneric, you can resolve and compose objects.

**Catalog and Composition**
```c#
var catalog = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
var genericCatalog = new GenericCatalog(catalog);
var container = new CompositionContainer(genericCatalog);

// Export Definition
public interface IUMC<T>
{
  void Say();
}

// Important : via only MEFGeneric, you can export definition of IUMC<> generic types.
[Export(typeof(IUMC<>))]
public class UMC<T> : IUMC<T>
{
	#region IUMC<T> Member

	public void Say()
	{
		Console.WriteLine(typeof(T).FullName);
	}

	#endregion
}
```

**Container Resolving / Injection**
```c#
container.GetExportedValueOrDefault<IUMC<int>>().Say();
container.GetExportedValueOrDefault<IUMC<string>>().Say();
```
