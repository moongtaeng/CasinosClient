﻿// Copyright (c) Cragon. All rights reserved.

namespace Casinos
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using ILRuntime.Runtime.Enviorment;

    public class LaunchInfo
    {
        public string LaunchVersion { get; set; }
        public List<string> LaunchFileList { get; set; }
    }

    public class MbLaunch : MonoBehaviour
    {
        //---------------------------------------------------------------------
        LaunchInfo LaunchInfo { get; set; }
        ILRuntime.Runtime.Enviorment.AppDomain AppDomain { get; set; } = null;
        MemoryStream MsScriptDll { get; set; }
        MemoryStream MsScriptPdb { get; set; }

        //---------------------------------------------------------------------
        public void Close()
        {
            OnDestroy();
        }

        //---------------------------------------------------------------------
        public void Init()
        {
            Start();
        }

        //---------------------------------------------------------------------
        void Start()
        {
            LaunchInfo = null;
            AppDomain = new ILRuntime.Runtime.Enviorment.AppDomain();

            // 读取VersionLaunchPersistent
            string launch_ver = string.Empty;
            if (PlayerPrefs.HasKey("VersionLaunchPersistent"))
            {
                launch_ver = PlayerPrefs.GetString("VersionLaunchPersistent");
            }

            if (string.IsNullOrEmpty(launch_ver))
            {
                StartCoroutine(_copyStreamingAssets2PersistentAsync(_launch));
            }
            else
            {
                _launch();
            }
        }

        //---------------------------------------------------------------------
        private void Update()
        {
            if (AppDomain != null)
            {
                AppDomain.Invoke("Cs.Main", "Update", null, new object[] { Time.deltaTime });
            }
        }

        //---------------------------------------------------------------------
        private void OnDestroy()
        {
            if (AppDomain != null)
            {
                AppDomain.Invoke("Cs.Main", "Destroy", null, new object[] { });
                AppDomain = null;
            }

            if (MsScriptDll != null)
            {
                MsScriptDll.Close();
                MsScriptDll = null;
            }

            if (MsScriptPdb != null)
            {
                MsScriptPdb.Close();
                MsScriptPdb = null;
            }

            Debug.Log("MbLaunch.OnDestroy()");
        }

        //---------------------------------------------------------------------
        IEnumerator _copyStreamingAssets2PersistentAsync(Action cb)
        {
            string s = Application.streamingAssetsPath;

            using (UnityWebRequest www_request = UnityWebRequest.Get(s + "/LaunchInfo.json"))
            {
                yield return www_request.SendWebRequest();

                LaunchInfo = LitJson.JsonMapper.ToObject<LaunchInfo>(www_request.downloadHandler.text);
            }

            Dictionary<string, UnityWebRequest> map_www = new Dictionary<string, UnityWebRequest>();
            foreach (var i in LaunchInfo.LaunchFileList)
            {
                UnityWebRequest www_request = UnityWebRequest.Get(s + "/" + i);
                www_request.SendWebRequest();
                map_www[i] = www_request;
            }

            List<string> list_key = new List<string>();
            while (true)
            {
                list_key.Clear();
                list_key.AddRange(map_www.Keys);
                foreach (var i in list_key)
                {
                    var www = map_www[i];
                    if (www.isDone)
                    {
                        string path = Application.persistentDataPath + "/" + i;

                        string p = Path.GetDirectoryName(path);
                        if (!Directory.Exists(p))
                        {
                            Directory.CreateDirectory(p);
                        }

                        File.WriteAllBytes(path, www.downloadHandler.data);

                        map_www.Remove(i);
                    }
                }

                if (map_www.Count == 0) break;
                yield return 0;
            }

            PlayerPrefs.SetString("VersionLaunchPersistent", LaunchInfo.LaunchVersion);

            cb();
        }

        //---------------------------------------------------------------------
        void _launch()
        {
            string s = Application.persistentDataPath + "/Launch/Cs/";
            //s = s.Replace('\\', '/');
            //s = s.Replace("Assets/StreamingAssets", "");
            //s += "Script/Script.CSharp/bin/";
            //Debug.Log(s);

#if UNITY_EDITOR
            // 检测Script.CSharp.dll是否存在，如不存在则给出提示
#endif

            byte[] dll = File.ReadAllBytes(s + "Script.dll");
            byte[] pdb = File.ReadAllBytes(s + "Script.pdb");

            MsScriptDll = new MemoryStream(dll);
            MsScriptPdb = new MemoryStream(pdb);

            AppDomain.LoadAssembly(MsScriptDll, MsScriptPdb, new ILRuntime.Mono.Cecil.Pdb.PdbReaderProvider());

            // 委托注册
            AppDomain.DelegateManager.RegisterMethodDelegate<string>();
            AppDomain.DelegateManager.RegisterMethodDelegate<List<string>>();
            AppDomain.DelegateManager.RegisterMethodDelegate<AssetBundle>();
            AppDomain.DelegateManager.RegisterMethodDelegate<Texture>();

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.EventCallback0>((action) =>
            {
                return new FairyGUI.EventCallback0(() =>
                {
                    ((System.Action)action)();
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.EventCallback1>((action) =>
            {
                return new FairyGUI.EventCallback1((context) =>
                {
                    ((System.Action<FairyGUI.EventContext>)action)(context);
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.GTweenCallback>((action) =>
            {
                return new FairyGUI.GTweenCallback(() =>
                {
                    ((System.Action)action)();
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.GTweenCallback1>((action) =>
            {
                return new FairyGUI.GTweenCallback1((a) =>
                {
                    ((System.Action<FairyGUI.GTweener>)action)(a);
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.ListItemRenderer>((action) =>
            {
                return new FairyGUI.ListItemRenderer((a, b) =>
                {
                    ((System.Action<int, FairyGUI.GObject>)action)(a, b);
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.PlayCompleteCallback>((action) =>
            {
                return new FairyGUI.PlayCompleteCallback(() =>
                {
                    ((System.Action)action)();
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.TimerCallback>((action) =>
            {
                return new FairyGUI.TimerCallback((a) =>
                {
                    ((System.Action<object>)action)(a);
                });
            });

            AppDomain.DelegateManager.RegisterDelegateConvertor<FairyGUI.TransitionHook>((action) =>
            {
                return new FairyGUI.TransitionHook(() =>
                {
                    ((System.Action)action)();
                });
            });

            // 值类型绑定
            AppDomain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
            AppDomain.RegisterValueTypeBinder(typeof(Quaternion), new QuaternionBinder());
            AppDomain.RegisterValueTypeBinder(typeof(Vector2), new Vector2Binder());

            // CLR绑定
            LitJson.JsonMapper.RegisterILRuntimeCLRRedirection(AppDomain);
            ILRuntime.Runtime.Generated.CLRBindings.Initialize(AppDomain);

            string platform = "Android";
            bool is_editor = false;
#if UNITY_STANDALONE_WIN
            platform = "PC";
#elif UNITY_ANDROID && UNITY_EDITOR
            platform = "Android";
#elif UNITY_ANDROID
            platform = "Android";
#elif UNITY_IPHONE
            platform = "iOS";
#endif

#if UNITY_EDITOR
            is_editor = true;
            AppDomain.DebugService.StartDebugService(56000);
#else
            is_editor = false;
#endif

            bool is_editor_debug = false;
            AppDomain.Invoke("Cs.Main", "Create", null, new object[] { platform, is_editor, is_editor_debug });
        }
    }
}