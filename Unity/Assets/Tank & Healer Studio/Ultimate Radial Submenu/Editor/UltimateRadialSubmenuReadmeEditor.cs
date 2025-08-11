/* UltimateRadialSubmenuReadmeEditor.cs */
/* Written by Kaz */
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
[CustomEditor( typeof( UltimateRadialSubmenuReadme ) )]
public class UltimateRadialSubmenuReadmeEditor : Editor
{
	static UltimateRadialSubmenuReadme readme;

	// LAYOUT STYLES //
	const string linkColor = "0062ff";
	const string Indent = "    ";
	int sectionSpace = 20;
	int itemHeaderSpace = 10;
	int paragraphSpace = 5;
	GUIStyle titleStyle = new GUIStyle();
	GUIStyle sectionHeaderStyle = new GUIStyle();
	GUIStyle itemHeaderStyle = new GUIStyle();
	GUIStyle paragraphStyle = new GUIStyle();
	GUIStyle versionStyle = new GUIStyle();

	// PAGE INFORMATION //
	class PageInformation
	{
		public string pageName = "";
		public delegate void TargetMethod ();
		public TargetMethod targetMethod;
	}
	static List<PageInformation> pageHistory = new List<PageInformation>();
	static PageInformation[] AllPages = new PageInformation[]
	{
		// MAIN MENU - 0 //
		new PageInformation()
		{
			pageName = "Product Manual"
		},
		// Getting Started - 1 //
		new PageInformation()
		{
			pageName = "Getting Started"
		},
		// Overview - 2 //
		new PageInformation()
		{
			pageName = "Overview"
		},
		// Documentation - 3 //
		new PageInformation()
		{
			pageName = "Documentation"
		},
		// Documentation - URM - 4 //
		new PageInformation()
		{
			pageName = "Documentation"
		},
		// Documentation - URBI - 5 //
		new PageInformation()
		{
			pageName = "Documentation"
		},
		// Version History - 6 //
		new PageInformation()
		{
			pageName = "Version History"
		},
		// Important Change - 7 //
		new PageInformation()
		{
			pageName = "Important Change"
		},
		// Thank You! - 8 //
		new PageInformation()
		{
			pageName = "Thank You!"
		},
		// Settings - 9 //
		new PageInformation()
		{
			pageName = "Settings"
		},
	};

	// OVERVIEW //
	bool showRadialMenuPositioning = false, showRadialMenuOptions = false, showButtonInteraction;
	bool showRadialButtonList = false, showScriptReference = false;

