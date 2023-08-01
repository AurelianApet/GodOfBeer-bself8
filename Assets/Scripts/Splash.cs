using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Splash : MonoBehaviour
{
    public float delay_time = 0.5f;
    int downloading_cnt = 0;
    int downloaded_cnt = 0;

    // Start is called before the first frame update
    void Start()
    {
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.fullScreen = true;
#if UNITY_ANDROID
        Global.setStatusBarValue(1024); // WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN
#endif
#if UNITY_IPHONE
		Global.imgPath = Application.persistentDataPath + "/bself8_img/";
#elif UNITY_ANDROID
        Global.imgPath = Application.persistentDataPath + "/bself8_img/";
#else
        if (Application.isEditor == true)
        {
            Global.imgPath = "/img/";
        }
#endif

#if UNITY_IPHONE
		Global.prePath = @"file://";
#elif UNITY_ANDROID
        Global.prePath = @"file:///";
#else
        Global.prePath = @"file://" + Application.dataPath.Replace("/Assets", "/");
#endif

        //delete all downloaded images
        try
        {
            if (Directory.Exists(Global.imgPath))
            {
                Directory.Delete(Global.imgPath, true);
            }
        }
        catch (Exception)
        {

        }
        LoadInfoFromPrefab();
    }

    void LoadInfoFromPrefab()
    {
        Global.ip = PlayerPrefs.GetString("ip");
        Global.api_url = "http://" + Global.ip + ":" + Global.api_server_port + "/m-api/self/";
        Global.socket_server = "ws://" + Global.ip + ":" + Global.api_server_port;
        Global.image_server_path = "http://" + Global.ip + ":" + Global.api_server_port + "/self/";
        Global.backImageServerPath = "http://" + Global.ip + ":" + Global.api_server_port + "/self/media/";
        for (int i = 0; i < 8; i ++)
        {
            Global.pInfo[i].serial_number = PlayerPrefs.GetInt("tapNo" + i);
            Global.pInfo[i].standby_type = PlayerPrefs.GetInt("standbyType" + i);
        }
        if(Global.ip != "")
        {
            WWWForm form = new WWWForm();
            WWW www = new WWW(Global.api_url + Global.get_media_api, form);
            StartCoroutine(DownloadResources(www));
        }
        else
        {
            Global.scene_type = 1;
            SceneManager.LoadScene("main");
        }
    }

    IEnumerator DownloadResources(WWW www)
    {
        yield return www;
        try
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            List<string> imageFiles = new List<string>();
            if (result == 1)
            {
                JSONNode fList = JSON.Parse(jsonNode["filelist"].ToString()/*.Replace("\"", "")*/);
                Debug.Log(fList);
                for(int i = 0; i < fList.Count; i++)
                {
                    if (Regex.IsMatch(fList[i], @"\.jpg$|\.png$"))
                    {
                        string url = Path.Combine(Global.backImageServerPath, fList[i]);
                        string savePath = Path.Combine(Global.imgPath, fList[i]);
                        downloading_cnt++;
                        StartCoroutine(downloadImage(url, savePath));
                        imageFiles.Add(savePath);
                    }
                    if (Regex.IsMatch(fList[i], @"\.mp4$"))
                    {
                        //downloading_cnt++;
                        Global.backVideoFile = Path.Combine(Global.backImageServerPath, fList[i]);
                        //string savePath = Path.Combine(Global.imgPath, fList[i]);
                        //StartCoroutine(downloadImage(Path.Combine(Global.backImageServerPath, fList[i]), savePath));
                        //Global.backVideoFile = savePath;
                        Debug.Log("video url:" + Global.backVideoFile);
                    }
                }
            }
            Global.backImgFiles = imageFiles;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        while(downloading_cnt > downloaded_cnt)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(delay_time);
        Global.scene_type = 0;
        //SceneManager.LoadScene("autoslide");
        SceneManager.LoadScene("main");
    }

    IEnumerator downloadImage(string url, string pathToSaveImage)
    {
        yield return new WaitForEndOfFrame();
        if (File.Exists(pathToSaveImage))
        {
            //Debug.Log(pathToSaveImage + " exists");
            downloaded_cnt++;
        }
        else
        {
            //Debug.Log(pathToSaveImage + " downloading--");
            WWW www = new WWW(url);
            StartCoroutine(_downloadImage(www, pathToSaveImage));
        }
    }

    private IEnumerator _downloadImage(WWW www, string savePath)
    {
        yield return www;
        //Check if we failed to send
        if (string.IsNullOrEmpty(www.error))
        {
            saveImage(savePath, www.bytes);
        }
        else
        {
            downloaded_cnt++;
            UnityEngine.Debug.Log("Error: " + www.error);
        }
    }

    void saveImage(string path, byte[] imageBytes)
    {
        try
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, imageBytes);
            downloaded_cnt++;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
            downloaded_cnt++;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
