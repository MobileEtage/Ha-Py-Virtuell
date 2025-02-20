using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EditStationController : MonoBehaviour
{
	public int scanningState = -1;
	
	[Space(10)]
	
	public GameObject options;
	public TextMeshProUGUI scanInfoLabel;
	public TextMeshProUGUI scannedAreaLabel;
	public GameObject continueButton;
	public TextMeshProUGUI continueButtonLabel;
	public GameObject saveTutorial;
	
	public static EditStationController instance;
	void Awake()
	{
		instance = this;
	}

    void Update()
	{
		//if( scanningState < 0 ) return;
	    //UpdateScan();
    }
    
	public void Init(){
		
		scanningState = 0;
		options.SetActive(true);
		scanInfoLabel.text = LanguageController.GetTranslation("Scanne den Bodenbereich vor dir, indem du das Mobilgerät langsam hin und her schwenkst.");
		continueButtonLabel.text = LanguageController.GetTranslation("Der nächste Schritt");
		
		ARController.instance.scanAnimator.scanInfo.GetComponentInChildren<Canvas>(true).enabled = false;
	}
	
	public void Reset(){
		
		scanningState = -1;
		scanInfoLabel.text = "";
		scannedAreaLabel.text = "";
		continueButton.SetActive(false);
		options.SetActive(false);
		saveTutorial.SetActive(false);
		
		ARController.instance.scanAnimator.scanInfo.GetComponentInChildren<Canvas>(true).enabled = true;
	}
    
	public void UpdateScan(){
		
		if( scanningState == -1 ){
			
			scanInfoLabel.text = "";
			scannedAreaLabel.text = "";
		}
		else if( scanningState == 0 || scanningState == 1 || scanningState == 2  ){
			
			scannedAreaLabel.text = LanguageController.GetTranslation("Erkannte Fläche: ") + 
				ARController.instance.GetTrackedArea().ToString("F2") + " qm";
		}
		else{
			
			scannedAreaLabel.text = "";
		}
	}
	
	public void ContinueScanStep(){

		/*
		scanningState++;

		if( scanningState == 1 ){
			
			continueButton.SetActive(true);
			scanInfoLabel.text = 
				LanguageController.GetTranslation("Der Bodenbereich wurde großflächig erkannt. Du hast deine nähere Umgebung ausreichend gescannt.");
		}
		else if( scanningState == 2 ){
			
			continueButton.SetActive(true);
			scanInfoLabel.text = 
				LanguageController.GetTranslation("Ab jetzt kannst du dich frei bewegen. Aber langsam!!! Denn der Raumscan wird weiter vervollständigt.");
		}
		else if( scanningState == 3 ){
			
			ExtendedImageTargetController.instance.addButton.SetActive(true);
			continueButton.SetActive(false);
			scanInfoLabel.text = LanguageController.GetTranslation("(3) 3D-Säulen ergänzen:\n\nErgänze nun mit dem „+“ Symbol das gescannte Spielfeld durch eine oder mehrere 3D-Säulen.");
		}
		else if( scanningState == 4 ){
			
			continueButton.SetActive(true);
			scanInfoLabel.text = LanguageController.GetTranslation("Du kannst die Säule/n per Touch umplatzieren. Ziehe sie an ihre Zielposition. Bist du damit fertig?");
		}else if( scanningState == 5 ){
			
			continueButton.SetActive(false);
			saveTutorial.SetActive(true);
			scanInfoLabel.text = "";
		}
		else if( scanningState == 6 ){
			
			saveTutorial.SetActive(false);
			scanInfoLabel.text = LanguageController.GetTranslation("Scanne den Marker!");
		}
		else if( scanningState == 7 ){
			
			scanInfoLabel.text = LanguageController.GetTranslation("Das Spiel ist eingerichtet.");
			continueButtonLabel.text = LanguageController.GetTranslation("Kameramodus schließen!");
			continueButton.SetActive(true);
		}
		else if( scanningState == 8 ){

            print("ContinueScanStep");
            TestController.instance.BackToAROptions();
		}
		*/
	}
}