	// DOCUMENTATION //
	class DocumentationInfo
	{
		public string functionName = "";
		public bool showMore = false;
		public string[] parameter;
		public string returnType = "";
		public string description = "";
		public string codeExample = "";
	}
	const string radialMenuName = "OptionsMenu";
	DocumentationInfo[] UltimateRadialSubmenu_StaticFunctions = new DocumentationInfo[]
	{
		// ReturnComponent
		new DocumentationInfo()
		{
			functionName = "ReturnComponent()",
			returnType = "UltimateRadialSubmenu",
			parameter = new string[]
			{
				"string radialMenuName - The registered name of the Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "This function will return the Ultimate Radial Submenu component that has been registered with the <i>radialMenuName</i> parameter.",
			codeExample = "UltimateRadialSubmenu submenu = UltimateRadialSubmenu.ReturnComponent( \"" + radialMenuName + "\" );"
		},
		// UpdatePositioning
		new DocumentationInfo()
		{
			functionName = "UpdatePositioning()",
			parameter = new string[]
			{
				"string radialMenuName - The string name that the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "Updates the positioning of the submenu buttons.",
			codeExample = "UltimateRadialSubmenu.UpdatePositioning( \"" + radialMenuName + "\" );"
		},
		// RegisterButton
		new DocumentationInfo()
		{
			functionName = "RegisterButton()",
			parameter = new string[]
			{
				"string radialMenuName - The registered name of the Ultimate Radial Menu that the targeted submenu is associated with.",
				"Action(+2 Overloads: <int>, <string>) ButtonCallback - The action callback to be called when the button is interacted with. This is your custom function that you want called when this button is interacted with.",
				"UltimateRadialSubButtonInfo buttonInfo - The button information to apply to the button. After sending in this ButtonInfo, you will then be able to use this class to communicate with the submenu.",
				"int buttonIndex = -1 - Optional parameter that allows the user to determine where this information will get registered to on the menu. Leave out this parameter to register the provided information to the first available button.",
			},
			description = "Registers the provided information to the targeted Ultimate Radial Submenu in your scene.",
			codeExample = "UltimateRadialSubmenu.RegisterButton( \"" + radialMenuName + "\", MyCallbackFunction, myButtonInfo );"
		},
		// Enable
		new DocumentationInfo()
		{
			functionName = "Enable()",
			parameter = new string[]
			{
				"string radialMenuName - The string name that the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "Enables the targeted Ultimate Radial Submenu so that it can be interacted with.",
			codeExample = "UltimateRadialSubmenu.Enable( \"" + radialMenuName + "\" );"
		},
		// Disable
		new DocumentationInfo()
		{
			functionName = "Disable()",
			parameter = new string[]
			{
				"string radialMenuName - The string name that the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "Disables the targeted Ultimate Radial Submenu so that it can not be interacted with.",
			codeExample = "UltimateRadialSubmenu.Disable( \"" + radialMenuName + "\" );"
		},
		// RemoveButton
		new DocumentationInfo()
		{
			functionName = "RemoveButton()",
			parameter = new string[]
			{
				"string radialMenuName - The string name that the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
				"int buttonIndex - The index to remove the sub button at.",
			},
			description = "Removes the sub button at the targeted index.",
			codeExample = "UltimateRadialSubmenu.RemoveButton( \"" + radialMenuName + "\", 0 );"
		},
		// ClearMenu
		new DocumentationInfo()
		{
			functionName = "ClearMenu()",
			parameter = new string[]
			{
				"string radialMenuName - The registered name of the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "Clears all of the buttons from the submenu.",
			codeExample = "UltimateRadialSubmenu.ClearMenu( \"" + radialMenuName + "\" );"
		},
		// RegisterStaticInformation
		new DocumentationInfo()
		{
			functionName = "RegisterStaticInformation()",
			parameter = new string[]
			{
				"string radialMenuName - The registered name of the targeted Ultimate Radial Menu that the targeted submenu is associated with.",
			},
			description = "Registers the static button information if the user had any. This will happen automatically, but will need to be called if you clear the menu and want the static buttons back on the submenu.",
			codeExample = "UltimateRadialSubmenu.RegisterStaticInformation( \"" + radialMenuName + "\" );"
		},
	};
	DocumentationInfo[] UltimateRadialSubmenu_PublicFunctions = new DocumentationInfo[]
	{
		// UpdatePositioning
		new DocumentationInfo()
		{
			functionName = "UpdatePositioning()",
			description = "Updates the positioning of the submenu according to the user's options.",
			codeExample = "submenu.UpdatePositioning();"
		},
		// RegisterButton
		new DocumentationInfo()
		{
			functionName = "RegisterButton()",
			parameter = new string[]
			{
				"Action(+2 Overloads: <int>, <string>) ButtonCallback - The action callback to be called when the button is interacted with.",
				"UltimateRadialSubButtonInfo buttonInfo - The button information to apply to the button.",
				"int buttonIndex = -1 - [OPTIONAL] Allows the user to determine where this information will get registered to on the menu. Leave out this parameter to register the provided information to the first available button.",
			},
			description = "Registered the provided information to the targeted Ultimate Radial Submenu.",
			codeExample = "submenu.RegisterButton( MyCallbackFunction, myButtonInfo );"
		},
		// Enable
		new DocumentationInfo()
		{
			functionName = "Enable()",
			description = "Enables the Ultimate Radial Submenu so that it can be interacted with.",
			codeExample = "submenu.Enable();"
		},
		// Disable
		new DocumentationInfo()
		{
			functionName = "Disable()",
			description = "Disables the Ultimate Radial Submenu so that it can not be interacted with.",
			codeExample = "submenu.Disable();"
		},
		// ClearMenu
		new DocumentationInfo()
		{
			functionName = "ClearMenu()",
			description = "Clears all of the buttons from the submenu.",
			codeExample = "submenu.ClearMenu();"
		},
		// RemoveButton
		new DocumentationInfo()
		{
			functionName = "RemoveButton()",
			parameter = new string[]
			{
				"int buttonIndex - The index of the button to be removed.",
			},
			description = "Removes the button at the targeted index.",
			codeExample = "submenu.RemoveButton( 0 );"
		},
		// RegisterStaticInformation
		new DocumentationInfo()
		{
			functionName = "RegisterStaticInformation()",
			description = "Registers the static button information if the user had any. This will happen automatically, but will need to be called if you clear the menu and want the static buttons back on the submenu.",
			codeExample = "submenu.RegisterStaticInformation();"
		},
	};
	DocumentationInfo[] UltimateRadialSubButtonInfo_PublicFunctions = new DocumentationInfo[]
	{
		// UpdateIcon
		new DocumentationInfo()
		{
			functionName = "UpdateIcon()",
			parameter = new string[]
			{
				"Sprite newIcon - The new sprite to assign as the icon for the button.",
			},
			description = "Assigns a new sprite to the button's icon image.",
			codeExample = "buttonInfo.UpdateIcon( myNewIcon );"
		},
		// UpdateText
		new DocumentationInfo()
		{
			functionName = "UpdateText()",
			parameter = new string[]
			{
				"string newText - The new string to apply to the button.",
			},
			description = "Applies a new string to the button's text component.",
			codeExample = "buttonInfo.UpdateText( \"New Text\" );"
		},
		// UpdateDescription
		new DocumentationInfo()
		{
			functionName = "UpdateDescription()",
			parameter = new string[]
			{
				"string newDescription - The new string to apply to the button's description.",
			},
			description = "Updates the button with a new description.",
			codeExample = "buttonInfo.UpdateDescription( \"New Description\" );"
		},
		// Enable
		new DocumentationInfo()
		{
			functionName = "Enable()",
			description = "Enables the button.",
			codeExample = "buttonInfo.Enable();"
		},
		// Disable
		new DocumentationInfo()
		{
			functionName = "Disable()",
			description = "Disables the button.",
			codeExample = "buttonInfo.Disable();"
		},
		// Selected
		new DocumentationInfo()
		{
			functionName = "Selected",
			description = "Gets or sets the selected state of this button.",
			codeExample = "buttonInfo.Selected = false;"
		},
		// Select
		new DocumentationInfo()
		{
			functionName = "Select()",
			parameter = new string[]
			{
				"bool exclusive = false - [OPTIONAL] Allows you to make sure that only this button is currently in the selected state on the submenu.",
			},
			description = "Selects this button.",
			codeExample = "buttonInfo.Select();"
		},
		// Deselect
		new DocumentationInfo()
		{
			functionName = "Deselect()",
			description = "Deselects this button.",
			codeExample = "buttonInfo.Deselect();"
		},
		// ToggleSelect
		new DocumentationInfo()
		{
			functionName = "ToggleSelect()",
			description = "Toggles the selection state of this button.",
			codeExample = "buttonInfo.ToggleSelect();"
		},
		// Remove
		new DocumentationInfo()
		{
			functionName = "Remove()",
			description = "Removes this button from the submenu.",
			codeExample = "buttonInfo.Remove();"
		},
		// Registered
		new DocumentationInfo()
		{
			functionName = "Registered()",
			returnType = "bool",
			description = "Returns the state of this information being registered on a submenu.",
			codeExample = "if( buttonInfo.Registered() )\n{\n" + Indent + "// The buttonInfo is assigned to a submenu, do something here...\n}"
		},
	};
	DocumentationInfo[] UltimateRadialSubmenu_Events = new DocumentationInfo[]
	{
		// OnButtonEnter
		new DocumentationInfo()
		{
			functionName = "OnButtonEnter",
			parameter = new string[]
			{
				"int - The index of the button that was entered.",
			},
			description = "This event is called when a new button is entered.",
			codeExample = "submenu.OnButtonEnter += YourOnButtonEnterFunction;",
		},
		// OnButtonExit
		new DocumentationInfo()
		{
			functionName = "OnButtonExit",
			parameter = new string[]
			{
				"int - The index of the button that was exited.",
			},
			description = "This event is called when the input exits the button that recently had focus.",
			codeExample = "submenu.OnButtonExit += YourOnButtonExitFunction;",
		},
		// OnButtonInputDown
		new DocumentationInfo()
		{
			functionName = "OnButtonInputDown",
			parameter = new string[]
			{
				"int - The index of the button that received the down input.",
			},
			description = "This callback will be called when the input has been pressed on a button.",
			codeExample = "submenu.OnButtonInputDown += YourOnButtonInputDownFunction;",
		},
		// OnButtonInputUp
		new DocumentationInfo()
		{
			functionName = "OnButtonInputUp",
			parameter = new string[]
			{
				"int - The index of the button that received the up input.",
			},
			description = "This callback will be called when the input has been released on a button.",
			codeExample = "submenu.OnButtonInputUp += YourOnButtonInputUpFunction;",
		},
		// OnButtonInteract
		new DocumentationInfo()
		{
			functionName = "OnButtonInteract",
			parameter = new string[]
			{
				"int - The index of the button that has been interacted with.",
			},
			description = "This callback will be called when a button has been interacted with.",
			codeExample = "submenu.OnButtonInteract += YourOnButtonInteractFunction;",
		},
		// OnButtonSelected
		new DocumentationInfo()
		{
			functionName = "OnButtonSelected",
			parameter = new string[]
			{
				"int - The index of the button that has been selected.",
			},
			description = "This callback will be called when a button has been selected.",
			codeExample = "submenu.OnButtonSelected += YourOnButtonSelectedFunction;",
		},
		// OnLostFocus
		new DocumentationInfo()
		{
			functionName = "OnLostFocus",
			description = "This callback will be called when the submenu has lost input focus.",
			codeExample = "submenu.OnLostFocus += YourOnLostFocusFunction;",
		},
		// OnEnabled
		new DocumentationInfo()
		{
			functionName = "OnEnabled",
			description = "This callback will be called when the submenu has been enabled.",
			codeExample = "submenu.OnEnabled += YourOnEnabledFunction;",
		},
		// OnDisabled
		new DocumentationInfo()
		{
			functionName = "OnDisabled",
			description = "This callback will be called when the submenu has been disabled.",
			codeExample = "submenu.OnDisabled += YourOnDisabledFunction;",
		},
		// OnUpdatePositioning
		new DocumentationInfo()
		{
			functionName = "OnUpdatePositioning",
			description = "This callback will be called when the submenu's positioning has been updated.",
			codeExample = "submenu.OnUpdatePositioning += YourOnUpdatePositioningFunction;",
		},
		// OnProcessInput
		new DocumentationInfo()
		{
			functionName = "OnProcessInput",
			parameter = new string[]
			{
				"Vector2 - The current input value relative to the center of the submenu.",
				"float - The distance that the input is from the center of the submenu.",
				"bool - The state of the input being down this frame.",
				"bool - The state of the input being released this frame.",
			},
			description = "This callback will be called when then submenu is receiving input.",
			codeExample = "submenu.OnProcessInput += YourOnProcessInputFunction;",
		},
	};

	// END PAGE COMMENTS //
	class EndPageComment
	{
		public string comment = "";
		public string url = "";
	}
	EndPageComment[] endPageComments = new EndPageComment[]
	{
		new EndPageComment()
		{
			comment = $"Enjoying the Ultimate Radial Submenu? Leave us a review on the <b><color=#{linkColor}>Unity Asset Store</color></b>!",
			url = "https://assetstore.unity.com/packages/slug/188051"
		},
		new EndPageComment()
		{
			comment = $"Looking for a mobile joystick for your game? Check out the <b><color=#{linkColor}>Ultimate Joystick</color></b>!",
			url = "https://www.tankandhealerstudio.com/ultimate-joystick.html"
		},
		new EndPageComment()
		{
			comment = $"Looking for a complete chat box for your game? Check out the <b><color=#{linkColor}>Ultimate Chat Box</color></b>!",
			url = "https://www.tankandhealerstudio.com/ultimate-chat-box.html"
		},
		new EndPageComment()
		{
			comment = $"Check out our <b><color=#{linkColor}>other products</color></b>!",
			url = "https://www.tankandhealerstudio.com/assets.html"
		},
	};
	int randomComment = 0;


	static UltimateRadialSubmenuReadmeEditor ()
	{
		EditorApplication.update += WaitForCompile;
	}

	static void WaitForCompile ()
	{
		if( EditorApplication.isCompiling )
			return;

		EditorApplication.update -= WaitForCompile;

		if( !EditorPrefs.HasKey( "UltimateRadialSubmenuVersion" ) )
		{
			EditorPrefs.SetInt( "UltimateRadialSubmenuVersion", UltimateRadialSubmenuReadme.ImportantChange );
			SelectReadmeFile();

			if( readme != null )
				NavigateForward( 8 );
		}
		else if( EditorPrefs.GetInt( "UltimateRadialSubmenuVersion" ) < UltimateRadialSubmenuReadme.ImportantChange )
		{
			EditorPrefs.SetInt( "UltimateRadialSubmenuVersion", UltimateRadialSubmenuReadme.ImportantChange );
			SelectReadmeFile();

			if( readme != null )
				NavigateForward( 7 );
		}
	}

	void OnEnable ()
	{
		readme = ( UltimateRadialSubmenuReadme )target;

		if( !EditorPrefs.HasKey( "URM_ColorHexSetup" ) )
		{
			EditorPrefs.SetBool( "URM_ColorHexSetup", true );
			ResetColors();
		}

		ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorDefaultHex" ), out readme.colorDefault );
		ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorValueChangedHex" ), out readme.colorValueChanged );
		ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorButtonSelectedHex" ), out readme.colorButtonSelected );
		ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorButtonUnselectedHex" ), out readme.colorButtonUnselected );
		ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorTextBoxHex" ), out readme.colorTextBox );

		AllPages[ 0 ].targetMethod = MainPage;
		AllPages[ 1 ].targetMethod = GettingStarted;
		AllPages[ 2 ].targetMethod = Overview;
		AllPages[ 3 ].targetMethod = Documentation;
		AllPages[ 4 ].targetMethod = Documentation_UltimateRadialSubmenu;
		AllPages[ 5 ].targetMethod = Documentation_UltimateRadialSubButtonInfo;
		AllPages[ 6 ].targetMethod = VersionHistory;
		AllPages[ 7 ].targetMethod = ImportantChange;
		AllPages[ 8 ].targetMethod = ThankYou;
		AllPages[ 9 ].targetMethod = Settings;
		
		pageHistory = new List<PageInformation>();
		for( int i = 0; i < readme.pageHistory.Count; i++ )
			pageHistory.Add( AllPages[ readme.pageHistory[ i ] ] );

		if( !pageHistory.Contains( AllPages[ 0 ] ) )
		{
			pageHistory.Insert( 0, AllPages[ 0 ] );
			readme.pageHistory.Insert( 0, 0 );
		}

		readme.versionHistory[ 0 ].showMore = true;

		randomComment = Random.Range( 0, endPageComments.Length );

		Undo.undoRedoPerformed += UndoRedoCallback;
	}

	void OnDisable ()
	{
		// Remove the UndoRedoCallback from the Undo event.
		Undo.undoRedoPerformed -= UndoRedoCallback;
	}

	void UndoRedoCallback ()
	{
		if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ 9 ] )
			return;

		EditorPrefs.SetString( "URM_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorDefault ) );
		EditorPrefs.SetString( "URM_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorValueChanged ) );
		EditorPrefs.SetString( "URM_ColorButtonSelectedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorButtonSelected ) );
		EditorPrefs.SetString( "URM_ColorButtonUnselectedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorButtonUnselected ) );
		EditorPrefs.SetString( "URM_ColorTextBoxHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorTextBox ) );
	}

	protected override void OnHeaderGUI ()
	{
		UltimateRadialSubmenuReadme readme = ( UltimateRadialSubmenuReadme )target;

		var iconWidth = Mathf.Min( EditorGUIUtility.currentViewWidth, 350f );

		if( readme.icon == null )
			return;

		Vector2 ratio = new Vector2( readme.icon.width, readme.icon.height ) / ( readme.icon.width > readme.icon.height ? readme.icon.width : readme.icon.height );

		GUILayout.BeginHorizontal( "In BigTitle" );
		{
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();
			GUILayout.Label( readme.icon, GUILayout.Width( iconWidth * ratio.x ), GUILayout.Height( iconWidth * ratio.y ) );
			GUILayout.Space( -20 );
			if( GUILayout.Button( readme.versionHistory[ 0 ].versionNumber, versionStyle ) && !pageHistory.Contains( AllPages[ 6 ] ) )
				NavigateForward( 6 );
			var rect = GUILayoutUtility.GetLastRect();
			if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ 6 ] )
				EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndHorizontal();
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		paragraphStyle = new GUIStyle( EditorStyles.label ) { wordWrap = true, richText = true, fontSize = 12 };
		itemHeaderStyle = new GUIStyle( paragraphStyle ) { fontSize = 12, fontStyle = FontStyle.Bold };
		sectionHeaderStyle = new GUIStyle( paragraphStyle ) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
		titleStyle = new GUIStyle( paragraphStyle ) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
		versionStyle = new GUIStyle( paragraphStyle ) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

		paragraphStyle.active.textColor = paragraphStyle.normal.textColor;

		// SETTINGS BUTTON //
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button( readme.settings, versionStyle, GUILayout.Width( 24 ), GUILayout.Height( 24 ) ) && !pageHistory.Contains( AllPages[ 9 ] ) )
			NavigateForward( 9 );
		var rect = GUILayoutUtility.GetLastRect();
		if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ 9 ] )
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
		GUILayout.EndHorizontal();
		GUILayout.Space( -24 );
		GUILayout.EndVertical();

		// BACK BUTTON //
		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup( pageHistory.Count <= 1 );
		if( GUILayout.Button( "◄", titleStyle, GUILayout.Width( 24 ) ) )
			NavigateBack();
		if( pageHistory.Count > 1 )
		{
			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
		}
		EditorGUI.EndDisabledGroup();
		GUILayout.Space( -24 );

		// PAGE TITLE //
		GUILayout.FlexibleSpace();
		EditorGUILayout.LabelField( pageHistory[ pageHistory.Count - 1 ].pageName, titleStyle );
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		// DISPLAY PAGE //
		pageHistory[ pageHistory.Count - 1 ].targetMethod?.Invoke();
	}

	static void NavigateBack ()
	{
		readme.pageHistory.RemoveAt( readme.pageHistory.Count - 1 );
		pageHistory.RemoveAt( pageHistory.Count - 1 );
		GUI.FocusControl( "" );

		readme.scrollValue = Vector2.zero;

		if( readme.pageHistory.Count == 1 )
			EditorUtility.SetDirty( readme );
	}

	static void NavigateForward ( int menuIndex )
	{
		pageHistory.Add( AllPages[ menuIndex ] );
		GUI.FocusControl( "" );

		readme.pageHistory.Add( menuIndex );
		readme.scrollValue = Vector2.zero;
	}

	void MainPage ()
	{
		EditorGUILayout.LabelField( "We hope that you are enjoying using the Ultimate Radial Submenu in your project! Here is a list of helpful resources for this asset:", paragraphStyle );

		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • Read the <b><color=#{linkColor}>Getting Started</color></b> section of this README!", paragraphStyle ) )
			NavigateForward( 1 );
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • To learn more about the sections of the inspector, read the <b><color=#{linkColor}>Overview</color></b> section!", paragraphStyle ) )
			NavigateForward( 2 );
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • Check out the <b><color=#{linkColor}>Documentation</color></b> section!", paragraphStyle ) )
			NavigateForward( 3 );
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
		
		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • Join our <b><color=#{linkColor}>Discord Server</color></b> so that you can get live help from us and other community members.", paragraphStyle ) )
		{
			Debug.Log( "Ultimate Radial Submenu\nOpening Tank & Healer Studio Discord Server" );
			Application.OpenURL( "https://discord.gg/YrtEHRqw6y" );
		}
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • <b><color=#{linkColor}>Contact Us</color></b> directly with your issue! We'll try to help you out as much as we can.", paragraphStyle ) )
		{
			Debug.Log( "Ultimate Radial Submenu\nOpening Online Contact Form" );
			Application.OpenURL( "https://www.tankandhealerstudio.com/contact-us.html" );
		}
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		EditorGUILayout.Space();

		if( GUILayout.Button( $"  • Using <b><color=#{linkColor}>TextMeshPro</color></b>? You can import the TextMeshPro package from the settings page.", paragraphStyle ) )
			NavigateForward( 9 );
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "Now you have the tools you need to get the Ultimate Radial Submenu working in your project. Now get out there and make your awesome game!", paragraphStyle );

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "Happy Game Making,\n" + Indent + "Tank & Healer Studio", paragraphStyle );

		GUILayout.Space( 20 );

		GUILayout.FlexibleSpace();

		if( GUILayout.Button( endPageComments[ randomComment ].comment, paragraphStyle ) )
			Application.OpenURL( endPageComments[ randomComment ].url );
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
	}

	void GettingStarted ()
	{
		EditorGUILayout.LabelField( "Video Introduction", sectionHeaderStyle );

		if( GUILayout.Button( $"{Indent}Although this package is very similar to the main Ultimate Radial Menu package, please take just a moment to view the <b><color=#{linkColor}>Introduction Video</color></b> from our website for the Ultimate Radial Submenu. This video will help you to get started with this submenu add-on and help you get it working in your project as fast as possible.", paragraphStyle ) )
		{
			Debug.Log( "Ultimate Radial Submenu\nOpening Introduction Video" );
			Application.OpenURL( "https://youtu.be/55ko0q7t-Nw" );
		}
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		GUILayout.Space( sectionSpace );

		EditorGUILayout.LabelField( "Written Introduction", sectionHeaderStyle );

		EditorGUILayout.LabelField( Indent + "The Ultimate Radial Submenu is designed to be as similar as possible to the main Ultimate Radial Menu component so that it is the easiest and most straight forward that it can be.", paragraphStyle );
		
		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "To begin we'll look at how to simply create an Ultimate Radial Submenu in your scene. After that we will go over how to reference the Ultimate Radial Submenu in your custom scripts. Let's begin!", paragraphStyle );

		GUILayout.Space( sectionSpace );

		EditorGUILayout.LabelField( "How To Create", sectionHeaderStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( Indent + "To create an Ultimate Radial Submenu in your scene, simply find the Ultimate Radial Submenu prefab that you would like to add and drag the prefab into the Ultimate Radial Menu that you want to use it with in your scene. If you just drag it into your scene and not in an existing Ultimate Radial Menu, it will find the first Ultimate Radial Menu in the scene and parent itself to that object.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "Prefabs can be found at: Assets/Tank & Healer Studio/Ultimate Radial Submenu/Prefabs.", paragraphStyle );

		GUILayout.Space( sectionSpace );

		EditorGUILayout.LabelField( "How To Reference", sectionHeaderStyle );
		EditorGUILayout.LabelField( Indent + "The submenu add-on is designed to be very similar to the Ultimate Radial Menu asset. It uses the same <b>Callback System</b> as the radial menu, and the <b>ButtonInfo</b> class is very similar as well. Below is a quick refresher of both those topics:", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "Callback System", itemHeaderStyle );
		EditorGUILayout.LabelField( "The Callback System is simply a way for you to subscribe one of your custom methods to be called when the button has been interacted with. When you get to implementing the code, you will see a parameter named: ButtonCallback. This is where you will pass the method that you want called.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "UltimateRadialSubButtonInfo Class", itemHeaderStyle );
		EditorGUILayout.LabelField( "The UltimateRadialSubButtonInfo class is used the exact same as the UltimateRadialButtonInfo from the Ultimate Radial Menu package. It is public and will be used inside of your own custom scripts, just like any other variable inside of your scripts.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "Just like the UltimateRadialButtonInfo class, you will send your UltimateRadialSubButtonInfo to the Ultimate Radial Submenu to update the information like: Name, Description, Key, ID, Icon, and so forth. After sending in your UltimateRadialSubButtonInfo to the submenu, it will then have access to a few functions that you can use to keep information updated, without referencing back to the Ultimate Radial Submenu.", paragraphStyle );
		
		GUILayout.Space( paragraphSpace );

		if( GUILayout.Button( $"For a full list of the functions available, please see the <b><color=#{linkColor}>Documentation</color></b> section of this README.", paragraphStyle ) )
			NavigateForward( 3 );

		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
	}

	void Overview ()
	{
		EditorGUILayout.LabelField( "Sections", sectionHeaderStyle );
		EditorGUILayout.LabelField( Indent + "The display below is mimicking the Ultimate Radial Submenu inspector. Expand each section to learn what each one is designed for.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11 };

		// SUBMENU POSITIONING //
		showRadialMenuPositioning = GUILayout.Toggle( showRadialMenuPositioning, ( showRadialMenuPositioning ? "▼" : "►" ) + "Submenu Positioning", toolbarStyle );
		if( showRadialMenuPositioning )
		{
			GUILayout.Space( paragraphSpace );
			EditorGUILayout.LabelField( "This section handles the positioning and input settings of the submenu.", paragraphStyle );
		}

		EditorGUILayout.Space();

		// SUBMENU OPTIONS //
		showRadialMenuOptions = GUILayout.Toggle( showRadialMenuOptions, ( showRadialMenuOptions ? "▼" : "►" ) + "Submenu Options", toolbarStyle );
		if( showRadialMenuOptions )
		{
			GUILayout.Space( paragraphSpace );
			EditorGUILayout.LabelField( "The options in this section will affect the submenu as a whole and the buttons collectively. The options here determine the game objects used for all the submenu buttons.", paragraphStyle );
		}

		EditorGUILayout.Space();

		// BUTTON INTERACTION //
		showButtonInteraction = GUILayout.Toggle( showButtonInteraction, ( showButtonInteraction ? "▼" : "►" ) + "Button Interaction", toolbarStyle );
		if( showButtonInteraction )
		{
			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "The settings in this section determine how the different states of the buttons look when being interacted with.", paragraphStyle );
		}

		EditorGUILayout.Space();

		// SUB BUTTON LIST //
		showRadialButtonList = GUILayout.Toggle( showRadialButtonList, ( showRadialButtonList ? "▼" : "►" ) + "Sub Button List", toolbarStyle );
		if( showRadialButtonList )
		{
			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "This section is a little different than the Ultimate Radial Menu version. Because the submenu is more than likely going to be used in a dynamic way the default view is for just seeing how the sub buttons will look in each position on the radial menu. However, if you would like to setup all your submenus in the editor, that can also be done in this section by changing the option at the top to: Static. Then you can edit each button individually.", paragraphStyle );
		}

		EditorGUILayout.Space();

		// SCRIPT REFERENCE //
		showScriptReference = GUILayout.Toggle( showScriptReference, ( showScriptReference ? "▼" : "►" ) + "Script Reference", toolbarStyle );
		if( showScriptReference )
		{
			GUILayout.Space( paragraphSpace );
			EditorGUILayout.LabelField( "In this section you will have access to the example code generator to make it easier and faster to get the submenu up and running in your project.", paragraphStyle );
		}

		GUILayout.Space( sectionSpace );

		EditorGUILayout.LabelField( "Tooltips", sectionHeaderStyle );
		EditorGUILayout.LabelField( Indent + "To learn more about each option in these sections, please select the Ultimate Radial Submenu in your scene, and hover over each item to read the provided tooltip.", paragraphStyle );
	}

	void Documentation ()
	{
		EditorGUILayout.LabelField( "Introduction", sectionHeaderStyle );
		EditorGUILayout.LabelField( Indent + "Welcome to the Documentation section. This section will go over the various functions that you have available. Please click on the class to learn more about each function.", paragraphStyle );

		GUILayout.Space( sectionSpace );

		// UltimateRadialSubmenu.cs
		if( GUILayout.Button( "UltimateRadialSubmenu.cs", itemHeaderStyle ) )
		{
			NavigateForward( 4 );
			GUI.FocusControl( "" );
		}
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		// UltimateRadialSubButtonInfo.cs
		if( GUILayout.Button( "UltimateRadialSubButtonInfo.cs", itemHeaderStyle ) )
		{
			NavigateForward( 5 );
			GUI.FocusControl( "" );
		}
		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
	}

	void Documentation_UltimateRadialSubmenu ()
	{
		// STATIC FUNCTIONS //
		EditorGUILayout.LabelField( "Static Functions", sectionHeaderStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( Indent + "The following functions can be referenced from your scripts without the need for an assigned local Ultimate Radial Submenu variable. However, each function must have the targeted Ultimate Radial Submenu name in order to find the correct Ultimate Radial Submenu in the scene. Each example code provided uses the name <i>" + radialMenuName + "</i> as the Radial Menu name registered on your main Ultimate Radial Menu that this submenu is for.", paragraphStyle );
		
		GUILayout.Space( paragraphSpace );

		for( int i = 0; i < UltimateRadialSubmenu_StaticFunctions.Length; i++ )
			ShowDocumentation( UltimateRadialSubmenu_StaticFunctions[ i ] );

		GUILayout.Space( sectionSpace );

		// PUBLIC FUNCTIONS //
		EditorGUILayout.LabelField( "Public Functions", sectionHeaderStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( Indent + "All of the following public functions are only available from a reference to the Ultimate Radial Submenu class. Each example provided relies on having a Ultimate Radial Submenu variable named 'submenu' stored inside your script. When using any of the example code provided, make sure that you have a Ultimate Radial Submenu variable like the one below:", paragraphStyle );

		EditorGUILayout.TextArea( "// Place this in your public variables and assign it in the inspector. //\npublic UltimateRadialSubmenu submenu;", GUI.skin.textArea );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "Please click on the function name to learn more.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		for( int i = 0; i < UltimateRadialSubmenu_PublicFunctions.Length; i++ )
			ShowDocumentation( UltimateRadialSubmenu_PublicFunctions[ i ] );

		GUILayout.Space( sectionSpace );

		// EVENTS //
		EditorGUILayout.LabelField( "Events", sectionHeaderStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( Indent + "These events are called when certain actions are performed on the submenu. By subscribing a function to these events you will be notified with the particular action is performed.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		for( int i = 0; i < UltimateRadialSubmenu_Events.Length; i++ )
			ShowDocumentation( UltimateRadialSubmenu_Events[ i ] );
	}

	void Documentation_UltimateRadialSubButtonInfo ()
	{
		EditorGUILayout.LabelField( "Public Functions", sectionHeaderStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( Indent + "All of the following public functions are only available from a reference to an UltimateRadialSubButtonInfo class:", paragraphStyle );

		EditorGUILayout.TextArea( "// Place these with your variables.\npublic UltimateRadialSubButtonInfo buttonInfo;", GUI.skin.textArea );

		EditorGUILayout.LabelField( Indent + "The examples provided rely on having an UltimateRadialSubButtonInfo variable named 'buttonInfo' stored inside your script. Please click on the function name to learn more.", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		for( int i = 0; i < UltimateRadialSubButtonInfo_PublicFunctions.Length; i++ )
			ShowDocumentation( UltimateRadialSubButtonInfo_PublicFunctions[ i ] );

		GUILayout.Space( itemHeaderSpace );
	}
	
	void VersionHistory ()
	{
		for( int i = 0; i < readme.versionHistory.Length; i++ )
		{
			if( GUILayout.Button( "Version " + readme.versionHistory[ i ].versionNumber, itemHeaderStyle ) )
				readme.versionHistory[ i ].showMore = !readme.versionHistory[ i ].showMore;

			for( int n = 0; n < readme.versionHistory[ i ].changes.Length && readme.versionHistory[ i ].showMore; n++ )
				EditorGUILayout.LabelField( "• " + readme.versionHistory[ i ].changes[ n ] + ".", paragraphStyle );

			if( i < ( readme.versionHistory.Length - 1 ) )
				GUILayout.Space( itemHeaderSpace );
		}
	}

	void ImportantChange ()
	{
		EditorGUILayout.LabelField( "No major update yet.", paragraphStyle );
		
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button( "Main Menu", GUILayout.Width( Screen.width / 2 ) ) )
			NavigateBack();

		rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}

	void ThankYou ()
	{
		EditorGUILayout.LabelField( "The two of us at Tank & Healer Studio would like to thank you for purchasing the Ultimate Radial Submenu add-on for the Ultimate Radial Menu!", paragraphStyle );

		GUILayout.Space( paragraphSpace );

		EditorGUILayout.LabelField( "We hope that the submenu will be a great help to you in the development of your game. After clicking the <i>Continue</i> button below, you will be presented with information to assist you in getting to know the Ultimate Radial Submenu and getting it implemented into your project.", paragraphStyle );

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "You can access this information at any time by clicking on the <b>README</b> file inside the Ultimate Radial Submenu folder.", paragraphStyle );

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "Again, thank you for downloading the Ultimate Radial Submenu. We hope that your project is a success!", paragraphStyle );

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "Happy Game Making,\n" + Indent + "Tank & Healer Studio", paragraphStyle );

		GUILayout.Space( 15 );

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button( "Continue", GUILayout.Width( Screen.width / 2 ) ) )
			NavigateBack();

		var rect2 = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect2, MouseCursor.Link );

		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}

	void Settings ()
	{
		EditorGUILayout.LabelField( "Text Mesh Pro", sectionHeaderStyle );
		if( GUILayout.Button( "Import Text Mesh Pro Support" ) )
		{
			if( !File.Exists( "Assets/Tank & Healer Studio/Ultimate Radial Submenu/Editor/Resources/UltimateRadialSubmenu_TextMeshProIntegration.unitypackage" ) )
				Debug.LogError( "No UnityPackage found at Tank & Healer Studio/Ultimate Radial Submenu/Editor/Resources." );
			else
				AssetDatabase.ImportPackage( "Assets/Tank & Healer Studio/Ultimate Radial Submenu/Editor/Resources/UltimateRadialSubmenu_TextMeshProIntegration.unitypackage", true );
		}
		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "Gizmo Colors", sectionHeaderStyle );
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorDefault" ), new GUIContent( "Default" ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetString( "URM_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorDefault ) );
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorValueChanged" ), new GUIContent( "Value Changed" ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetString( "URM_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorValueChanged ) );
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorButtonSelected" ), new GUIContent( "Button Selected" ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetString( "URM_ColorButtonSelectedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorButtonSelected ) );
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorButtonUnselected" ), new GUIContent( "Button Unselected" ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetString( "URM_ColorButtonUnselectedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorButtonUnselected ) );
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorTextBox" ), new GUIContent( "Text Box" ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetString( "URM_ColorTextBoxHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorTextBox ) );
		}

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button( "Reset", GUILayout.Width( EditorGUIUtility.currentViewWidth / 2 ) ) )
		{
			if( EditorUtility.DisplayDialog( "Reset Gizmo Color", "Are you sure that you want to reset the gizmo colors back to default?", "Yes", "No" ) )
			{
				ResetColors();
			}
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle( EditorPrefs.GetBool( "UUI_DisableDragAndDrop" ), " Disable Drag & Drop Opens Sections", EditorStyles.radioButton );
		if( EditorGUI.EndChangeCheck() )
			EditorPrefs.SetBool( "UUI_DisableDragAndDrop", !EditorPrefs.GetBool( "UUI_DisableDragAndDrop" ) );

		if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) )
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Development Mode", sectionHeaderStyle );
			base.OnInspectorGUI();
			EditorGUILayout.Space();
		}

		GUILayout.FlexibleSpace();

		GUILayout.Space( sectionSpace );

		EditorGUI.BeginChangeCheck();
		GUILayout.Toggle( EditorPrefs.GetBool( "UUI_DevelopmentMode" ), ( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) ? "Disable" : "Enable" ) + " Development Mode", EditorStyles.radioButton );
		if( EditorGUI.EndChangeCheck() )
		{
			if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) == false )
			{
				if( EditorUtility.DisplayDialog( "Enable Development Mode", "Are you sure you want to enable development mode for Tank & Healer Studio assets? This mode will allow you to see the default inspector for this asset which is useful when adding variables to this script without having to edit the custom editor script.", "Enable", "Cancel" ) )
				{
					EditorPrefs.SetBool( "UUI_DevelopmentMode", !EditorPrefs.GetBool( "UUI_DevelopmentMode" ) );
				}
			}
			else
				EditorPrefs.SetBool( "UUI_DevelopmentMode", !EditorPrefs.GetBool( "UUI_DevelopmentMode" ) );
		}
	}

	void ResetColors ()
	{
		serializedObject.FindProperty( "colorDefault" ).colorValue = Color.black;
		serializedObject.FindProperty( "colorValueChanged" ).colorValue = Color.cyan;
		serializedObject.FindProperty( "colorButtonSelected" ).colorValue = Color.yellow;
		serializedObject.FindProperty( "colorButtonUnselected" ).colorValue = Color.white;
		serializedObject.FindProperty( "colorTextBox" ).colorValue = Color.yellow;
		serializedObject.ApplyModifiedProperties();

		EditorPrefs.SetString( "URM_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.black ) );
		EditorPrefs.SetString( "URM_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.cyan ) );
		EditorPrefs.SetString( "URM_ColorButtonSelectedHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.yellow ) );
		EditorPrefs.SetString( "URM_ColorButtonUnselectedHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.white ) );
		EditorPrefs.SetString( "URM_ColorTextBoxHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.yellow ) );
	}

	void ShowDocumentation ( DocumentationInfo info )
	{
		GUILayout.Space( paragraphSpace );

		if( GUILayout.Button( info.functionName, itemHeaderStyle ) )
		{
			info.showMore = !info.showMore;
			GUI.FocusControl( "" );
		}
		var rect = GUILayoutUtility.GetLastRect();
		EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

		if( info.showMore )
		{
			EditorGUILayout.LabelField( Indent + "<i>Description:</i> " + info.description, paragraphStyle );

			if( info.parameter != null )
			{
				for( int i = 0; i < info.parameter.Length; i++ )
					EditorGUILayout.LabelField( Indent + "<i>Parameter:</i> " + info.parameter[ i ], paragraphStyle );
			}
			if( info.returnType != string.Empty )
				EditorGUILayout.LabelField( Indent + "<i>Return type:</i> " + info.returnType, paragraphStyle );

			if( info.codeExample != string.Empty )
				EditorGUILayout.TextArea( info.codeExample, GUI.skin.textArea );

			GUILayout.Space( paragraphSpace );
		}
	}

	public static void OpenReadmeDocumentation ()
	{
		SelectReadmeFile();

		if( !pageHistory.Contains( AllPages[ 3 ] ) )
			NavigateForward( 3 );

		if( !pageHistory.Contains( AllPages[ 4 ] ) )
			NavigateForward( 4 );
	}

	[MenuItem( "Window/Tank and Healer Studio/Ultimate Radial Submenu", false, 6 )]
	public static void SelectReadmeFile ()
	{
		var ids = AssetDatabase.FindAssets( "README t:UltimateRadialSubmenuReadme" );
		if( ids.Length == 1 )
		{
			var readmeObject = AssetDatabase.LoadMainAssetAtPath( AssetDatabase.GUIDToAssetPath( ids[ 0 ] ) );
			Selection.objects = new Object[] { readmeObject };
			readme = ( UltimateRadialSubmenuReadme )readmeObject;
		}
		else
			Debug.LogError( "There is no README object in the Ultimate Radial Submenu folder." );
	}
}