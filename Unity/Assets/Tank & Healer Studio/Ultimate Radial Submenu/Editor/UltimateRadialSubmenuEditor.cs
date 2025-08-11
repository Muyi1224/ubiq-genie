/* UltimateRadialSubmenuEditor.cs */
/* Written by Kaz */
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

[CustomEditor( typeof( UltimateRadialSubmenu ) )]
public class UltimateRadialSubmenuEditor : Editor
{
	UltimateRadialSubmenu targ;
	
	// VARIABLES //
	Image baseImage;
	Sprite baseSprite = null;
	Color baseColor = Color.white;
	Image pointerImage;
	Sprite pointerSprite = null;
	bool syncBaseAndEndAngles = false;
	string subButtonTextTemp = "Text";
	Sprite endImageSprite;
	Color endImageColor = Color.white;
	bool prefabRootError = false;
	bool isInProjectWindow = false;
	bool isInPrefabScene = false;
	SerializedProperty useButtonIcon, useButtonText;

	// DRAG AND DROP //
	bool disableDragAndDrop = false;
	bool isDraggingObject = false;
	Vector2 dragAndDropMousePos = Vector2.zero;
	double dragAndDropStartTime = 0.0f;
	double dragAndDropCurrentTime = 0.0f;

	// SUB BUTTON LIST //
	static int submenuButtonIndex = 0;
	ReorderableList ReorderableSubmenuInformations;
	SerializedProperty EditorRadialMenuIndex;
	SerializedProperty useStaticInformation;

	// SCRIPT REFERENCE //
	class ExampleCode
	{
		public string optionName = "";
		public string optionDescription = "";
		public string basicCode = "";
	}
	ExampleCode[] StaticExampleCodes = new ExampleCode[]
	{
		new ExampleCode() { optionName = "RegisterButton", optionDescription = "Registers the provided information to the targeted submenu.", basicCode = "UltimateRadialSubmenu.RegisterButton( \"{0}\", YourCallbackFunction, buttonInfo );" },
		new ExampleCode() { optionName = "Enable", optionDescription = "Enables the submenu visually.", basicCode = "UltimateRadialSubmenu.Enable( \"{0}\" );" },
		new ExampleCode() { optionName = "Disable", optionDescription = "Disables the submenu visually.", basicCode = "UltimateRadialSubmenu.Disable( \"{0}\" );" },
		new ExampleCode() { optionName = "ClearMenu", optionDescription = "Removes all of the sub buttons on the menu.", basicCode = "UltimateRadialSubmenu.ClearMenu( \"{0}\" );" },
	};
	ExampleCode[] PublicExampleCodes = new ExampleCode[]
	{
		new ExampleCode() { optionName = "RegisterButton", optionDescription = "Registers the provided information to this submenu.", basicCode = "submenu.RegisterButton( YourCallbackFunction, buttonInfo );" },
		new ExampleCode() { optionName = "Enable", optionDescription = "Enables the submenu visually.", basicCode = "submenu.Enable();" },
		new ExampleCode() { optionName = "Disable", optionDescription = "Disables the submenu visually.", basicCode = "submenu.Disable();" },
		new ExampleCode() { optionName = "ClearMenu", optionDescription = "Removes all of the sub buttons on this submenu.", basicCode = "submenu.ClearMenu();" },
	};
	List<string> exampleCodeOptions = new List<string>();
	int exampleCodeIndex = 0;

	// DEVELOPMENT MODE //
	bool showDefaultInspector = false;

	// SCENE GUI //
	class DisplaySceneGizmo
	{
		public bool hover = false;

		public bool HighlightGizmo
		{
			get
			{
				return hover;
			}
		}
	}
	DisplaySceneGizmo DisplayMinRange = new DisplaySceneGizmo();
	DisplaySceneGizmo DisplayMaxRange = new DisplaySceneGizmo();
	DisplaySceneGizmo DisplayDeactivationAngle = new DisplaySceneGizmo();
	static bool isDirty = false;
	bool wasDirtyLastFrame = false;
	// Gizmo Colors //
	Color colorDefault = Color.black;
	Color colorValueChanged = Color.cyan;
	Color colorButtonSelected = Color.yellow;
	Color colorButtonUnselected = Color.white;
	Color colorTextBox = Color.yellow;

	// EDITOR STYLES //
	GUIStyle collapsableSectionStyle = new GUIStyle();
	GUIStyle textFieldStyle = new GUIStyle();
	GUIStyle helpBoxStyle = new GUIStyle();


