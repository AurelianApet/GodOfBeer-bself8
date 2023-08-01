using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using SocketIO;
using System.Net.Sockets;
using System.Net;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainManager : MonoBehaviour
{
    public GameObject mainSceneObj;
    public GameObject autoSlideSceneObj;

    //auto slider scene
    public GameObject slideObj;
    public GameObject videoObj;
    public GameObject imgSlidePrefab;
    public GameObject imgslideParent;


    //main scene
    public RawImage totalBackImg;
    public GameObject[] product;
    public GameObject[] backImg;
    public GameObject[] failMsg;

    public GameObject[] textObj;
    public Text[] title;
    public Text[] type;
    public Text[] bintage;
    public Text[] location;
    public Text[] kind;
    public Text[] description;

    public GameObject[] priceObj;
    public Text[] priceTxt;

    public GameObject[] languageBtn;

    public GameObject[] setObj;
    public GameObject[] viewObj;
    public Text[] vappNo;
    public Text[] vtapNo;
    public GameObject[] editObj;
    public InputField[] eappNo;
    public InputField[] etapNo;
    public InputField[] ip;
    public Toggle[] imageOption;
    public Toggle[] textOption;
    public GameObject[] savePopup;

    public GameObject[] bottleChangePopup;
    public GameObject[] bottleInitPopup;
    public GameObject[] washPopup;
    public GameObject[] errorPopup;
    public Text[] err_content;
    public GameObject[] serverErrPopup;
    public Text[] server_content;

    //work
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;
    public AudioSource[] soundObjs; //0-sound, 1-alarm, 2-touch, 3-start_app

    int[] clickingCnt = new int[8];
    int waitTime = 180;
    int noSocketTime = 0;

    int curProductIndex = -1;
    bool is_loading = false;

    VideoPlayer videoPlayer;
    RawImage rawImage;

    bool is_counting = false;
    bool[] wait_count = new bool[8] { false, false, false, false, false, false, false, false };
    void Awake()
    {
        Caching.compressionEnabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = videoObj.GetComponent<VideoPlayer>();
        videoPlayer.errorReceived += delegate (VideoPlayer v, string message)
        {
            //videoPlayer.isLooping = true;
            //videoPlayer.Play();
            Debug.LogWarning("[VideoPlayer] Play Movie Error: " + message);
            //Handheld.PlayFullScreenMovie(Global.backVideoFile, Color.black, FullScreenMovieControlMode.CancelOnInput, FullScreenMovieScalingMode.AspectFit);
        };
        rawImage = videoObj.GetComponent<RawImage>();
        soundObjs[3].Play();
        string downloadImgUrl = Global.image_server_path + "bg.jpg";
        string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
        StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, totalBackImg));
        if(Global.scene_type == 0)
        {
            loadAutoSlideScene();
        }
        else
        {
            loadMainScene();
        }
    }

    void loadMainScene()
    {
        Debug.Log("video Stop.");
        videoPlayer.Stop();
        autoSlideSceneObj.SetActive(false);
        mainSceneObj.SetActive(true);
        for (int i = 0; i < clickingCnt.Length; i++)
        {
            clickingCnt[i] = 0;
        }
        LoadScenes();
    }

    void loadAutoSlideScene()
    {
        Debug.Log("load auto slide scene : " + DateTime.Now);
        autoSlideSceneObj.SetActive(true);
        mainSceneObj.SetActive(false);
        is_counting = false;
        for(int i = 0; i < 8; i++)
        {
            wait_count[i] = false;
        }
        try
        {
            if (Global.backImgFiles.Count > 0)
            {
                slideObj.SetActive(true);
                Debug.Log("video Stop.");
                videoObj.SetActive(false);
                for (int i = 0; i < Global.backImgFiles.Count; i++)
                {
                    GameObject slideobj = Instantiate(imgSlidePrefab);
                    slideobj.transform.SetParent(imgslideParent.transform);
                    Texture2D tex = NativeGallery.LoadImageAtPath(Global.backImgFiles[i]); // image will be downscaled if its width or height is larger than 1024px
                    if (tex != null)
                    {
                        slideobj.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
                    }
                    slideobj.transform.localScale = Vector3.one;
                    slideobj.transform.localPosition = Vector3.zero;
                    if (Global.backImgFiles.Count == 1)
                    {
                        slideobj.GetComponent<Image>().type = Image.Type.Simple;
                    }
                    else
                    {
                        slideobj.GetComponent<Image>().type = Image.Type.Sliced;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(Global.backVideoFile))
            {
                slideObj.SetActive(false);
                Debug.Log("video player!");
                videoObj.SetActive(true);
                StartCoroutine(playVideo());
            }
        }
        catch (Exception ex)
        {

        }
    }

    IEnumerator playVideo()
    {
        if (videoPlayer != null && rawImage != null && !string.IsNullOrEmpty(Global.backVideoFile))
        {
            AndroidRuntimePermissions.Permission result = AndroidRuntimePermissions.RequestPermission("android.permission.WRITE_EXTERNAL_STORAGE");
            if (result == AndroidRuntimePermissions.Permission.Granted)
            {
                Debug.Log("We have permission to access external storage!");
                videoPlayer.playOnAwake = false;
                videoPlayer.source = VideoSource.Url;
                //Debug.Log("playVideo before url:" + Global.backVideoFile);
                //Global.backVideoFile = Global.prePath + Global.backVideoFile;
                //Debug.Log("playVideo middle url:" + Global.backVideoFile);
                //Global.backVideoFile = Global.backVideoFile.Replace(@"\\\\", @"\\\");
                //Global.backVideoFile = Global.backVideoFile.Replace(@"////", @"///");
                //Debug.Log("playVideo after url:" + Global.backVideoFile);
                videoPlayer.url = Global.backVideoFile;
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.isLooping = true;
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared)
                {
                    yield return new WaitForEndOfFrame();
                }
                rawImage.texture = videoPlayer.texture;
                videoPlayer.Play();
            }
            else
            {
                Debug.Log("Permission state: " + result);
            }
        }
    }

    void InitSocketFunctions()
    {
        socketObj = Instantiate(socketPrefab);
        socket = socketObj.GetComponent<SocketIOComponent>();
        socket.On("open", socketOpen);
        socket.On("soldout", soldoutEventHandler);
        socket.On("infochanged", InfoChangedEventHandler);
        socket.On("flowmeterStart", flowmeterStartEventHandler);
        socket.On("flowmeterValue", flowmeterValueEventHandler);
        socket.On("flowmeterFinish", flowmeterFinishEventHandler);
        socket.On("errorReceived", errorReceivedEventHandler);
        socket.On("adminReceived", adminReceivedEventHandler);
        socket.On("error", socketError);
        socket.On("close", socketClose);
    }

    void LoadScenes()
    {
        if(Global.ip == "")
        {
            for (int i = 0; i < 8; i++)
            {
                ShowSettingScene(i);
            }
        }
        else if(!is_loading)
        {
            WWWForm form = new WWWForm();
            form.AddField("type", 8);
            WWW www = new WWW(Global.api_url + Global.check_db_api, form);
            StartCoroutine(ipCheck(www));
        }
        else
        {
            for(int i = 0; i < 8; i++)
            {
                showWorkScene(i);
            }
        }
    }

    void ShowSettingScene(int i, bool is_set = true)
    {
        setObj[i].SetActive(is_set);
        viewObj[i].SetActive(is_set);
        editObj[i].SetActive(!is_set);
        savePopup[i].SetActive(!is_set);
        //vappNo[i].text = Global.appNo.ToString();
        vtapNo[i].text = Global.pInfo[i].serial_number.ToString();
    }

    IEnumerator ShowStandbyScene(int i)
    {
        WWWForm form = new WWWForm();
        form.AddField("serial_number", Global.pInfo[i].serial_number);
        WWW www = new WWW(Global.api_url + Global.get_product_api, form);
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                Global.pInfo[i].id = jsonNode["id"].AsInt;
                Global.pInfo[i].server_id = jsonNode["server_id"].AsInt;
                Global.pInfo[i].is_soldout = jsonNode["is_soldout"].AsInt;
                Global.pInfo[i].sell_type = jsonNode["sell_type"].AsInt;
                Global.pInfo[i].unit_price = jsonNode["unit_price"].AsInt;
                Global.pInfo[i].cup_size = jsonNode["cup_size"].AsInt;
                Global.pInfo[i].drink_name = jsonNode["drink_name"];
                //Global.pInfo[i].kind = jsonNode["kind"];
                //Global.pInfo[i].bintage = jsonNode["bintage"];
                Global.pInfo[i].country = jsonNode["country"];
                Global.pInfo[i].styles = jsonNode["styles"];
                Global.pInfo[i].description = jsonNode["description"];
                Global.pInfo[i].gw_no = jsonNode["gw_no"].AsInt;
                Global.pInfo[i].gw_ch = jsonNode["gw_channel"].AsInt;
                Global.pInfo[i].board_no = jsonNode["board_no"].AsInt;
                Global.pInfo[i].board_ch = jsonNode["board_channel"].AsInt;
                if (jsonNode["is_soldout"].AsInt == 1)
                {
                    Global.pInfo[i].sceneType = WorkSceneType.soldout;
                }
                else
                {
                    Global.pInfo[i].sceneType = WorkSceneType.standby;
                }
                failMsg[i].SetActive(false);
                showWorkScene(i);
                string downloadImgUrl = Global.image_server_path + "Standby" + jsonNode["server_id"].AsInt + ".jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                downloadImgUrl = Global.image_server_path + "eStandby" + jsonNode["server_id"].AsInt + ".jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
            }
            else
            {
                err_content[i].text = "조작에 실패했습니다. 잠시후에 다시 시도해주세요.";
                errorPopup[i].SetActive(true);
            }
        }
        else
        {
            err_content[i].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
            errorPopup[i].SetActive(true);
        }
    }

    void showWorkScene(int index, int capacity = 0)
    {
        Debug.Log("Show work scene:" + index + ":" + Global.pInfo[index].sceneType);
        try
        {
            textObj[index].SetActive(false);
            StopCoroutine(waitAndConvertLang(index));
            switch (Global.pInfo[index].sceneType)
            {
                case WorkSceneType.pour:
                    {
                        languageBtn[index].SetActive(false);
                        string downloadImgUrl = Global.image_server_path + "Pour.jpg";
                        string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
                        StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
                        priceObj[index].SetActive(true);
                        priceTxt[index].text = Global.GetPriceFormat(capacity) + " ml";
                        break;
                    }
                case WorkSceneType.remain:
                    {
                        languageBtn[index].SetActive(false);
                        string downloadImgUrl = Global.image_server_path + "Remain.jpg";
                        string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
                        StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
                        priceObj[index].SetActive(true);
                        priceTxt[index].text = Global.GetPriceFormat(capacity) + " 원";
                        break;
                    }
                case WorkSceneType.soldout:
                    {
                        soundObjs[1].Play();
                        languageBtn[index].SetActive(false);
                        string downloadImgUrl = Global.image_server_path + "Soldout.jpg";
                        string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
                        StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
                        priceObj[index].SetActive(false);
                        break;
                    }
                case WorkSceneType.standby:
                    {
                        soundObjs[0].Play();
                        priceObj[index].SetActive(true);
                        if (Global.pInfo[index].standby_type != 0)
                        {
                            //text 방식
                            textObj[index].SetActive(true);
                        }
                        if (Global.pInfo[index].sell_type == 0)
                        {
                            //cup
                            priceTxt[index].text = Global.GetPriceFormat(Global.pInfo[index].unit_price * Global.pInfo[index].cup_size) + " 원/" + Global.GetPriceFormat(Global.pInfo[index].cup_size) + "ml";
                        }
                        else
                        {
                            //ml
                            priceTxt[index].text = Global.GetPriceFormat(Global.pInfo[index].unit_price) + " 원/ml";
                        }
                        if (Global.pInfo[index].standby_type == 0)
                        {
                            //image
                            string downloadImgUrl = Global.image_server_path + "Standby" + Global.pInfo[index].server_id + ".jpg";
                            string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
                            string failImgUrl = Global.image_server_path + "tap.jpg";
                            Debug.Log(filepath);
                            StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
                            StartCoroutine(checkDownImage(filepath, failImgUrl, index));
                            languageBtn[index].SetActive(true);
                            languageBtn[index].GetComponent<Image>().sprite = Resources.Load<Sprite>("english");
                        }
                        else
                        {
                            //text
                            string failImgUrl = Global.image_server_path + "tap.jpg";
                            string filepath = Global.imgPath + Path.GetFileName(failImgUrl);
                            Debug.Log(filepath);
                            StartCoroutine(downloadAndLoadImage(failImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
                            title[index].text = Global.pInfo[index].drink_name;
                            //type[index].text = "종류: " + Global.pInfo[index].kind;
                            //bintage[index].text = "빈티지: " + Global.pInfo[index].bintage.ToString();
                            location[index].text = "지역: " + Global.pInfo[index].country;
                            kind[index].text = Global.pInfo[index].styles;
                            description[index].text = Global.pInfo[index].description;
                            languageBtn[index].SetActive(false);
                        }
                        StartCoroutine(waitAndSlide(index));
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator checkDownImage(string imgPath, string failPath, int index)
    {
        yield return new WaitForSeconds(3f);
        if (!File.Exists(imgPath))
        {
            string filepath = Global.imgPath + Path.GetFileName(failPath);
            Debug.Log(filepath);
            StartCoroutine(downloadAndLoadImage(failPath, filepath, backImg[index].GetComponent<RawImage>()));
        }
    }

    IEnumerator ipCheck(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                if (socket != null)
                {
                    socket.Close();
                    socket.OnDestroy();
                    socket.OnApplicationQuit();
                }
                if (socketObj != null)
                {
                    DestroyImmediate(socketObj);
                }

                for (int i = 0; i < 8; i++)
                {
                    if (Global.pInfo[i].serial_number == 0)
                    {
                        ShowSettingScene(i);
                    }
                    else
                    {
                        StartCoroutine(ShowStandbyScene(i));
                    }
                }

                string downloadImgUrl = Global.image_server_path + "Pour.jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                downloadImgUrl = Global.image_server_path + "Remain.jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                downloadImgUrl = Global.image_server_path + "Soldout.jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                downloadImgUrl = Global.image_server_path + "tap.jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));

                InitSocketFunctions();
                is_loading = true;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    ShowSettingScene(i);
                }
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                ShowSettingScene(i);
            }
        }
    }

    public void editSet(int index)
    {
        editObj[index].SetActive(true);
        viewObj[index].SetActive(false);
        //eappNo[index].text = Global.appNo.ToString();
        etapNo[index].text = Global.pInfo[index].serial_number.ToString();
        ip[index].text = Global.ip.ToString();
        if(Global.pInfo[index].standby_type == 0)
        {
            //image
            imageOption[index].isOn = true;
        }
        else
        {
            //text
            textOption[index].isOn = true;
        }
    }

    public void onBack(int index)
    {
        Debug.Log("back:" + index);
        if (editObj[index].activeSelf)
        {
            editObj[index].SetActive(false);
            viewObj[index].SetActive(true);
            vtapNo[index].text = Global.pInfo[index].serial_number.ToString();
        }
        else if(Global.ip != "" && Global.pInfo[index].serial_number != 0)
        {
            ShowSettingScene(index, false);
            Global.pInfo[index].sceneType = WorkSceneType.standby;
            showWorkScene(index);
            tagControl(index, 1);//release tag
        }
        else
        {
            err_content[index].text = "설정값들을 정확히 입력하세요.";
            errorPopup[index].SetActive(true);
        }
    }

    public void Wash(int index)
    {
        washPopup[index].SetActive(true);
        valveControl(index, 0, 1);
    }

    public void onConfirmWashPopup(int index)
    {
        washPopup[index].SetActive(false);
        //ShowSettingScene(index, false);
        //Global.pInfo[index].sceneType = WorkSceneType.standby;
        //showWorkScene(index);
        valveControl(index, 0, 0);
    }

    public void BottleChange(int index)
    {
        bottleChangePopup[index].SetActive(true);
        //tagControl(index);
        valveControl(index, 0, 1);
    }

    public void onConfirmBottlePopup(int index)
    {
        bottleChangePopup[index].SetActive(false);
        bottleInitPopup[index].SetActive(true);
    }

    public void onConfirmBottleInitPopup(int index)
    {
        bottleInitPopup[index].SetActive(false);
        ShowSettingScene(index, false);
        Global.pInfo[index].sceneType = WorkSceneType.standby;
        showWorkScene(index);
        valveControl(index, 0, 0);
        tagControl(index, 1);
        //WWWForm form = new WWWForm();
        //form.AddField("appNo", Global.appNo);
        //form.AddField("tapNo", Global.pInfo[index].tapNo);
        //WWW www = new WWW(Global.api_url + Global.bottle_init_confirm_api, form);
        //StartCoroutine(ProcessKegInit(www, index));
    }

    IEnumerator ProcessKegInit(WWW www, int index)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                ShowSettingScene(index, false);
                Global.pInfo[index].sceneType = WorkSceneType.standby;
                showWorkScene(index);
                valveControl(index, 0, 0);
                tagControl(index, 1);
            }
            else
            {
                err_content[index].text = "조작에 실패했습니다. 잠시후에 다시 시도해주세요.";
                errorPopup[index].SetActive(true);
            }
        }
        else
        {
            err_content[index].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
            errorPopup[index].SetActive(true);
        }
    }

    public void onCancelBottleInitPopup(int index)
    {
        bottleInitPopup[index].SetActive(false);
        valveControl(index, 0, 0);
    }

    public void Soldout(int index)
    {
        WWWForm form = new WWWForm();
        form.AddField("serial_number", Global.pInfo[index].serial_number);
        WWW www = new WWW(Global.api_url + Global.soldout_api, form);
        StartCoroutine(SoldoutProcess(www, index));
    }

    IEnumerator SoldoutProcess(WWW www, int index)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                ShowSettingScene(index, false);
                Global.pInfo[index].is_soldout = 1;
                Global.pInfo[index].sceneType = WorkSceneType.soldout;
                showWorkScene(index);
            }
            else
            {
                err_content[index].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
                errorPopup[index].SetActive(true);
            }
        }
        else
        {
            err_content[index].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
            errorPopup[index].SetActive(true);
        }
    }

    public void soldoutEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] Soldout received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for(int i = 0; i < 8; i++)
            {
                if(Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            Global.pInfo[index].is_soldout = 1;
            ShowSettingScene(index, false);
            Global.pInfo[index].sceneType = WorkSceneType.soldout;
            showWorkScene(index);
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void InfoChangedEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] InfoChangedEvent received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            noSocketTime = 0;
            Global.pInfo[index].server_id = jsonNode["server_id"].AsInt;
            Global.pInfo[index].id = jsonNode["id"].AsInt;
            //Global.pInfo[index].bintage = jsonNode["bintage"].AsInt;
            Global.pInfo[index].country = jsonNode["country"];
            Global.pInfo[index].description = jsonNode["description"];
            Global.pInfo[index].drink_name = jsonNode["drink_name"];
            //Global.pInfo[index].kind = jsonNode["kind"];
            Global.pInfo[index].styles = jsonNode["styles"];
            Global.pInfo[index].cup_size = jsonNode["cup_size"].AsInt;
            Global.pInfo[index].unit_price = jsonNode["unit_price"].AsInt;
            Global.pInfo[index].sell_type = jsonNode["sell_type"].AsInt;
            Global.pInfo[index].gw_no = jsonNode["gw_no"].AsInt;
            Global.pInfo[index].gw_ch = jsonNode["gw_channel"].AsInt;
            Global.pInfo[index].board_no = jsonNode["board_no"].AsInt;
            Global.pInfo[index].board_ch = jsonNode["board_channel"].AsInt;
            Global.pInfo[index].is_soldout = jsonNode["is_soldout"].AsInt;
            if (jsonNode["is_soldout"].AsInt == 1)
            {
                Global.pInfo[index].sceneType = WorkSceneType.soldout;
            }
            else
            {
                Global.pInfo[index].sceneType = WorkSceneType.standby;
            }
            string downloadImgUrl = Global.image_server_path + "Standby" + jsonNode["server_id"].AsInt + ".jpg";
            StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
            downloadImgUrl = Global.image_server_path + "eStandby" + jsonNode["server_id"].AsInt + ".jpg";
            StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
            showWorkScene(index);
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void flowmeterStartEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] FlowmeterStartEvent received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            noSocketTime = 0;
            Global.pInfo[index].is_soldout = 0;
            Global.pInfo[index].sceneType = WorkSceneType.pour;
            showWorkScene(index, 0);
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void flowmeterValueEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] FlowmeterValueEvent received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            noSocketTime = 0;
            int value = jsonNode["value"].AsInt;
            priceTxt[index].text = Global.GetPriceFormat(value) + " ml";
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void flowmeterFinishEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] FinishEvent received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            int type = jsonNode["type"].AsInt;//0-pour, 1-soldout, 2-remain
            int value = jsonNode["value"].AsInt;
            if (type == 1)
            {
                //soldout 완료
                priceTxt[index].text = Global.GetPriceFormat(value) + " ml";
                StartCoroutine(GotoSoldout(index));
            }
            else if(type == 2)
            {
                //remain 완료
                Global.pInfo[index].sceneType = WorkSceneType.pour;
                showWorkScene(index, value);
                StartCoroutine(ReturntoRemain(index, jsonNode["remain_value"].AsInt));
            }
            else
            {
                //정상완료
                Global.pInfo[index].is_soldout = 0;
                int is_pay_after = jsonNode["is_pay_after"].AsInt;
                Global.pInfo[index].sceneType = WorkSceneType.pour;
                showWorkScene(index, value);
                if (is_pay_after == 1)
                {
                    //후불
                    StartCoroutine(ReturntoStandby(index));
                }
                else
                {
                    //선불
                    StartCoroutine(ReturntoRemain(index, jsonNode["remain_value"].AsInt));
                }
            }
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    IEnumerator GotoSoldout(int index)
    {
        yield return new WaitForSeconds(1f);
        Global.pInfo[index].is_soldout = 1;
        ShowSettingScene(index, false);
        Global.pInfo[index].sceneType = WorkSceneType.soldout;
        showWorkScene(index);
    }

    IEnumerator ReturntoRemain(int index, int remain_value)
    {
        yield return new WaitForSeconds(1f);
        Global.pInfo[index].sceneType = WorkSceneType.remain;
        showWorkScene(index, remain_value);
        yield return new WaitForSeconds(3f);
        Global.pInfo[index].sceneType = WorkSceneType.standby;
        showWorkScene(index);
    }

    IEnumerator ReturntoStandby(int index)
    {
        yield return new WaitForSeconds(1f);
        Global.pInfo[index].sceneType = WorkSceneType.standby;
        showWorkScene(index);
    }

    IEnumerator closeServerErrPopup(int index)
    {
        yield return new WaitForSeconds(3f);
        serverErrPopup[index].SetActive(false);
        ShowSettingScene(index, false);
        Global.pInfo[index].sceneType = WorkSceneType.standby;
        showWorkScene(index);
    }

    public void errorReceivedEventHandler(SocketIOEvent e)
    {
        try
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            Debug.Log("errorReceivedEvent:" + jsonNode.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                Debug.Log(":::" + Global.pInfo[i].serial_number);
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            Debug.Log("index:" + index);
            noSocketTime = 0;
            int type = jsonNode["type"].AsInt;
            if (type == 1)
            {
                server_content[index].text = jsonNode["content"];
                serverErrPopup[index].SetActive(true);
                StartCoroutine(closeServerErrPopup(index));
            }
            else
            {
                int is_close = jsonNode["is_close"].AsInt;
                if(is_close == 1)
                {
                    serverErrPopup[index].SetActive(false);
                }
                else
                {
                    server_content[index].text = jsonNode["content"];
                    serverErrPopup[index].SetActive(true);
                }
            }
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void adminReceivedEventHandler(SocketIOEvent e)
    {
        try
        {
            Debug.Log("[SocketIO] AdminReceivedEvent received: " + e.name + " " + e.data);
            JSONNode jsonNode = SimpleJSON.JSON.Parse(e.data.ToString());
            int id = jsonNode["id"].AsInt;
            int index = -1;
            for (int i = 0; i < 8; i++)
            {
                if (Global.pInfo[i].serial_number == id)
                {
                    index = i; break;
                }
            }
            if (index == -1)
                return;
            ShowSettingScene(index);
        }
        catch (Exception err)
        {
            Debug.Log(err);
        }
    }

    public void saveSet(int index)
    {
        if(ip[index].text == "")
        {
            err_content[index].text = "ip를 입력하세요.";
            errorPopup[index].SetActive(true);
        }
        //else if(eappNo[index].text == "")
        //{
        //    err_content[index].text = "App No를 입력하세요.";
        //    errorPopup[index].SetActive(true);
        //}
        else if(etapNo[index].text == "" || etapNo[index].text == "0")
        {
            err_content[index].text = "Tap No를 입력하세요.";
            errorPopup[index].SetActive(true);
        }
        else
        {
            int standby_type = 0;
            if (textOption[index].isOn)
            {
                standby_type = 1;
            }
            try
            {
                string tmp_url = "http://" + ip[index].text.Trim() + ":" + Global.api_server_port + "/m-api/self/";
                WWWForm form = new WWWForm();
                form.AddField("serial_number", int.Parse(etapNo[index].text));
                form.AddField("type", 8);
                WWW www = new WWW(tmp_url + Global.save_setinfo_api, form);
                StartCoroutine(saveSetInfoProcess(www, index, standby_type));
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    IEnumerator saveSetInfoProcess(WWW www, int index, int standby_type)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            Debug.Log(jsonNode);
            string result = jsonNode["suc"].ToString()/*.Replace("\"", "")*/;
            if (result == "1")
            {
                if(Global.ip != ip[index].text)
                {
                    Global.ip = ip[index].text;
                    PlayerPrefs.SetString("ip", Global.ip);
                    Global.api_url = "http://" + Global.ip + ":" + Global.api_server_port + "/m-api/self/";
                    Global.socket_server = "ws://" + Global.ip + ":" + Global.api_server_port;
                    Global.image_server_path = "http://" + Global.ip + ":" + Global.api_server_port + "/self/";
                    string _pourImgUrl = Global.image_server_path + "Pour.jpg";
                    string _remainImgUrl = Global.image_server_path + "Remain.jpg";
                    string _soldoutImgUrl = Global.image_server_path + "Soldout.jpg";
                    string _tapImgUrl = Global.image_server_path + "tap.jpg";
                    if (File.Exists(_pourImgUrl))
                    {
                        File.Delete(_pourImgUrl);
                    }
                    if (File.Exists(_remainImgUrl))
                    {
                        File.Delete(_remainImgUrl);
                    }
                    if (File.Exists(_soldoutImgUrl))
                    {
                        File.Delete(_soldoutImgUrl);
                    }
                    if (File.Exists(_tapImgUrl))
                    {
                        File.Delete(_tapImgUrl);
                    }
                    if (socket != null)
                    {
                        socket.Close();
                        socket.OnDestroy();
                        socket.OnApplicationQuit();
                    }
                    if (socketObj != null)
                    {
                        DestroyImmediate(socketObj);
                    }
                    InitSocketFunctions();
                }
                Global.pInfo[index].serial_number = int.Parse(etapNo[index].text);
                PlayerPrefs.SetInt("tapNo" + index, Global.pInfo[index].serial_number);
                Global.pInfo[index].standby_type = standby_type;
                PlayerPrefs.SetInt("standby_type" + index, Global.pInfo[index].standby_type);

                Global.pInfo[index].id = jsonNode["id"].AsInt;
                Global.pInfo[index].server_id = jsonNode["server_id"].AsInt;
                Global.pInfo[index].is_soldout = jsonNode["is_soldout"].AsInt;
                Global.pInfo[index].sell_type = jsonNode["sell_type"].AsInt;
                Global.pInfo[index].unit_price = jsonNode["unit_price"].AsInt;
                Global.pInfo[index].cup_size = jsonNode["cup_size"].AsInt;
                Global.pInfo[index].drink_name = jsonNode["drink_name"];
                //Global.pInfo[index].kind = jsonNode["kind"];
                //Global.pInfo[index].bintage = jsonNode["bintage"].AsInt;
                Global.pInfo[index].country = jsonNode["country"];
                Global.pInfo[index].styles = jsonNode["styles"];
                Global.pInfo[index].description = jsonNode["description"];
                Global.pInfo[index].gw_no = jsonNode["gw_no"].AsInt;
                Global.pInfo[index].gw_ch = jsonNode["gw_channel"].AsInt;
                Global.pInfo[index].board_no = jsonNode["board_no"].AsInt;
                Global.pInfo[index].board_ch = jsonNode["board_channel"].AsInt;
                if (jsonNode["sold_out"].AsInt == 1)
                {
                    Global.pInfo[index].sceneType = WorkSceneType.soldout;
                }
                else
                {
                    Global.pInfo[index].sceneType = WorkSceneType.standby;
                }
                string pourImgUrl = Global.image_server_path + "Pour.jpg";
                string remainImgUrl = Global.image_server_path + "Remain.jpg";
                string soldoutImgUrl = Global.image_server_path + "Soldout.jpg";
                string tapImgUrl = Global.image_server_path + "tap.jpg";

                StartCoroutine(downloadFile(pourImgUrl, Global.imgPath + Path.GetFileName(pourImgUrl)));
                StartCoroutine(downloadFile(remainImgUrl, Global.imgPath + Path.GetFileName(remainImgUrl)));
                StartCoroutine(downloadFile(soldoutImgUrl, Global.imgPath + Path.GetFileName(soldoutImgUrl)));
                StartCoroutine(downloadFile(tapImgUrl, Global.imgPath + Path.GetFileName(tapImgUrl)));

                string downloadImgUrl = Global.image_server_path + "Standby" + jsonNode["server_id"].AsInt + ".jpg";
                //StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));
                string failImgUrl = Global.image_server_path + "tap.jpg";
                StartCoroutine(downloadFile(failImgUrl, Global.imgPath + Path.GetFileName(failImgUrl)));

                downloadImgUrl = Global.image_server_path + "eStandby" + jsonNode["server_id"].AsInt + ".jpg";
                StartCoroutine(downloadFile(downloadImgUrl, Global.imgPath + Path.GetFileName(downloadImgUrl)));


                PlayerPrefs.SetString("ip", Global.ip);
                PlayerPrefs.SetInt("tapNo" + index, Global.pInfo[index].serial_number);
                PlayerPrefs.SetInt("standbyType" + index, Global.pInfo[index].standby_type);
                savePopup[index].SetActive(true);
            }
            else
            {
                err_content[index].text = "정보를 정확히 입력하세요.";
                errorPopup[index].SetActive(true);
            }
        }
        else
        {
            err_content[index].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
            errorPopup[index].SetActive(true);
        }
    }

    public void closeSavePopup(int index)
    {
        savePopup[index].SetActive(false);
        clickingCnt[index] = 0;
    }

    IEnumerator waitAndConvertLang(int index)
    {
        Debug.Log(index + " wait function:" + Global.pInfo[index].sceneType);
        yield return new WaitForSeconds(30f);
        while(Global.pInfo[index].sceneType == WorkSceneType.pour)
        {
            yield return new WaitForSeconds(1f);
        }
        Debug.Log(Global.pInfo[index].sceneType + " starts");
        languageBtn[index].GetComponent<Image>().sprite = Resources.Load<Sprite>("english");
        showWorkScene(index);
        //if (Global.pInfo[index].sceneType == WorkSceneType.standby)
        //{
        //    string downloadImgUrl = Global.image_server_path + "Standby" + Global.pInfo[index].server_id + ".jpg";
        //    string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
        //    StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
        //    string failImgUrl = Global.image_server_path + "tap.jpg";
        //    StartCoroutine(checkDownImage(filepath, failImgUrl, index));
        //}
    }

    IEnumerator waitAndSlide(int index)
    {
        Debug.Log("wait for slide: " + index + ": " + DateTime.Now);
        if (is_counting)
        {
            noSocketTime = 0;
        }
        else
        {
            Debug.Log("new wait for slide: " + index + ": " + DateTime.Now);
            is_counting = true;
            wait_count[index] = true;
            while (noSocketTime < waitTime)
            {
                for(int i = 0; i < 8; i++)
                {
                    if (Global.pInfo[index].sceneType != WorkSceneType.standby || 
                        setObj[index].activeSelf)
                    {
                        noSocketTime = 0;
                        break;
                    }
                }
                yield return new WaitForSeconds(1f);
                noSocketTime++;
            }
            if (noSocketTime == waitTime)
            {
                Global.scene_type = 0;
                loadAutoSlideScene();
                //SceneManager.LoadScene("autoslide");
            }
        }
    }

    public void onRefresh()
    {
        for(int i = 0; i < 8; i++)
        {
            ShowSettingScene(i, false);
            //Global.pInfo[i].sceneType = WorkSceneType.standby;
            showWorkScene(i);
        }
    }

    public void onRefreshI(int i)
    {
        Debug.Log("refresh i:" + i);
        ShowSettingScene(i, false);
        //Global.pInfo[i].sceneType = WorkSceneType.standby;
        showWorkScene(i);
    }

    public void onChangeLanguage(int index, bool type = false/*0-korean->english*/)
    {
        Debug.Log(index + ", " + type);
        StopCoroutine(waitAndConvertLang(index));
        if (!type)
        {
            languageBtn[index].GetComponent<Image>().sprite = Resources.Load<Sprite>("korean");
            string downloadImgUrl = Global.image_server_path + "eStandby" + Global.pInfo[index].server_id + ".jpg";
            string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
            StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
            string failImgUrl = Global.image_server_path + "tap.jpg";
            StartCoroutine(checkDownImage(filepath, failImgUrl, index));
            StartCoroutine(waitAndConvertLang(index));
        }
        else
        {
            languageBtn[index].GetComponent<Image>().sprite = Resources.Load<Sprite>("english");
            string downloadImgUrl = Global.image_server_path + "Standby" + Global.pInfo[index].server_id + ".jpg";
            string filepath = Global.imgPath + Path.GetFileName(downloadImgUrl);
            StartCoroutine(downloadAndLoadImage(downloadImgUrl, filepath, backImg[index].GetComponent<RawImage>()));
            string failImgUrl = Global.image_server_path + "tap.jpg";
            StartCoroutine(checkDownImage(filepath, failImgUrl, index));
        }
    }

    public void closeErrorPopup(int index)
    {
        errorPopup[index].SetActive(false);
    }

    public void socketOpen(SocketIOEvent e)
    {
        Debug.Log("socket_open");
        if (is_socket_open)
        {
            return;
        }
        int index = -1;
        for(int i = 0; i < 8; i++)
        {
            if(Global.pInfo[i].serial_number != 0)
            {
                index = i;
                break;
            }
        }
        if (index > -1 && socket != null)
        {
            string sId = "{\"no\":\"" + Global.pInfo[index].serial_number + "\"}";
            socket.Emit("self8SetInfo", JSONObject.Create(sId));
            Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
            is_socket_open = true;
        }
    }

    public void socketError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void socketClose(SocketIOEvent e)
    {
        is_socket_open = false;
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }

    //download image
    IEnumerator downloadFile(string url, string pathToSaveImage)
    {
        yield return new WaitForEndOfFrame();
        if (!File.Exists(pathToSaveImage))
        {
            WWW www = new WWW(url);
            StartCoroutine(_downloadFile(www, pathToSaveImage));
        }
    }

    IEnumerator downloadAndLoadImage(string url, string pathToSaveImage, RawImage img)
    {
        try
        {
            if (img != null)
            {
                if (File.Exists(pathToSaveImage))
                {
                    StartCoroutine(LoadPictureToTexture(pathToSaveImage, img));
                }
                else
                {
                    WWW www = new WWW(url);
                    StartCoroutine(_downloadAndLoadImage(www, pathToSaveImage, img));
                }
            }
        }
        catch (Exception ex)
        {

        }
        yield return null;
    }

    private IEnumerator _downloadAndLoadImage(WWW www, string savePath, RawImage img)
    {
        yield return www;
        if (img != null)
        {
            Debug.Log(savePath);
            //Check if we failed to send
            if (string.IsNullOrEmpty(www.error))
            {
                saveAndLoadImage(savePath, www.bytes, img);
            }
            else
            {
                UnityEngine.Debug.Log("Error: " + www.error);
            }
        }
    }

    void saveAndLoadImage(string path, byte[] imageBytes, RawImage img)
    {
        try
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllBytes(path, imageBytes);
            //Debug.Log("Download Image: " + path.Replace("/", "\\"));
            StartCoroutine(LoadPictureToTexture(path, img));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
        }
    }

    IEnumerator LoadPictureToTexture(string name, RawImage img)
    {
        Debug.Log("load image = " + Global.prePath + name);
        WWW pictureWWW = new WWW(Global.prePath + name);
        yield return pictureWWW;
        try
        {
            if (img != null)
            {
                //img.sprite = Sprite.Create(pictureWWW.texture, new Rect(0, 0, pictureWWW.texture.width, pictureWWW.texture.height), new Vector2(0, 0), 8f, 0, SpriteMeshType.FullRect);
                img.texture = pictureWWW.texture;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private IEnumerator _downloadFile(WWW www, string path)
    {
        yield return www;
        //Check if we failed to send
        if (string.IsNullOrEmpty(www.error))
        {
            try
            {
                //Create Directory if it does not exist
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                File.WriteAllBytes(path, www.bytes);
                //Debug.Log("Download Image: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void clearClickingIndex(int index)
    {
        for(int i = 0; i < clickingCnt.Length; i ++)
        {
            if(i != index)
            {
                clickingCnt[i] = 0;
            }
        }
    }

    public void onBottomLeft(int index)
    {
        if (clickingCnt[index] == 0)
        {
            clickingCnt[index] = 1;
        }
        else if (clickingCnt[index] == 1)
        {
            clickingCnt[index] = 2;
        }
        else if (clickingCnt[index] == 2)
        {
            clickingCnt[index] = 3;
        }
        else if (clickingCnt[index] == 3)
        {
            clickingCnt[index] = 0;
            ShowSettingScene(index);
            tagControl(index, 0);
        }
        else
        {
            clickingCnt[index] = 0;
        }
    }

    public void onBottomRight(int index)
    {
        clickingCnt[index] = 0;
        if (Global.pInfo[index].is_soldout == 1)
        {
            WWWForm form = new WWWForm();
            form.AddField("serial_number", Global.pInfo[index].serial_number);
            WWW www = new WWW(Global.api_url + Global.cancel_soldout_api, form);
            StartCoroutine(CancelSoldout(www, index));
        }
    }

    IEnumerator CancelSoldout(WWW www, int index)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                Global.pInfo[index].is_soldout = 0;
                ShowSettingScene(index, false);
                Global.pInfo[index].sceneType = WorkSceneType.standby;
                showWorkScene(index);
            }
            else
            {
                err_content[index].text = "조작에 실패했습니다. 잠시후에 다시 시도해주세요.";
                errorPopup[index].SetActive(true);
            }
        }
        else
        {
            err_content[index].text = "서버와 연결 중입니다. 잠시만 기다려주세요.";
            errorPopup[index].SetActive(true);
        }
    }

    float time = 0f;
    private bool is_socket_open = false;

    void tagControl(int index, int type = 0/*0-lock, 1-release*/)
    {
        if(socket != null)
        {
            string tagGWData = "{\"tagGW_no\":\"" + Global.pInfo[index].gw_no + "\"," +
            "\"ch_value\":\"" + Global.pInfo[index].gw_ch + "\"," +
            "\"status\":\"" + type + "\"}";
            socket.Emit("deviceTagLock", JSONObject.Create(tagGWData));
        }
    }

    void valveControl(int index, int valve = 0, int type = 0/*0-close, 1-open*/)
    {
        if(socket != null)
        {
            string data = "{\"board_no\":\"" + Global.pInfo[index].board_no + "\"," +
            "\"ch_value\":\"" + Global.pInfo[index].board_ch + "\"," +
            "\"valve\":\"" + valve + "\"," +
            "\"status\":\"" + type + "\"}";
            socket.Emit("boardValveCtrl", JSONObject.Create(data));
        }
    }

    void FixedUpdate()
    {
        if (!Input.anyKey)
        {
            time += Time.deltaTime;
        }
        else
        {
            if (time != 0f)
            {
                soundObjs[2].Play();
                noSocketTime = 0;
                time = 0f;
            }
        }
    }

    public void ReturntoMain()
    {
        Global.scene_type = 1;
        loadMainScene();
    }
}
