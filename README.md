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

The InteractionManager is a mediator between interactions of an `IBrailleIOAdapter` from the [BrailleIO framework](https://github.com/TUD-INF-IAI-MCI/BrailleIO) and handlers for button-, **button-combinations**, gesture or Braille-text-inputs.

Therefor you have to register available input devices (`BrailleIO. IBrailleIOAdapter`) to the InteractionManager by using the method

``` C#
public bool AddNewDevice(IBrailleIOAdapter device)
```

Automatically the `InteractionManager` registers to the devices’ input events, interpret them and forward them to listeners to handle them.


### ScriptFunctionProxy

The `ScriptFunctionProxy` allows adding specialized interaction handlers that can be activated and deactivate based on the current application state or context. 

The `ScriptFunctionProxy` forwards interaction events, such as button combinations etc., to registered specialized function proxies implementing the `IInteractionContextProxy` interface. Those specialized function proxies have to take care about their activation and deactivation based on the current application context. The specialized function proxies are ordered and are called in a cascading way. So the can handle the same input command but are called one after another based on the ordering (`ZIndex` – as higher as earlier it is called).

It has to be initialized with an `InteractionManager` instance to register to the available interaction events. 

### BrailleKeyboard

The BrailleKeyboard allows translating a Unicode sign or a character into a dot-pattern representation and vice-versa. Basically loaded is the Unicode Braille-char part that will be overwritten by the given *.cti translation table, based on the definitions of the [**liblouis** project]( https://github.com/liblouis).

#### How to use

Set up a new BrailleKeyboard and load a Braille-Char to dot-pattern translation table

``` C#
// in this example the German translation table is loaded
public readonly Control.BrailleKeyboard BrailleKeyboard = new tud.mci.tangram.TangramLector.Control.BrailleKeyboard(tud.mci.tangram.TangramLector.Control.BrailleKeyboard.GetCurrentDllDirectory() + @"/config/tables/de-chardefs8.cti");
```

Now you can ask for a character from a specific dot pattern

``` C#
String c = BrailleKeyboard.GetCharFromDots("14"); // will return "c"
```

Or you can ask for the dot pattern of a specific character

``` C#
String pattern = BrailleKeyboard.GetDotsForChar('g'); // will return "1245"
```

### BrailleKeyboardInput

The `BrailleKeyboardInput` is some kind of *virtual text-input field* that can be filled with text, holds a cursor/caret for defining the input position and allows for other text-works operations.

The `BrailleKeyboardInput` is automatically registered to the Braille-keyboard events of the global singleton `ScriptFunctionProxy` instance:

``` C#
        /// <summary>
        /// Occurs when a single BrailleKeyboard letter was entered.
        /// </summary>
        public event EventHandler<BrailleKeyboardEventArgs> BrailleKeyboardKey;
        /// <summary>
        /// Occurs when a complex braille keyboard command was entered.
        /// </summary>
        public event EventHandler<BrailleKeyboardCommandEventArgs> BrailleKeyboardCommand;
```



#### Caret handling

The input-cursor or caret is the position where text is entered if no text-input position is defined. The caret can be moved or set to a fixed position inside the text.

Actions can be registered to hook the caret moving functions (left, right, up, down, etc.) to adapt the handling if necessary.


### BrailleKeyboard handling in the InteractionManager

The Braille keyboard available at the `InteractionManager` in combination with the `ScriptFunctionProxy` interprets button combinations from the BrailleDis like key settings to either Braille charters or Keyboard interaction commands.

The buttons "k1","k2","k3","k4","k5","k6","k7" and "k8" (of a Braille-keyboard-like button-layout) will be transformed into the corresponding dot in the Braille character dot-pattern.

Several standard key commands for text-work are defined. Therefor some additional function keys are necessary (commonly they are lying in between the both button groups for the left and the right hand are activated with the thumbs).

![Example of a BrailleKeyboard with 12 keys (from left to right): k7, k3, k2, k1, l, lr, rl, r, k4, k5, k6, k8](/doc_imgs/Braille-Keyboard.png)


#### Commands

Description | Button code | Implemented in `tud.mci.tangram.TangramLector.Control.BrailleKeyboardInput`?
------------|-------------|--------------
`Return` / next Line | `k8` | yes ("\r\n")
`Space` | `lr` or `rl` | yes
`Del. ` | `Space` + `k5` + `k6` | yes
`Backspace` | `lr` + `rl` | yes
`ESC` | `Space` + `k7` | no
`Ctrl. ` commands | ... + `k7` + `k8` (e.g. `Ctrl.`+C = `k1` + `k4` + `k7` + `k8`) | only `Ctrl.`+V
Mark text | `Ctr.`+T to set start- and endpoint = `k2` + `k3` + `k4` + `k5` + `k7` + `k8` | no
Unmark text | `ESC` = `Space` + `k7` | no
Move cursor left | `Space` + `k1` | yes
Move cursor right | `Space` + `k4` | yes
Move cursor up | `Space` + `k2` | partial (-36 chars)
Move cursor down | `Space` + `k5` | partial (+36 chars)
Page up (25 lined up) | `Space` + `k3` | no
Page down (25 lines down) | `Space` + `k6` | no
`Pos1` (start of line) | `Space` + `k1` + `k2` | partial (Start pos of whole text)
`End` (end of line) | `Space` + `k5` + `k5` | partial (last pos of whole text)
`Tab` | `Space` + `k4` + `k5` + `k6` | yes
`Shift` + `Tab` | `Space` + `k1` + `k2` + `k3` | yes


#### How to use

The `BrailleKeyboardInput` of the `InteractionManager` instance is global available by calling

``` C#
BrailleKeyboardInput input = InteractionManager.Instance.BKI;
```

To activate the Braille interpretation you have to switch the to the Braille mode

``` C#
InteractionManager.Instance.Mode = InteractionMode.Braille
```

**ATTENTION** you have to clear the `BrailleKeyboardInput` before allowing to write a new text. Otherwise, the currently set text will be extended.

``` C#
// get the current text input
String text = InteractionManager.Instance.BKI.Input;

// clear the text input
InteractionManager.Instance.BKI.Reset();
```

## How to use:

The basic sequence of handling user interactions is displayed in the following diagram:

![Sequence of handling user interactions (buttons/gesture) by the interaction manager and the connected proxies](/doc_imgs/UML-InteractionManager.png)



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

- [DotNet_AudioRenderer](https://github.com/TUD-INF-IAI-MCI/DotNet_AudioRenderer) - audio renderer for speech output
- [DotNet_Extensibility](https://github.com/TUD-INF-IAI-MCI/DotNet_Extensibility) - extension framework for easy extensibility
- [DotNet_LanguageLocalization](https://github.com/TUD-INF-IAI-MCI/DotNet_LanguageLocalization) - localizing framework for different languages
- [DotNet_Logger](https://github.com/TUD-INF-IAI-MCI/DotNet_Logger) - logger to store logging information with timestamp
- [Tangram_Interfaces](https://github.com/TUD-INF-IAI-MCI/Tangram_Interfaces) - interfaces of the tangram framework for inter-project-compatibility
- [BrailleIO](https://github.com/TUD-INF-IAI-MCI/BrailleIO) - tactile display abstraction framework


## You want to know more?

--	TODO: build help from code doc

For getting a very detailed overview use the [code documentation section](/Help/index.html) of this project.

