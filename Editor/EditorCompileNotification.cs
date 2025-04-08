using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace Isshi777
{
	[InitializeOnLoad]
	public static class EditorCompileNotification
	{
		/// <summary> コンパイル成功時に表示する画像 </summary>
		private const string SuccessTexturePath = "Assets/EditorCompileNotification/Editor/Texture/Success.png";
		/// <summary> コンパイル失敗時に表示する画像 </summary>
		private const string FailureTexturePath = "Assets/EditorCompileNotification/Editor/Texture/Failure.png";

		// SessionStateのKey
		private const string SessionStateKeyStartCompileTime = "EditorCompileNotification_CompileStartTime";
		private const string SessionStateKeyPlayMode = "EditorCompileNotification_PlayMode";


		static EditorCompileNotification()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			CompilationPipeline.compilationStarted += OnCompileStarted;
			//CompilationPipeline.compilationFinished += OnCompileFinished;  
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			SessionState.SetInt(SessionStateKeyPlayMode, (int)state);
		}

		private static void OnCompileStarted(object obj)
		{
			if (IsEnable())
			{
				Application.logMessageReceived += OnLogReceived;
				SessionState.SetString(SessionStateKeyStartCompileTime, DateTime.Now.ToString("O"));
				Debug.Log("コンパイル開始");
			}
		}

		private static void OnCompileFinished(object obj)
		{
			// 使用しない
		}

		[DidReloadScripts()]
		private static void OnDidReloadScripts()
		{
			if (IsEnable())
			{
				// コンパイルが成功したら呼ばれる
				OutputLog(false);
				DisplayTexture(false);
				Application.logMessageReceived -= OnLogReceived;
			}
		}

		private static void OnLogReceived(string condition, string stackTrace, LogType type)
		{
			if (IsEnable())
			{
				if ((type == LogType.Error || type == LogType.Exception) && (condition.Contains("error CS") || condition.Contains("error Unity")))
				{
					// コンパイル失敗扱い
					OutputLog(true);
					DisplayTexture(true);
					Application.logMessageReceived -= OnLogReceived;
				}
			}
		}

		private static bool IsEnable()
		{
			var state = (PlayModeStateChange)SessionState.GetInt(SessionStateKeyPlayMode, (int)PlayModeStateChange.EnteredEditMode);
			return state switch
			{
				PlayModeStateChange.EnteredEditMode => true,    // 停止時
				PlayModeStateChange.ExitingEditMode => false,   // 再生直前
				PlayModeStateChange.EnteredPlayMode => false,   // 再生後
				PlayModeStateChange.ExitingPlayMode => true,    // 停止直前
				_ => false,
			};
		}
		 
		private static void OutputLog(bool isCompileError)
		{
			var timeLog = "計測不能"; 
			var timeStr = SessionState.GetString(SessionStateKeyStartCompileTime, null);
			if (timeStr != null && DateTime.TryParse(timeStr, out var startTime))
			{
				timeLog = $"{(int)(DateTime.Now - startTime).TotalSeconds}秒";
			}
			var successLog = isCompileError ? "コンパイル失敗" : "コンパイル成功";
			Debug.Log(successLog + timeLog);
		}

		private static void DisplayTexture(bool isCompileError)
		{
			var texturePath = isCompileError ? FailureTexturePath : SuccessTexturePath;
			Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
			if (texture != null)
			{
				var assembly = typeof(EditorWindow).Assembly;
				var type = assembly.GetType("UnityEditor.GameView");
				EditorWindow.GetWindow(type).ShowNotification(new GUIContent(texture));
			}
		}
	}
}
