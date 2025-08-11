/* SubmenuPrefabDisplay.cs */
/* Written by Kaz */
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SubmenuPrefabDisplay : MonoBehaviour
{
	[Header( "Common" )]
	public Text prefabNameText;
	public int radialMenuButtons = 4;
	public UltimateRadialButtonInfo radialMenuButtonInfo;
	public int submenuButtons = 4;
	public UltimateRadialSubButtonInfo submenuButtonInfo;
	int currentSubmenuIndex = 0;

	[Header( "Blank" )]
	public UltimateRadialMenu radialMenu_Blank;
	public GameObject submenuPrefab_Blank00;
	public GameObject submenuPrefab_Blank01;
	public GameObject submenuPrefab_Blank02;

	[Header( "Circle" )]
	public UltimateRadialMenu radialMenu_Circle;
	public GameObject submenuPrefab_Circle;

	[Header( "Dark" )]
	public UltimateRadialMenu radialMenu_Dark;
	public GameObject submenuPrefab_Dark00;
	public GameObject submenuPrefab_Dark01;
	public GameObject submenuPrefab_Dark02;
	public GameObject submenuPrefab_Dark03;

	[Header( "Gun Wheel" )]
	public UltimateRadialMenu radialMenu_GunWheel;
	public GameObject submenuPrefab_GunWheel;

	[Header( "Hexagon" )]
	public UltimateRadialMenu radialMenu_Hexagon;
	public GameObject submenuPrefab_Hexagon;

	[Header( "Inverse" )]
	public UltimateRadialMenu radialMenu_Inverse;
	public GameObject submenuPrefab_Inverse00;
	public GameObject submenuPrefab_Inverse01;
	public GameObject submenuPrefab_Inverse02;

	[Header( "Item Wheel" )]
	public UltimateRadialMenu radialMenu_ItemWheel;
	public GameObject submenuPrefab_ItemWheel;

	[Header( "Minimalist" )]
	public UltimateRadialMenu radialMenu_Minimalist;
	public GameObject submenuPrefab_Minimalist00;
	public GameObject submenuPrefab_Minimalist01;
	public GameObject submenuPrefab_Minimalist02;

	[Header( "RPG" )]
	public UltimateRadialMenu radialMenu_RPG;
	public GameObject submenuPrefab_RPG00;
	public GameObject submenuPrefab_RPG01;
	public GameObject submenuPrefab_RPG02;

	List<UltimateRadialSubmenu> allSubmenus = new List<UltimateRadialSubmenu>();
	List<GameObject> allPrefabs = new List<GameObject>();


	private void Start ()
	{
		// Add and create all of the blank submenus.
		allSubmenus.Add( Instantiate( submenuPrefab_Blank00, radialMenu_Blank.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Blank00 );
		allSubmenus.Add( Instantiate( submenuPrefab_Blank01, radialMenu_Blank.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Blank01 );
		allSubmenus.Add( Instantiate( submenuPrefab_Blank02, radialMenu_Blank.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Blank02 );

		// Add and create the circle submenu.
		allSubmenus.Add( Instantiate( submenuPrefab_Circle, radialMenu_Circle.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Circle );
		radialMenu_Circle.gameObject.SetActive( false );

		// Add and create all of the dark submenus.
		allSubmenus.Add( Instantiate( submenuPrefab_Dark00, radialMenu_Dark.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Dark00 );
		allSubmenus.Add( Instantiate( submenuPrefab_Dark01, radialMenu_Dark.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Dark01 );
		allSubmenus.Add( Instantiate( submenuPrefab_Dark02, radialMenu_Dark.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Dark02 );
		allSubmenus.Add( Instantiate( submenuPrefab_Dark03, radialMenu_Dark.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Dark03 );
		radialMenu_Dark.gameObject.SetActive( false );

		// Add and create the gun wheel submenu.
		allSubmenus.Add( Instantiate( submenuPrefab_GunWheel, radialMenu_GunWheel.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_GunWheel );
		radialMenu_GunWheel.gameObject.SetActive( false );

		// Add and create the hexagon submenu.
		allSubmenus.Add( Instantiate( submenuPrefab_Hexagon, radialMenu_Hexagon.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Hexagon );
		radialMenu_Hexagon.gameObject.SetActive( false );

		// Add and create all of the inverse submenus.
		allSubmenus.Add( Instantiate( submenuPrefab_Inverse00, radialMenu_Inverse.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Inverse00 );
		allSubmenus.Add( Instantiate( submenuPrefab_Inverse01, radialMenu_Inverse.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Inverse01 );
		allSubmenus.Add( Instantiate( submenuPrefab_Inverse02, radialMenu_Inverse.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Inverse02 );
		radialMenu_Inverse.gameObject.SetActive( false );

		// Add and create the item wheel submenu.
		allSubmenus.Add( Instantiate( submenuPrefab_ItemWheel, radialMenu_ItemWheel.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_ItemWheel );
		radialMenu_ItemWheel.gameObject.SetActive( false );

		// Add and create all of the minimalist submenus.
		allSubmenus.Add( Instantiate( submenuPrefab_Minimalist00, radialMenu_Minimalist.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Minimalist00 );
		allSubmenus.Add( Instantiate( submenuPrefab_Minimalist01, radialMenu_Minimalist.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Minimalist01 );
		allSubmenus.Add( Instantiate( submenuPrefab_Minimalist02, radialMenu_Minimalist.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_Minimalist02 );
		radialMenu_Minimalist.gameObject.SetActive( false );

		// Add and create all of the RPG submenus.
		allSubmenus.Add( Instantiate( submenuPrefab_RPG00, radialMenu_RPG.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_RPG00 );
		allSubmenus.Add( Instantiate( submenuPrefab_RPG01, radialMenu_RPG.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_RPG01 );
		allSubmenus.Add( Instantiate( submenuPrefab_RPG02, radialMenu_RPG.transform ).GetComponent<UltimateRadialSubmenu>() );
		allPrefabs.Add( submenuPrefab_RPG02 );
		radialMenu_RPG.gameObject.SetActive( false );

		// Register the dummy functions for the blank radial menu to start.
		RegisterDummyRadialMenuButtons( radialMenu_Blank );
	}

	public void NextSubmenu ()
	{
		// Configure the target index as one more than the current.
		int targetSubmenuIndex = currentSubmenuIndex + 1;

		// If the target is out of range, then set it to the 0 index.
		if( targetSubmenuIndex >= allSubmenus.Count )
			targetSubmenuIndex = 0;

		// Clear the menu of the current menu.
		allSubmenus[ currentSubmenuIndex ].radialMenu.ClearMenu();

		// If the current radial menu is different than the target radial menu that we will be using next...
		if( allSubmenus[ currentSubmenuIndex ].radialMenu != allSubmenus[ targetSubmenuIndex ].radialMenu )
		{
			// Disable the current one and enable the new one.
			allSubmenus[ currentSubmenuIndex ].radialMenu.gameObject.SetActive( false );
			allSubmenus[ targetSubmenuIndex ].radialMenu.gameObject.SetActive( true );
		}

		// Register the dummy buttons to the target radial menu.
		RegisterDummyRadialMenuButtons( allSubmenus[ targetSubmenuIndex ].radialMenu );

		// Update the current submenu index.
		currentSubmenuIndex = targetSubmenuIndex;
	}

	public void PreviousSubmenu ()
	{
		// Configure the target index.
		int targetIndex = currentSubmenuIndex - 1;

		// If the target index is out of range, then set it to the end of the list.
		if( targetIndex < 0 )
			targetIndex = allSubmenus.Count - 1;

		// Clear the menu of the current menu.
		allSubmenus[ currentSubmenuIndex ].radialMenu.ClearMenu();

		// If the current radial menu is different than the target radial menu that we will be using next...
		if( allSubmenus[ currentSubmenuIndex ].radialMenu != allSubmenus[ targetIndex ].radialMenu )
		{
			// Disable the current one and enable the new one.
			allSubmenus[ currentSubmenuIndex ].radialMenu.gameObject.SetActive( false );
			allSubmenus[ targetIndex ].radialMenu.gameObject.SetActive( true );
		}

		// Register the dummy buttons to the target radial menu.
		RegisterDummyRadialMenuButtons( allSubmenus[ targetIndex ].radialMenu );

		// Update the current submenu index.
		currentSubmenuIndex = targetIndex;
	}

	public void SelectPrefabInProject ()
	{
		// If this is run in the editor, then select the prefab gameobject in the project window.
		#if UNITY_EDITOR
		UnityEditor.Selection.activeGameObject = allPrefabs[ currentSubmenuIndex ];
		#endif
	}

	// Increase button count and re-register the submenu buttons.
	public void IncreaseSubmenuButtonCount ()
	{
		submenuButtons++;
		RegisterDummySubmenuButtons();
	}

	// Decrease the button count ONLY if it's over 1, and then re-register the submenu buttons.
	public void DecreaseSubmenuButtonCount ()
	{
		if( submenuButtons <= 1 )
			return;

		submenuButtons--;
		RegisterDummySubmenuButtons();
	}

	// Increase radial button count and re-register the radial menu buttons.
	public void IncreaseRadialButtonCount ()
	{
		radialMenuButtons++;
		RegisterDummyRadialMenuButtons( allSubmenus[ currentSubmenuIndex ].radialMenu );
	}

	// Decrease the radial menu button count ONLY if it's over 2, and then re-register the buttons.
	public void DecreaseRadialButtonCount ()
	{
		if( radialMenuButtons <= 2 )
			return;

		radialMenuButtons--;
		RegisterDummyRadialMenuButtons( allSubmenus[ currentSubmenuIndex ].radialMenu );
	}

	void RegisterDummyRadialMenuButtons ( UltimateRadialMenu radialMenu )
	{
		// Clear the radial menu.
		radialMenu.ClearMenu();

		// Loop through the amount of buttons to display, registered dummy info to them.
		for( int i = 0; i < radialMenuButtons; i++ )
			radialMenu.RegisterButton( RegisterDummySubmenuButtons, new UltimateRadialButtonInfo() { icon = radialMenuButtonInfo.icon } );

		// Update the name of the current prefab.
		prefabNameText.text = "Prefab name: " + allSubmenus[ currentSubmenuIndex ].name.Split( '(' )[ 0 ];
	}

	void RegisterDummySubmenuButtons ()
	{
		// Clear the submenu.
		allSubmenus[ currentSubmenuIndex ].ClearMenu();

		// Loop through the amount of buttons to display, registered dummy info to them.
		for( int i = 0; i < submenuButtons; i++ )
			allSubmenus[ currentSubmenuIndex ].RegisterButton( DummyVoid, new UltimateRadialSubButtonInfo() { icon = submenuButtonInfo.icon } );

		// Enable the submenu.
		allSubmenus[ currentSubmenuIndex ].Enable();
	}

	// Literally nothing. Just a dummy void.
	void DummyVoid ()
	{

	}
}