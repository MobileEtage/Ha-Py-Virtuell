using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MPUIKIT;
using TMPro;

public class HighscoreListElement : MonoBehaviour
{
	public TextMeshProUGUI rankLabel;
	public TextMeshProUGUI userNameLabel;
	public TextMeshProUGUI scoreLabel;
	
	public void SetUser( string rank, string userName, string score ){
		
		rankLabel.text = rank;
		userNameLabel.text = userName;
		scoreLabel.text = score;
	}
}
