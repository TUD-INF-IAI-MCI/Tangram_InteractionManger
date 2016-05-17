Tangram_InteractionManger
=========
Interaction manager for interaction on tactile displays based on the BrailleIO framework.


## Intension:

Make the usage of [BrailleIO](https://github.com/TUD-INF-IAI-MCI/BrailleIO) hardware key interaction a bit more easy. Furthermore, provide some basic functionalities for interactions, such as a Braille-keyboard.

**Attention**: this all works at this point only with [BrailleDis 7200]( http://web.metec-ag.de/graphik%20display.html)-like Pin-Matrix devices or with the `ShowOffAdapter` of [BrailleIO](https://github.com/TUD-INF-IAI-MCI/BrailleIO).

## Components

### InteractionManager

Singleton construct and center class for interaction handling. 

Have its own public available `BrailleKeyboardInput` for enabling Braille text input and basic text-work options.

Has an `InteractionMode` (Flag) that can be a combination of the following modes to identify how the interactions should be interpreted:

InteractionMode| Value | Description
------------ | ------------- | -------------
None | 0 | No mode is set
Normal | 1 | Normal mode - all inputs will be forwarded
Braille | 2 | Inputs will be interpreted as Braille keyboard inputs
Gesture | 4 | Gesture mode was activated - touch inputs should be collected for a gesture interpretation
Manipulation | 8 | Manipulation mode was activated - elements e.g. in a document will be manipulated directly by inputs

#### How it works

The InteractionManager is a mediator between interactions of an `IBrailleIOAdapter` from the [BrailleIO framework] ](https://github.com/TUD-INF-IAI-MCI/BrailleIO) and handlers for button-, **button-combinations**, gesture or Braille-text-inputs.

Therefor you have to register available input devices (`BrailleIO. IBrailleIOAdapter`) to the InteractionManager by using the method

``` C#
public bool AddNewDevice(IBrailleIOAdapter device)
```

Automatically the `InteractionManager` registers to the devices’ input events, interpret them and forward them to listeners to handle them.


### ScriptFunctionProxy

The `ScriptFunctionProxy` allows adding specialized interaction handlers that can be activated and deactivate based on the current application state or context. 

The `ScriptFunctionProxy` forwards interaction events, such as button combinations etc., to registered specialized function proxies implementing the `IInteractionContextProxy` interface. Those specialized function proxies have to take care about their activation and deactivation based on the current application context. The specialized function proxies are ordered and are called in a cascading way. So the can handle the same input command but are called one after another based on the ordering (ZIndex – as higher as earlier it is called).

It has to be initialized with an `InteractionManager` instance to register to the available interaction events. 

## How to use:


--	TODO: build a small workflow

### Basic Set-Up

Set up basic global variables for the `InteractionManager` 

``` C#
readonly InteractionManager interactionManager = InteractionManager.Instance;
```

Set up a new [BrailleIO.IBrailleIOAdapter](https://github.com/TUD-INF-IAI-MCI/BrailleIO) and add it to the interaction manager as an input device

``` C#
// see BrailleIO framework for further explanation
BrailleIOMediator io = BrailleIOMediator.Instance;

// set up a example/debug pin-matrix-device
io.AdapterManager = new ShowOffBrailleIOAdapterManager();
AbstractBrailleIOAdapterBase adapter = io.AdapterManager.ActiveAdapter as AbstractBrailleIOAdapterBase;

// add the adapter to the interaction manager as input device
interactionManager.AddNewDevice(adapter); 
```

### Handling by the ScriptFunctionProxy

For handling inputs by the interaction manger through the extensible `ScriptFunctionProxy` you can set up as follows:

Set up basic global variables for the ` ScriptFunctionProxy ` 

``` C#
readonly ScriptFunctionProxy functionProxy = ScriptFunctionProxy.Instance;
```

After getting the singleton instance, you have to connect the function proxy with the InteractionManager who delivers the inputs.

``` C#
functionProxy.Initialize(interactionManager);
```

To add a specialized function handler for handling inputs an implemention of the `IInteractionContextProxy` interface can be added to the global `ScriptFunctionProxy`

``` C#
IInteractionContextProxy specialProxy = null // you have to build one to handle your specific commands in a specific context

functionProxy.AddProxy(specialProxy);
 ```

### Handle events in a specialized script function proxy

For handling interaction events of the `InteractionManager` the interfaces for `tud.mci.tangram.TangramLector.IInteractionContextProxy` and `tud.mci.tangram.TangramLector.IInteractionEventProxy` have to be implemented.

For this purpose an abstract basic implement exists: `tud.mci.tangram.TangramLector.SpecializedFunctionProxies.AbstractSpecializedFunctionProxyBase`. Here you can override the functionalities you need.

``` C#
public class ExampleSpecializedFunctionProxy : AbstractSpecializedFunctionProxyBase
{
	// override the handler for button combinations
	protected override void im_ButtonCombinationReleased(object sender, ButtonReleasedEventArgs e)
	{
		// check if this function proxy is active and should handle this event
		if (Active)
		{
			// handle a generic button combination of 4 keys reeased
			if (e.ReleasedGenericKeys.Intersect(new List<String> { "k2", "k3", "k4", "k5" }).ToList().Count == 4)
			{
				//TODO: handle whatever should be done 
				e.Cancel = true; // setting the cancel field to TRUE, stops the cascading forwarding to following further specialized function proxies!
			}
		}
	}
}
```


### Submodules
Keep attention to get and update all the embedded submodules. Sometimes it seems to be necessary to update the submodules resources – do not submit this to the main branch of submodule

**TIP:** When doing submodule updates use the option **recursive** to get all submodules of the submodules!!! (`git submodule update --init --recursive`)

**TIP:** For checking out the repository, do also use the **recursice** option (`git clone --recursive git://github.com/mysociety/whatdotheyknow.git`)

- [DotNet_AudioRenderer](https://github.com/TUD-INF-IAI-MCI/DotNet_AudioRenderer) - audio renderer for speech output
- [DotNet_Extensibility](https://github.com/TUD-INF-IAI-MCI/DotNet_Extensibility) - extension framework for easy extensibility
- [DotNet_LanguageLocalization](https://github.com/TUD-INF-IAI-MCI/DotNet_LanguageLocalization) - localizing framework for different languages
- [DotNet_Logger](https://github.com/TUD-INF-IAI-MCI/DotNet_Logger) - logger to store logging information with timestamp
- [Tangram_Interfaces](https://github.com/TUD-INF-IAI-MCI/Tangram_Interfaces) - interfaces of the tangram framework for inter-project-compatibility
       - [BrailleIO](https://github.com/TUD-INF-IAI-MCI/BrailleIO) - tactile display abstraction framework


## You want to know more?

--	TODO: build help from code doc

For getting a very detailed overview use the [code documentation section](/Help/index.html) of this project.