	void OnEnable ()
	{
		StoreReferences();

		Undo.undoRedoPerformed += StoreReferences;

		if( EditorPrefs.HasKey( "URM_ColorHexSetup" ) )
		{
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorDefaultHex" ), out colorDefault );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorValueChangedHex" ), out colorValueChanged );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorButtonSelectedHex" ), out colorButtonSelected );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorButtonUnselectedHex" ), out colorButtonUnselected );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "URM_ColorTextBoxHex" ), out colorTextBox );
		}

		prefabRootError = false;
		if( !isInPrefabScene && PrefabUtility.GetPrefabAssetType( targ.gameObject ) != PrefabAssetType.NotAPrefab )
		{
			if( PrefabUtility.GetOutermostPrefabInstanceRoot( targ.gameObject ) != targ.gameObject )
			{
				if( PrefabUtility.GetOutermostPrefabInstanceRoot( targ.gameObject ) != null )
					prefabRootError = true;
			}
			else if( PrefabUtility.GetOutermostPrefabInstanceRoot( targ.gameObject ) == targ.gameObject )
				PrefabUtility.UnpackPrefabInstance( targ.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction );
		}

		exampleCodeOptions = new List<string>();
		if( targ.radialMenu != null && targ.radialMenu.radialMenuName != string.Empty )
		{
			for( int i = 0; i < StaticExampleCodes.Length; i++ )
				exampleCodeOptions.Add( StaticExampleCodes[ i ].optionName );
		}
		else
		{
			for( int i = 0; i < PublicExampleCodes.Length; i++ )
				exampleCodeOptions.Add( PublicExampleCodes[ i ].optionName );
		}

		disableDragAndDrop = EditorPrefs.GetBool( "UUI_DisableDragAndDrop" );
	}

	void OnDisable ()
	{
		Undo.undoRedoPerformed -= StoreReferences;
	}
	
	void StoreReferences ()
	{
		targ = ( UltimateRadialSubmenu )target;

		if( targ == null )
			return;

		// If the selected object is NOT in the asset database, that means it's actually in the scene.
		isInProjectWindow = Selection.activeGameObject != null && AssetDatabase.Contains( Selection.activeGameObject );

		// If the current stage of the selected object is NOT the main stage, then it is in the prefab editing stage.
		isInPrefabScene = !isInProjectWindow && StageUtility.GetCurrentStageHandle() != StageUtility.GetMainStageHandle();

		if( !isInPrefabScene && targ.radialMenu == null )
		{
			UltimateRadialMenu radialMenu = targ.GetComponentInParent<UltimateRadialMenu>();

			// Attempt to get a radial menu in a parent, and if there is a radial menu, ensure that this submenu is a child of the main radial menu transform and not some nested child.
			if( radialMenu != null )
			{
				serializedObject.FindProperty( "radialMenu" ).objectReferenceValue = radialMenu;
				serializedObject.ApplyModifiedProperties();

				if( targ.radialMenu.transform != targ.transform.parent.transform )
					Undo.SetTransformParent( targ.transform, targ.radialMenu.transform, "Update Submenu Parent" );
			}

			// If the radial menu is still unassigned, find all the radial menus in the scene and try to parent to that. If none are found, just parent to a canvas at least.
			if( targ.radialMenu == null && Selection.activeGameObject != null && !AssetDatabase.Contains( Selection.activeGameObject ) )
			{
#if UNITY_2022_2_OR_NEWER
				UltimateRadialMenu[] allRadialMenus = FindObjectsByType<UltimateRadialMenu>( FindObjectsSortMode.None );
#else
				UltimateRadialMenu[] allRadialMenus = FindObjectsOfType<UltimateRadialMenu>();
#endif
				for( int i = 0; i < allRadialMenus.Length; i++ )
				{
					if( allRadialMenus[ i ].GetComponentInChildren<UltimateRadialSubmenu>() )
						continue;

					Undo.SetTransformParent( Selection.activeGameObject.transform, allRadialMenus[ i ].transform, "Update Submenu Parent" );
					serializedObject.FindProperty( "radialMenu" ).objectReferenceValue = allRadialMenus[ i ];
					serializedObject.ApplyModifiedProperties();
					break;
				}

				if( Selection.activeGameObject.transform.parent == null )
				{
#if UNITY_2022_2_OR_NEWER
					UnityEngine.Canvas canvas = FindAnyObjectByType<UnityEngine.Canvas>();
#else
					UnityEngine.Canvas canvas = Object.FindObjectOfType<UnityEngine.Canvas>();
#endif
					if( canvas != null )
						Undo.SetTransformParent( Selection.activeGameObject.transform, canvas.transform, "Update Submenu Parent" );
					else
						Debug.LogError( "<b>Ultimate Radial Submenu Editor</b>\n" +
							"<color=red><b>×</b></color> <i><b>Error:</b></i> There isn't even a UI Canvas in the scene.\n" +
							"<color=green><b>√</b></color> <i><b>Solution:</b></i> Please create an Ultimate Radial Menu <i>before</i> trying to add a Submenu to your scene.\n" );
				}
			}
		}

		EditorRadialMenuIndex = serializedObject.FindProperty( "EditorRadialMenuIndex" );
		useStaticInformation = serializedObject.FindProperty( "useStaticInformation" );

		useButtonIcon = serializedObject.FindProperty( "useButtonIcon" );
		useButtonText = serializedObject.FindProperty( "useButtonText" );
		
		pointerImage = ( Image )serializedObject.FindProperty( "pointerImage" ).objectReferenceValue;
		if( pointerImage != null )
			pointerSprite = pointerImage.sprite;

		baseImage = ( Image )serializedObject.FindProperty( "baseImage" ).objectReferenceValue;
		if( baseImage != null )
		{
			baseSprite = baseImage.sprite;
			baseColor = baseImage.color;
		}

		if( !useStaticInformation.boolValue && targ.UltimateRadialSubButtonList.Count > 0 && useButtonText.boolValue && targ.UltimateRadialSubButtonList[ 0 ].text != null )
			subButtonTextTemp = targ.UltimateRadialSubButtonList[ 0 ].text.text;

		Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
		if( endImageLeft != null )
		{
			endImageSprite = endImageLeft.sprite;
			endImageColor = endImageLeft.color;
		}

		syncBaseAndEndAngles = serializedObject.FindProperty( "baseImageAngleModifier" ).floatValue == serializedObject.FindProperty( "endImageAngleModifier" ).floatValue;

		SetupReorderableList();
	}

	[MenuItem( "GameObject/UI/Ultimate Radial Submenu" )]
	public static void CreateSubmenuFromScratch ()
	{
#if UNITY_2022_2_OR_NEWER
		UltimateRadialMenu ultimateRadialMenuObj = FindAnyObjectByType<UltimateRadialMenu>();
#else
		UltimateRadialMenu ultimateRadialMenuObj = FindObjectOfType<UltimateRadialMenu>();
#endif
		if( Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<UltimateRadialMenu>() )
			ultimateRadialMenuObj = Selection.activeGameObject.GetComponent<UltimateRadialMenu>();

		if( ultimateRadialMenuObj != null )
		{
			GameObject ultimateRadialSubmenu = new GameObject( "Ultimate Radial Submenu" );
			ultimateRadialSubmenu.layer = LayerMask.NameToLayer( "UI" );
			ultimateRadialSubmenu.AddComponent<RectTransform>();
			ultimateRadialSubmenu.AddComponent<CanvasGroup>();

			UltimateRadialSubmenu submenu = ultimateRadialSubmenu.AddComponent<UltimateRadialSubmenu>();
			submenu.radialMenu = ultimateRadialMenuObj;

			ultimateRadialSubmenu.transform.SetParent( ultimateRadialMenuObj.transform );
			ultimateRadialSubmenu.transform.SetAsLastSibling();

			Selection.activeGameObject = ultimateRadialSubmenu;
			Undo.RegisterCreatedObjectUndo( ultimateRadialSubmenu, "Create Ultimate Radial Submenu Object" );
		}
		else
		{
			Debug.LogError( "<b>Ultimate Radial Submenu Editor</b>\n" +
				"<color=red><b>×</b></color> <i><b>Error:</b></i> There are no Ultimate Radial Menus in your scene to associate this submenu with.\n" +
				"<color=green><b>√</b></color> <i><b>Solution:</b></i> Please create an Ultimate Radial Menu <i>before</i> trying to add a Submenu to your scene.\n" );
		}
	}

	bool DisplayHeaderDropdown ( string headerName, string editorPref )
	{
		EditorGUILayout.Space();

		GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11 };
		GUILayout.BeginHorizontal();
		GUILayout.Space( -10 );
		EditorGUI.BeginChangeCheck();
		EditorPrefs.SetBool( editorPref, GUILayout.Toggle( EditorPrefs.GetBool( editorPref ), ( EditorPrefs.GetBool( editorPref ) ? "▼ " : "► " ) + headerName, toolbarStyle ) );
		if( EditorGUI.EndChangeCheck() )
			isDirty = true;
		GUILayout.EndHorizontal();
		
		if( EditorPrefs.GetBool( editorPref ) )
		{
			EditorGUILayout.Space();
			return true;
		}

		return false;
	}

	void EndMainSection ( string editorPref )
	{
		if( EditorPrefs.GetBool( editorPref ) )
			GUILayout.Space( 1 );
		else if( DragAndDropHover() )
		{
			EditorPrefs.SetBool( editorPref, true );
			SceneView.RepaintAll();
		}
	}

	bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, bool error = false )
	{
		if( error )
			sectionTitle += " <color=#ff0000ff>*</color>";

		EditorGUILayout.BeginVertical( "Box" );

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Bold;

		if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
		{
			EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );
			SceneView.RepaintAll();
		}

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Normal;

		return EditorPrefs.GetBool( editorPref );
	}

	bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, SerializedProperty enabledProp, ref bool valueChanged, bool error = false )
	{
		valueChanged = false;

		if( enabledProp.boolValue && error )
			sectionTitle += " <color=#ff0000ff>*</color>";

		EditorGUILayout.BeginVertical( "Box" );

		if( EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue )
			collapsableSectionStyle.fontStyle = FontStyle.Bold;
		else if( isInProjectWindow && !enabledProp.boolValue )
			EditorPrefs.SetBool( editorPref, false );

		EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginDisabledGroup( isInProjectWindow );
		EditorGUI.BeginChangeCheck();
		enabledProp.boolValue = EditorGUILayout.Toggle( enabledProp.boolValue, GUILayout.Width( 25 ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();
			EditorPrefs.SetBool( editorPref, enabledProp.boolValue );
			valueChanged = true;
			SceneView.RepaintAll();
		}
		EditorGUI.EndDisabledGroup();

		GUILayout.Space( -25 );

		EditorGUI.BeginDisabledGroup( !enabledProp.boolValue );
		if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
		{
			EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );
			SceneView.RepaintAll();
		}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.EndHorizontal();

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Normal;

		return EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue;
	}

	void EndCollapsibleSection ( string editorPref )
	{
		if( EditorPrefs.GetBool( editorPref ) )
			GUILayout.Space( 1 );
		else if( DragAndDropHover() )
		{
			EditorPrefs.SetBool( editorPref, true );
			SceneView.RepaintAll();
		}

		EditorGUILayout.EndVertical();
	}

	bool DragAndDropHover ()
	{
		if( disableDragAndDrop )
			return false;

		if( DragAndDrop.objectReferences.Length == 0 )
		{
			dragAndDropStartTime = 0.0f;
			dragAndDropCurrentTime = 0.0f;
			isDraggingObject = false;
			return false;
		}
		
		isDraggingObject = true;

		var rect = GUILayoutUtility.GetLastRect();
		if( Event.current.type == EventType.Repaint && rect.Contains( Event.current.mousePosition ) )
		{
			if( dragAndDropStartTime == 0.0f )
			{
				dragAndDropStartTime = EditorApplication.timeSinceStartup;
				dragAndDropCurrentTime = 0.0f;
			}

			if( dragAndDropMousePos == Event.current.mousePosition )
				dragAndDropCurrentTime = EditorApplication.timeSinceStartup - dragAndDropStartTime;
			else
			{
				dragAndDropStartTime = EditorApplication.timeSinceStartup;
				dragAndDropCurrentTime = 0.0f;
			}

			if( dragAndDropCurrentTime >= 0.5f )
			{
				dragAndDropStartTime = 0.0f;
				dragAndDropCurrentTime = 0.0f;
				return true;
			}

			dragAndDropMousePos = Event.current.mousePosition;
		}

		return false;
	}

	void CheckPropertyHover ( DisplaySceneGizmo displaySceneGizmo )
	{
		displaySceneGizmo.hover = false;
		var rect = GUILayoutUtility.GetLastRect();
		if( Event.current.type == EventType.Repaint && rect.Contains( Event.current.mousePosition ) )
		{
			displaySceneGizmo.hover = true;
			isDirty = true;
		}
	}

	void GUILayoutAfterIndentSpace ()
	{
		GUILayout.Space( 2 );
	}

	void DisplayNormalButtonSpriteAndColor ()
	{
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "normalSprite" ), new GUIContent( "Button Sprite", "The default sprite to apply to the submenu button image." ) );
		if( EditorGUI.EndChangeCheck() )
		{
			if( serializedObject.FindProperty( "normalSprite" ).objectReferenceValue == null )
			{
				serializedObject.FindProperty( "spriteSwap" ).boolValue = false;
				serializedObject.FindProperty( "colorChange" ).boolValue = false;
			}
			serializedObject.ApplyModifiedProperties();

			for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
			{
				Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Submenu Button Sprite" );
				if( targ.NormalSprite != null )
				{
					if( !targ.UltimateRadialSubButtonList[ i ].buttonDisabled || !serializedObject.FindProperty( "spriteSwap" ).boolValue )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = targ.NormalSprite;

					if( !targ.UltimateRadialSubButtonList[ i ].buttonDisabled || !serializedObject.FindProperty( "colorChange" ).boolValue )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.color = targ.NormalColor;
				}
				else
				{
					if( !targ.UltimateRadialSubButtonList[ i ].buttonDisabled || !serializedObject.FindProperty( "spriteSwap" ).boolValue )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = null;

					if( !targ.UltimateRadialSubButtonList[ i ].buttonDisabled || !serializedObject.FindProperty( "colorChange" ).boolValue )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.color = Color.clear;
				}

				// This is added just in case the user has not broken the prefab, at least we can keep the sprites up to date.
				if( prefabRootError )
					PrefabUtility.RecordPrefabInstancePropertyModifications( targ.UltimateRadialSubButtonList[ i ].buttonImage );
			}
		}

		EditorGUI.BeginDisabledGroup( targ.NormalSprite == null );
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "normalColor" ), new GUIContent( "Button Color", "The default color to apply to the submenu button image." ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();

			if( targ.NormalSprite != null )
			{
				for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
				{
					if( targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite == null )
						continue;

					if( !targ.UltimateRadialSubButtonList[ i ].buttonDisabled || !serializedObject.FindProperty( "colorChange" ).boolValue )
					{
						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Submenu Button Color" );
						targ.UltimateRadialSubButtonList[ i ].buttonImage.color = targ.NormalColor;
					}
				}
			}
		}
		EditorGUI.EndDisabledGroup();
	}

	void SetupReorderableList ()
	{
		if( targ.UltimateRadialSubButtonList.Count == 0 || isInProjectWindow )
			return;

		if( targ.radialMenu != null )
		{
			if( targ.SubmenuInformations.Count < targ.radialMenu.UltimateRadialButtonList.Count )
			{
				for( int i = targ.SubmenuInformations.Count; i < targ.radialMenu.UltimateRadialButtonList.Count; i++ )
				{
					serializedObject.FindProperty( "SubmenuInformations" ).InsertArrayElementAtIndex( i );
					serializedObject.ApplyModifiedProperties();

					serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].registerSubmenu", targ.SubmenuInformations.Count - 1 ) ).boolValue = true;
					serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations", targ.SubmenuInformations.Count - 1 ) ).ClearArray();
					serializedObject.ApplyModifiedProperties();

					serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations", targ.SubmenuInformations.Count - 1 ) ).arraySize = 1;
					serializedObject.ApplyModifiedProperties();
				}
			}

			if( serializedObject.FindProperty( "useStaticInformation" ).boolValue )
			{
				for( int i = 0; i < targ.radialMenu.UltimateRadialButtonList.Count; i++ )
				{
					if( targ.radialMenu.useButtonIcon && targ.radialMenu.UltimateRadialButtonList[ i ].icon != null )
						serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.icon", i ) ).objectReferenceValue = targ.radialMenu.UltimateRadialButtonList[ i ].icon.sprite;

					serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.name", i ) ).stringValue = targ.radialMenu.UltimateRadialButtonList[ i ].name;
					serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.description", i ) ).stringValue = targ.radialMenu.UltimateRadialButtonList[ i ].description;

					serializedObject.ApplyModifiedProperties();
				}
			}
		}

		ReorderableSubmenuInformations = new ReorderableList( serializedObject, serializedObject.FindProperty( "SubmenuInformations.Array" ), true, false, false, targ.radialMenu != null && targ.SubmenuInformations.Count > targ.radialMenu.UltimateRadialButtonList.Count );

		if( isInPrefabScene && useStaticInformation.boolValue )
		{
			useStaticInformation.boolValue = false;
			serializedObject.ApplyModifiedProperties();
		}

		if( !useStaticInformation.boolValue )
		{
			int informations = 4;
			if( targ.radialMenu != null )
				informations = targ.radialMenu.UltimateRadialButtonList.Count;

			List<UltimateRadialSubmenu.SubmenuInformation> DynamicInformations = new List<UltimateRadialSubmenu.SubmenuInformation>();
			for( int i = 0; i < informations; i++ )
				DynamicInformations.Add( new UltimateRadialSubmenu.SubmenuInformation() );

			ReorderableSubmenuInformations = new ReorderableList( DynamicInformations, typeof( UltimateRadialSubmenu.SubmenuInformation ), true, false, false, false );

			ReorderableSubmenuInformations.draggable = false;
		}

		ReorderableSubmenuInformations.headerHeight = 0.0f;

		if( !ReorderableSubmenuInformations.displayRemove )
			ReorderableSubmenuInformations.footerHeight = 0.0f;
		
		ReorderableSubmenuInformations.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
		{
			if( index > targ.SubmenuInformations.Count - 1 )
				return;

			GUIStyle elementStyle = EditorStyles.label;
			elementStyle.richText = true;
			string elementString = "Radial Menu Button " + index.ToString( "00" );
			if( useStaticInformation.boolValue )
			{
				SerializedProperty prop = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].registerSubmenu", index ) );
				bool disableInformation = false;

				elementString = "Submenu Information " + index.ToString( "00" );
				if( index >= targ.radialMenu.UltimateRadialButtonList.Count )
				{
					elementString += " <color=red>(Out of Index)</color>";
					disableInformation = true;
				}
				else if( targ.radialMenu.UltimateRadialButtonList[ index ].buttonDisabled )
				{
					elementString += " <color=red>(Disabled)</color>";
					disableInformation = true;
				}
				else if( targ.radialMenu.UltimateRadialButtonList[ index ].unityEvent.GetPersistentEventCount() > 0 )
				{
					elementString += " <color=red>(Event Registered)</color>";
					disableInformation = true;
				}

				EditorGUI.BeginChangeCheck();
				prop.boolValue = EditorGUI.ToggleLeft( new Rect( rect.x, rect.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight ), GUIContent.none, prop.boolValue );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginDisabledGroup( disableInformation || !prop.boolValue );
				EditorGUI.LabelField( new Rect( rect.x + EditorGUIUtility.singleLineHeight, rect.y, EditorGUIUtility.currentViewWidth - EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight ), elementString, elementStyle );
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUI.BeginDisabledGroup( targ.radialMenu != null && index < targ.radialMenu.UltimateRadialButtonList.Count && targ.radialMenu.UltimateRadialButtonList[ index ].buttonDisabled );
				EditorGUI.LabelField( new Rect( rect.x, rect.y, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight ), elementString, elementStyle );
				EditorGUI.EndDisabledGroup();
			}
		};

		ReorderableSubmenuInformations.onSelectCallback = ( ReorderableList l ) =>
		{
			if( l.index == EditorRadialMenuIndex.intValue )
				return;

			ReorderableSubmenuInformations.index = l.index;
			EditorRadialMenuIndex.intValue = ReorderableSubmenuInformations.index;
			submenuButtonIndex = 0;
			serializedObject.ApplyModifiedProperties();

			if( useStaticInformation.boolValue )
				GenerateSubmenuButtons();

			targ.UpdatePositioning();
		};

		ReorderableSubmenuInformations.onReorderCallbackWithDetails = ( ReorderableList l, int oldIndex, int newIndex ) =>
		{
			EditorRadialMenuIndex.intValue = ReorderableSubmenuInformations.index;
			serializedObject.ApplyModifiedProperties();

			targ.UpdatePositioning();
		};

		ReorderableSubmenuInformations.onRemoveCallback = ( ReorderableList l ) =>
		{
			if( EditorUtility.DisplayDialog( "Ultimate Radial Submenu", "Are you sure you want to remove this static sub button information?", "Yes", "No" ) )
			{
				serializedObject.FindProperty( string.Format( "SubmenuInformations.Array", EditorRadialMenuIndex.intValue ) ).DeleteArrayElementAtIndex( l.index );
				EditorRadialMenuIndex.intValue = 0;
				serializedObject.ApplyModifiedProperties();
				submenuButtonIndex = 0;

				ReorderableSubmenuInformations.index = EditorRadialMenuIndex.intValue;

				GenerateSubmenuButtons();
				targ.UpdatePositioning();
				
				SetupReorderableList();
			}
		};

		ReorderableSubmenuInformations.index = EditorRadialMenuIndex.intValue;
	}

	void HelpBox ( string problem, string solution, MessageType messageType = MessageType.Error )
	{
		EditorGUILayout.Space();
		if( messageType == MessageType.Error )
			EditorGUILayout.LabelField( "<color=red><b>×</b></color> <i><b>Error:</b></i> " + problem + ".", helpBoxStyle );
		else if( messageType == MessageType.Warning )
			EditorGUILayout.LabelField( "<color=yellow><b>▲</b></color> <i><b>Warning:</b></i> " + problem + ".", helpBoxStyle );
		else
			EditorGUILayout.LabelField( "• <i><b>Info:</b></i> " + problem + ".", helpBoxStyle );

		EditorGUILayout.Space();
		EditorGUILayout.LabelField( "<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".", helpBoxStyle );
		EditorGUILayout.Space();
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		if( targ == null )
			return;

		collapsableSectionStyle = new GUIStyle( EditorStyles.label ) { richText = true, alignment = TextAnchor.MiddleCenter, onActive = new GUIStyleState() { textColor = Color.black } };
		collapsableSectionStyle.active.textColor = collapsableSectionStyle.normal.textColor;

		textFieldStyle = new GUIStyle( GUI.skin.textField ) { wordWrap = true };

		helpBoxStyle = new GUIStyle( EditorStyles.label ) { richText = true, wordWrap = true };

		if( isInProjectWindow )
		{
			EditorGUILayout.BeginVertical( "Box" );
			HelpBox( "Cannot edit prefabs in the project window", "Please drag this prefab into your scene to edit it" );
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space();
			GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
			GUILayout.BeginHorizontal();
			GUILayout.Space( -10 );
			showDefaultInspector = GUILayout.Toggle( showDefaultInspector, ( showDefaultInspector ? "▼" : "►" ) + " Default Inspector", toolbarStyle );
			GUILayout.EndHorizontal();

			if( showDefaultInspector )
			{
				EditorGUILayout.Space();

				base.OnInspectorGUI();
			}
			else if( DragAndDropHover() )
				showDefaultInspector = true;
			return;
		}

		if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) )
		{
			EditorGUILayout.Space();
			GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
			GUILayout.BeginHorizontal();
			GUILayout.Space( -10 );
			showDefaultInspector = GUILayout.Toggle( showDefaultInspector, ( showDefaultInspector ? "▼" : "►" ) + "<color=#ff0000ff> Development Inspector</color>", toolbarStyle );
			GUILayout.EndHorizontal();

			if( showDefaultInspector )
			{
				EditorGUILayout.Space();

				base.OnInspectorGUI();
			}
			else if( DragAndDropHover() )
				showDefaultInspector = true;
		}

		if( targ.radialMenu == null && !isInPrefabScene )
		{
			EditorGUILayout.BeginVertical( "Box" );
			HelpBox( "This submenu is not a child of an Ultimate Radial Menu object", "Please make sure to place this object as a child of the Ultimate Radial Menu that you want to use this submenu for. To fix this error, assign the targeted Ultimate Radial Menu component to the <b>Radial Menu</b> property below, or click the <b>Attempt Fix</b> button" );

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "radialMenu" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();

				if( serializedObject.FindProperty( "radialMenu" ).objectReferenceValue != null )
					Undo.SetTransformParent( targ.transform, targ.radialMenu.transform, "Attempt Fix Submenu" );
			}

			if( GUILayout.Button( "Attempt Fix" ) )
			{
#if UNITY_2022_2_OR_NEWER
				UltimateRadialMenu radialMenu = FindAnyObjectByType<UltimateRadialMenu>();
#else
				UltimateRadialMenu radialMenu = FindObjectOfType<UltimateRadialMenu>();
#endif
				if( radialMenu != null )
				{
					Undo.SetTransformParent( targ.transform, radialMenu.transform, "Attempt Fix Submenu" );
					serializedObject.FindProperty( "radialMenu" ).objectReferenceValue = radialMenu;
					serializedObject.ApplyModifiedProperties();
				}
			}
			EditorGUILayout.EndVertical();
			return;
		}
		
		if( targ.radialMenu != null && targ.radialMenu.gameObject == targ.gameObject )
		{
			EditorGUILayout.BeginVertical( "Box" );
			HelpBox( "The Ultimate Radial Submenu component is on the same object as the Ultimate Radial Menu it is being used for", "Please move the Ultimate Radial Submenu component to a different object that is a child of the Ultimate Radial Menu, or click the button below to automatically fix this error" );
			if( GUILayout.Button( "Fix" ) )
			{
				CreateSubmenuFromScratch();
				DestroyImmediate( targ );
			}
			EditorGUILayout.EndVertical();

			return;
		}

		if( targ.UltimateRadialSubButtonList.Count == 0 && !Application.isPlaying )
		{
			EditorGUILayout.BeginVertical( "Box" );
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "normalSprite" ) );

			if( targ.NormalSprite == null )
			{
				GUIStyle noteStyle = new GUIStyle( EditorStyles.miniLabel ) { alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = true };
				EditorGUILayout.LabelField( "<color=red>*</color> Please assign a button sprite to use when generating images.", noteStyle );
			}

			EditorGUILayout.PropertyField( serializedObject.FindProperty( "followOrbitalRotation" ) );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();

			if( GUILayout.Button( "Generate" ) )
			{
				useButtonIcon.boolValue = false;
				useButtonText.boolValue = false;
				serializedObject.ApplyModifiedProperties();
				
				if( targ.NormalSprite == null )
				{
					if( EditorUtility.DisplayDialog( "Ultimate Radial Menu", "You are about to create a submenu with no button sprite.", "Continue", "Cancel" ) )
						GenerateSubmenuButtons();
				}
				else
					GenerateSubmenuButtons();

				StoreReferences();
			}

			EditorGUILayout.EndVertical();
			Repaint();
			return;
		}

		if( prefabRootError )
		{
			EditorGUILayout.BeginVertical( "Box" );
			HelpBox( "The Ultimate Radial Submenu is not the root of this prefab and therefore cannot be unpacked properly. This can cause some strange behavior, as well as not being able to remove buttons in the editor that are part of the prefab. This is caused because of Unity's new prefab manager", "Please remove the Ultimate Radial Submenu from the prefab in order to edit it, or <b>Unpack</b> the root prefab object" );
			EditorGUILayout.EndVertical();
		}

		bool valueChanged = false;

		if( DisplayHeaderDropdown( "Submenu Positioning", "URM_RadialMenuPositioning" ) )
		{
			// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
			if( Application.isPlaying )
			{
				EditorGUILayout.HelpBox( "The application is running. Changes made here will not be kept.", MessageType.Warning );
				EditorGUI.BeginChangeCheck();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "submenuDistance" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "anglePerButton" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "buttonSize" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "followOrbitalRotation" ), new GUIContent( "Orbital Rotation" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "smartSequencing" ) );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();
			
			// INPUT SETTINGS //
			if( DisplayCollapsibleBoxSection( "Input Settings", "URM_InputSettings" ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "minRange" ), 0.5f, serializedObject.FindProperty( "maxRange" ).floatValue );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				CheckPropertyHover( DisplayMinRange );

				if( targ.radialMenu != null && !targ.radialMenu.infiniteMaxRange && serializedObject.FindProperty( "minRange" ).floatValue > targ.radialMenu.maxRange )
					EditorGUILayout.HelpBox( "The minimum range of the submenu is greater than the maximum range of the main Ultimate Radial Menu. This can cause strange input interactions when the input is in between the menus.", MessageType.Warning );

				EditorGUI.BeginDisabledGroup( serializedObject.FindProperty( "infiniteMaxRange" ).boolValue );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "maxRange" ), serializedObject.FindProperty( "minRange" ).floatValue, 2.0f );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				CheckPropertyHover( DisplayMaxRange );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "infiniteMaxRange" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "submenuDeactivationAngle" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				CheckPropertyHover( DisplayDeactivationAngle );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "disableOnInteract" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "invokeAction" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_InputSettings" );
			// END INPUT SETTINGS //
			
			// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
			if( Application.isPlaying )
			{
				if( EditorGUI.EndChangeCheck() )
					targ.UpdatePositioning();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "submenuSiblingIndex" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();

				if( targ.radialMenu != null && serializedObject.FindProperty( "submenuSiblingIndex" ).enumValueIndex != ( int )UltimateRadialMenu.SetSiblingIndex.Disabled )
				{
					Undo.RegisterCompleteObjectUndo( targ.radialMenu.transform, "Set Submenu Sibling Index" );
					if( serializedObject.FindProperty( "submenuSiblingIndex" ).enumValueIndex == ( int )UltimateRadialMenu.SetSiblingIndex.First )
						targ.transform.SetAsFirstSibling();
					else
						targ.transform.SetAsLastSibling();
				}
			}
		}
		EndMainSection( "URM_RadialMenuPositioning" );

		if( DisplayHeaderDropdown( "Submenu Options", "URM_RadialMenuOptions" ) )
		{
			// NORMAL SPRITE AND COLOR //
			DisplayNormalButtonSpriteAndColor();
			// END NORMAL SPRITE AND COLOR //

			// RADIAL MENU INTERACTION //
			if( targ.radialMenu != null )
			{
				if( DisplayCollapsibleBoxSection( "Radial Menu Interaction", "URM_RadialMenuInteraction" ) )
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "selectRadialButton" ) );
					EditorGUI.BeginDisabledGroup( !targ.radialMenu.displayButtonName && !targ.radialMenu.displayButtonDescription );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "overwriteRadialMenuText" ), new GUIContent( "Overwrite Menu Text" ) );
					EditorGUI.EndDisabledGroup();
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

					GUILayout.Space( 1 );
				}
				EndCollapsibleSection( "URM_RadialMenuInteraction" );
			}
			// END RADIAL MENU INTERACTION //
			
			// BUTTON ICON //
			if( DisplayCollapsibleBoxSection( "Button Icon", "URM_ButtonIcon", useButtonIcon, ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconNormalColor" ), new GUIContent( "Icon Color", "The color of the icon image." ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled || targ.UltimateRadialSubButtonList[ i ].icon == null || targ.UltimateRadialSubButtonList[ i ].icon.sprite == null )
							continue;

						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Submenu Icon Color" );
						targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconSize" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconVerticalPosition" ), new GUIContent( "Vertical Position" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconRotation" ), new GUIContent( "Rotation Offset" ) );
				if( serializedObject.FindProperty( "followOrbitalRotation" ).boolValue )
				{
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconLocalRotation" ), new GUIContent( "Local Rotation" ) );

					if( serializedObject.FindProperty( "iconLocalRotation" ).boolValue )
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconSmartRotation" ), new GUIContent( "Smart Rotation" ) );
						EditorGUI.indentLevel--;
					}
				}
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( !serializedObject.FindProperty( "useStaticInformation" ).boolValue )
				{
					EditorGUI.BeginChangeCheck();
					serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue = EditorGUILayout.ObjectField( new GUIContent( "Icon Placeholder" ), serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue, typeof( Sprite ), false );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						if( !serializedObject.FindProperty( "useStaticInformation" ).boolValue )
						{
							for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
							{
								if( targ.UltimateRadialSubButtonList[ i ].icon == null )
									continue;

								Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Submenu Icon Placeholder" );
								targ.UltimateRadialSubButtonList[ i ].icon.enabled = false;
								targ.UltimateRadialSubButtonList[ i ].icon.sprite = ( Sprite )serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue;

								if( serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue != null )
									targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
								else
									targ.UltimateRadialSubButtonList[ i ].icon.color = Color.clear;

								targ.UltimateRadialSubButtonList[ i ].icon.enabled = true;
							}
						}
					}
				}
			}
			EndCollapsibleSection( "URM_ButtonIcon" );
			if( valueChanged )
				CheckForButtonIcon();
			// END BUTTON ICON //

			// BUTTON TEXT //
			if( DisplayCollapsibleBoxSection( "Button Text", "URM_ButtonText", useButtonText, ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textNormalColor" ), new GUIContent( "Text Color" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int n = 0; n < targ.UltimateRadialSubButtonList.Count; n++ )
					{
						if( targ.UltimateRadialSubButtonList[ n ].buttonDisabled || targ.UltimateRadialSubButtonList[ n ].text == null )
							continue;

						Undo.RecordObject( targ.UltimateRadialSubButtonList[ n ].text, "Update Submenu Text Color" );
						targ.UltimateRadialSubButtonList[ n ].text.color = serializedObject.FindProperty( "textNormalColor" ).colorValue;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "buttonTextFont" ), new GUIContent( "Text Font" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int n = 0; n < targ.UltimateRadialSubButtonList.Count; n++ )
					{
						Undo.RecordObject( targ.UltimateRadialSubButtonList[ n ].text, "Update Submenu Text Font" );
						targ.UltimateRadialSubButtonList[ n ].text.font = ( Font )serializedObject.FindProperty( "buttonTextFont" ).objectReferenceValue;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "buttonTextOutline" ), new GUIContent( "Text Outline" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						if( targ.UltimateRadialSubButtonList[ i ].text != null )
						{
							if( serializedObject.FindProperty( "buttonTextOutline" ).boolValue && !targ.UltimateRadialSubButtonList[ i ].text.gameObject.GetComponent<UnityEngine.UI.Outline>() )
							{
								Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text.gameObject, "Update Submenu Text Outline" );
								targ.UltimateRadialSubButtonList[ i ].text.gameObject.AddComponent<UnityEngine.UI.Outline>();

								serializedObject.FindProperty( "buttonTextOutlineColor" ).colorValue = targ.UltimateRadialSubButtonList[ i ].text.gameObject.GetComponent<UnityEngine.UI.Outline>().effectColor;
								serializedObject.ApplyModifiedProperties();
							}

							if( targ.UltimateRadialSubButtonList[ i ].text.gameObject.GetComponent<UnityEngine.UI.Outline>() )
							{
								Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text.gameObject.GetComponent<UnityEngine.UI.Outline>(), "Update Submenu Text Outline" );
								targ.UltimateRadialSubButtonList[ i ].text.gameObject.GetComponent<UnityEngine.UI.Outline>().enabled = serializedObject.FindProperty( "buttonTextOutline" ).boolValue;
							}
						}
					}
				}

				if( serializedObject.FindProperty( "buttonTextOutline" ).boolValue )
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "buttonTextOutlineColor" ), new GUIContent( "Outline Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].text != null && targ.UltimateRadialSubButtonList[ i ].text.GetComponent<UnityEngine.UI.Outline>() )
							{
								Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text.GetComponent<UnityEngine.UI.Outline>(), "Update Submenu Text Outline" );
								targ.UltimateRadialSubButtonList[ i ].text.GetComponent<UnityEngine.UI.Outline>().enabled = false;
								targ.UltimateRadialSubButtonList[ i ].text.GetComponent<UnityEngine.UI.Outline>().effectColor = serializedObject.FindProperty( "buttonTextOutlineColor" ).colorValue;
								targ.UltimateRadialSubButtonList[ i ].text.GetComponent<UnityEngine.UI.Outline>().enabled = true;
							}
						}
					}
					GUILayoutAfterIndentSpace();
					EditorGUI.indentLevel--;
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "displayNameOnButton" ), new GUIContent( "Display Name" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUILayout.Space();

				EditorGUILayout.LabelField( "Positioning", EditorStyles.boldLabel );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "textAreaRatio.x" ), 0.0f, 1.0f, new GUIContent( "Ratio X", "The horizontal ratio of the text transform." ) );
				EditorGUILayout.Slider( serializedObject.FindProperty( "textAreaRatio.y" ), 0.0f, 1.0f, new GUIContent( "Ratio Y", "The horizontal ratio of the text transform." ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textSize" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textLocalPosition" ), new GUIContent( "Local Position" ) );
				if( serializedObject.FindProperty( "textLocalPosition" ).boolValue )
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textLocalRotation" ), new GUIContent( "Local Rotation" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textRotationVertical" ), new GUIContent( "Vertical Rotation" ) );
					GUILayoutAfterIndentSpace();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textVerticalPosition" ), new GUIContent( "Vertical Position" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "relativeToIcon" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
			}
			EndCollapsibleSection( "URM_ButtonText" );
			if( valueChanged )
				CheckForButtonText();
			// END BUTTON TEXT //
			
			// MENU TOGGLE SETTINGS //
			if( DisplayCollapsibleBoxSection( "Menu Toggle", "URM_MenuToggleSettings", serializedObject.FindProperty( "useMenuToggle" ), ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "menuToggleAlpha" ), new GUIContent( "Use Alpha" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "menuToggleScale" ), new GUIContent( "Use Scale" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( !serializedObject.FindProperty( "menuToggleAlpha" ).boolValue && !serializedObject.FindProperty( "menuToggleScale" ).boolValue )
					EditorGUILayout.HelpBox( "Both Alpha and Scale are disabled. Please select at least one of these options.", MessageType.Warning );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "toggleInDuration" ), new GUIContent( "Toggle In Duration" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "toggleOutDuration" ), new GUIContent( "Toggle Out Duration" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( serializedObject.FindProperty( "toggleInDuration" ).floatValue < 0 )
						serializedObject.FindProperty( "toggleInDuration" ).floatValue = 0.0f;

					if( serializedObject.FindProperty( "toggleOutDuration" ).floatValue < 0 )
						serializedObject.FindProperty( "toggleOutDuration" ).floatValue = 0.0f;

					serializedObject.ApplyModifiedProperties();
				}

				if( serializedObject.FindProperty( "toggleInDuration" ).floatValue == 0.0f && serializedObject.FindProperty( "toggleOutDuration" ).floatValue == 0.0f )
				{
					HelpBox( "Both the toggle in and out durations are set to zero. This will make the menu function the same as just having the Menu Toggle setting disabled", "Please set the duration values to something higher than 0 or uncheck the Menu Toggle option above" );
				}

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_MenuToggleSettings" );
			// END MENU TOGGLE SETTINGS //

			// BASE IMAGE //
			if( DisplayCollapsibleBoxSection( "Base Image", "URM_BaseImageSettings", serializedObject.FindProperty( "useBaseImage" ), ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "baseImage" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					baseImage = ( Image )serializedObject.FindProperty( "baseImage" ).objectReferenceValue;

					if( baseImage != null )
					{
						baseColor = baseImage.color;
						baseSprite = baseImage.sprite;
					}
				}

				EditorGUI.BeginChangeCheck();
				baseSprite = ( Sprite )EditorGUILayout.ObjectField( "Base Sprite", baseSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && baseImage != null )
				{
					Undo.RecordObject( baseImage, "Update Base Image Sprite" );
					baseImage.enabled = false;
					baseImage.sprite = baseSprite;
					baseImage.enabled = true;
				}

				if( baseImage == null )
				{
					EditorGUI.BeginDisabledGroup( baseSprite == null );
					if( GUILayout.Button( "Generate Base Image", EditorStyles.miniButton ) )
					{
						GameObject newBaseImage = new GameObject( "Base Image", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
						baseImage = newBaseImage.GetComponent<Image>();

						newBaseImage.transform.SetParent( targ.transform );

						baseImage.sprite = baseSprite;
						baseImage.color = baseColor;

						baseImage.rectTransform.pivot = new Vector2( 0.5f, 0.5f );
						baseImage.rectTransform.localScale = Vector3.one;

						serializedObject.FindProperty( "baseImageUseFill" ).boolValue = false;
						serializedObject.FindProperty( "baseImage" ).objectReferenceValue = baseImage;
						serializedObject.ApplyModifiedProperties();
						
						Undo.RegisterCreatedObjectUndo( newBaseImage, "Create Base Image Object" );
					}
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					baseColor = EditorGUILayout.ColorField( "Base Image Color", baseColor );
					if( EditorGUI.EndChangeCheck() )
					{
						Undo.RecordObject( baseImage, "Update Base Image Color" );
						baseImage.enabled = false;
						baseImage.color = baseColor;
						baseImage.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "baseImageSize" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "baseImageUseFill" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						if( baseImage != null )
						{
							Undo.RecordObject( baseImage, "Update Base Image Fill" );
							if( serializedObject.FindProperty( "baseImageUseFill" ).boolValue )
							{
								baseImage.type = Image.Type.Filled;
								baseImage.fillMethod = Image.FillMethod.Radial360;
								baseImage.fillOrigin = ( int )Image.Origin360.Top;
							}
							else
								baseImage.type = Image.Type.Simple;
						}
					}

					if( serializedObject.FindProperty( "baseImageUseFill" ).boolValue )
					{
						EditorGUI.indentLevel++;
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "baseImageAngleModifier" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							serializedObject.ApplyModifiedProperties();

							if( syncBaseAndEndAngles && serializedObject.FindProperty( "useEndImages" ).boolValue )
							{
								serializedObject.FindProperty( "endImageAngleModifier" ).floatValue = serializedObject.FindProperty( "baseImageAngleModifier" ).floatValue;
								serializedObject.ApplyModifiedProperties();
							}
						}

						if( serializedObject.FindProperty( "useEndImages" ).boolValue )
						{
							EditorGUI.BeginChangeCheck();
							syncBaseAndEndAngles = EditorGUILayout.ToggleLeft( "Sync w/ End Image", syncBaseAndEndAngles );
							if( EditorGUI.EndChangeCheck() )
							{
								if( syncBaseAndEndAngles )
								{
									serializedObject.FindProperty( "endImageAngleModifier" ).floatValue = serializedObject.FindProperty( "baseImageAngleModifier" ).floatValue;
									serializedObject.ApplyModifiedProperties();
								}
							}
							if( syncBaseAndEndAngles )
								EditorGUILayout.HelpBox( "Angle value will apply to End Images as well.", MessageType.None );
						}

						EditorGUI.indentLevel--;
					}
				}
			}
			EndCollapsibleSection( "URM_BaseImageSettings" );
			if( valueChanged && baseImage != null )
			{
				if( serializedObject.FindProperty( "useBaseImage" ).boolValue )
				{
					Undo.RecordObject( baseImage.gameObject, "Enable Base Image Object" );
					baseImage.gameObject.SetActive( true );
				}
				else
				{
					Undo.RecordObject( baseImage.gameObject, "Disable Base Image Object" );
					baseImage.gameObject.SetActive( false );
				}
			}
			// END BASE IMAGE //

			// POINTER //
			if( DisplayCollapsibleBoxSection( "Pointer", "URM_Pointer", serializedObject.FindProperty( "usePointer" ), ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerImage" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					pointerImage = ( Image )serializedObject.FindProperty( "pointerImage" ).objectReferenceValue;
					if( pointerImage != null )
					{
						serializedObject.FindProperty( "pointerNormalColor" ).colorValue = pointerImage.color;
						serializedObject.ApplyModifiedProperties();

						pointerSprite = pointerImage.sprite;
					}
				}
				
				EditorGUI.BeginChangeCheck();
				pointerSprite = ( Sprite )EditorGUILayout.ObjectField( "Pointer Sprite", pointerSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && pointerImage != null )
				{
					Undo.RecordObject( pointerImage, "Update Pointer Sprite" );
					pointerImage.enabled = false;
					pointerImage.sprite = pointerSprite;
					pointerImage.enabled = true;
				}

				if( pointerImage == null )
				{
					EditorGUI.BeginDisabledGroup( pointerSprite == null );
					if( GUILayout.Button( "Generate Pointer Image", EditorStyles.miniButton ) )
					{
						GameObject newPointerImage = new GameObject( "Submenu Pointer", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
						Image pointerImg = newPointerImage.GetComponent<Image>();

						newPointerImage.transform.SetParent( targ.transform );

						pointerImg.sprite = pointerSprite;
						pointerImg.color = serializedObject.FindProperty( "pointerNormalColor" ).colorValue;

						pointerImg.rectTransform.pivot = new Vector2( 0.5f, 0.5f );
						pointerImg.rectTransform.localScale = Vector3.one;

						serializedObject.FindProperty( "pointerImage" ).objectReferenceValue = pointerImg;
						serializedObject.ApplyModifiedProperties();

						pointerImage = pointerImg;

						Undo.RegisterCreatedObjectUndo( newPointerImage, "Create Pointer Object" );
					}
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerNormalColor" ), new GUIContent( "Normal Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						Undo.RecordObject( pointerImage, "Update Pointer Color" );
						pointerImage.color = serializedObject.FindProperty( "pointerNormalColor" ).colorValue;
					}

					if( targ.radialMenu != null )
					{
						SerializedObject radialMenuSerializedObj = new SerializedObject( targ.radialMenu );
						if( radialMenuSerializedObj.FindProperty( "usePointer" ).boolValue && serializedObject.FindProperty( "pointerNormalColor" ).colorValue != radialMenuSerializedObj.FindProperty( "pointerNormalColor" ).colorValue )
						{
							if( GUILayout.Button( "Copy Radial Menu Value" ) )
							{
								serializedObject.FindProperty( "pointerNormalColor" ).colorValue = radialMenuSerializedObj.FindProperty( "pointerNormalColor" ).colorValue;
								serializedObject.ApplyModifiedProperties();

								Undo.RecordObject( pointerImage, "Update Pointer Color" );
								pointerImage.enabled = false;
								pointerImage.color = serializedObject.FindProperty( "pointerNormalColor" ).colorValue;
								pointerImage.enabled = true;
							}
						}
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerSize" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerSnapOption" ) );
					if( serializedObject.FindProperty( "pointerSnapOption" ).enumValueIndex != ( int )UltimateRadialMenu.PointerSnapOption.Instant )
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerTargetTime" ) );
						EditorGUILayout.Space();
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerRotationOffset" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerColorChange" ) );
					if( serializedObject.FindProperty( "pointerColorChange" ).boolValue )
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerActiveColor" ), new GUIContent( "Active Color" ) );

						if( targ.radialMenu != null )
						{
							SerializedObject radialMenuSerializedObj = new SerializedObject( targ.radialMenu );
							if( radialMenuSerializedObj.FindProperty( "usePointer" ).boolValue && serializedObject.FindProperty( "pointerActiveColor" ).colorValue != radialMenuSerializedObj.FindProperty( "pointerActiveColor" ).colorValue )
							{
								if( GUILayout.Button( "Copy Radial Menu Value" ) )
								{
									serializedObject.FindProperty( "pointerActiveColor" ).colorValue = radialMenuSerializedObj.FindProperty( "pointerActiveColor" ).colorValue;
									serializedObject.ApplyModifiedProperties();
								}
							}
						}

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerFadeInDuration" ) );
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerFadeOutDuration" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							if( serializedObject.FindProperty( "pointerFadeInDuration" ).floatValue < 0.0f )
								serializedObject.FindProperty( "pointerFadeInDuration" ).floatValue = 0.0f;

							if( serializedObject.FindProperty( "pointerFadeOutDuration" ).floatValue < 0.0f )
								serializedObject.FindProperty( "pointerFadeOutDuration" ).floatValue = 0.0f;

							serializedObject.ApplyModifiedProperties();
						}
						EditorGUILayout.Space();
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "pointerSiblingIndex" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						if( serializedObject.FindProperty( "pointerSiblingIndex" ).enumValueIndex != ( int )UltimateRadialMenu.SetSiblingIndex.Disabled )
						{
							Undo.RegisterCompleteObjectUndo( targ.transform, "Set Pointer Sibling Index" );
							if( serializedObject.FindProperty( "pointerSiblingIndex" ).enumValueIndex == ( int )UltimateRadialMenu.SetSiblingIndex.First )
							{
								if( serializedObject.FindProperty( "useBaseImage" ).boolValue && serializedObject.FindProperty( "baseImage" ).objectReferenceValue != null )
									pointerImage.transform.SetSiblingIndex( 1 );
								else
									pointerImage.transform.SetAsFirstSibling();
							}
							else
								pointerImage.transform.SetAsLastSibling();
						}
					}
				}
			}
			EndCollapsibleSection( "URM_Pointer" );
			if( valueChanged && pointerImage != null )
			{
				if( serializedObject.FindProperty( "usePointer" ).boolValue )
				{
					Undo.RecordObject( pointerImage.gameObject, "Enable Pointer Object" );
					pointerImage.gameObject.SetActive( true );
				}
				else
				{
					Undo.RecordObject( pointerImage.gameObject, "Disable Pointer Object" );
					pointerImage.gameObject.SetActive( false );
				}
			}
			// END POINTER //

			// END IMAGES //
			if( DisplayCollapsibleBoxSection( "End Images", "URM_EndImages", serializedObject.FindProperty( "useEndImages" ), ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "endImageLeft" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "endImageRight" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginChangeCheck();
				endImageSprite = ( Sprite )EditorGUILayout.ObjectField( new GUIContent( "End Image Sprite" ), endImageSprite, typeof( Sprite ), false );
				if( EditorGUI.EndChangeCheck() )
				{
					Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
					if( endImageLeft != null )
					{
						Undo.RecordObject( endImageLeft, "Update End Image Sprites" );
						endImageLeft.enabled = false;
						endImageLeft.sprite = endImageSprite;
						endImageLeft.enabled = true;
					}
					Image endImageRight = ( Image )serializedObject.FindProperty( "endImageRight" ).objectReferenceValue;
					if( endImageRight != null )
					{
						Undo.RecordObject( endImageRight, "Update End Image Sprites" );
						endImageRight.enabled = false;
						endImageRight.sprite = endImageSprite;
						endImageRight.enabled = true;
					}
				}

				if( serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue == null || serializedObject.FindProperty( "endImageRight" ).objectReferenceValue == null )
				{
					EditorGUI.BeginDisabledGroup( endImageSprite == null );
					if( GUILayout.Button( "Generate" ) )
					{
						if( serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue == null )
						{
							GameObject endImageLeft = new GameObject( "End Image Left", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
							endImageLeft.GetComponent<Image>().color = endImageColor;
							endImageLeft.GetComponent<Image>().sprite = endImageSprite;

							Undo.RegisterCreatedObjectUndo( endImageLeft, "Create Submenu End Images" );
							serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue = endImageLeft.GetComponent<Image>();

							endImageLeft.transform.SetParent( targ.transform );
							endImageLeft.transform.SetAsFirstSibling();
						}

						if( serializedObject.FindProperty( "endImageRight" ).objectReferenceValue == null )
						{
							GameObject endImageRight = new GameObject( "End Image Right", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
							endImageRight.GetComponent<Image>().color = endImageColor;
							endImageRight.GetComponent<Image>().sprite = endImageSprite;

							Undo.RegisterCreatedObjectUndo( endImageRight, "Create Submenu End Images" );
							serializedObject.FindProperty( "endImageRight" ).objectReferenceValue = endImageRight.GetComponent<Image>();

							endImageRight.transform.SetParent( targ.transform );
							endImageRight.transform.SetAsFirstSibling();
						}
						serializedObject.ApplyModifiedProperties();
					}
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					endImageColor = EditorGUILayout.ColorField( new GUIContent( "End Image Color" ), endImageColor );
					if( EditorGUI.EndChangeCheck() )
					{
						Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
						Image endImageRight = ( Image )serializedObject.FindProperty( "endImageRight" ).objectReferenceValue;

						Undo.RecordObject( endImageLeft, "Update End Image Color" );
						Undo.RecordObject( endImageRight, "Update End Image Color" );

						endImageLeft.enabled = false;
						endImageRight.enabled = false;
						endImageLeft.color = endImageColor;
						endImageRight.color = endImageColor;
						endImageLeft.enabled = true;
						endImageRight.enabled = true;
					}

					Image endImages = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
					bool imageFilled = endImages.type == Image.Type.Filled;
					EditorGUI.BeginChangeCheck();
					imageFilled = EditorGUILayout.Toggle( "Fill Images", imageFilled );
					if( EditorGUI.EndChangeCheck() )
					{
						Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
						Image endImageRight = ( Image )serializedObject.FindProperty( "endImageRight" ).objectReferenceValue;

						Undo.RecordObject( endImageLeft, "Update End Image Type" );
						Undo.RecordObject( endImageRight, "Update End Image Type" );

						endImageLeft.enabled = false;
						endImageRight.enabled = false;

						if( imageFilled )
						{
							endImageLeft.type = Image.Type.Filled;
							endImageRight.type = Image.Type.Filled;
							endImageLeft.fillMethod = Image.FillMethod.Horizontal;
							endImageRight.fillMethod = Image.FillMethod.Horizontal;
						}
						else
						{
							endImageLeft.type = Image.Type.Simple;
							endImageRight.type = Image.Type.Simple;
						}

						endImageLeft.enabled = true;
						endImageRight.enabled = true;
					}

					if( imageFilled )
					{
						EditorGUI.indentLevel++;
						float imageFill = endImages.fillAmount;
						EditorGUI.BeginChangeCheck();
						imageFill = EditorGUILayout.Slider( "Fill Amount", imageFill, 0.0f, 1.0f );
						if( EditorGUI.EndChangeCheck() )
						{
							Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
							Image endImageRight = ( Image )serializedObject.FindProperty( "endImageRight" ).objectReferenceValue;

							Undo.RecordObject( endImageLeft, "Update End Image Fill" );
							Undo.RecordObject( endImageRight, "Update End Image Fill" );

							endImageLeft.enabled = false;
							endImageRight.enabled = false;

							endImageLeft.fillAmount = imageFill;
							endImageRight.fillAmount = imageFill;

							endImageLeft.enabled = true;
							endImageRight.enabled = true;
						}
						EditorGUI.indentLevel--;
						EditorGUILayout.Space();
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "endImageSize" ), new GUIContent( "Image Size" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "endImageDistanceModifier" ), new GUIContent( "Distance Modifier" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "endImageAngleModifier" ), new GUIContent( "Angle Modifier" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( syncBaseAndEndAngles && serializedObject.FindProperty( "baseImageUseFill" ).boolValue )
					{
						serializedObject.FindProperty( "baseImageAngleModifier" ).floatValue = serializedObject.FindProperty( "endImageAngleModifier" ).floatValue;
						serializedObject.ApplyModifiedProperties();
					}
				}

				if( serializedObject.FindProperty( "useBaseImage" ).boolValue && serializedObject.FindProperty( "baseImageUseFill" ).boolValue )
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					syncBaseAndEndAngles = EditorGUILayout.ToggleLeft( "Sync w/ Base Image", syncBaseAndEndAngles );
					if( EditorGUI.EndChangeCheck() )
					{
						if( syncBaseAndEndAngles )
						{
							serializedObject.FindProperty( "baseImageAngleModifier" ).floatValue = serializedObject.FindProperty( "endImageAngleModifier" ).floatValue;
							serializedObject.ApplyModifiedProperties();
						}
					}
					if( syncBaseAndEndAngles )
						EditorGUILayout.HelpBox( "Angle value will apply to Base Image as well.", MessageType.None );
					EditorGUI.indentLevel--;
				}
			}
			EndCollapsibleSection( "URM_EndImages" );
			if( valueChanged )
			{
				Image endImageLeft = ( Image )serializedObject.FindProperty( "endImageLeft" ).objectReferenceValue;
				Image endImageRight = ( Image )serializedObject.FindProperty( "endImageRight" ).objectReferenceValue;

				if( serializedObject.FindProperty( "useEndImages" ).boolValue )
				{
					if( endImageLeft != null && !endImageLeft.gameObject.activeInHierarchy )
					{
						Undo.RecordObject( endImageLeft.gameObject, "Enable End Images" );
						endImageLeft.gameObject.SetActive( true );
					}
					if( endImageRight != null && !endImageRight.gameObject.activeInHierarchy )
					{
						Undo.RecordObject( endImageRight.gameObject, "Enable End Images" );
						endImageRight.gameObject.SetActive( true );
					}
				}
				else
				{
					if( endImageLeft != null && endImageLeft.gameObject.activeInHierarchy )
					{
						Undo.RecordObject( endImageLeft.gameObject, "Disable End Images" );
						endImageLeft.gameObject.SetActive( false );
					}
					if( endImageRight != null && endImageRight.gameObject.activeInHierarchy )
					{
						Undo.RecordObject( endImageRight.gameObject, "Disable End Images" );
						endImageRight.gameObject.SetActive( false );
					}
				}
			}
			// END END IMAGES //
		}
		EndMainSection( "URM_RadialMenuOptions" );

		if( DisplayHeaderDropdown( "Button Interaction", "URM_ButtonInteraction" ) )
		{
			EditorGUI.BeginDisabledGroup( targ.NormalSprite == null );
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "spriteSwap" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();

				for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Submenu Button Sprite" );

					if( serializedObject.FindProperty( "spriteSwap" ).boolValue && serializedObject.FindProperty( "disabledSprite" ).objectReferenceValue != null && targ.UltimateRadialSubButtonList[ i ].buttonDisabled )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = ( Sprite )serializedObject.FindProperty( "disabledSprite" ).objectReferenceValue;
					else
						targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = targ.NormalSprite;
				}
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorChange" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();

				for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Submenu Button Color" );
					if( serializedObject.FindProperty( "colorChange" ).boolValue && targ.UltimateRadialSubButtonList[ i ].buttonDisabled )
						targ.UltimateRadialSubButtonList[ i ].buttonImage.color = serializedObject.FindProperty( "disabledColor" ).colorValue;
					else
						targ.UltimateRadialSubButtonList[ i ].buttonImage.color = targ.NormalColor;
				}
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "scaleTransform" ) );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();

			if( useButtonIcon.boolValue )
			{
				EditorGUILayout.Space();

				EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconColorChange" ), new GUIContent( "Color Change" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						if( targ.UltimateRadialSubButtonList[ i ].icon == null || targ.UltimateRadialSubButtonList[ i ].icon.sprite == null )
							continue;

						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Sub Button Icon Color" );
						if( serializedObject.FindProperty( "iconColorChange" ).boolValue && targ.UltimateRadialSubButtonList[ i ].buttonDisabled )
							targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconDisabledColor" ).colorValue;
						else
							targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconScaleTransform" ), new GUIContent( "Scale Transform" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
			}

			if( useButtonText.boolValue )
			{
				EditorGUILayout.Space();

				EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textColorChange" ), new GUIContent( "Color Change" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						if( targ.UltimateRadialSubButtonList[ i ].text == null )
							continue;

						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text, "Update Sub Button Text Color" );
						if( serializedObject.FindProperty( "textColorChange" ).boolValue && targ.UltimateRadialSubButtonList[ i ].buttonDisabled )
							targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textDisabledColor" ).colorValue;
						else
							targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textNormalColor" ).colorValue;
					}
				}
			}

			// BUTTON INTERACTION SETTINGS //
			if( DisplayCollapsibleBoxSection( "Normal", "URM_ButtonNormal" ) )
			{
				DisplayNormalButtonSpriteAndColor();

				if( useButtonIcon.boolValue && serializedObject.FindProperty( "iconColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconNormalColor" ), new GUIContent( "Normal Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled || targ.UltimateRadialSubButtonList[ i ].icon == null || targ.UltimateRadialSubButtonList[ i ].icon.sprite == null )
								continue;

							Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Sub Button Icon Color" );
							targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
						}
					}
				}

				if( useButtonText.boolValue && serializedObject.FindProperty( "textColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textNormalColor" ), new GUIContent( "Text Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled || targ.UltimateRadialSubButtonList[ i ].text == null )
								continue;

							Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text, "Update Sub Button Text Color" );
							targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textNormalColor" ).colorValue;
						}
					}
				}

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_ButtonNormal" );

			if( DisplayCollapsibleBoxSection( "Highlighted", "URM_ButtonHighlighted" ) )
			{
				EditorGUI.BeginChangeCheck();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "spriteSwap" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "highlightedSprite" ), new GUIContent( "Button Sprite" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "colorChange" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "highlightedColor" ), new GUIContent( "Button Color" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "scaleTransform" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "highlightedScaleModifier" ), new GUIContent( "Button Scale" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "positionModifier" ), new GUIContent( "Position Modifier" ) );
				EditorGUI.EndDisabledGroup();

				if( useButtonIcon.boolValue && ( serializedObject.FindProperty( "iconColorChange" ).boolValue || serializedObject.FindProperty( "iconScaleTransform" ).boolValue ) )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );
					if( serializedObject.FindProperty( "iconColorChange" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconHighlightedColor" ), new GUIContent( "Highlighted Color" ) );

					if( serializedObject.FindProperty( "iconScaleTransform" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconHighlightedScaleModifier" ), new GUIContent( "Highlighted Scale" ) );
				}

				if( useButtonText.boolValue && serializedObject.FindProperty( "textColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textHighlightedColor" ), new GUIContent( "Highlighted Color" ) );
				}

				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_ButtonHighlighted" );

			if( DisplayCollapsibleBoxSection( "Pressed", "URM_ButtonPressed" ) )
			{
				EditorGUI.BeginChangeCheck();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "spriteSwap" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "pressedSprite" ), new GUIContent( "Button Sprite" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "colorChange" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "pressedColor" ), new GUIContent( "Button Color" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "scaleTransform" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "pressedScaleModifier" ), new GUIContent( "Button Scale" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "pressedPositionModifier" ), new GUIContent( "Position Modifier" ) );
				EditorGUI.EndDisabledGroup();

				if( useButtonIcon.boolValue && ( serializedObject.FindProperty( "iconColorChange" ).boolValue || serializedObject.FindProperty( "iconScaleTransform" ).boolValue ) )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );
					if( serializedObject.FindProperty( "iconColorChange" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconPressedColor" ), new GUIContent( "Pressed Color" ) );

					if( serializedObject.FindProperty( "iconScaleTransform" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconPressedScaleModifier" ), new GUIContent( "Pressed Scale" ) );
				}

				if( useButtonText.boolValue && serializedObject.FindProperty( "textColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textPressedColor" ), new GUIContent( "Pressed Color" ) );
				}

				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_ButtonPressed" );

			if( DisplayCollapsibleBoxSection( "Selected", "URM_ButtonSelected" ) )
			{
				EditorGUI.BeginChangeCheck();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "spriteSwap" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "selectedSprite" ), new GUIContent( "Button Sprite" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "colorChange" ).boolValue );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "selectedColor" ), new GUIContent( "Button Color" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "scaleTransform" ).boolValue );
				EditorGUILayout.Slider( serializedObject.FindProperty( "selectedScaleModifier" ), 0.5f, 1.5f, new GUIContent( "Button Scale" ) );
				EditorGUILayout.Slider( serializedObject.FindProperty( "selectedPositionModifier" ), -0.2f, 0.2f, new GUIContent( "Position Modifier" ) );
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField( "Automatic Selection", EditorStyles.boldLabel );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "selectButtonOnInteract" ), new GUIContent( "Select On Interact" ) );
				if( serializedObject.FindProperty( "selectButtonOnInteract" ).boolValue )
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "toggleSelection" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "allowMultipleSelected" ), new GUIContent( "Allow Multi Select" ) );
					EditorGUI.indentLevel--;
				}
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( useButtonIcon.boolValue && ( serializedObject.FindProperty( "iconColorChange" ).boolValue || serializedObject.FindProperty( "iconScaleTransform" ).boolValue ) )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );

					if( serializedObject.FindProperty( "iconColorChange" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconSelectedColor" ), new GUIContent( "Selected Color" ) );

					if( serializedObject.FindProperty( "iconScaleTransform" ).boolValue )
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconSelectedScaleModifier" ), new GUIContent( "Selected Scale" ) );
				}

				if( useButtonText.boolValue && serializedObject.FindProperty( "textColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textSelectedColor" ), new GUIContent( "Selected Color" ) );
				}

				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_ButtonSelected" );

			if( DisplayCollapsibleBoxSection( "Disabled", "URM_ButtonDisabled" ) )
			{
				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "spriteSwap" ).boolValue );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "disabledSprite" ), new GUIContent( "Button Sprite" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Disabled Button Sprite" );
						if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled && serializedObject.FindProperty( "disabledSprite" ).objectReferenceValue != null )
							targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = ( Sprite )serializedObject.FindProperty( "disabledSprite" ).objectReferenceValue;
						else
							targ.UltimateRadialSubButtonList[ i ].buttonImage.sprite = targ.NormalSprite;
					}
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "colorChange" ).boolValue );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "disabledColor" ), new GUIContent( "Button Color" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					{
						if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled )
						{
							Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].buttonImage, "Update Button Disabled Color" );
							targ.UltimateRadialSubButtonList[ i ].buttonImage.color = serializedObject.FindProperty( "disabledColor" ).colorValue;
						}
					}
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "scaleTransform" ).boolValue );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "disabledScaleModifier" ), 0.5f, 1.5f, new GUIContent( "Button Scale" ) );
				EditorGUILayout.Slider( serializedObject.FindProperty( "disabledPositionModifier" ), -0.2f, 0.2f, new GUIContent( "Position Modifier" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				EditorGUI.EndDisabledGroup();

				if( useButtonIcon.boolValue && ( serializedObject.FindProperty( "iconColorChange" ).boolValue || serializedObject.FindProperty( "iconScaleTransform" ).boolValue ) )
				{
					EditorGUILayout.Space();

					EditorGUILayout.LabelField( "Button Icon", EditorStyles.boldLabel );
					if( serializedObject.FindProperty( "iconColorChange" ).boolValue )
					{
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "iconDisabledColor" ), new GUIContent( "Disabled Color" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							serializedObject.ApplyModifiedProperties();

							for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
							{
								if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled && useButtonIcon.boolValue && targ.UltimateRadialSubButtonList[ i ].icon != null && targ.UltimateRadialSubButtonList[ i ].icon.sprite != null )
								{
									Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Button Disabled Color" );
									targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconDisabledColor" ).colorValue;
								}
							}
						}
					}

					if( serializedObject.FindProperty( "iconScaleTransform" ).boolValue )
					{
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.Slider( serializedObject.FindProperty( "iconDisabledScaleModifier" ), 0.5f, 1.5f, new GUIContent( "Disabled Scale" ) );
						if( EditorGUI.EndChangeCheck() )
							serializedObject.ApplyModifiedProperties();
					}
				}

				if( useButtonText.boolValue && serializedObject.FindProperty( "textColorChange" ).boolValue )
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Button Text", EditorStyles.boldLabel );
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "textDisabledColor" ), new GUIContent( "Disabled Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].buttonDisabled && targ.UltimateRadialSubButtonList[ i ].text != null )
							{
								Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text, "Update Text Disabled Color" );
								targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textDisabledColor" ).colorValue;
							}
						}
					}
				}

				GUILayout.Space( 1 );
			}
			EndCollapsibleSection( "URM_ButtonDisabled" );
		}
		EndMainSection( "URM_ButtonInteraction" );

		if( DisplayHeaderDropdown( "Sub Button List", "URM_RadialButtonList" ) )
		{
			if( Application.isPlaying )
				EditorGUILayout.HelpBox( "Application is currently playing! Settings here cannot be modified during runtime.", MessageType.Warning );
			else
			{
				if( !isInPrefabScene )
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle( !useStaticInformation.boolValue, new GUIContent( "Dynamic", "A Dynamic sub button setup allows you to add/remove buttons at runtime which is a much more flexible way to use the submenu." ), EditorStyles.miniButtonLeft );
					if( EditorGUI.EndChangeCheck() && useStaticInformation.boolValue )
					{
						useStaticInformation.boolValue = false;
						serializedObject.ApplyModifiedProperties();

						SetupReorderableList();
						GenerateSubmenuButtons();
					}

					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle( useStaticInformation.boolValue, new GUIContent( " Static ", "The information below can be set in the editor but is not editable at runtime." ), EditorStyles.miniButtonRight );
					if( EditorGUI.EndChangeCheck() && !useStaticInformation.boolValue )
					{
						useStaticInformation.boolValue = true;
						EditorRadialMenuIndex.intValue = 0;
						serializedObject.ApplyModifiedProperties();

						SetupReorderableList();
						GenerateSubmenuButtons();
					}
					EditorGUILayout.EndHorizontal();
				}

				if( !useStaticInformation.boolValue && ReorderableSubmenuInformations != null )
				{
					if( !isInPrefabScene )
					{
						ReorderableSubmenuInformations.DoLayoutList();
						EditorGUILayout.Space();
					}

					EditorGUILayout.LabelField( "Testing Values", EditorStyles.boldLabel );

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField( "Button Count: " + targ.UltimateRadialSubButtonList.Count );
					GUILayout.FlexibleSpace();
					if( GUILayout.Button( "+", EditorStyles.miniButtonLeft, GUILayout.Width( 20 ) ) )
					{
						int index = targ.UltimateRadialSubButtonList.Count;

						serializedObject.FindProperty( "UltimateRadialSubButtonList" ).InsertArrayElementAtIndex( index );
						serializedObject.ApplyModifiedProperties();

						GameObject newSubButton = new GameObject( "Submenu Button" );
						RectTransform trans = newSubButton.AddComponent<RectTransform>();
						newSubButton.AddComponent<CanvasRenderer>();
						Image image = newSubButton.AddComponent<Image>();

						if( targ.NormalSprite != null )
						{
							newSubButton.GetComponent<Image>().sprite = targ.NormalSprite;
							newSubButton.GetComponent<Image>().color = targ.NormalColor;
						}
						else
							newSubButton.GetComponent<Image>().color = Color.clear;

						newSubButton.transform.SetParent( targ.transform );
						newSubButton.transform.SetSiblingIndex( targ.UltimateRadialSubButtonList[ targ.UltimateRadialSubButtonList.Count - 1 ].buttonTransform.GetSiblingIndex() + 1 );

						trans.anchorMin = new Vector2( 0.5f, 0.5f );
						trans.anchorMax = new Vector2( 0.5f, 0.5f );
						trans.pivot = new Vector2( 0.5f, 0.5f );

						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].submenu", index ) ).objectReferenceValue = targ;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonTransform", index ) ).objectReferenceValue = trans;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonImage", index ) ).objectReferenceValue = image;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].icon", index ) ).objectReferenceValue = null;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconTransform", index ) ).objectReferenceValue = null;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].text", index ) ).objectReferenceValue = null;
						serializedObject.ApplyModifiedProperties();

						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].useIconUnique", index ) ).boolValue = false;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonDisabled", index ) ).boolValue = false;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].name", index ) ).stringValue = string.Empty;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].description", index ) ).stringValue = string.Empty;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconSize", index ) ).floatValue = 0.0f;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconHorizontalPosition", index ) ).floatValue = 0.0f;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconVerticalPosition", index ) ).floatValue = 0.0f;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconRotation", index ) ).floatValue = 0.0f;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].id", index ) ).intValue = 0;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].key", index ) ).stringValue = string.Empty;
						serializedObject.ApplyModifiedProperties();

						Undo.RegisterCreatedObjectUndo( newSubButton, "Create Submenu Button Object" );

						CheckForOptionalSettings();
					}
					EditorGUI.BeginDisabledGroup( targ.UltimateRadialSubButtonList.Count <= 1 );
					if( GUILayout.Button( "-", EditorStyles.miniButtonRight, GUILayout.Width( 20 ) ) )
					{
						GameObject objToDestroy = targ.UltimateRadialSubButtonList[ targ.UltimateRadialSubButtonList.Count - 1 ].buttonImage.gameObject;
						serializedObject.FindProperty( "UltimateRadialSubButtonList" ).DeleteArrayElementAtIndex( targ.UltimateRadialSubButtonList.Count - 1 );
						serializedObject.ApplyModifiedProperties();

						Undo.DestroyObjectImmediate( objToDestroy );

						StoreReferences();
					}
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();

					EditorGUI.BeginDisabledGroup( !useButtonIcon.boolValue );
					EditorGUI.BeginChangeCheck();
					serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue = EditorGUILayout.ObjectField( new GUIContent( "Icon Placeholder" ), serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue, typeof( Sprite ), false, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].icon == null )
								continue;

							Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Sub Button Icon" );
							targ.UltimateRadialSubButtonList[ i ].icon.enabled = false;
							targ.UltimateRadialSubButtonList[ i ].icon.sprite = ( Sprite )serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue;

							if( serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue != null )
								targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
							else
								targ.UltimateRadialSubButtonList[ i ].icon.color = Color.clear;

							targ.UltimateRadialSubButtonList[ i ].icon.enabled = true;
						}
					}
					EditorGUI.EndDisabledGroup();

					EditorGUI.BeginDisabledGroup( !useButtonText.boolValue );
					EditorGUI.BeginChangeCheck();
					subButtonTextTemp = EditorGUILayout.TextField( new GUIContent( "Text Value" ), subButtonTextTemp );
					if( EditorGUI.EndChangeCheck() )
					{
						for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
						{
							if( targ.UltimateRadialSubButtonList[ i ].text == null )
								continue;

							Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text, "Update Sub Button Text" );
							targ.UltimateRadialSubButtonList[ i ].text.enabled = false;

							if( subButtonTextTemp == string.Empty )
								targ.UltimateRadialSubButtonList[ i ].text.text = "Text";
							else
								targ.UltimateRadialSubButtonList[ i ].text.text = subButtonTextTemp;

							targ.UltimateRadialSubButtonList[ i ].text.enabled = true;
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				else if( useStaticInformation.boolValue && ReorderableSubmenuInformations != null )
				{
					GUIStyle headerStyle = new GUIStyle( GUI.skin.label )
					{
						fontStyle = FontStyle.Bold,
						alignment = TextAnchor.MiddleCenter,
						wordWrap = true
					};

					if( ReorderableSubmenuInformations.index != EditorRadialMenuIndex.intValue )
						ReorderableSubmenuInformations.index = EditorRadialMenuIndex.intValue;

					ReorderableSubmenuInformations.DoLayoutList();

					if( targ.SubmenuInformations.Count > targ.radialMenu.UltimateRadialButtonList.Count )
					{
						EditorGUILayout.BeginVertical( "Box" );
						HelpBox( "There are more stored static informations than there are main radial menu buttons", "To fix this, please remove the unneeded static information using the minus button above" );
						EditorGUILayout.EndVertical();
					}

					if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.SubmenuInformations.Count )
					{
						bool registerSubmenu = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].registerSubmenu", EditorRadialMenuIndex.intValue ) ).boolValue;
						int radialMenuButtonIndex = EditorRadialMenuIndex.intValue;

						if( radialMenuButtonIndex >= targ.radialMenu.UltimateRadialButtonList.Count )
							radialMenuButtonIndex = targ.radialMenu.UltimateRadialButtonList.Count - 1;

						bool menuButtonDisabled = targ.radialMenu.UltimateRadialButtonList[ radialMenuButtonIndex ].buttonDisabled;

						if( EditorRadialMenuIndex.intValue < targ.radialMenu.UltimateRadialButtonList.Count )
						{
							if( DisplayCollapsibleBoxSection( "Radial Button Info", "URSM_RadialButtonInfo" ) || !registerSubmenu || menuButtonDisabled )
							{
								SerializedObject radialMenuComponent = new SerializedObject( targ.radialMenu );
								if( menuButtonDisabled )
									HelpBox( "This Ultimate Radial Menu button is disabled", "If you would like to have a submenu for this button, please enable the button on the Ultimate Radial Menu", MessageType.Warning );

								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( radialMenuComponent.FindProperty( string.Format( "UltimateRadialButtonList.Array.data[{0}].buttonDisabled", radialMenuButtonIndex ) ), new GUIContent( "Disable Button", "Determines if this button should be disabled or not." ) );
								if( EditorGUI.EndChangeCheck() )
								{
									radialMenuComponent.ApplyModifiedProperties();

									if( radialMenuComponent.FindProperty( string.Format( "UltimateRadialButtonList.Array.data[{0}].buttonDisabled", radialMenuButtonIndex ) ).boolValue )
										targ.radialMenu.UltimateRadialButtonList[ radialMenuButtonIndex ].OnDisable();
									else
										targ.radialMenu.UltimateRadialButtonList[ radialMenuButtonIndex ].OnEnable();
								}

								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( radialMenuComponent.FindProperty( string.Format( "UltimateRadialButtonList.Array.data[{0}].name", EditorRadialMenuIndex.intValue ) ) );
								if( EditorGUI.EndChangeCheck() )
								{
									radialMenuComponent.ApplyModifiedProperties();

									if( targ.radialMenu.useButtonText && targ.radialMenu.displayNameOnButton && targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].text != null )
									{
										Undo.RecordObject( targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].text, "Update Radial Button Text" );
										targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].text.enabled = false;
										targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].text.text = targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].name;
										targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].text.enabled = true;
									}

									serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.name", EditorRadialMenuIndex.intValue ) ).stringValue = targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].name;
									serializedObject.ApplyModifiedProperties();
								}

								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( radialMenuComponent.FindProperty( string.Format( "UltimateRadialButtonList.Array.data[{0}].description", EditorRadialMenuIndex.intValue ) ) );
								if( EditorGUI.EndChangeCheck() )
								{
									radialMenuComponent.ApplyModifiedProperties();

									serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.description", EditorRadialMenuIndex.intValue ) ).stringValue = targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].description;
									serializedObject.ApplyModifiedProperties();
								}

								if( targ.radialMenu.useButtonIcon )
								{
									EditorGUI.BeginChangeCheck();
									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.icon", EditorRadialMenuIndex.intValue ) ) );
									if( EditorGUI.EndChangeCheck() )
									{
										serializedObject.ApplyModifiedProperties();

										if( targ.radialMenu.useButtonIcon && targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon != null )
										{
											Undo.RecordObject( targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon, "Update Radial Button Icon" );
											targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.enabled = false;
											targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.sprite = ( Sprite )serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].radialMenuButtonInfo.icon", EditorRadialMenuIndex.intValue ) ).objectReferenceValue;

											if( targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.sprite != null )
												targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.color = targ.radialMenu.iconNormalColor;
											else
												targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.color = Color.clear;

											targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].icon.enabled = true;
										}
									}
								}

								if( !registerSubmenu || targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].unityEvent.GetPersistentEventCount() > 0 )
								{
									if( registerSubmenu )
									{
										HelpBox( "This radial button has an event registered to it already. This could cause unwanted behavior with the static menu setup", "Please remove the event from the Ultimate Radial Menu button, or disable this submenu information", MessageType.Warning );
									}

									EditorGUI.BeginChangeCheck();
									EditorGUILayout.PropertyField( radialMenuComponent.FindProperty( string.Format( "UltimateRadialButtonList.Array.data[{0}].unityEvent", EditorRadialMenuIndex.intValue ) ) );
									if( EditorGUI.EndChangeCheck() )
										radialMenuComponent.ApplyModifiedProperties();
								}
							}
							EndCollapsibleSection( "URSM_RadialButtonInfo" );
						}

						if( !registerSubmenu && !menuButtonDisabled )
						{
							EditorGUILayout.BeginVertical( "Box" );
							HelpBox( "You have disabled using a submenu on this button", "If you would like to use and edit a submenu on this radial menu button, please make sure to enable it in the list above", MessageType.Warning );
							EditorGUILayout.EndVertical();
						}
						else if( !menuButtonDisabled )
						{
							// SUB BUTTON TOOLBAR //
							EditorGUILayout.BeginVertical( "Box" );
							GUILayout.Space( 1 );

							// SUB BUTTONS //
							EditorGUILayout.BeginHorizontal();
							if( GUILayout.Button( "◄", headerStyle, GUILayout.Width( 25 ) ) )
							{
								submenuButtonIndex = submenuButtonIndex == 0 ? targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count - 1 : submenuButtonIndex - 1;
								GUI.FocusControl( "" );
							}

							List<string> ButtonNamesArray = new List<string>();
							for( int i = 0; i < targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count; i++ )
								ButtonNamesArray.Add( $"Sub Button {i.ToString( "00" )}" );

							GUILayout.FlexibleSpace();

							EditorGUI.BeginChangeCheck();
							submenuButtonIndex = EditorGUILayout.Popup( submenuButtonIndex, ButtonNamesArray.ToArray(), headerStyle );
							if( EditorGUI.EndChangeCheck() )
								EditorGUIUtility.PingObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].buttonTransform );

							GUILayout.FlexibleSpace();
							if( GUILayout.Button( "►", headerStyle, GUILayout.Width( 25 ) ) )
							{
								submenuButtonIndex = submenuButtonIndex == targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count - 1 ? 0 : submenuButtonIndex + 1;
								GUI.FocusControl( "" );
							}
							EditorGUILayout.EndHorizontal();
							// END SUB BUTTONS //

							EditorGUILayout.BeginHorizontal();
							EditorGUI.BeginDisabledGroup( Application.isPlaying );
							if( GUILayout.Button( "Insert", EditorStyles.miniButtonLeft ) )
							{
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array", EditorRadialMenuIndex.intValue ) ).InsertArrayElementAtIndex( submenuButtonIndex + 1 );
								serializedObject.ApplyModifiedProperties();

								SerializedProperty buttonInfoProp = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].subButtonInfo", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) );

								buttonInfoProp.FindPropertyRelative( "name" ).stringValue = string.Empty;
								buttonInfoProp.FindPropertyRelative( "description" ).stringValue = string.Empty;
								buttonInfoProp.FindPropertyRelative( "icon" ).objectReferenceValue = null;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).boolValue = false;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].useIconUnique", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).boolValue = false;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconSize", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).floatValue = 0.0f;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconHorizontalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).floatValue = 0.0f;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconVerticalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).floatValue = 0.0f;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconRotation", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).floatValue = 0.0f;
								serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].invertScaleY", EditorRadialMenuIndex.intValue, submenuButtonIndex + 1 ) ).boolValue = false;
								serializedObject.ApplyModifiedProperties();

								submenuButtonIndex++;

								GenerateSubmenuButtons();
							}

							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count == 1 );
							if( GUILayout.Button( "Remove", EditorStyles.miniButtonRight ) )
							{
								if( EditorUtility.DisplayDialog( "Ultimate Radial Submenu", "Warning!\n\nAre you sure that you want to delete this static sub button information?", "Yes", "No" ) )
								{
									serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations", EditorRadialMenuIndex.intValue ) ).DeleteArrayElementAtIndex( submenuButtonIndex );
									serializedObject.ApplyModifiedProperties();

									if( submenuButtonIndex == targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count )
										submenuButtonIndex = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count - 1;

									GenerateSubmenuButtons();
								}
							}
							EditorGUI.EndDisabledGroup();
							EditorGUILayout.EndHorizontal();
							// END SUB BUTTON TOOLBAR //

							EditorGUILayout.Space();

							// This is an extra check, because sometimes the array hasn't been initialized so this will handle that before there can be any errors.
							if( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ) == null )
								SetupReorderableList();

							if( submenuButtonIndex > targ.UltimateRadialSubButtonList.Count )
								submenuButtonIndex = 0;

							EditorGUI.BeginChangeCheck();
							EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ), new GUIContent( "Disable Button" ) );
							if( EditorGUI.EndChangeCheck() )
							{
								serializedObject.ApplyModifiedProperties();

								string undoName = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).boolValue ? "Disable Submenu Button" : "Enable Submenu Button";

								Undo.RecordObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].buttonImage, undoName );

								if( useButtonIcon.boolValue && targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon != null )
									Undo.RecordObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon, undoName );

								if( useButtonText.boolValue && targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text != null )
									Undo.RecordObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text, undoName );

								if( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).boolValue )
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].OnDisable();
								else
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].OnEnable();
							}

							SerializedProperty prop = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].subButtonInfo", EditorRadialMenuIndex.intValue, submenuButtonIndex ) );
							EditorGUI.BeginDisabledGroup( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].buttonDisabled", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).boolValue );
							EditorGUI.BeginChangeCheck();
							EditorGUILayout.PropertyField( prop.FindPropertyRelative( "name" ) );
							if( EditorGUI.EndChangeCheck() )
							{
								serializedObject.ApplyModifiedProperties();

								if( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text != null && targ.radialMenu.displayNameOnButton )
								{
									Undo.RecordObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text, "Update Submenu Button Name" );
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text.enabled = false;
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text.text = prop.FindPropertyRelative( "name" ).stringValue;
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].text.enabled = true;
								}
							}

							if( prop.FindPropertyRelative( "description" ).stringValue == string.Empty && Event.current.type == EventType.Repaint )
							{
								textFieldStyle.normal.textColor = new Color( 0.5f, 0.5f, 0.5f, 0.75f );
								textFieldStyle.wordWrap = true;
								EditorGUILayout.TextField( GUIContent.none, "Description", textFieldStyle, GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) );
							}
							else
							{
								Event mEvent = Event.current;

								if( mEvent.type == EventType.KeyDown && mEvent.keyCode == KeyCode.Return )
								{
									GUI.SetNextControlName( "DescriptionField" );
									if( GUI.GetNameOfFocusedControl() == "DescriptionField" )
										GUI.FocusControl( "" );
								}

								EditorGUI.BeginChangeCheck();
								prop.FindPropertyRelative( "description" ).stringValue = EditorGUILayout.TextArea( prop.FindPropertyRelative( "description" ).stringValue, textFieldStyle, GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 ) );
								if( EditorGUI.EndChangeCheck() )
									serializedObject.ApplyModifiedProperties();
							}

							EditorGUILayout.Space();

							// ------------------------------------------- ICON SETTINGS ------------------------------------------- //
							if( useButtonIcon.boolValue )
							{
								EditorGUILayout.LabelField( "Icon Settings", EditorStyles.boldLabel );

								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( prop.FindPropertyRelative( "icon" ) );
								if( EditorGUI.EndChangeCheck() )
								{
									serializedObject.ApplyModifiedProperties();

									Undo.RecordObject( targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon, "Update Submenu Button Icon" );

									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon.enabled = false;
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon.sprite = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].subButtonInfo.icon;
									targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon.enabled = true;

									if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].subButtonInfo.icon != null )
										targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
									else
										targ.UltimateRadialSubButtonList[ submenuButtonIndex ].icon.color = Color.clear;
								}

								if( serializedObject.FindProperty( "iconLocalRotation" ).boolValue )
								{
									EditorGUI.BeginChangeCheck();
									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].invertScaleY", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ), new GUIContent( "Invert Y Scale" ) );
									if( EditorGUI.EndChangeCheck() )
									{
										serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].invertScaleY", submenuButtonIndex ) ).boolValue = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].invertScaleY", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).boolValue;
										serializedObject.ApplyModifiedProperties();
									}
								}

								SerializedProperty useIconUnique = serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].useIconUnique", EditorRadialMenuIndex.intValue, submenuButtonIndex ) );
								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( useIconUnique, new GUIContent( "Unique Positioning" ) );
								if( EditorGUI.EndChangeCheck() )
								{
									if( useIconUnique.boolValue )
									{
										if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconSize == 0.0f )
											serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconSize", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).floatValue = serializedObject.FindProperty( "iconSize" ).floatValue;

										if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconHorizontalPosition == 0.0f )
											serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconHorizontalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).floatValue = serializedObject.FindProperty( "iconHorizontalPosition" ).floatValue;

										if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconVerticalPosition == 0.0f )
											serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconVerticalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).floatValue = serializedObject.FindProperty( "iconVerticalPosition" ).floatValue;

										if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconRotation == 0.0f )
											serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconRotation", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ).floatValue = serializedObject.FindProperty( "iconRotation" ).floatValue;
									}
									serializedObject.ApplyModifiedProperties();

									serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].useIconUnique", submenuButtonIndex ) ).boolValue = useIconUnique.boolValue;
									serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconSize", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconSize;
									serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconHorizontalPosition", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconHorizontalPosition;
									serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconVerticalPosition", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconVerticalPosition;
									serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconRotation", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconRotation;

									serializedObject.ApplyModifiedProperties();
								}
								if( useIconUnique.boolValue )
								{
									EditorGUI.BeginChangeCheck();

									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconSize", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ) );
									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconHorizontalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ), new GUIContent( "Horizontal Position" ) );
									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconVerticalPosition", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ), new GUIContent( "Vertical Position" ) );
									EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].iconRotation", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ), new GUIContent( "Rotation Offset" ) );
									if( EditorGUI.EndChangeCheck() )
									{
										serializedObject.ApplyModifiedProperties();

										serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconSize", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconSize;
										serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconHorizontalPosition", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconHorizontalPosition;
										serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconVerticalPosition", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconVerticalPosition;
										serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconRotation", submenuButtonIndex ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].iconRotation;

										serializedObject.ApplyModifiedProperties();
									}
								}
								EditorGUILayout.Space();
							}

							// UNITY EVENTS //
							EditorGUILayout.BeginHorizontal();
							GUIStyle unityEventLabelStyle = new GUIStyle( GUI.skin.label );

							if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ submenuButtonIndex ].unityEvent.GetPersistentEventCount() > 0 )
								unityEventLabelStyle.fontStyle = FontStyle.Bold;

							EditorGUILayout.LabelField( "Unity Events", unityEventLabelStyle );
							GUILayout.FlexibleSpace();
							if( GUILayout.Button( EditorPrefs.GetBool( "URM_RadialButtonUnityEvents" ) ? "-" : "+", EditorStyles.miniButton, GUILayout.Width( 17 ) ) )
								EditorPrefs.SetBool( "URM_RadialButtonUnityEvents", !EditorPrefs.GetBool( "URM_RadialButtonUnityEvents" ) );
							EditorGUILayout.EndHorizontal();
							if( EditorPrefs.GetBool( "URM_RadialButtonUnityEvents" ) )
							{
								EditorGUI.BeginChangeCheck();
								EditorGUILayout.PropertyField( serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations.Array.data[{1}].unityEvent", EditorRadialMenuIndex.intValue, submenuButtonIndex ) ) );
								if( EditorGUI.EndChangeCheck() )
									serializedObject.ApplyModifiedProperties();
							}
							EditorGUI.EndDisabledGroup();

							if( GUILayout.Button( "Clear Sub Buttons", EditorStyles.miniButton ) )
							{
								if( EditorUtility.DisplayDialog( "Ultimate Radial Submenu", "Warning!\n\nAre you sure that you want to delete all of the sub buttons in this submenu information?", "Yes", "No" ) )
								{
									submenuButtonIndex = 0;

									serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations", EditorRadialMenuIndex.intValue ) ).ClearArray();
									serializedObject.ApplyModifiedProperties();

									serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].SubButtonInformations", EditorRadialMenuIndex.intValue ) ).arraySize = 1;
									serializedObject.ApplyModifiedProperties();

									GenerateSubmenuButtons();
									Repaint();
									return;
								}
							}
							GUILayout.Space( 1 );
							EditorGUILayout.EndVertical();
						}
					}
				}
			}
		}
		EndMainSection( "URM_RadialButtonList" );

		if( !isInPrefabScene && DisplayHeaderDropdown( "Script Reference", "UUI_ScriptReference" ) )
		{
			GUIStyle wordWrappedTextArea = new GUIStyle( GUI.skin.textArea ) { wordWrap = true };
			GUIStyle wordWrappedLabel = new GUIStyle( GUI.skin.label ) { wordWrap = true };

			EditorGUILayout.BeginVertical( "Box" );
			GUILayout.Space( 1 );

			if( targ.radialMenu != null && targ.radialMenu.radialMenuName != string.Empty )
				EditorGUILayout.LabelField( $"Radial Menu Name: {targ.radialMenu.radialMenuName}", EditorStyles.boldLabel );
			else
			{
				EditorGUILayout.LabelField( "Submenu Variable", EditorStyles.boldLabel );
				EditorGUILayout.TextArea( "public UltimateRadialSubmenu submenu;", wordWrappedTextArea );
				EditorGUILayout.LabelField( "Paste this into your variable declaration section of your script before trying to reference this Ultimate Radial Submenu.", wordWrappedLabel );
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Example Code Generator", EditorStyles.boldLabel );

			exampleCodeIndex = EditorGUILayout.Popup( "Function", exampleCodeIndex, exampleCodeOptions.ToArray() );
			ExampleCode[] ExampleCodes = PublicExampleCodes;
			if( targ.radialMenu != null && targ.radialMenu.radialMenuName != string.Empty )
				ExampleCodes = StaticExampleCodes;

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Function Description", EditorStyles.boldLabel );
			EditorGUILayout.LabelField( ExampleCodes[ exampleCodeIndex ].optionDescription, wordWrappedLabel );
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Example Code", EditorStyles.boldLabel );
			EditorGUILayout.TextArea( string.Format( ExampleCodes[ exampleCodeIndex ].basicCode, targ.radialMenu == null ? "UNASSIGNED" : targ.radialMenu.radialMenuName ), wordWrappedTextArea );

			if( exampleCodeIndex == 0 )
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField( "Needed Variable", EditorStyles.boldLabel );
				EditorGUILayout.TextArea( "UltimateRadialSubButtonInfo buttonInfo;", wordWrappedTextArea );
				EditorGUILayout.LabelField( "This variable is what you will pass to the submenu when registering a sub button to the menu. This variable can be used afterwards to communicate with the button that it is assigned to on the menu.", wordWrappedLabel );
			}

			GUILayout.Space( 1 );
			EditorGUILayout.EndVertical();

			if( GUILayout.Button( "Open Documentation" ) )
				UltimateRadialSubmenuReadmeEditor.OpenReadmeDocumentation();
		}
		EndMainSection( "UUI_ScriptReference" );

		EditorGUILayout.Space();

		if( !disableDragAndDrop && isDraggingObject )
			Repaint();

		if( isDirty || ( !isDirty && wasDirtyLastFrame ) )
			SceneView.RepaintAll();

		wasDirtyLastFrame = isDirty;
		isDirty = false;
	}

	void GenerateSubmenuButtons ()
	{
		// If there are current buttons, then clear the list.
		if( targ.UltimateRadialSubButtonList.Count > 0 )
		{
			for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
			{
				if( targ.UltimateRadialSubButtonList[ i ].buttonTransform != null )
				{
					if( targ.UltimateRadialSubButtonList[ i ].buttonTransform != null )
						Undo.DestroyObjectImmediate( targ.UltimateRadialSubButtonList[ i ].buttonTransform.gameObject );
				}
			}

			serializedObject.FindProperty( "UltimateRadialSubButtonList" ).ClearArray();
			serializedObject.ApplyModifiedProperties();
		}

		GameObject submenuButtonObject = new GameObject( "Submenu Button", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
		
		if( targ.NormalSprite != null )
		{
			submenuButtonObject.GetComponent<Image>().sprite = targ.NormalSprite;
			submenuButtonObject.GetComponent<Image>().color = targ.NormalColor;
		}
		else
			submenuButtonObject.GetComponent<Image>().color = Color.clear;

		submenuButtonObject.transform.SetParent( targ.transform );

		int buttonsToGenerate = 4;
		if( useStaticInformation.boolValue )
		{
			if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.SubmenuInformations.Count )
				buttonsToGenerate = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count;
		}

		for( int i = 0; i < buttonsToGenerate; i++ )
		{
			GameObject submenuButton = Instantiate( submenuButtonObject, Vector3.zero, Quaternion.identity );
			submenuButton.transform.SetParent( targ.transform );

			submenuButton.gameObject.name = "Submenu Button";

			RectTransform trans = submenuButton.GetComponent<RectTransform>();

			trans.anchorMin = new Vector2( 0.5f, 0.5f );
			trans.anchorMax = new Vector2( 0.5f, 0.5f );
			trans.pivot = new Vector2( 0.5f, 0.5f );

			serializedObject.FindProperty( "UltimateRadialSubButtonList" ).arraySize++;
			serializedObject.ApplyModifiedProperties();

			targ.UltimateRadialSubButtonList[ i ] = new UltimateRadialSubmenu.UltimateRadialSubButton();
			serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].submenu", i ) ).objectReferenceValue = targ;
			serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonTransform", i ) ).objectReferenceValue = trans;
			serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonImage", i ) ).objectReferenceValue = submenuButton.GetComponent<Image>();
			serializedObject.ApplyModifiedProperties();

			Undo.RegisterCreatedObjectUndo( submenuButton, "Create Submenu Button Objects" );
		}
		
		serializedObject.ApplyModifiedProperties();

		CheckForOptionalSettings();

		DestroyImmediate( submenuButtonObject );
	}
	
	void CheckForOptionalSettings ()
	{
		CheckForButtonIcon();
		CheckForButtonText();

		if( useStaticInformation.boolValue )
		{
			if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.SubmenuInformations.Count )
			{
				for( int i = 0; i < targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count; i++ )
				{
					if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].buttonDisabled )
					{
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].buttonDisabled", i ) ).boolValue = true;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
	}

	void CheckForButtonIcon ()
	{
		for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
		{
			if( !useButtonIcon.boolValue )
			{
				if( targ.UltimateRadialSubButtonList[ i ].icon != null )
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon.gameObject, "Disable Sub Button Icon" );
					targ.UltimateRadialSubButtonList[ i ].icon.gameObject.SetActive( false );
				}
			}
			else
			{
				if( targ.UltimateRadialSubButtonList[ i ].icon == null )
				{
					GameObject subButtonIcon = new GameObject( "Submenu Button Icon", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );

					Image iconImage = subButtonIcon.GetComponent<Image>();
					iconImage.raycastTarget = false;
					if( !useStaticInformation.boolValue && serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue != null )
						iconImage.sprite = ( Sprite )serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue;

					iconImage.color = iconImage.sprite != null ? serializedObject.FindProperty( "iconNormalColor" ).colorValue : Color.clear;

					subButtonIcon.transform.SetParent( targ.UltimateRadialSubButtonList[ i ].buttonTransform );
					subButtonIcon.transform.SetAsLastSibling();

					RectTransform iconTrans = subButtonIcon.GetComponent<RectTransform>();
					iconTrans.anchorMin = new Vector2( 0.5f, 0.5f );
					iconTrans.anchorMax = new Vector2( 0.5f, 0.5f );
					iconTrans.offsetMin = Vector2.zero;
					iconTrans.offsetMax = Vector2.zero;
					iconTrans.localScale = Vector3.one;

					serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].icon", i ) ).objectReferenceValue = iconImage;
					serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconTransform", i ) ).objectReferenceValue = iconTrans;
					serializedObject.ApplyModifiedProperties();

					Undo.RegisterCreatedObjectUndo( subButtonIcon, "Create Submenu Button Objects" );
				}
				else
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon.gameObject, "Enable Submenu Button Icon" );
					targ.UltimateRadialSubButtonList[ i ].icon.gameObject.SetActive( true );

					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Enable Submenu Button Icon" );
					if( !useStaticInformation.boolValue && serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue != null )
						targ.UltimateRadialSubButtonList[ i ].icon.sprite = ( Sprite )serializedObject.FindProperty( "IconPlaceholderSprite" ).objectReferenceValue;

					targ.UltimateRadialSubButtonList[ i ].icon.color = targ.UltimateRadialSubButtonList[ i ].icon.sprite != null ? serializedObject.FindProperty( "iconNormalColor" ).colorValue : Color.clear;
				}
			}
		}

		if( useStaticInformation.boolValue )
		{
			if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.SubmenuInformations.Count )
			{
				for( int i = 0; i < targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count; i++ )
				{
					if( targ.UltimateRadialSubButtonList[ i ].icon != null && targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.icon != null )
					{
						Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].icon, "Update Submenu Button Icon" );

						targ.UltimateRadialSubButtonList[ i ].icon.sprite = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.icon;
						targ.UltimateRadialSubButtonList[ i ].icon.enabled = false;
						targ.UltimateRadialSubButtonList[ i ].icon.enabled = true;

						if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.icon != null )
						{
							if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].buttonDisabled && serializedObject.FindProperty( "iconColorChange" ).boolValue )
								targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconDisabledColor" ).colorValue;
							else
								targ.UltimateRadialSubButtonList[ i ].icon.color = serializedObject.FindProperty( "iconNormalColor" ).colorValue;
						}
						else
							targ.UltimateRadialSubButtonList[ i ].icon.color = Color.clear;
					}

					if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].useIconUnique )
					{
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].useIconUnique", i ) ).boolValue = true;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconSize", i ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].iconSize;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconHorizontalPosition", i ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].iconHorizontalPosition;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconVerticalPosition", i ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].iconVerticalPosition;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].iconRotation", i ) ).floatValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].iconRotation;
						serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].invertScaleY", i ) ).boolValue = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].invertScaleY;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
	}

	void CheckForButtonText ()
	{
		if( serializedObject.FindProperty( "buttonTextFont" ).objectReferenceValue == null )
		{
			serializedObject.FindProperty( "buttonTextFont" ).objectReferenceValue = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
			serializedObject.ApplyModifiedProperties();
		}

		for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
		{
			if( useButtonText.boolValue )
			{
				if( targ.UltimateRadialSubButtonList[ i ].text == null )
				{
					GameObject newText = new GameObject( "Submenu Button Text", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Text ) );

					newText.transform.SetParent( targ.UltimateRadialSubButtonList[ i ].buttonTransform );
					newText.transform.SetAsLastSibling();
					
					newText.GetComponent<RectTransform>().localScale = Vector3.one;
					newText.GetComponent<RectTransform>().pivot = new Vector2( 0.5f, 0.5f );

					Text textComponent = newText.GetComponent<Text>();
					if( !useStaticInformation.boolValue && subButtonTextTemp != string.Empty )
						textComponent.text = subButtonTextTemp;
					else
						textComponent.text = "Text";

					textComponent.resizeTextForBestFit = true;
					textComponent.resizeTextMinSize = 0;
					textComponent.resizeTextMaxSize = 300;
					textComponent.alignment = TextAnchor.MiddleCenter;
					textComponent.color = serializedObject.FindProperty( "textNormalColor" ).colorValue;

					if( serializedObject.FindProperty( "buttonTextFont" ).objectReferenceValue != null )
						textComponent.font = ( Font )serializedObject.FindProperty( "buttonTextFont" ).objectReferenceValue;

					if( serializedObject.FindProperty( "buttonTextOutline" ).boolValue && !newText.gameObject.GetComponent<UnityEngine.UI.Outline>() )
					{
						UnityEngine.UI.Outline outline = newText.gameObject.AddComponent<UnityEngine.UI.Outline>();
						outline.effectColor = serializedObject.FindProperty( "buttonTextOutlineColor" ).colorValue;
					}

					serializedObject.FindProperty( string.Format( "UltimateRadialSubButtonList.Array.data[{0}].text", i ) ).objectReferenceValue = textComponent;
					serializedObject.ApplyModifiedProperties();

					Undo.RegisterCreatedObjectUndo( newText, "Create Submenu Text Objects" );
				}
				else
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text.gameObject, "Enable Button Text" );
					targ.UltimateRadialSubButtonList[ i ].text.gameObject.SetActive( true );
				}
			}
			else
			{
				if( targ.UltimateRadialSubButtonList[ i ].text != null )
				{
					Undo.RecordObject( targ.UltimateRadialSubButtonList[ i ].text.gameObject, "Disable Button Text" );
					targ.UltimateRadialSubButtonList[ i ].text.gameObject.SetActive( false );
				}
			}
		}

		if( useStaticInformation.boolValue )
		{
			if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.SubmenuInformations.Count )
			{
				for( int i = 0; i < targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations.Count; i++ )
				{
					targ.UltimateRadialSubButtonList[ i ].name = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.name;
					targ.UltimateRadialSubButtonList[ i ].description = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.description;
					if( targ.UltimateRadialSubButtonList[ i ].text != null && targ.radialMenu.displayNameOnButton )
					{
						targ.UltimateRadialSubButtonList[ i ].text.text = targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].subButtonInfo.name;

						if( targ.SubmenuInformations[ EditorRadialMenuIndex.intValue ].SubButtonInformations[ i ].buttonDisabled && serializedObject.FindProperty( "textColorChange" ).boolValue )
							targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textDisabledColor" ).colorValue;
						else
							targ.UltimateRadialSubButtonList[ i ].text.color = serializedObject.FindProperty( "textNormalColor" ).colorValue;
					}
				}
			}
		}
	}

	void OnSceneGUI ()
	{
		if( targ == null || Selection.activeGameObject == null || Selection.activeGameObject != targ.gameObject )
			return;

		float centerAngle = 90.0f;
		float parentCanvasScale = 1.0f;

		if( targ.radialMenu != null )
		{
			if( EditorRadialMenuIndex.intValue >= 0 && EditorRadialMenuIndex.intValue < targ.radialMenu.UltimateRadialButtonList.Count )
				centerAngle = targ.radialMenu.UltimateRadialButtonList[ EditorRadialMenuIndex.intValue ].angle;

			if( targ.radialMenu.ParentCanvas != null )
				parentCanvasScale = targ.radialMenu.ParentCanvas.GetComponent<RectTransform>().localScale.x;
		}

		RectTransform submenuTransform = targ.transform.GetComponent<RectTransform>();
		float submenuSize = submenuTransform.sizeDelta.x * parentCanvasScale;
		
		// Stored serialized data and configure needed variables.
		float submenuDeactivationAngle = serializedObject.FindProperty( "submenuDeactivationAngle" ).floatValue;
		float minRange = serializedObject.FindProperty( "minRange" ).floatValue;
		float maxRange = serializedObject.FindProperty( "maxRange" ).floatValue;
		float halfTotalAngle = ( serializedObject.FindProperty( "anglePerButton" ).floatValue * ( targ.UltimateRadialSubButtonList.Count <= 0 ? 4 : targ.UltimateRadialSubButtonList.Count ) ) / 2.0f;
		float startAngle = centerAngle + halfTotalAngle;
		float angleInRadians = serializedObject.FindProperty( "anglePerButton" ).floatValue * Mathf.Deg2Rad;
		Vector3 startingDirection = Quaternion.AngleAxis( centerAngle - halfTotalAngle - submenuDeactivationAngle, Vector3.forward ) * Vector3.right;

		// MIN RANGE //
		Handles.color = colorDefault;
		if( DisplayMinRange.HighlightGizmo )
			Handles.color = colorValueChanged;
		else if( !EditorPrefs.GetBool( "URM_RadialMenuPositioning" ) && targ.NormalSprite == null )
		{
			Color halfColor = colorDefault;
			halfColor.a /= 2;
			Handles.color = halfColor;
		}
		Handles.DrawWireArc( submenuTransform.position, Vector3.forward, startingDirection, ( halfTotalAngle * 2 ) + ( submenuDeactivationAngle * 2 ), ( submenuSize / 2 ) * minRange );

		// RADIAL MENU MIN RANGE //
		if( targ.radialMenu != null )
		{
			Color radialMenuInfoColor = colorDefault;
			radialMenuInfoColor.a /= 2;
			Handles.color = radialMenuInfoColor;
			Handles.DrawWireDisc( submenuTransform.position, Selection.activeGameObject.transform.forward, ( submenuSize / 2 ) * targ.radialMenu.maxRange );
			Handles.Label( submenuTransform.position + ( -submenuTransform.transform.up * ( ( submenuSize / 2 ) * targ.radialMenu.maxRange ) ), "Radial Menu Range: " + targ.radialMenu.maxRange, new GUIStyle( EditorStyles.label ) { alignment = TextAnchor.LowerCenter, fixedWidth = 500, fontStyle = FontStyle.Bold } );
		}

		// MAX RANGE //
		if( !serializedObject.FindProperty( "infiniteMaxRange" ).boolValue )
		{
			Handles.color = colorDefault;
			if( DisplayMaxRange.HighlightGizmo )
				Handles.color = colorValueChanged;
			else if( !EditorPrefs.GetBool( "URM_RadialMenuPositioning" ) && targ.NormalSprite == null )
			{
				Color halfColor = colorDefault;
				halfColor.a /= 2;
				Handles.color = halfColor;
			}
			Handles.DrawWireArc( submenuTransform.position, Vector3.forward, startingDirection, ( halfTotalAngle * 2 ) + ( submenuDeactivationAngle * 2 ), ( submenuSize / 2 ) * maxRange );
		}

		// DEACTIVATION ANGLE //
		if( submenuDeactivationAngle > 0.0f )
		{
			Vector3 leftDeactivationAngle = Vector3.zero;
			leftDeactivationAngle.x += Mathf.Cos( ( startAngle * Mathf.Deg2Rad ) + ( submenuDeactivationAngle * Mathf.Deg2Rad ) );
			leftDeactivationAngle.y += Mathf.Sin( ( startAngle * Mathf.Deg2Rad ) + ( submenuDeactivationAngle * Mathf.Deg2Rad ) );
			Vector3 rightDeactivationAngle = Vector3.zero;
			rightDeactivationAngle.x += Mathf.Cos( ( ( startAngle - halfTotalAngle * 2 ) * Mathf.Deg2Rad ) - ( submenuDeactivationAngle * Mathf.Deg2Rad ) );
			rightDeactivationAngle.y += Mathf.Sin( ( ( startAngle - halfTotalAngle * 2 ) * Mathf.Deg2Rad ) - ( submenuDeactivationAngle * Mathf.Deg2Rad ) );

			Handles.color = colorDefault;
			if( DisplayDeactivationAngle.HighlightGizmo )
				Handles.color = colorValueChanged;
			else if( !EditorPrefs.GetBool( "URM_RadialMenuPositioning" ) && targ.NormalSprite == null )
			{
				Color halfColor = colorDefault;
				halfColor.a /= 2;
				Handles.color = halfColor;
			}

			Handles.DrawLine( submenuTransform.position + leftDeactivationAngle * ( submenuSize / 2 * minRange ), submenuTransform.position + leftDeactivationAngle * ( submenuSize / 2 * maxRange ) );
			Handles.DrawLine( submenuTransform.position + rightDeactivationAngle * ( submenuSize / 2 * minRange ), submenuTransform.position + rightDeactivationAngle * ( submenuSize / 2 * maxRange ) );
		}

		// BUTTON TEXT BOXES //
		if( EditorPrefs.GetBool( "URM_RadialMenuOptions" ) && EditorPrefs.GetBool( "URM_ButtonText" ) && useButtonText.boolValue )
		{
			Handles.color = colorTextBox;
			for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
			{
				if( targ.UltimateRadialSubButtonList[ i ].text == null )
					continue;

				DrawWireBox( targ.UltimateRadialSubButtonList[ i ].text.rectTransform.InverseTransformPoint( targ.UltimateRadialSubButtonList[ i ].text.rectTransform.position ), targ.UltimateRadialSubButtonList[ i ].text.rectTransform.sizeDelta, targ.UltimateRadialSubButtonList[ i ].text.rectTransform );
			}
		}

		// STATIC BUTTON INFO //
		if( EditorPrefs.GetBool( "URM_RadialButtonList" ) && useStaticInformation.boolValue )
		{
			if( useStaticInformation.boolValue && !serializedObject.FindProperty( string.Format( "SubmenuInformations.Array.data[{0}].registerSubmenu", EditorRadialMenuIndex.intValue ) ).boolValue )
			{
				Handles.color = Color.red;
				Handles.DrawWireArc( submenuTransform.position, Vector3.forward, startingDirection, ( halfTotalAngle * 2 ) + ( submenuDeactivationAngle * 2 ), ( submenuSize / 2 ) * ( Mathf.Lerp( minRange, maxRange, 0.5f ) ) );
			}

			for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
			{
				if( submenuButtonIndex == i )
					Handles.color = colorButtonSelected;
				else
					Handles.color = colorButtonUnselected;

				float handleSize = submenuSize / 10;
				float distanceMod = ( ( submenuSize + targ.UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta.y ) * serializedObject.FindProperty( "submenuDistance" ).floatValue ) + handleSize;

				Vector3 difference = submenuTransform.position - targ.UltimateRadialSubButtonList[ i ].buttonTransform.position;

				Vector3 handlePos = submenuTransform.position;
				handlePos += -difference.normalized * distanceMod;

				if( Handles.Button( handlePos, Quaternion.identity, handleSize, submenuSize / 10, Handles.SphereHandleCap ) )
				{
					submenuButtonIndex = i;
					EditorGUIUtility.PingObject( targ.UltimateRadialSubButtonList[ i ].buttonTransform );
				}
				GUIStyle labelStyle = new GUIStyle( GUI.skin.label )
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
				};
				Handles.Label( handlePos, i.ToString( "00" ), labelStyle );
			}
		}

		// DISPLAY INVISIBLE BUTTONS //
		if( targ.NormalSprite == null && targ.UltimateRadialSubButtonList.Count > 0 )
		{
			Handles.color = colorDefault;
			if( EditorPrefs.GetBool( "URM_RadialMenuPositioning" ) )
			{
				Color halfColor = colorDefault;
				halfColor.a /= 2;
				Handles.color = halfColor;
			}
			// Else the positioning section isn't open, so draw the min and max ranges so that the null sprite buttons look better.
			else
			{
				// Recalculate the starting direction so that the ranges can be displayed with the deactivation angle included.
				startingDirection = Quaternion.AngleAxis( centerAngle - halfTotalAngle, Vector3.forward ) * Vector3.right;

				// MIN RANGE //
				Handles.DrawWireArc( submenuTransform.position, Vector3.forward, startingDirection, ( halfTotalAngle * 2 ), ( submenuSize / 2 ) * minRange );

				// MAX RANGE //
				Handles.DrawWireArc( submenuTransform.position, Vector3.forward, startingDirection, ( halfTotalAngle * 2 ), ( submenuSize / 2 ) * maxRange );
			}

			if( serializedObject.FindProperty( "followOrbitalRotation" ).boolValue )
			{
				float lineOuterRadius = ( submenuSize / 2 ) * maxRange;
				float lineInnerRadius = ( submenuSize / 2 ) * minRange;
				
				float halfAngle = halfTotalAngle * Mathf.Deg2Rad;
				
				for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
				{
					Vector3 lineStart = Vector3.zero;
					lineStart.x += Mathf.Cos( ( startAngle * Mathf.Deg2Rad ) - ( angleInRadians * i ) ) * lineOuterRadius;
					lineStart.y += Mathf.Sin( ( startAngle * Mathf.Deg2Rad ) - ( angleInRadians * i ) ) * lineOuterRadius;

					Vector3 lineEnd = Vector3.zero;
					lineEnd.x += Mathf.Cos( ( startAngle * Mathf.Deg2Rad ) - ( angleInRadians * i ) ) * lineInnerRadius;
					lineEnd.y += Mathf.Sin( ( startAngle * Mathf.Deg2Rad ) - ( angleInRadians * i ) ) * lineInnerRadius;

					lineStart = targ.transform.TransformPoint( lineStart );
					lineEnd = targ.transform.TransformPoint( lineEnd );

					Handles.DrawLine( lineStart, lineEnd );
				}

				// Show last button end line.
				Vector3 lastButtonEnd = Vector3.zero;
				lastButtonEnd.x += Mathf.Cos( ( startAngle - halfTotalAngle * 2 ) * Mathf.Deg2Rad );
				lastButtonEnd.y += Mathf.Sin( ( startAngle - halfTotalAngle * 2 ) * Mathf.Deg2Rad );
				Handles.DrawLine( submenuTransform.position + lastButtonEnd * ( submenuSize / 2 * minRange ), submenuTransform.position + lastButtonEnd * ( submenuSize / 2 * maxRange ) );
			}
			else
			{
				for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
					DrawWireBox( targ.UltimateRadialSubButtonList[ i ].buttonTransform.localPosition, targ.UltimateRadialSubButtonList[ i ].buttonTransform.sizeDelta, targ.transform );
			}
		}

		// EMPTY ICONS //
		if( ( EditorPrefs.GetBool( "URM_ButtonIcon" ) || EditorPrefs.GetBool( "URM_RadialButtonList" ) ) && useButtonIcon.boolValue )
		{
			Handles.color = colorDefault;
			for( int i = 0; i < targ.UltimateRadialSubButtonList.Count; i++ )
			{
				if( targ.UltimateRadialSubButtonList[ i ].icon == null || targ.UltimateRadialSubButtonList[ i ].iconTransform == null || targ.UltimateRadialSubButtonList[ i ].icon.sprite != null )
					continue;

				Handles.DrawWireDisc( targ.UltimateRadialSubButtonList[ i ].iconTransform.position, Selection.activeGameObject.transform.forward, ( targ.UltimateRadialSubButtonList[ i ].iconTransform.sizeDelta.x * parentCanvasScale ) / 2 );

				GUIStyle labelStyle = new GUIStyle( GUI.skin.label )
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
				};
				Handles.Label( targ.UltimateRadialSubButtonList[ i ].iconTransform.position, "Icon", labelStyle );
			}
		}
	}

	void DrawWireBox ( Vector3 center, Vector2 sizeDelta, Transform trans )
	{
		float halfHeight = sizeDelta.y / 2;
		float halfWidth = sizeDelta.x / 2;

		Vector3 topLeft = center + new Vector3( -halfWidth, halfHeight, 0 );
		Vector3 topRight = center + new Vector3( halfWidth, halfHeight, 0 );
		Vector3 bottomRight = center + new Vector3( halfWidth, -halfHeight, 0 );
		Vector3 bottomLeft = center + new Vector3( -halfWidth, -halfHeight, 0 );

		topLeft = trans.TransformPoint( topLeft );
		topRight = trans.TransformPoint( topRight );
		bottomRight = trans.TransformPoint( bottomRight );
		bottomLeft = trans.TransformPoint( bottomLeft );

		Handles.DrawLine( topLeft, topRight );
		Handles.DrawLine( topRight, bottomRight );
		Handles.DrawLine( bottomRight, bottomLeft );
		Handles.DrawLine( bottomLeft, topLeft );
	}
}