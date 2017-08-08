﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class RoadWindowEditor : EditorWindow
{
	Anchor selectedAnchor;
	Vector3 savedPos;
	public static RoadEditorSettings Settings;
	public bool IntersectionsFoldout;
	public Vector2 IntersectionCanvas;

	float RoadMeshScale = 1;
	int SelectedRoadTile = 0;

	int TileBrowserGridColumns = 3;

	[MenuItem ("Window/RoadEditor")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		RoadWindowEditor window = (RoadWindowEditor)EditorWindow.GetWindow (typeof (RoadWindowEditor));
		window.Show();
	}

	void OnGUI ()
	{
		if (GUILayout.Button("New Road Settings"))
		{
			NewRoadSettings();
		}
		Settings = (RoadEditorSettings)EditorGUILayout.ObjectField("Road Editor Settings",Settings,typeof(RoadEditorSettings),false);

		GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

		if (Settings == null)
		{
			var path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("LastRoadSettingsGUID"));
			Settings = AssetDatabase.LoadAssetAtPath<RoadEditorSettings>(path);
			if (Settings == null){return;}
		}

		Settings.roadMaterial = (Material)EditorGUILayout.ObjectField("Road Material",Settings.roadMaterial,typeof(Material),false);
		Settings.extudeShape = (ExtrudeShape)EditorGUILayout.ObjectField("Extrude Shape",Settings.extudeShape,typeof(ExtrudeShape),false);

		IntersectionsFoldout = EditorGUILayout.Foldout(IntersectionsFoldout,"Edit Intersections");



		if (IntersectionsFoldout)
		{
			ListHeader(Settings.Intersections,"Intersections");
			IntersectionCanvas = EditorGUILayout.BeginScrollView(IntersectionCanvas);
			for (int i = 0; Settings.Intersections.Count > i;i++)
			{
				GUILayout.Space(2);
				GUILayout.BeginHorizontal();
				GUILayout.Label(AssetPreview.GetAssetPreview(Settings.Intersections[i].Prefab),GUILayout.Width(64),GUILayout.Height(64));
				GUILayout.BeginVertical();
				Settings.Intersections[i].Name = EditorGUILayout.TextField("Name",Settings.Intersections[i].Name);
				Settings.Intersections[i].Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab",Settings.Intersections[i].Prefab,typeof(GameObject),false);
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				//Intersections[i].Prefabs = editoruig
			}
			EditorGUILayout.EndScrollView();
		}
		else
		{
			List<Texture2D> intersectionImages = new List<Texture2D>();
			for(int i = 0; i<Settings.Intersections.Count;i++)
			{
				intersectionImages.Add(AssetPreview.GetAssetPreview(Settings.Intersections[i].Prefab));
			}
			SelectedRoadTile = GUILayout.SelectionGrid(SelectedRoadTile,intersectionImages.ToArray(),TileBrowserGridColumns);
		}

		//RoadMeshScale = EditorGUILayout.Slider("Curve Secion Multiplier",RoadMeshScale,0.1f,2);

		if (GUILayout.Button("Recalculate All Curves"))
		{
			foreach(var intersection in Object.FindObjectsOfType<Intersection>())
			{
				intersection.ForceAnchorDirections();
			}
		}

		/*if (GUILayout.Button("Rebuild All Roads"))
		{
			foreach(var intersection in Object.FindObjectsOfType<Intersection>())
			{
				intersection.ForceAnchorDirections();
				intersection.RecalculateAllAnchoredPaths();
			}
			//RebuildAllRoads();
		}*/

		if (GUILayout.Button("Rebuild All Road Meshes"))
		{
			EditorUtility.DisplayProgressBar("Rebuild All Road Meshes","Generating and saving meshes for all roads",0);

			var allIntersections = Object.FindObjectsOfType<Intersection>();
			for (int i = 0; i<allIntersections.Length;i++)
			{
				EditorUtility.DisplayProgressBar("Rebuild All Road Meshes","Generating and saving meshes for all roads",i/allIntersections.Length);
				allIntersections[i].RebuildAllAnchoredPaths(false);
			}
			AssetDatabase.SaveAssets();
			EditorUtility.ClearProgressBar();
			UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
		}

		if (GUILayout.Button("Refresh All Props"))
		{
			foreach(var spawners in FindObjectsOfType<PathDetail>())
			{
				spawners.Clear();
				spawners.PlacePrefabs();
			}
			UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(Settings);
		}

		EditorGUI.EndDisabledGroup();
	}

	void NewRoadSettings()
	{
		RoadEditorSettings asset = ScriptableObject.CreateInstance<RoadEditorSettings>();

		AssetDatabase.CreateAsset(asset, "Assets/NewScripableObject.asset");
		AssetDatabase.SaveAssets();

		EditorUtility.FocusProjectWindow();

		Selection.activeObject = asset;
		Settings = asset;
	}

	[System.Obsolete("called in roadbuilder gui")]
	void RefreshProps()
	{
		foreach(var spawners in FindObjectsOfType<PathDetail>())
		{
			spawners.Clear();
			spawners.PlacePrefabs();
		}
	}

	[System.Obsolete("use Intersection RebuildAllAnchoredPaths instead")]
	void Bake()
	{
		foreach (var pathMesh in FindObjectsOfType<PathMesh>())
		{
			pathMesh.ClearMesh();
			//TODO remove old meshes from asset database!
			pathMesh.Rebuild();
			//AssetDatabase.CreateAsset(pathMesh.GetComponent<MeshFilter>().mesh,"Assets/RoadMesh/Road"+pathMesh.GetComponent<MeshFilter>().mesh.GetInstanceID().ToString()+".asset");
		}
		AssetDatabase.SaveAssets();
	}

	// Window has been selected
	void OnFocus() {
		// Remove delegate listener if it has previously
		// been assigned.
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		// Add (or re-add) the delegate.
		SceneView.onSceneGUIDelegate += this.OnSceneGUI;


		if (Settings != null)
		{
			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Settings));
			EditorPrefs.SetString("LastRoadSettingsGUID",guid);
		}
	}

	void OnDestroy() {
		// When the window is destroyed, remove the delegate
		// so that it will no longer do any drawing.

		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}

	void Clear()
	{
		selectedAnchor = null;
		showWindow = false;
	}

	public void ListHeader(List<IntersectionType> _list, string _label)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(_label);
		if (_list.Count > 0)
		{
			if (GUILayout.Button("-",GUILayout.Width(50)))
				_list.RemoveAt(_list.Count-1);
		}
		if (GUILayout.Button("+",GUILayout.Width(50)))
			_list.Add(new IntersectionType());
		GUILayout.EndHorizontal();
	}

	void BuildRoad(Anchor begin, Anchor end)
	{
		if (begin == null || end == null){return;}

		GameObject go = new GameObject("Road");
		var curve = go.AddComponent<CubicBezierPath>();
		var mesh = go.AddComponent<PathMesh>();
		mesh.material = Settings.roadMaterial;
		mesh.ExtrudeShape = Settings.extudeShape;

		curve.pts[0] = begin.transform.position;
		curve.pts[1] = begin.transform.position + begin.transform.forward * begin.Power;
		curve.pts[2] = end.transform.position + end.transform.forward * end.Power;
		curve.pts[3] = end.transform.position;

		begin.Path = go;
		end.Path = go;

		EditorUtility.SetDirty(begin);
		EditorUtility.SetDirty(end);
		UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
		//Selection.activeGameObject = curve.gameObject;
	}

	bool showWindow;
	void ShowWindow()
	{
		Handles.BeginGUI();
		foreach (var v in Settings.Intersections)
		{
			if (GUILayout.Button(v.Name))
			{
				var prefab = (GameObject)PrefabUtility.InstantiatePrefab(v.Prefab);
				prefab.transform.position = savedPos;
				Selection.activeGameObject = prefab;
				showWindow = false;
			}
		}
		Handles.EndGUI();
	}

	void OnSceneGUI(SceneView sceneView) {
		// Do your drawing here using Handles.

		if (Settings != null)
		{
			//redraw this window
			if (showWindow){ShowWindow();}
		}

		Event e = Event.current;
		Ray r = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );

		RaycastHit hit = new RaycastHit();
		Plane zeroplane = new Plane(Vector3.up,Vector2.zero);
		float zeroplaneDistance = 0;
		Vector3 hitPoint = Vector3.zero;

		if (Physics.Raycast(r,out hit, 1000))
		{
			//Debug.DrawRay(hit.point,Vector3.up);
			hitPoint = hit.point;
		}
		else if (zeroplane.Raycast(r,out zeroplaneDistance))
		{
			hitPoint = r.GetPoint(zeroplaneDistance);
		}

		if (Settings != null)
		{
			if (e.type == EventType.keyDown)
			{
				if (e.keyCode == KeyCode.Escape)
				{
					Clear();
				}

				if (e.keyCode == KeyCode.I)
					showWindow = !showWindow;
				//gui popup window
			}

			foreach (var a in FindObjectsOfType<Anchor>())
			{
				if (a.Path != null)
				{
					//a.Curve.DrawCurve();
					continue;
				}
				//Handles.DrawWireDisc(j.transform.position,j.transform.up,4);
				if (Handles.Button(a.transform.position+a.transform.forward * 3.5f,Quaternion.LookRotation(Vector3.up),1,1,Handles.CircleCap))
				{
					if (selectedAnchor == null)
					{
						selectedAnchor = a;
						break;
					}
					else
					{
						BuildRoad(selectedAnchor,a);
						Clear();
					}
				}
			}
		}

		if (!showWindow)
		{
			savedPos = hitPoint;
		}
		Handles.color = Color.blue;
		Handles.DrawWireDisc(savedPos,Vector3.up,5);

		if (selectedAnchor != null)
		{
			Handles.DrawDottedLine(selectedAnchor.transform.position,savedPos,3);
		}

		if (Selection.activeGameObject != null)
		{
			var curve = Selection.activeGameObject.GetComponent<CubicBezierPath>();

			if (curve)
			{
				foreach (var a in FindObjectsOfType<Anchor>())
				{
					if (a.Path == curve)
					{
						Handles.color = Color.white;
						Vector3 value = Handles.Slider(a.transform.position + Vector3.up,a.transform.forward * a.Power);

						float delta = Vector3.Distance(value+Vector3.down,a.transform.position);
						if (delta > 0.01f)
						{
							a.Power = delta;
							//Debug.Log(a.Power);
							EditorUtility.SetDirty(a);
							UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
						}
						Handles.Label(a.transform.position + Vector3.up,"Power "+a.Power);
					}
				}
			}
		}
	}

	/// <summary>
	/// For each curve, if it can find a beginning and end anchor, update the curve to those anchor points
	/// </summary>
	[System.Obsolete("use Intersection RebuildAllAnchoredPaths instead")]
	void RebuildAllRoads()
	{
		Dictionary<CubicBezierPath,Anchor>Curves = new Dictionary<CubicBezierPath,Anchor>();
		foreach (Anchor a in Object.FindObjectsOfType<Anchor>())
		{
			CubicBezierPath curve = a.Path.GetComponent<CubicBezierPath>();
			if (curve == null){continue;}
			if (Curves.ContainsKey(curve))
			{
				//rebuilt
				curve.pts[0] = Curves[curve].transform.position;
				curve.pts[1] = Curves[curve].transform.position + Curves[curve].transform.forward * Curves[curve].Power;
				curve.pts[2] = a.transform.position + a.transform.forward * a.Power;
				curve.pts[3] = a.transform.position;

				//TODO section count should be on the mesh generator
				//curve.SectionCount = (int)(Vector3.Distance(Curves[curve].transform.position,a.transform.position) * RoadMeshScale);

				Curves.Remove(curve);
			}
			else
			{
				Curves.Add(curve,a);
			}
		}
	}
}