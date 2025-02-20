using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TextCore.LowLevel;

using TMPro;

public class FontController : MonoBehaviour
{
	public bool useJapaneseFontLoading = false;
	public TMP_FontAsset mainFontAsset;
	
	[Space(10)]
	
	private List<string> fontSearchPatterns = new List<string>()
	{
		"Noto Sans CJK JP",
		"Noto Sans CJK Japanese",
		"Noto Sans Japanese",
		"Hiragino Sans",
		"Hiragino Mincho ProN",
		"Hiragino Mincho",
		"NotoSansCJK-Regular",
		"Noto Sans CJK Regular",
		"Noto Sans CJK",
		"Noto CJK",
		"CJK",
		"Hiragino",
		"Mincho"
	};
	
	private List<string> fontPathList = new List<string>();
	private bool foundJapaneseFont = false;

	public static FontController instance;
	void Awake(){
		instance = this;
	}
	
    void Start()
	{
		if( useJapaneseFontLoading && LanguageController.GetLanguageCode() == "ja" ){
			LoadCJKFont();
		}
	}
    
	public bool JapaneseFontLoaded(){
		if( !useJapaneseFontLoading ) return true;
		return foundJapaneseFont;
	}
	
	private void LoadCJKFont(){

		string[] fontPaths = Font.GetPathsToOSFonts();
		
		if( fontPaths.Length == 0 ){
			print("No fonts found");
			return;
		}
		
		for( int i = 0; i < fontPaths.Length; i++ ){
			print( fontPaths[i] );
			fontPathList.Add( fontPaths[i] );
		}
		
		int targetFontIndex = GetValidFontPathIndex( fontPathList );
				
		if( targetFontIndex >= 0 ){
						
			Font osFont = new Font(fontPathList[targetFontIndex]);
			TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(osFont);
			if( fontAsset != null ){
				
				// Create and add some fallback textures, because we might not have enough space on one texture for all used japanese characters 
				fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
				TMP_FontAsset fallbackFontAsset1 = TMP_FontAsset.CreateFontAsset(osFont, 44, 5,  GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
				TMP_FontAsset fallbackFontAsset2 = TMP_FontAsset.CreateFontAsset(osFont, 44, 5,  GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
				TMP_FontAsset fallbackFontAsset3 = TMP_FontAsset.CreateFontAsset(osFont, 44, 5,  GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
				fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
				fontAsset.fallbackFontAssetTable.Add(fallbackFontAsset1);
				fontAsset.fallbackFontAssetTable.Add(fallbackFontAsset2);
				fontAsset.fallbackFontAssetTable.Add(fallbackFontAsset3);
				
				mainFontAsset.fallbackFontAssetTable.Add(fontAsset);
				
				foundJapaneseFont = true;
			}
			
		}
	}
	
	private int GetValidFontPathIndex( List<string> fontPaths ){
		
		for( int i = 0; i < fontSearchPatterns.Count; i++ ){
			
			for( int j = 0; j < fontPaths.Count; j++ ){
				
				string[] patterns = fontSearchPatterns[i].Split(' ');
				
				bool validFont = true;
				for( int k = 0; k < patterns.Length; k++ ){
					
					string font = fontPaths[j].ToLower();
					string pattern = patterns[k].ToLower();
					
					//if( !fontPaths[j].Contains( patterns[k] ) ){
					if( !font.Contains( pattern ) ){
							validFont = false;
					}
				}
				
				if( validFont ){
					print( "Found valid font");
					print( fontPaths[j] );
					print( "Pattern " + fontSearchPatterns[i] );
					return j;
				}
			}		
		}
		
		return -1;
	}
	
	/*
	void Update(){
		if( Input.GetMouseButtonDown(1) ){
			string[] fontPaths = Font.GetPathsToOSFonts();
			if( fontPaths.Length == 0 ){
				print("No fonts found");
				return;
			}
		
			for( int i = 0; i < fontPaths.Length; i++ ){
				print( fontPaths[i] );
				fontPathList.Add( fontPaths[i] );
			}
		}
	}
	*/
}
