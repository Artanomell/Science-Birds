﻿using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

class LevelEditor : EditorWindow {
 
	public static BIRDS  _birdsOps;
	public static PIGS   _pigsOps;
	public static BLOCKS _blocksOps;
	public static MATERIALS _material;

	private static Dictionary<string, GameObject> _birds;
	private static Dictionary<string, GameObject> _pigs;
	private static Dictionary<string, GameObject> _blocks;
	private static GameObject   _platform;

	private static int _birdsAdded = 0;
	private static int _birdsAmounInARow = 5;
	private static Vector3 _groundPos;

	[MenuItem ("Window/Level Editor %l")]
	static void Init () {

		if (EditorSceneManager.GetActiveScene ().name != "GameWorld") {

			if (EditorUtility.DisplayDialog ("Warning", "This window can only be opened in the Game World scene. " +
			   "Do you wanna go to that scene?", "Yes", "Cancel")) {

				EditorSceneManager.OpenScene ("Assets/Scenes/GameWorld.unity");
			}
			else 
				return;
		}
			
		LevelEditor window = (LevelEditor)EditorWindow.GetWindow(typeof(LevelEditor));
		window.Show ();

	}

	void OnEnable ()
	{
		_birds = LevelLoader.LoadABResource ("Prefabs/GameWorld/Characters/Birds");
		_pigs = LevelLoader.LoadABResource ("Prefabs/GameWorld/Characters/Pigs");
		_blocks = LevelLoader.LoadABResource ("Prefabs/GameWorld/Blocks");
		_platform = Resources.Load("Prefabs/GameWorld/Platform") as GameObject;

		_groundPos = new Vector3 (0f, -2.74f, 0f);

		_birdsAdded = GameObject.Find ("Birds").transform.childCount;
	}
		
	void OnFocus() {

		ToggleGizmos (false);

		HideLayer (LayerMask.NameToLayer("UI"));
	}

	void OnHierarchyChange() {

		if (EditorSceneManager.GetActiveScene ().name != "GameWorld")
			Close ();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal ();

		_birdsOps = (BIRDS) EditorGUILayout.EnumPopup("", _birdsOps);
		if (GUILayout.Button ("Create Bird", GUILayout.Width (80), GUILayout.Height (20))) {

			CreateBird ();
		}

		EditorGUILayout.EndVertical ();

		// Pigs section
		EditorGUILayout.BeginHorizontal ();

		_pigsOps = (PIGS) EditorGUILayout.EnumPopup("", _pigsOps);

		if (GUILayout.Button ("Create Pig", GUILayout.Width (80), GUILayout.Height (20))) {

			GameObject pig = InstantiateGameObject (_pigs[_pigsOps.ToString()]);
			pig.transform.parent = GameObject.Find ("Blocks").transform;
		}

		EditorGUILayout.EndVertical ();

		// Blocks section
		EditorGUILayout.BeginHorizontal ();

		_blocksOps = (BLOCKS) EditorGUILayout.EnumPopup("", _blocksOps);
		_material = (MATERIALS) EditorGUILayout.EnumPopup("", _material);

		if (GUILayout.Button ("Create Block", GUILayout.Width (80), GUILayout.Height (20))) {

			GameObject block = InstantiateGameObject (_blocks[_blocksOps.ToString()]);
			block.transform.parent = GameObject.Find ("Blocks").transform;

			BlockEditor.UpdateBlockMaterial (block.GetComponent<ABBlock>(), _material);
		}

		EditorGUILayout.EndHorizontal ();

		if (GUILayout.Button ("Create Platform")) {

			GameObject platform = InstantiateGameObject (_platform);
			platform.transform.parent = GameObject.Find ("Platforms").transform;
		}

		EditorGUILayout.BeginHorizontal ();

		if (GUILayout.Button ("Load Level"))
			LoadLevel ();

		if (GUILayout.Button ("New Level")) {

			if (EditorUtility.DisplayDialog ("Create New Level",
				   "Are you sure you want to create a new level? This operation will " +
				"not save the current level in the scene", "Yes", "Cancel")) {

				CleanLevel ();
			}
		}
		
		if (GUILayout.Button ("Save Level"))
			SaveLevel ();

		EditorGUILayout.EndHorizontal ();
	}
		
