/* UltimateRadialSubButtonInfo.cs */
/* Written by Kaz */
using System;
using UnityEngine;

[Serializable]
public class UltimateRadialSubButtonInfo
{
	public UltimateRadialSubmenu.UltimateRadialSubButton subButton;
	bool selected = false;
	/// <summary>
	/// Updates the selected state of this button info.
	/// </summary>
	public bool Selected
	{
		get
		{
			// Return the selected bool.
			return selected;
		}
		set
		{
			// Set selected to equal the value sent.
			selected = value;

			// If this info has not been assigned to a button, then return WITHOUT informing the user that the selected state cannot be applied.
			if( SubButtonError_NoWarning )
				return;

			// If this info is selected, select the button.
			if( selected )
				subButton.OnSelect();
			// Else deselect it.
			else
				subButton.OnDeselect();
		}
	}
	/// <summary>
	/// Returns the state of this information being registered on a submenu.
	/// </summary>
	public bool Registered
	{
		get
		{
			// If the sub button is assigned, then return true that this information is attached.
			if( subButton != null && subButton.submenu != null )
				return true;

			// Otherwise return false since there is no button attached to this information.
			return false;
		}
	}
	/// <summary>
	/// Returns the index of this button on the submenu.
	/// </summary>
	public int ButtonIndex
	{
		get
		{
			// If this button info has not been registered, then inform the user and just return 0.
			if( SubButtonError )
				return 0;

			// Return the associated sub button's index.
			return subButton.buttonIndex;
		}
	}

	public string key;
	public int id;

	public string name;
	public string description;
	public Sprite icon;
	

	/// <summary>
	/// Applies a new string to the button's text component.
	/// </summary>
	/// <param name="newText">The new string to apply to the button.</param>
	public void UpdateText ( string newText )
	{
		// If the button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Call the UpdateText function on the associated sub button.
		subButton.UpdateText( newText );
	}

	/// <summary>
	/// Assigns a new sprite to the button's icon image.
	/// </summary>
	/// <param name="newIcon">The new sprite to assign as the icon for the button.</param>
	public void UpdateIcon ( Sprite newIcon )
	{
		// Assign the new icon.
		icon = newIcon;

		// If the button is null, then the user hasn't registered this button info yet, so just return.
		if( SubButtonError_NoWarning )
			return;
		
		// Call the UpdateIcon function on the associated sub button.
		subButton.UpdateIcon( newIcon );
	}

	/// <summary>
	/// Updates the button with a new name.
	/// </summary>
	/// <param name="newName">The new string to apply to the button's name.</param>
	public void UpdateName ( string newName )
	{
		// Assign the new name.
		name = newName;

		// If the button is null, then the user hasn't registered this button info yet, so just return.
		if( SubButtonError_NoWarning )
			return;

		// Call the UpdateName function on the associated sub button.
		subButton.UpdateName( newName );
	}

	/// <summary>
	/// Updates the button with a new description.
	/// </summary>
	/// <param name="newDescription">The new string to apply to the button's description.</param>
	public void UpdateDescription ( string newDescription )
	{
		// Assign the new description.
		description = newDescription;

		// If the button is null, then the user hasn't registered this button info yet, so just return.
		if( SubButtonError_NoWarning )
			return;

		// Apply the description to the button.
		subButton.UpdateDescription( description );
	}

	/// <summary>
	/// Enables the sub button.
	/// </summary>
	public void EnableButton ()
	{
		// If the button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Call the EnableButton() function on the button.
		subButton.OnEnable();
	}

	/// <summary>
	/// Disables the sub button.
	/// </summary>
	public void DisableButton ()
	{
		// If the button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Call the DisableButton() function on the button.
		subButton.OnDisable();
	}

	/// <summary>
	/// Selects this button on the submenu.
	/// </summary>
	/// <param name="exclusive">Should this button be the only one on the submenu that is selected?</param>
	public void SelectButton ( bool exclusive = false )
	{
		// If the sub button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Set this selected property to true.
		Selected = true;

		// If the user wants to select this button exclusively...
		if( exclusive )
		{
			// Loop through all the radial menu buttons...
			for( int i = 0; i < subButton.submenu.UltimateRadialSubButtonList.Count; i++ )
			{
				// If this index is the same as our index, then skip this index.
				if( i == subButton.buttonIndex )
					continue;

				// If the radial button is selected, deselect it.
				if( subButton.submenu.UltimateRadialSubButtonList[ i ].Selected )
					subButton.submenu.UltimateRadialSubButtonList[ i ].OnDeselect();
			}
		}
	}

	/// <summary>
	/// Deselects this button.
	/// </summary>
	public void DeselectButton ()
	{
		// Set Selected to false for reference.
		Selected = false;
	}

	/// <summary>
	/// Toggles the selected state of this button.
	/// </summary>
	public void ToggleSelect ()
	{
		// If the button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Set Selected to the opposite value.
		Selected = !Selected;
	}

	/// <summary>
	/// Removes the button from the submenu.
	/// </summary>
	public void RemoveFromMenu ()
	{
		// If the button is null, notify the user and return.
		if( SubButtonError )
			return;

		// Remove the button from the menu.
		subButton.submenu.RemoveButton( subButton.buttonIndex );
	}

	/// <summary>
	/// [Internal] This function is subscribed to the OnClearButtonInformation callback on the button that this is assigned to.
	/// </summary>
	public void OnClearButtonInformation ()
	{
		// Reset this information since the button information was cleared.
		subButton = null;
	}

	/// <summary>
	/// [INTERNAL] This function is subscribed to the OnSelectedStateChanged callback on the button that this is assigned to.
	/// </summary>
	public void OnSelectedStateChanged ( bool selected )
	{
		// Copy the selected state of the button.
		this.selected = selected;
	}

	/// <summary>
	/// Returns true if the sub button is not assigned.
	/// </summary>
	bool SubButtonError_NoWarning
	{
		get
		{
			// If the sub button is null return true for there being an error.
			if( subButton == null || subButton.submenu == null )
				return true;

			// Else there is no problem, so return false.
			return false;
		}
	}

	/// <summary>
	/// Returns true if the sub button is not assigned or if this button index is out of range and displays an error.
	/// </summary>
	bool SubButtonError
	{
		get
		{
			// If the button is null, or the button is for some reason not on a submenu...
			if( subButton == null || subButton.submenu == null )
			{
				// Inform the user that there is no button and return true for there being an error.
				Debug.LogWarning( FormatDebug( "No sub button has been assigned to this button info", "Please make sure to register this info to the Ultimate Radial Submenu using the RegisterButton function <i>before</i> attempting to edit it", "Unknown (User Script)" ) );
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// [INTERNAL] Formats the debug to make it easier to read and understand.
	/// </summary>
	string FormatDebug ( string error, string solution, string objectName )
	{
		return "<b>Ultimate Radial Sub Button Info</b>\n" +
			"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
			"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
			"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
	}
}