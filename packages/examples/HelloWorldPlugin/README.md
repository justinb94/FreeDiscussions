# How to create a plugin

In this tutorial, we will create a small "Hello World" plugin to illustrate how to extend the Free Discussions Client.

---

## 1. Create a new project.

At first, we need a project to work with.
In Visual Studio, go to File > New > Project. This will open the "Create project dialog".

On the first page:

- Choose "Class Library".
- Click Next

On the second page:

- Give the project a name and a Solution name. (in our case, we choose "HelloWorldPlugin")
- Click Next

On the third page:

- Select the Framework .NET Core 3.1 (Long-term support).

## We now have an empty project. Let's make a plugin out of it!

---

## 2. Project References.

Free Discussions uses the [Managed Extensibility Framework (MEF)](https://docs.microsoft.com/en-US/dotnet/framework/mef/).

- Add reference to `System.ComponentModel.Composition` using

  `dotnet add package System.ComponentModel.Composition --version 6.0.0`

- Check out this repository `git clone https://github.com/justinb94/FreeDiscussions.git`

- Add reference to `FreeDiscussions.Plugin`. 

## 3. Create our plugin.

The FreeDiscussions Client is looking for .DLL files containing a class which extends `FreeDiscussions.Plugin.BasePlugin`.

### 3.1 Create our plugin class

In order for the client to find the plugin, we need to extend the `BasePlugin` class located in the `FreeDiscussions.Plugin.Plugin` namespace.

For our plugin, we create a new class called `Plugin.cs`.

```csharp
using FreeDiscussions.Plugin;
using System.Threading.Tasks;

    namespace HelloWorldPlugin
    {
		[System.ComponentModel.Composition.Export(typeof(IPlugin))]
		public class Plugin : FreeDiscussions.Plugin.BasePlugin
		{
			// location of the plugin's ui, we choose to show it in the sidebar
			public override PanelType Type { get => PanelType.Sidebar; set { } }
			// unique id of the plugin
			public override string Guid { get => "HelloWorldPlugin"; set { } }
			// name of the plugin
			public override string Name { get => "HelloWorld"; set { } }
			// patch to the plugin's icon
			public override string IconPath { get => "/HelloWorldPlugin;component/Resources/icon.svg"; set { } }

			// Create our ui
			public override async Task<TabItemModel> Create(params object[] args)
			{
				// return a new tab item
				return new TabItemModel(this.Guid)
				{
					// text in the tab
					HeaderText = "Hello World",
					// gets called when the user closes the tab
					Close = new DelegateCommand<string>((o) => { }),
					// path to the icon (this can differ from the plugin's icon)
					IconPath = this.IconPath,
					// UserControl to display
					Control = new Panel(() =>
					{
						// do something when the user closes the tab
					})
				};
			}
		}
	}
```

### 3.2 Create our UI

Since we're only want to show "Hello World" on the screen, we create a fairly simple ui.

- Add a new UserControl, in our example we're call it "Panel.cs".

The Panel.xaml file contains the following:

```xml
<plugin:Panel x:Class="HelloWorldPlugin.Panel" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" xmlns:d="http://schemas.microsoft.com/expression/blend/2008" xmlns:plugin="clr-namespace:FreeDiscussions.Plugin;assembly=FreeDiscussions.Plugin" mc:Ignorable="d" d:DesignHeight="450" d:DesignWidth="800">
    <Grid>
        <TextBlock>Hello World!</TextBlock>
    </Grid>
</plugin:Panel>
```

The Panel.cs file contains the following:

```csharp
using System;

namespace HelloWorldPlugin
{
	/// <summary>
	/// Interaction logic for Panel.xaml
	/// </summary>
	public partial class Panel : FreeDiscussions.Plugin.Panel
	{
		public Panel(Action onClose) : base(onClose)
		{
			InitializeComponent();

		}
	}
}
```

## 4. Testing the plugin.

In order to test the plugin, we set the output path of our plugin to the plugin's folder of the FreeDiscussions Client.

- In Visual Studio, go to your projects Properties page.
- Go to Build > Output
- Change the Base Output Path to `<PathToTheClient>/plugins`.
- Build your project
- Run the Client

## Congratulations! You have now successfully created your first plugin for the Free Discussions Client!

![Congratulations](https://media3.giphy.com/media/mGK1g88HZRa2FlKGbz/giphy.gif?cid=790b761179ba0375102e188cbbb3a95b6fa09e231a05b39a&rid=giphy.gif&ct=g)