	GameObject InstantiateGameObject(GameObject[]source, int index) {

		GameObject cube = (GameObject)PrefabUtility.InstantiatePrefab (source[index]);
		cube.transform.position = Vector3.zero;

		return cube;
	}

	GameObject InstantiateGameObject(GameObject source) {

		GameObject cube = (GameObject)PrefabUtility.InstantiatePrefab (source);
		cube.transform.position = Vector3.zero;

		return cube;
	}

	void CleanLevel() {

		_birdsAdded = 0;

		DestroyImmediate (GameObject.Find ("Birds").gameObject);
		GameObject birds = new GameObject ();
		birds.name = "Birds";
		birds.transform.parent = GameObject.Find ("GameWorld").transform;

		DestroyImmediate (GameObject.Find ("Blocks").gameObject);
		GameObject blocks = new GameObject ();
		blocks.name = "Blocks";
		blocks.transform.parent = GameObject.Find ("GameWorld").transform;

		DestroyImmediate (GameObject.Find ("Platforms").gameObject);
		GameObject plats = new GameObject ();
		plats.name = "Platforms";
		plats.transform.parent = GameObject.Find ("GameWorld").transform;
	}

	void LoadLevel() {

		string path = EditorUtility.OpenFilePanel("Select level to open", "Assets/Resources/Levels/", "");

		if (path != "") {

			CleanLevel ();

			Debug.Log (path);

//			TextAsset levelFile = (TextAsset)Resources.Load (finalPath);
			string levelText = LevelLoader.ReadXmlLevel (path);
			Debug.Log (levelText);
			ABLevel level = LevelLoader.LoadXmlLevel (levelText);

			DecodeLevel (level);
		}
	}

	void SaveLevel() {

		string path = EditorUtility.SaveFilePanel("Select location to save level", "Assets/Resources/Levels/", "level.xml", "xml");

		if (path != "") {

			LevelLoader.SaveXmlLevel (EncodeLevel (), path);
		}
	}

	void CreateBird() {

		GameObject bird = InstantiateGameObject (_birds["BirdRed"]);
		bird.name = bird.name + "_" + _birdsAdded;
		bird.transform.parent = GameObject.Find ("Birds").transform;

		Vector3 birdsPos = ABConstants.SLING_SELECT_POS;

		// From the second Bird on, they are added to the ground
		if(_birdsAdded >= 1)
		{
			birdsPos.y = _groundPos.y;

			for(int i = 0; i < _birdsAdded; i++) {

				if ((i + 1) % _birdsAmounInARow == 0) {

					float coin = (UnityEngine.Random.value < 0.5f ? 1f : -1);
					birdsPos.x = ABConstants.SLING_SELECT_POS.x + (UnityEngine.Random.value * 0.5f * coin);
				}

				birdsPos.x -=  bird.GetComponent<SpriteRenderer>().bounds.size.x * 1.75f;
			}
		}

		bird.transform.position = birdsPos;

		_birdsAdded++;
	}

	public ABLevel EncodeLevel() 
	{
		ABLevel level = new ABLevel();

		level.pigs = new List<OBjData>();
		level.blocks = new List<OBjData>();
		level.platforms = new List<OBjData>();

		foreach (Transform child in GameObject.Find ("Blocks").transform) {

			OBjData obj = new OBjData ();

			obj.type = child.name;
			obj.x = child.transform.position.x;
			obj.y = child.transform.position.y;
			obj.rotation = child.transform.rotation.eulerAngles.z;

			if (child.GetComponent<ABPig> () != null) {

				obj.material = "";
				level.pigs.Add (obj);
			} 
			else if (child.GetComponent<ABBlock> () != null) {
				
				obj.material = child.GetComponent<ABBlock> ()._material.ToString ();
				level.blocks.Add (obj);
			}

		}

		foreach (Transform child in GameObject.Find ("Platforms").transform) {

			OBjData obj = new OBjData ();

			obj.type = child.name;
			obj.material = "";
			obj.x = child.transform.position.x;
			obj.y = child.transform.position.y;
			obj.rotation = child.transform.rotation.eulerAngles.z;

			level.platforms.Add (obj);
		}

		level.birdsAmount = _birdsAdded;

		return level;
	}

