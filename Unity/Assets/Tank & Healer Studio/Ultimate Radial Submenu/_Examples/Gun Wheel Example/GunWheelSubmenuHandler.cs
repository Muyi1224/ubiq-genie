/* GunWheelSubmenuHandler.cs */
/* Written by Kaz */
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UltimateRadialSubmenuExample
{
	public class GunWheelSubmenuHandler : MonoBehaviour
	{
		[System.Serializable]
		public class WeaponBase
		{
			public string name, description;

			public int damage = 10;
			public int roundsPerMinute = 100;

			public Sprite weaponIcon;
		}

		// Weapons List Info
		public WeaponBase[] LightWeapons;
		public WeaponBase[] HeavyWeapons;
		public WeaponBase[] UtilityWeapons;
		Dictionary<string, WeaponBase> WeaponDictionary = new Dictionary<string, WeaponBase>();
		WeaponBase currentWeapon;

		// Pause Menu
		public GameObject backgroundPanel;
		public UltimateRadialSubmenu submenu;

		// Text For Displaying Information
		public Text nameText, descriptionText, damageText, roundsPerMiinute;
		public Image gunIcon;
		

		void Start ()
		{
			// Register each of the light weapons to the dictionary with the weapon sprite name.
			for( int i = 0; i < LightWeapons.Length; i++ )
				WeaponDictionary.Add( LightWeapons[ i ].weaponIcon.name, LightWeapons[ i ] );

			// Register each of the utility weapons to the dictionary with the weapon sprite name.
			for( int i = 0; i < UtilityWeapons.Length; i++ )
				WeaponDictionary.Add( UtilityWeapons[ i ].weaponIcon.name, UtilityWeapons[ i ] );

			// Register each of the heavy weapons to the dictionary with the weapon sprite name.
			for( int i = 0; i < HeavyWeapons.Length; i++ )
				WeaponDictionary.Add( HeavyWeapons[ i ].weaponIcon.name, HeavyWeapons[ i ] );

			// Subscribe to the On Enabled and On Disabled functions of the radial menu.
			submenu.radialMenu.OnMenuEnabled += OnRadialMenuEnabled;
			submenu.radialMenu.OnMenuDisabled += OnRadialMenuDisabled;

			// Turn off the background panel game object.
			backgroundPanel.SetActive( false );
		}
		
		// This function is subscribed to the Ultimate Radial Menu, so when the radial menu is enabled it will call this function.
		void OnRadialMenuEnabled ()
		{
			// Set the background panel to visible.
			backgroundPanel.SetActive( true );
		}

		// This function will be called when the radial menu is disabled.
		void OnRadialMenuDisabled ()
		{
			// Disable the background panel.
			backgroundPanel.SetActive( false );
		}

		public void ShowCurrentWeaponInfo ( string key )
		{
			// If the dictionary does not contain this key, then return.
			if( !WeaponDictionary.ContainsKey( key ) )
				return;
			
			// Update the information in the scene so that the user can see the values changing.
			nameText.text = WeaponDictionary[ key ].name;
			descriptionText.text = "Description:\n" + WeaponDictionary[ key ].description;
			damageText.text = "Damage: " + WeaponDictionary[ key ].damage.ToString();
			roundsPerMiinute.text = "Rounds Per Minute: " + WeaponDictionary[ key ].roundsPerMinute.ToString();
			gunIcon.sprite = WeaponDictionary[ key ].weaponIcon;

			// Store the current weapon.
			currentWeapon = WeaponDictionary[ key ];
		}
	}
}