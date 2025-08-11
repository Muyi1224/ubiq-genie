/* UltimateRadialSubmenuReadme.cs */
/* Written by Kaz */
using UnityEngine;
using System.Collections.Generic;

//[CreateAssetMenu( fileName = "README", menuName = "Tank and Healer Studio/Ultimate Radial Submenu README File", order = 1 )]
public class UltimateRadialSubmenuReadme : ScriptableObject
{
	public Texture2D icon;
	public Texture2D scriptReference;
	public Texture2D settings;

	// GIZMO COLORS //
	[HideInInspector]
	public Color colorDefault = Color.black;
	[HideInInspector]
	public Color colorValueChanged = Color.cyan;
	[HideInInspector]
	public Color colorButtonSelected = Color.yellow;
	[HideInInspector]
	public Color colorButtonUnselected = Color.white;
	[HideInInspector]
	public Color colorTextBox = Color.yellow;

	public static int ImportantChange = 0;
	public class VersionHistory
	{
		public string versionNumber = "";
		public string[] changes;
		public bool showMore = false;
	}
	public VersionHistory[] versionHistory = new VersionHistory[]
	{
		// VERSION 1.1.0
		new VersionHistory ()
		{
			versionNumber = "1.1.0",
			changes = new string[]
			{
				// ADDED //
				"Added full support for TextMeshPro for users that want it. This is available from the README file, which will import a UnityPackage that will update the code to use TextMeshPro",
				"Added the ability to determine which menu buttons have submenus through the static method of using the submenu",
				"Added a new callback for when a submenu button is hovered: OnButtonHover",
				"Added a new callback for when the submenu gains focus: OnMenuFocused",
				// BUG FIXES //
				"Fixed an error that could happen if the user had the Ultimate Radial Submenu object selected when playing the scene in the editor",
				// QUALITY OF LIFE //
				"Moved the Development Inspector to the top of the inspector to make it easier to access if Development Mode is enabled",
			},
		},
		// VERSION 1.0.1
		new VersionHistory ()
		{
			versionNumber = "1.0.1",
			changes = new string[]
			{
				// BUG FIXES //
				"Fixed warnings in Unity 2022.2+ dealing with obsolete FindObjectOfType references",
			},
		},
		// VERSION 1.0.0
		new VersionHistory ()
		{
			versionNumber = "1.0.0",
			changes = new string[]
			{
				"Initial Release",
			},
		},
	};

	[HideInInspector]
	public List<int> pageHistory = new List<int>();
	[HideInInspector]
	public Vector2 scrollValue = new Vector2();
}