	public void DecodeLevel(ABLevel level) 
	{
		foreach (OBjData gameObj in level.pigs) {

			Vector2 pos = new Vector2 (gameObj.x, gameObj.y);
			Quaternion rotation = Quaternion.Euler (0, 0, gameObj.rotation);

			GameObject pig = InstantiateGameObject (_pigs[gameObj.type]);
			pig.transform.parent = GameObject.Find ("Blocks").transform;
			pig.transform.position = pos;
			pig.transform.rotation = rotation;
		}

		foreach(OBjData gameObj in level.blocks) {

			Vector2 pos = new Vector2 (gameObj.x, gameObj.y);
			Quaternion rotation = Quaternion.Euler (0, 0, gameObj.rotation);

			GameObject block = InstantiateGameObject (_blocks[gameObj.type]);
			block.transform.parent = GameObject.Find ("Blocks").transform;
			block.transform.position = pos;
			block.transform.rotation = rotation;

			MATERIALS material = (MATERIALS)Enum.Parse(typeof(MATERIALS), gameObj.material);
			BlockEditor.UpdateBlockMaterial(block.GetComponent<ABBlock>(), material);
		}

		foreach(OBjData gameObj in level.platforms)
		{
			Vector2 pos = new Vector2 (gameObj.x, gameObj.y);
			Quaternion rotation = Quaternion.Euler (0, 0, gameObj.rotation);

			GameObject platform = InstantiateGameObject (_platform);
			platform.transform.parent = GameObject.Find ("Platforms").transform;
			platform.transform.position = pos;
			platform.transform.rotation = rotation;
		}

		for (int i = 0; i < level.birdsAmount; i++)
			CreateBird ();

		_birdsAdded = level.birdsAmount;
	}

	public static void ToggleGizmos(bool gizmosOn) {

		int val = gizmosOn ? 1 : 0;

		Assembly asm = Assembly.GetAssembly(typeof(Editor));
		Type type = asm.GetType("UnityEditor.AnnotationUtility");

		if (type != null) {

			MethodInfo getAnnotations = type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo setGizmoEnabled = type.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo setIconEnabled = type.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

			var annotations = getAnnotations.Invoke(null, null);

			foreach (object annotation in (IEnumerable)annotations) {

				Type annotationType = annotation.GetType();
				FieldInfo classIdField = annotationType.GetField("classID", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo scriptClassField = annotationType.GetField("scriptClass", BindingFlags.Public | BindingFlags.Instance);

				if (classIdField != null && scriptClassField != null) {
					int classId = (int)classIdField.GetValue(annotation);

					string scriptClass = (string)scriptClassField.GetValue(annotation);
					setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
					setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
				}
			}
		}
	}

	static void HideLayer(int layerNumber)
	{
		LayerMask layerNumberBinary = 1 << layerNumber;
		LayerMask blocksLayer = 1 << LayerMask.NameToLayer ("Blocks");
		LayerMask birdsLayer = 1 << LayerMask.NameToLayer ("Birds");
		LayerMask pigsLayer = 1 << LayerMask.NameToLayer ("Pigs");
		LayerMask platLayer = 1 << LayerMask.NameToLayer ("Platforms");

		Tools.visibleLayers = ~layerNumberBinary; 
		Tools.lockedLayers = ~(blocksLayer | birdsLayer | pigsLayer | platLayer);

		SceneView.RepaintAll();
	}
}